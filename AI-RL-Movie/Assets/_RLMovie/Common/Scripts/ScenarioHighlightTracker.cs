using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace RLMovie.Common
{
    /// <summary>
    /// Detects sparse highlight candidates and exports only bookmarks plus periodic snapshots.
    /// </summary>
    public class ScenarioHighlightTracker : MonoBehaviour
    {
        [Header("=== Tracker References ===")]
        [SerializeField] private BaseRLAgent targetAgent;
        [SerializeField] private string scenarioLabel = "Scenario";
        [SerializeField] private string trackedAgentRole = "hero";
        [SerializeField] private string[] trackedAgentRoles = Array.Empty<string>();
        [SerializeField] private string trackedAgentTeam = string.Empty;

        [Header("=== Highlight Detection ===")]
        [SerializeField] private bool enableHighlightDetection = true;
        [SerializeField] private float rewardSpikeThreshold = 0.35f;
        [SerializeField] private int droughtEpisodesForHighlight = 20;
        [SerializeField] private float longEpisodeSeconds = 20f;
        [SerializeField] private float highlightBannerLifetime = 6f;

        [Header("=== Sparse Export ===")]
        [SerializeField] private bool exportHighlightsToJsonl = true;
        [SerializeField] private bool exportSnapshotsToJsonl = true;
        [Min(0)]
        [SerializeField] private int snapshotEveryEpisodes = 250;
        [Min(8)]
        [SerializeField] private int rollingWindowSize = 32;
        [Min(8)]
        [SerializeField] private int maxStoredHighlights = 64;

        private readonly List<ScenarioHighlightRecord> _storedHighlights = new List<ScenarioHighlightRecord>();
        private readonly Queue<float> _rollingRewards = new Queue<float>();
        private readonly Queue<float> _rollingDurations = new Queue<float>();
        private BaseRLAgent _subscribedAgent;
        private ScenarioHighlightRecord _latestHighlight;
        private bool _hasLatestHighlight;
        private float _latestHighlightExpiresAt;
        private float _bestCompletedReward = float.MinValue;
        private float _worstCompletedReward = float.MaxValue;
        private float _fastestSuccessDuration = float.MaxValue;
        private int _lastSuccessCompletedEpisode;
        private int _successesObserved;
        private float _rollingRewardSum;
        private float _rollingDurationSum;
        private string _sessionRootPath;
        private string _highlightFilePath;
        private string _snapshotFilePath;
        private bool _exportPathsReady;
        private bool _loggedExportFailure;
        private bool _runtimeTrackingEnabled = true;

        private void Awake()
        {
            _runtimeTrackingEnabled = !IsHeadlessRuntime();
            if (!_runtimeTrackingEnabled)
            {
                enabled = false;
                return;
            }

            TryAutoResolveReferences();
        }

        private void OnEnable()
        {
            RefreshSubscription();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (!_runtimeTrackingEnabled)
            {
                return;
            }

            TryAutoResolveReferences();
            RefreshSubscription();
        }

        public bool TryGetActiveHighlight(out ScenarioHighlightRecord highlight)
        {
            if (_hasLatestHighlight && Time.realtimeSinceStartup <= _latestHighlightExpiresAt)
            {
                highlight = _latestHighlight;
                return true;
            }

            highlight = null;
            return false;
        }

        private void RefreshSubscription()
        {
            if (targetAgent == _subscribedAgent)
            {
                return;
            }

            Unsubscribe();
            if (targetAgent == null)
            {
                return;
            }

            targetAgent.TelemetryEventRecorded += HandleTelemetryEvent;
            _subscribedAgent = targetAgent;
        }

        private void Unsubscribe()
        {
            if (_subscribedAgent == null)
            {
                return;
            }

            _subscribedAgent.TelemetryEventRecorded -= HandleTelemetryEvent;
            _subscribedAgent = null;
        }

        private void TryAutoResolveReferences()
        {
            if (string.IsNullOrWhiteSpace(scenarioLabel) || scenarioLabel == "Scenario")
            {
                scenarioLabel = SceneManager.GetActiveScene().name;
            }

            if (targetAgent != null)
            {
                return;
            }

            var spine = GetComponentInParent<ScenarioGoldenSpine>();
            if (spine != null)
            {
                BaseRLAgent resolvedAgent;
                if (!string.IsNullOrWhiteSpace(trackedAgentRole) &&
                    spine.TryGetAgentRole(trackedAgentRole, out resolvedAgent) &&
                    resolvedAgent != null)
                {
                    targetAgent = resolvedAgent;
                }
                else if (trackedAgentRoles != null && trackedAgentRoles.Length > 0)
                {
                    for (int i = 0; i < trackedAgentRoles.Length; i++)
                    {
                        string role = trackedAgentRoles[i];
                        if (string.IsNullOrWhiteSpace(role))
                        {
                            continue;
                        }

                        if (spine.TryGetAgentRole(role, out resolvedAgent) && resolvedAgent != null)
                        {
                            targetAgent = resolvedAgent;
                            break;
                        }
                    }
                }

                if (targetAgent == null
                    && !string.IsNullOrWhiteSpace(trackedAgentTeam)
                    && spine.TryGetPrimaryAgentForTeam(trackedAgentTeam, out resolvedAgent)
                    && resolvedAgent != null)
                {
                    targetAgent = resolvedAgent;
                }

                if (targetAgent == null)
                {
                    targetAgent = spine.PrimaryAgent;
                }
            }
        }

        private void HandleTelemetryEvent(AgentTelemetryEvent telemetry)
        {
            if (!enableHighlightDetection || targetAgent == null)
            {
                return;
            }

            switch (telemetry.EventType)
            {
                case AgentTelemetryEventType.Reward:
                    HandleRewardTelemetry(telemetry);
                    break;

                case AgentTelemetryEventType.Success:
                    HandleEpisodeCompleted(telemetry, true);
                    break;

                case AgentTelemetryEventType.Failure:
                    HandleEpisodeCompleted(telemetry, false);
                    break;

                case AgentTelemetryEventType.Marker:
                case AgentTelemetryEventType.Info:
                    HandleMarkerTelemetry(telemetry);
                    break;
            }
        }

        private void HandleRewardTelemetry(AgentTelemetryEvent telemetry)
        {
            if (Mathf.Abs(telemetry.Value) < rewardSpikeThreshold)
            {
                return;
            }

            RegisterHighlight(
                telemetry,
                telemetry.Value > 0f ? "Reward Spike" : "Penalty Spike",
                telemetry.Reason,
                Mathf.Abs(telemetry.Value),
                3.5f,
                2.5f);
        }

        private void HandleMarkerTelemetry(AgentTelemetryEvent telemetry)
        {
            bool explicitHighlight = telemetry.Reason.IndexOf("highlight", StringComparison.OrdinalIgnoreCase) >= 0
                || telemetry.Reason.IndexOf("near_miss", StringComparison.OrdinalIgnoreCase) >= 0
                || telemetry.Reason.IndexOf("comedy", StringComparison.OrdinalIgnoreCase) >= 0
                || telemetry.Reason.IndexOf("milestone", StringComparison.OrdinalIgnoreCase) >= 0
                || Mathf.Abs(telemetry.Value) >= rewardSpikeThreshold;

            if (!explicitHighlight)
            {
                return;
            }

            RegisterHighlight(
                telemetry,
                string.IsNullOrWhiteSpace(telemetry.Message) ? "Scenario Marker" : telemetry.Message,
                telemetry.Reason,
                Mathf.Max(0.5f, Mathf.Abs(telemetry.Value)),
                4f,
                4f);
        }

        private void HandleEpisodeCompleted(AgentTelemetryEvent telemetry, bool wasSuccess)
        {
            PushRollingSample(_rollingRewards, ref _rollingRewardSum, targetAgent.LastCompletedEpisodeReward);
            PushRollingSample(_rollingDurations, ref _rollingDurationSum, targetAgent.LastCompletedEpisodeDurationSeconds);

            float completedReward = targetAgent.LastCompletedEpisodeReward;
            float completedDuration = targetAgent.LastCompletedEpisodeDurationSeconds;
            int completedEpisodes = targetAgent.CompletedEpisodes;

            if (snapshotEveryEpisodes > 0 && completedEpisodes > 0 && completedEpisodes % snapshotEveryEpisodes == 0)
            {
                ExportSnapshot();
            }

            if (wasSuccess)
            {
                float highlightScore = 1f;
                string label = "Successful Run";
                string reason = telemetry.Reason;
                bool shouldHighlight = false;

                if (_successesObserved == 0)
                {
                    shouldHighlight = true;
                    highlightScore += 1.5f;
                    label = "First Success";
                    reason = "first_success";
                }

                int droughtEpisodes = _successesObserved == 0
                    ? 0
                    : Mathf.Max(0, completedEpisodes - _lastSuccessCompletedEpisode - 1);

                if (droughtEpisodes >= Mathf.Max(1, droughtEpisodesForHighlight))
                {
                    shouldHighlight = true;
                    highlightScore += 1f + droughtEpisodes * 0.05f;
                    label = "Clutch Recovery";
                    reason = "success_after_drought";
                }

                if (completedReward > _bestCompletedReward + 0.001f)
                {
                    shouldHighlight = true;
                    highlightScore += 1.25f;
                    label = "New Best Reward";
                    reason = "new_best_reward";
                    _bestCompletedReward = completedReward;
                }
                else if (_bestCompletedReward == float.MinValue)
                {
                    _bestCompletedReward = completedReward;
                }

                if (completedDuration < _fastestSuccessDuration - 0.001f)
                {
                    shouldHighlight = true;
                    highlightScore += 0.75f;
                    if (label == "Successful Run")
                    {
                        label = "Fastest Success";
                        reason = "fastest_success";
                    }

                    _fastestSuccessDuration = completedDuration;
                }

                if (shouldHighlight)
                {
                    RegisterHighlight(telemetry, label, reason, highlightScore, 4f, 5f);
                }

                _successesObserved++;
                _lastSuccessCompletedEpisode = completedEpisodes;
                return;
            }

            bool longTensionFailure = completedDuration >= Mathf.Max(3f, longEpisodeSeconds);
            bool newWorstReward = completedReward < _worstCompletedReward - 0.001f;

            if (newWorstReward)
            {
                _worstCompletedReward = completedReward;
            }
            else if (_worstCompletedReward == float.MaxValue)
            {
                _worstCompletedReward = completedReward;
            }

            if (!longTensionFailure && !newWorstReward)
            {
                return;
            }

            RegisterHighlight(
                telemetry,
                longTensionFailure ? "Near Miss Failure" : "Heavy Collapse",
                longTensionFailure ? "late_failure" : "new_worst_reward",
                longTensionFailure ? 1.1f : 0.9f,
                4f,
                3f);
        }

        private void RegisterHighlight(
            AgentTelemetryEvent telemetry,
            string label,
            string reason,
            float score,
            float clipPreRollSeconds,
            float clipPostRollSeconds)
        {
            var record = new ScenarioHighlightRecord
            {
                Scenario = scenarioLabel,
                SceneName = SceneManager.GetActiveScene().name,
                Label = label,
                Reason = reason,
                Score = score,
                EpisodeNumber = telemetry.EpisodeNumber,
                TimestampSeconds = telemetry.TimestampSeconds,
                ClipStartSeconds = Mathf.Max(0f, telemetry.TimestampSeconds - clipPreRollSeconds),
                ClipEndSeconds = telemetry.TimestampSeconds + Mathf.Max(0.5f, clipPostRollSeconds),
                EventMessage = telemetry.Message,
                EventType = telemetry.EventType.ToString(),
                RewardTotal = ResolveRewardTotalForTelemetry(telemetry),
                EpisodeDurationSeconds = ResolveEpisodeDurationForTelemetry(telemetry)
            };

            _latestHighlight = record;
            _hasLatestHighlight = true;
            _latestHighlightExpiresAt = Time.realtimeSinceStartup + Mathf.Max(1f, highlightBannerLifetime);

            _storedHighlights.Add(record);
            while (_storedHighlights.Count > Mathf.Max(8, maxStoredHighlights))
            {
                _storedHighlights.RemoveAt(0);
            }

            ExportJsonLine(record, _highlightFilePath, exportHighlightsToJsonl);
        }

        private void ExportSnapshot()
        {
            int rewardSamples = _rollingRewards.Count;
            int durationSamples = _rollingDurations.Count;
            var snapshot = new ScenarioSnapshotRecord
            {
                Scenario = scenarioLabel,
                SceneName = SceneManager.GetActiveScene().name,
                CompletedEpisodes = targetAgent.CompletedEpisodes,
                SuccessRate = targetAgent.SuccessRate,
                AverageReward = rewardSamples > 0 ? _rollingRewardSum / rewardSamples : 0f,
                AverageDurationSeconds = durationSamples > 0 ? _rollingDurationSum / durationSamples : 0f,
                BestCompletedReward = _bestCompletedReward == float.MinValue ? 0f : _bestCompletedReward,
                WorstCompletedReward = _worstCompletedReward == float.MaxValue ? 0f : _worstCompletedReward,
                WindowSize = Mathf.Max(rewardSamples, durationSamples),
                CapturedAtSeconds = Time.time
            };

            ExportJsonLine(snapshot, _snapshotFilePath, exportSnapshotsToJsonl);
        }

        private void PushRollingSample(Queue<float> queue, ref float runningSum, float value)
        {
            queue.Enqueue(value);
            runningSum += value;

            int maxWindow = Mathf.Max(8, rollingWindowSize);
            while (queue.Count > maxWindow)
            {
                runningSum -= queue.Dequeue();
            }
        }

        private void PrepareExportPaths()
        {
            if (_exportPathsReady)
            {
                return;
            }

            string safeScenario = string.IsNullOrWhiteSpace(scenarioLabel)
                ? SanitizeFileName(SceneManager.GetActiveScene().name)
                : SanitizeFileName(scenarioLabel);
            string sessionStamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            _sessionRootPath = Path.Combine(Application.persistentDataPath, "RLMovieHighlights", $"{safeScenario}_{sessionStamp}");
            _highlightFilePath = Path.Combine(_sessionRootPath, "highlights.jsonl");
            _snapshotFilePath = Path.Combine(_sessionRootPath, "snapshots.jsonl");
            _exportPathsReady = true;
        }

        private void ExportJsonLine<T>(T record, string path, bool exportEnabled)
        {
            if (!exportEnabled)
            {
                return;
            }

            PrepareExportPaths();

            try
            {
                Directory.CreateDirectory(_sessionRootPath);
                File.AppendAllText(path, JsonUtility.ToJson(record) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                if (_loggedExportFailure)
                {
                    return;
                }

                _loggedExportFailure = true;
                Debug.LogWarning($"[ScenarioHighlightTracker] Failed to export highlight metadata: {ex.Message}");
            }
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }

        private float ResolveRewardTotalForTelemetry(AgentTelemetryEvent telemetry)
        {
            if (targetAgent == null)
            {
                return telemetry.Value;
            }

            return telemetry.EventType == AgentTelemetryEventType.Success || telemetry.EventType == AgentTelemetryEventType.Failure
                ? targetAgent.LastCompletedEpisodeReward
                : targetAgent.CurrentEpisodeReward;
        }

        private float ResolveEpisodeDurationForTelemetry(AgentTelemetryEvent telemetry)
        {
            if (targetAgent == null)
            {
                return 0f;
            }

            return telemetry.EventType == AgentTelemetryEventType.Success || telemetry.EventType == AgentTelemetryEventType.Failure
                ? targetAgent.LastCompletedEpisodeDurationSeconds
                : targetAgent.CurrentEpisodeDurationSeconds;
        }

        private static bool IsHeadlessRuntime()
        {
            return Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }
    }

    [Serializable]
    public class ScenarioHighlightRecord
    {
        public string Scenario;
        public string SceneName;
        public string Label;
        public string Reason;
        public string EventType;
        public string EventMessage;
        public float Score;
        public int EpisodeNumber;
        public float TimestampSeconds;
        public float ClipStartSeconds;
        public float ClipEndSeconds;
        public float RewardTotal;
        public float EpisodeDurationSeconds;
    }

    [Serializable]
    public class ScenarioSnapshotRecord
    {
        public string Scenario;
        public string SceneName;
        public int CompletedEpisodes;
        public float SuccessRate;
        public float AverageReward;
        public float AverageDurationSeconds;
        public float BestCompletedReward;
        public float WorstCompletedReward;
        public int WindowSize;
        public float CapturedAtSeconds;
    }
}
