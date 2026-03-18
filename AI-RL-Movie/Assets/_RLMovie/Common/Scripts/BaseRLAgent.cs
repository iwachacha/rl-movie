using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering;

namespace RLMovie.Common
{
    /// <summary>
    /// Shared baseline for RL Movie agents.
    /// Keeps episode stats, lightweight telemetry, and simple visual debug feedback.
    /// </summary>
    public abstract class BaseRLAgent : Agent
    {
        [Header("=== Base RL Agent Settings ===")]
        [Tooltip("Show lightweight on-screen debug info in local play mode.")]
        [SerializeField] protected bool showDebugInfo = true;

        [Tooltip("Reset the transform back to the configured start pose at each episode start.")]
        [SerializeField] protected bool autoResetPosition = true;

        [Tooltip("Agent start position used when auto reset is enabled.")]
        [SerializeField] protected Vector3 startPosition = Vector3.zero;

        [Tooltip("Agent start rotation used when auto reset is enabled.")]
        [SerializeField] protected Quaternion startRotation = Quaternion.identity;

        [Tooltip("Recent telemetry entries kept for video HUD and highlight extraction.")]
        [Min(4)]
        [SerializeField] private int maxTelemetryEntries = 24;

        protected int totalEpisodes = 0;
        protected int successCount = 0;
        protected int failCount = 0;
        protected float episodeReward = 0f;
        protected float lastCompletedEpisodeReward = 0f;
        protected float lastCompletedEpisodeDuration = 0f;

        private readonly List<AgentTelemetryEvent> _recentTelemetry = new List<AgentTelemetryEvent>();
        private long _nextTelemetrySequenceId = 1;
        private AgentTelemetryEvent _latestTelemetry;
        private bool _hasLatestTelemetry;
        private float _episodeStartTimeSeconds = 0f;
        private string _lastTerminalReason = "not_finished";
        private string _lastTerminalMessage = "Episode in progress";

        private Renderer _agentRenderer;
        private Color _originalColor;
        private float _flashTimer = 0f;
        private bool _isFlashing = false;
        private bool _visualDebugEnabled = true;

        public event Action<AgentTelemetryEvent> TelemetryEventRecorded;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _visualDebugEnabled = !RLMovieRuntime.IsHeadless;

            if (!_visualDebugEnabled)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[{name}] Visual debug is disabled for headless training runtime.");
                }

