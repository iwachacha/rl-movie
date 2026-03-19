using System.Collections.Generic;
using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// Displays success rate over recent episodes as percentage + bar.
    /// Uses base class telemetry subscription to track success/fail outcomes.
    /// Attach to any Renderer (Quad on a monitor, sign, billboard, etc.).
    /// </summary>
    public class SuccessRateDisplay : InWorldDisplayBase
    {
        [Header("=== Success Rate Settings ===")]
        [SerializeField] private int mainFontScale = 4;
        [SerializeField] private int barHeight = 10;
        [SerializeField] private int windowSize = 50;
        [SerializeField] private bool showBar = true;

        private readonly Queue<bool> outcomes = new Queue<bool>();
        private int successCount;

        protected override void Awake()
        {
            base.Awake();
            SubscribeToAgentTelemetry();
        }

        protected override void OnAgentTelemetryEvent(AgentTelemetryEvent evt)
        {
            if (evt.EventType == AgentTelemetryEventType.Success)
                RecordOutcome(true);
            else if (evt.EventType == AgentTelemetryEventType.Failure)
                RecordOutcome(false);
        }

        private void RecordOutcome(bool success)
        {
            outcomes.Enqueue(success);
            if (success) successCount++;

            while (outcomes.Count > windowSize)
            {
                bool removed = outcomes.Dequeue();
                if (removed) successCount--;
            }
        }

        protected override void RenderContent(int x, int y, int w, int h)
        {
            if (outcomes.Count == 0)
            {
                DrawTextCentered("---", y + h / 2 - mainFontScale * 3, mainFontScale, DimColor32);
                return;
            }

            float rate = (float)successCount / outcomes.Count;
            int pct = Mathf.RoundToInt(rate * 100f);
            string text = pct + "%";

            Color32 rateColor;
            if (rate >= 0.7f) rateColor = AccentColor32;
            else if (rate >= 0.3f) rateColor = PrimaryColor32;
            else rateColor = WarningColor32;

            int numberAreaH = h;
            if (showBar)
            {
                numberAreaH -= barHeight + 4;
                int barY = y + h - barHeight;
                DrawProgressBar(rate, x, barY, w, barHeight, rateColor, DimColor32);
            }

            int textW = MeasureText(text, mainFontScale);
            int textH = InWorldDisplayFont.CharHeight * mainFontScale;
            DrawText(text, x + (w - textW) / 2, y + (numberAreaH - textH) / 2, mainFontScale, rateColor);
        }
    }
}