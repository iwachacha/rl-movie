using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RLMovie.Common
{
    /// <summary>
    /// Video-facing HUD that keeps the scenario readable and shows meaningful reward beats.
    /// </summary>
    public class ScenarioBroadcastOverlay : MonoBehaviour
    {
        [Header("=== Overlay References ===")]
        [SerializeField] private BaseRLAgent targetAgent;
        [SerializeField] private ScenarioHighlightTracker highlightTracker;

        [Header("=== Labels ===")]
        [SerializeField] private string scenarioLabel = "Scenario";
        [TextArea(2, 4)]
        [SerializeField] private string goalDescription = "Describe the goal here.";

        [Header("=== Overlay Toggles ===")]
        [SerializeField] private bool showHud = true;
        [SerializeField] private bool showRewardPopups = true;
        [SerializeField] private bool showLatestEvent = true;

        [Header("=== Reward Popups ===")]
        [SerializeField] private float rewardPopupThreshold = 0.05f;
        [SerializeField] private float rewardPopupLifetime = 1.2f;
        [Min(1)]
        [SerializeField] private int maxRewardPopups = 8;
        [SerializeField] private Color positivePopupColor = new Color(0.35f, 0.95f, 0.55f);
        [SerializeField] private Color negativePopupColor = new Color(1f, 0.45f, 0.35f);
        [SerializeField] private Color neutralPopupColor = new Color(1f, 0.95f, 0.55f);

        private readonly List<RewardPopup> _activePopups = new List<RewardPopup>();
        private BaseRLAgent _subscribedAgent;
        private bool _runtimeUiEnabled = true;

#if !UNITY_SERVER
        private Texture2D _whiteTexture;

        private Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }

                return _whiteTexture;
            }
        }
#endif

        private void Awake()
        {
            _runtimeUiEnabled = !IsHeadlessRuntime();
            if (!_runtimeUiEnabled)
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
            if (!_runtimeUiEnabled)
            {
                return;
            }

            TryAutoResolveReferences();
            RefreshSubscription();
            UpdateRewardPopups();
        }

#if !UNITY_SERVER
        private void OnGUI()
        {
            if (!_runtimeUiEnabled || !showHud || targetAgent == null)
            {
                return;
            }

            DrawHudPanel();
            DrawHighlightBanner();
            DrawRewardPopups();
        }

        private void DrawHudPanel()
        {
            Rect panelRect = new Rect(16f, 16f, 380f, 168f);
            DrawRect(panelRect, new Color(0.03f, 0.05f, 0.08f, 0.86f));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.95f, 1f, 0.95f) }
            };

            GUIStyle accentStyle = new GUIStyle(labelStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.86f, 1f, 1f) }
            };

            float x = panelRect.x + 14f;
            float y = panelRect.y + 10f;
            float width = panelRect.width - 28f;

            GUI.Label(new Rect(x, y, width, 24f), scenarioLabel, titleStyle);
            y += 26f;

            if (!string.IsNullOrWhiteSpace(goalDescription))
            {
                GUI.Label(new Rect(x, y, width, 34f), $"Goal: {goalDescription}", labelStyle);
                y += 36f;
            }

            GUI.Label(new Rect(x, y, width * 0.52f, 20f), $"Episode {targetAgent.CurrentEpisodeNumber}", accentStyle);
            GUI.Label(new Rect(x + 170f, y, width * 0.4f, 20f), $"Time {targetAgent.CurrentEpisodeDurationSeconds:F1}s", labelStyle);
            y += 20f;

            GUI.Label(new Rect(x, y, width * 0.52f, 20f), $"Live Reward {targetAgent.CurrentEpisodeReward:F2}", labelStyle);
            GUI.Label(new Rect(x + 170f, y, width * 0.4f, 20f), $"Last {targetAgent.LastCompletedEpisodeReward:F2}", labelStyle);
            y += 20f;

            string lastEndText = !string.IsNullOrWhiteSpace(targetAgent.LastTerminalMessage)
                ? targetAgent.LastTerminalMessage
                : PrettifyReason(targetAgent.LastTerminalReason);
            GUI.Label(new Rect(x, y, width * 0.52f, 20f), $"Success {targetAgent.SuccessRate:P1}", labelStyle);
            GUI.Label(new Rect(x + 170f, y, width * 0.4f, 20f), $"Last End {lastEndText}", labelStyle);
            y += 22f;

            if (showLatestEvent && TryGetLatestMeaningfulTelemetry(out AgentTelemetryEvent telemetry))
            {
                string latestText = telemetry.EventType == AgentTelemetryEventType.EpisodeBegin
                    ? telemetry.Message
                    : $"{telemetry.Message} ({telemetry.Value:+0.00;-0.00;0.00})";
                GUI.Label(new Rect(x, y, width, 38f), $"Latest: {latestText}", labelStyle);
            }
        }

        private void DrawHighlightBanner()
        {
            if (highlightTracker == null || !highlightTracker.TryGetActiveHighlight(out ScenarioHighlightRecord highlight))
            {
                return;
            }

            Rect bannerRect = new Rect(Screen.width * 0.5f - 220f, 18f, 440f, 34f);
            DrawRect(bannerRect, new Color(0.92f, 0.58f, 0.14f, 0.92f));

            GUIStyle bannerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.1f, 0.06f, 0.02f, 1f) }
            };

            GUI.Label(bannerRect, $"Highlight: {highlight.Label}", bannerStyle);
        }

        private void DrawRewardPopups()
        {
            if (!showRewardPopups || _activePopups.Count == 0)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            GUIStyle popupStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            for (int i = 0; i < _activePopups.Count; i++)
            {
                RewardPopup popup = _activePopups[i];
                Vector3 worldPos = popup.WorldPosition + Vector3.up * popup.VerticalOffset;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
                if (screenPos.z <= 0f)
                {
                    continue;
                }

                popupStyle.normal.textColor = popup.Color;
                Rect rect = new Rect(screenPos.x - 80f, Screen.height - screenPos.y - 18f, 160f, 24f);
                GUI.Label(rect, popup.Text, popupStyle);
            }
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = previous;
        }