                showDebugInfo = false;
                return;
            }

            _agentRenderer = GetComponentInChildren<Renderer>();
            if (_agentRenderer != null)
            {
                _originalColor = _agentRenderer.material.color;
            }
        }

        protected virtual void Update()
        {
            if (!_visualDebugEnabled)
            {
                return;
            }

            if (_isFlashing)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f)
                {
                    _isFlashing = false;
                    if (_agentRenderer != null)
                    {
                        _agentRenderer.material.color = _originalColor;
                    }
                }
            }
        }

        #endregion

        #region ML-Agents Overrides

        public override void Initialize()
        {
            base.Initialize();
            OnAgentInitialize();
        }

        public override void OnEpisodeBegin()
        {
            totalEpisodes++;
            episodeReward = 0f;
            _episodeStartTimeSeconds = Time.time;
            _lastTerminalReason = "running";
            _lastTerminalMessage = "Episode in progress";

            if (autoResetPosition)
            {
                transform.localPosition = startPosition;
                transform.localRotation = startRotation;

                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            OnEpisodeReset();
            RecordTelemetry(
                AgentTelemetryEventType.EpisodeBegin,
                "episode_begin",
                "Episode Start",
                0f);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            CollectAgentObservations(sensor);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            ExecuteActions(actions);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ProvideHeuristicInput(actionsOut);
        }

        #endregion

        #region Abstract Methods

        protected abstract void OnAgentInitialize();

        protected abstract void OnEpisodeReset();

        protected abstract void CollectAgentObservations(VectorSensor sensor);

        protected abstract void ExecuteActions(ActionBuffers actions);

        protected abstract void ProvideHeuristicInput(in ActionBuffers actionsOut);

        #endregion

        #region Helper Methods

        protected void Success(float reward = 1.0f, string reason = "success", string message = "Success")
        {
            successCount++;
            AddReward(reward);
            episodeReward += reward;
            lastCompletedEpisodeReward = episodeReward;
            lastCompletedEpisodeDuration = CurrentEpisodeDurationSeconds;
            _lastTerminalReason = SanitizeReason(reason, "success");
            _lastTerminalMessage = string.IsNullOrWhiteSpace(message) ? "Success" : message;
            RecordTelemetry(AgentTelemetryEventType.Success, _lastTerminalReason, _lastTerminalMessage, reward);

            FlashColor(Color.green, 0.3f);

            if (showDebugInfo)
            {
                Debug.Log(
                    $"[{name}] SUCCESS Episode {totalEpisodes} | Reward: {episodeReward:F2} | Duration: {lastCompletedEpisodeDuration:F2}s | Reason: {_lastTerminalReason} | Rate: {SuccessRate:P1}");
            }

            EndEpisode();
        }

        protected void Fail(float penalty = -1.0f, string reason = "failure", string message = "Failure")
        {
            failCount++;
            AddReward(penalty);
            episodeReward += penalty;
            lastCompletedEpisodeReward = episodeReward;
            lastCompletedEpisodeDuration = CurrentEpisodeDurationSeconds;
            _lastTerminalReason = SanitizeReason(reason, "failure");
            _lastTerminalMessage = string.IsNullOrWhiteSpace(message) ? "Failure" : message;
            RecordTelemetry(AgentTelemetryEventType.Failure, _lastTerminalReason, _lastTerminalMessage, penalty);

            FlashColor(Color.red, 0.3f);

            if (showDebugInfo)
            {
                Debug.Log(
                    $"[{name}] FAIL Episode {totalEpisodes} | Reward: {episodeReward:F2} | Duration: {lastCompletedEpisodeDuration:F2}s | Reason: {_lastTerminalReason} | Rate: {SuccessRate:P1}");
            }

            EndEpisode();
        }

        protected void AddTrackedReward(float reward)
        {
            AddReward(reward);
            episodeReward += reward;
        }

        protected void AddTrackedReward(float reward, string reason, string message, bool emitTelemetry = true)
        {
            AddReward(reward);
            episodeReward += reward;

            if (emitTelemetry)
            {
                RecordTelemetry(
                    AgentTelemetryEventType.Reward,
                    SanitizeReason(reason, reward >= 0f ? "reward_gain" : "reward_loss"),
                    string.IsNullOrWhiteSpace(message) ? BuildDefaultRewardMessage(reward) : message,
                    reward);
            }
        }

        protected void RecordMarkerEvent(
            string reason,
            string message,
            float value = 0f,
            AgentTelemetryEventType eventType = AgentTelemetryEventType.Marker)
        {
            RecordTelemetry(eventType, SanitizeReason(reason, "marker"), message, value);
        }

        protected void FlashColor(Color color, float duration = 0.3f)
        {
            if (!_visualDebugEnabled || _agentRenderer == null)
            {
                return;
            }

            _agentRenderer.material.color = color;
            _flashTimer = duration;
            _isFlashing = true;
        }

        public float SuccessRate => CompletedEpisodes > 0 ? (float)successCount / CompletedEpisodes : 0f;

        public float CurrentEpisodeReward => episodeReward;

        public float LastCompletedEpisodeReward => lastCompletedEpisodeReward;

        public float LastCompletedEpisodeDurationSeconds => lastCompletedEpisodeDuration;

        public new int CompletedEpisodes => successCount + failCount;

        public int TotalEpisodes => totalEpisodes;

        public int CurrentEpisodeNumber => totalEpisodes;

        public float CurrentEpisodeDurationSeconds => Mathf.Max(0f, Time.time - _episodeStartTimeSeconds);

        public string LastTerminalReason => _lastTerminalReason;

        public string LastTerminalMessage => _lastTerminalMessage;

        public IReadOnlyList<AgentTelemetryEvent> RecentTelemetry => _recentTelemetry;

        public bool TryGetLatestTelemetry(out AgentTelemetryEvent telemetry)
        {
            telemetry = _latestTelemetry;
            return _hasLatestTelemetry;
        }

        #endregion

        #region Debug GUI

#if !UNITY_SERVER
        private void OnGUI()
        {
            if (!showDebugInfo || !_visualDebugEnabled)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f)
                : Vector3.zero;

            if (screenPos.z > 0)
            {
                float x = screenPos.x - 90f;
                float y = Screen.height - screenPos.y - 44f;

                GUI.Label(new Rect(x, y, 220f, 20f), $"Ep: {CurrentEpisodeNumber} | R: {episodeReward:F2}", style);
                GUI.Label(new Rect(x, y + 20f, 220f, 20f), $"Success: {SuccessRate:P1}", style);
            }
        }
#endif

        private void RecordTelemetry(
            AgentTelemetryEventType eventType,
            string reason,
            string message,
            float value)
        {
            var telemetry = new AgentTelemetryEvent(
                _nextTelemetrySequenceId++,
                totalEpisodes,
                Time.time,
                eventType,
                reason,
                message,
                value,
                transform.position + Vector3.up * 1.35f);

            int maxEntries = Mathf.Max(4, maxTelemetryEntries);
            if (_recentTelemetry.Count >= maxEntries)
            {
                _recentTelemetry.RemoveAt(0);
            }

            _recentTelemetry.Add(telemetry);
            _latestTelemetry = telemetry;
            _hasLatestTelemetry = true;
            TelemetryEventRecorded?.Invoke(telemetry);
        }

        private static string SanitizeReason(string reason, string fallback)
        {
            return string.IsNullOrWhiteSpace(reason) ? fallback : reason.Trim();
        }

        private static string BuildDefaultRewardMessage(float reward)
        {
            return reward >= 0f
                ? $"Reward +{reward:F2}"
                : $"Penalty {reward:F2}";
        }



        #endregion
    }
}
