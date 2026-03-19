using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// Displays current episode step progress as a bar and/or countdown.
    /// Shows step/maxStep when MaxStep > 0, otherwise elapsed steps.
    /// Attach to any Renderer (Quad on a monitor, sign, billboard, etc.).
    /// </summary>
    public class EpisodeTimerDisplay : InWorldDisplayBase
    {
        [Header("=== Timer Settings ===")]
        [SerializeField] private int numberFontScale = 4;
        [SerializeField] private int barHeight = 12;
        [SerializeField] private bool showProgressBar = true;
        [SerializeField] private bool showStepNumbers = true;

        protected override void RenderContent(int x, int y, int w, int h)
        {
            if (Agent == null)
            {
                DrawTextCentered("---", y + h / 2 - numberFontScale * 3, numberFontScale, DimColor32);
                return;
            }

            int step = Agent.StepCount;
            int maxStep = Agent.MaxStep;
            bool hasLimit = maxStep > 0;

            if (showProgressBar && hasLimit)
            {
                float ratio = (float)step / maxStep;
                int barY = y + h - barHeight;
                Color32 barColor = ratio > 0.85f ? WarningColor32 : AccentColor32;
                DrawProgressBar(ratio, x, barY, w, barHeight, barColor, DimColor32);

                if (showStepNumbers)
                {
                    int remaining = Mathf.Max(0, maxStep - step);
                    string text = remaining.ToString();
                    int textH = InWorldDisplayFont.CharHeight * numberFontScale;
                    int textY = y + (barY - y - textH) / 2;
                    Color32 numColor = ratio > 0.85f ? WarningColor32 : PrimaryColor32;
                    DrawTextCentered(text, textY, numberFontScale, numColor);
                }
            }
            else
            {
                string text = step.ToString();
                int textW = MeasureText(text, numberFontScale);
                int textH = InWorldDisplayFont.CharHeight * numberFontScale;
                DrawText(text, x + (w - textW) / 2, y + (h - textH) / 2, numberFontScale, PrimaryColor32);
            }
        }
    }
}