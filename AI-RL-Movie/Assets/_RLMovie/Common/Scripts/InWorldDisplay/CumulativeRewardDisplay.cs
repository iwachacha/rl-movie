using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// Displays the current episode's cumulative reward as a large number.
    /// Color shifts based on positive/negative value.
    /// Attach to any Renderer (Quad on a monitor, sign, billboard, etc.).
    /// </summary>
    public class CumulativeRewardDisplay : InWorldDisplayBase
    {
        [Header("=== Reward Display Settings ===")]
        [SerializeField] private int mainFontScale = 4;
        [SerializeField] private int decimals = 2;
        [SerializeField] private bool colorBySign = true;

        protected override void RenderContent(int x, int y, int w, int h)
        {
            if (Agent == null)
            {
                DrawTextCentered("---", y + h / 2 - mainFontScale * 3, mainFontScale, DimColor32);
                return;
            }

            float reward = Agent.GetCumulativeReward();
            string text = FormatNumber(reward, decimals);

            Color32 color = PrimaryColor32;
            if (colorBySign)
            {
                if (reward > 0.01f) color = AccentColor32;
                else if (reward < -0.01f) color = WarningColor32;
                else color = DimColor32;
            }

            int textW = MeasureText(text, mainFontScale);
            int textH = InWorldDisplayFont.CharHeight * mainFontScale;
            DrawText(text, x + (w - textW) / 2, y + (h - textH) / 2, mainFontScale, color);
        }
    }
}