#endif

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
            if (targetAgent != null && highlightTracker != null)
            {
                if (string.IsNullOrWhiteSpace(scenarioLabel) || scenarioLabel == "Scenario")
                {
                    scenarioLabel = SceneManager.GetActiveScene().name;
                }

                return;
            }

            var spine = GetComponentInParent<ScenarioGoldenSpine>();
            if (spine == null)
            {
                if (string.IsNullOrWhiteSpace(scenarioLabel) || scenarioLabel == "Scenario")
                {
                    scenarioLabel = SceneManager.GetActiveScene().name;
                }

                return;
            }

            if (targetAgent == null)
            {
                targetAgent = spine.PrimaryAgent;
            }

            if (highlightTracker == null)
            {
                highlightTracker = spine.ScenarioHighlightTracker;
            }

            if (string.IsNullOrWhiteSpace(scenarioLabel) || scenarioLabel == "Scenario")
            {
                scenarioLabel = SceneManager.GetActiveScene().name;
            }
        }

        private bool TryGetLatestMeaningfulTelemetry(out AgentTelemetryEvent telemetry)
        {
            if (targetAgent == null)
            {
                telemetry = default;
                return false;
            }

            var recentTelemetry = targetAgent.RecentTelemetry;
            for (int i = recentTelemetry.Count - 1; i >= 0; i--)
            {
                AgentTelemetryEvent candidate = recentTelemetry[i];
                if (candidate.EventType == AgentTelemetryEventType.Reward && Mathf.Abs(candidate.Value) < rewardPopupThreshold)
                {
                    continue;
                }

                telemetry = candidate;
                return true;
            }

            return targetAgent.TryGetLatestTelemetry(out telemetry);
        }

        private void HandleTelemetryEvent(AgentTelemetryEvent telemetry)
        {
            if (!showRewardPopups || !_runtimeUiEnabled)
            {
                return;
            }

            if (telemetry.EventType == AgentTelemetryEventType.EpisodeBegin)
            {
                return;
            }

            if (telemetry.EventType == AgentTelemetryEventType.Reward && Mathf.Abs(telemetry.Value) < rewardPopupThreshold)
            {
                return;
            }

            Color popupColor = telemetry.EventType switch
            {
                AgentTelemetryEventType.Success => positivePopupColor,
                AgentTelemetryEventType.Failure => negativePopupColor,
                AgentTelemetryEventType.Reward when telemetry.Value > 0f => positivePopupColor,
                AgentTelemetryEventType.Reward when telemetry.Value < 0f => negativePopupColor,
                _ => neutralPopupColor
            };

            _activePopups.Add(new RewardPopup
            {
                Text = BuildPopupText(telemetry),
                Color = popupColor,
                CreatedAtRealtime = Time.realtimeSinceStartup,
                ExpiresAtRealtime = Time.realtimeSinceStartup + rewardPopupLifetime,
                WorldPosition = telemetry.WorldPosition,
                VerticalOffset = 0f
            });

            while (_activePopups.Count > Mathf.Max(1, maxRewardPopups))
            {
                _activePopups.RemoveAt(0);
            }
        }

        private void UpdateRewardPopups()
        {
            if (_activePopups.Count == 0)
            {
                return;
            }

            float realtime = Time.realtimeSinceStartup;
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                RewardPopup popup = _activePopups[i];
                if (realtime >= popup.ExpiresAtRealtime)
                {
                    _activePopups.RemoveAt(i);
                    continue;
                }

                float age01 = Mathf.InverseLerp(popup.CreatedAtRealtime, popup.ExpiresAtRealtime, realtime);
                popup.VerticalOffset = Mathf.Lerp(0f, 1.25f, age01);
                _activePopups[i] = popup;
            }
        }

        private static string BuildPopupText(AgentTelemetryEvent telemetry)
        {
            string valueText = telemetry.EventType == AgentTelemetryEventType.Marker || telemetry.EventType == AgentTelemetryEventType.Info
                ? string.Empty
                : $" {telemetry.Value:+0.00;-0.00;0.00}";
            return $"{telemetry.Message}{valueText}".Trim();
        }

        private static string PrettifyReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "Unknown";
            }

            return reason.Replace('_', ' ');
        }

        private static bool IsHeadlessRuntime()
        {
            return Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }

        private struct RewardPopup
        {
            public string Text;
            public Color Color;
            public float CreatedAtRealtime;
            public float ExpiresAtRealtime;
            public Vector3 WorldPosition;
            public float VerticalOffset;
        }
    }
}
