using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// Displays total completed episode count as a large number.
    /// Attach to any Renderer (Quad on a monitor, sign, billboard, etc.).
    /// </summary>
    public class EpisodeCountDisplay : InWorldDisplayBase
    {
        [Header("=== Episode Count Settings ===")]
        [SerializeField] private int mainFontScale = 5;
        [SerializeField] private bool useLargeNumberFormat = true;

        protected override void RenderContent(int x, int y, int w, int h)
        {
            if (Agent == null)
            {
                DrawTextCentered("---", y + h / 2 - mainFontScale * 3, mainFontScale, DimColor32);
                return;
            }

            int episodes = Agent.CompletedEpisodes;
            string text = useLargeNumberFormat ? FormatLargeNumber(episodes) : episodes.ToString();

            int textW = MeasureText(text, mainFontScale);
            int textH = InWorldDisplayFont.CharHeight * mainFontScale;
            int tx = x + (w - textW) / 2;
            int ty = y + (h - textH) / 2;

            DrawText(text, tx, ty, mainFontScale, AccentColor32);
        }
    }
}