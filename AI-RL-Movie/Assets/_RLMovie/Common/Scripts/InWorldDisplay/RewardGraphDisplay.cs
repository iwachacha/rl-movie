using System.Collections.Generic;
using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// Displays a line graph of cumulative reward per episode.
    /// Uses base class telemetry subscription to record end-of-episode rewards.
    /// Attach to any Renderer (Quad on a monitor, sign, billboard, etc.).
    /// </summary>
    public class RewardGraphDisplay : InWorldDisplayBase
    {
        [Header("=== Graph Settings ===")]
        [SerializeField] private int maxHistory = 100;
        [SerializeField] private Color graphLineColor = new Color(0.3f, 0.9f, 0.55f, 1f);
        [SerializeField] private Color graphFillColor = new Color(0.15f, 0.45f, 0.28f, 0.5f);
        [SerializeField] private bool showLatestValue = true;
        [SerializeField] private int valueFontScale = 2;

        private readonly List<float> rewardHistory = new List<float>();

        protected override void Awake()
        {
            base.Awake();
            SubscribeToAgentTelemetry();
        }

        protected override void OnAgentTelemetryEvent(AgentTelemetryEvent evt)
        {
            if (evt.EventType == AgentTelemetryEventType.Success ||
                evt.EventType == AgentTelemetryEventType.Failure)
            {
                rewardHistory.Add(evt.Value);
                if (rewardHistory.Count > maxHistory) rewardHistory.RemoveAt(0);
            }
        }

        protected override void RenderContent(int x, int y, int w, int h)
        {
            if (rewardHistory.Count < 2)
            {
                DrawTextCentered("---", y + h / 2 - valueFontScale * 3, valueFontScale + 1, DimColor32);
                return;
            }

            int graphH = h;
            int graphY = y;

            if (showLatestValue)
            {
                int labelH = InWorldDisplayFont.CharHeight * valueFontScale + 4;
                graphH -= labelH;
                float latest = rewardHistory[rewardHistory.Count - 1];
                Color32 valColor = latest >= 0f ? AccentColor32 : WarningColor32;
                DrawTextRight(FormatNumber(latest, 2), x + w, y, valueFontScale, valColor);
                graphY += labelH;
            }

            if (graphH > 4)
                DrawGraph(rewardHistory, x, graphY, w, graphH, (Color32)graphLineColor, (Color32)graphFillColor);
        }
    }
}