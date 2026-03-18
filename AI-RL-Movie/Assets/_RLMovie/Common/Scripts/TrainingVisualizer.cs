using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RLMovie.Common
{
    /// <summary>
    /// Lightweight runtime training monitor.
    /// In server/headless builds the IMGUI path is compiled out entirely.
    /// </summary>
    public class TrainingVisualizer : MonoBehaviour
    {
        [Header("=== Visualizer Settings ===")]
        [SerializeField] private BaseRLAgent targetAgent;
        [SerializeField] private bool showUI = true;

        [Header("=== Graph Settings ===")]
        [SerializeField] private int maxDataPoints = 200;
        [SerializeField] private Color graphColor = new Color(0.2f, 0.8f, 0.4f);
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.7f);

        private readonly List<float> rewardHistory = new();
        private float maxReward = 1f;
        private float minReward = -1f;
        private int lastCompletedEpisodeCount;
        private bool runtimeUiEnabled = true;

#if !UNITY_SERVER
        private readonly float panelWidth = 320f;
        private readonly float panelHeight = 200f;
        private readonly float panelMargin = 10f;
        private readonly float graphHeight = 100f;
#endif

        private void Awake()
        {
            runtimeUiEnabled = !RLMovieRuntime.IsHeadless;
            if (!runtimeUiEnabled)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (!runtimeUiEnabled || targetAgent == null)
            {
                return;
            }

            if (targetAgent.CompletedEpisodes <= lastCompletedEpisodeCount)
            {
                return;
            }

            lastCompletedEpisodeCount = targetAgent.CompletedEpisodes;
            rewardHistory.Add(targetAgent.LastCompletedEpisodeReward);

            int clampedMaxDataPoints = Mathf.Max(2, maxDataPoints);
            if (rewardHistory.Count > clampedMaxDataPoints)
            {
                rewardHistory.RemoveAt(0);
            }

            RecalculateRewardBounds();
        }

#if !UNITY_SERVER
        private void OnGUI()
        {
            if (!runtimeUiEnabled || !showUI || targetAgent == null)
            {
                return;
            }

            float x = Screen.width - panelWidth - panelMargin;
            float y = panelMargin;

            DrawRect(new Rect(x, y, panelWidth, panelHeight), bgColor);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(x, y + 4, panelWidth, 24), "Training Monitor", titleStyle);

            GUIStyle statStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            float statY = y + 30;
            float statX = x + 10;

            GUI.Label(new Rect(statX, statY, panelWidth, 18),
                $"Completed: {targetAgent.CompletedEpisodes}", statStyle);

            GUI.Label(new Rect(statX + 150, statY, panelWidth, 18),
                $"Success: {targetAgent.SuccessRate:P1}", statStyle);

            GUI.Label(new Rect(statX, statY + 18, panelWidth, 18),
                $"Live Reward: {targetAgent.CurrentEpisodeReward:F2}", statStyle);

            GUI.Label(new Rect(statX + 150, statY + 18, panelWidth, 18),
                $"Last Reward: {targetAgent.LastCompletedEpisodeReward:F2}", statStyle);

            float graphX = x + 10;
            float graphY = statY + 42;
            float graphW = panelWidth - 20;

            RLMovieIMGUI.DrawRect(new Rect(graphX, graphY, graphW, graphHeight), new Color(0.1f, 0.1f, 0.1f, 0.8f));

            if (rewardHistory.Count > 1)
            {
                float range = Mathf.Max(maxReward - minReward, 0.01f);
                float pointDivisor = Mathf.Max(1, rewardHistory.Count - 1);

                for (int i = 1; i < rewardHistory.Count; i++)
                {
                    float x1 = graphX + (i - 1) / pointDivisor * graphW;
                    float x2 = graphX + i / pointDivisor * graphW;
                    float y1 = graphY + graphHeight - ((rewardHistory[i - 1] - minReward) / range) * graphHeight;
                    float y2 = graphY + graphHeight - ((rewardHistory[i] - minReward) / range) * graphHeight;

                    RLMovieIMGUI.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), graphColor, 2f);
                }

                float zeroY = graphY + graphHeight - ((0f - minReward) / range) * graphHeight;
                if (zeroY > graphY && zeroY < graphY + graphHeight)
                {
                    RLMovieIMGUI.DrawLine(
                        new Vector2(graphX, zeroY),
                        new Vector2(graphX + graphW, zeroY),
                        new Color(1f, 1f, 1f, 0.3f),
                        1f);
                }
            }

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(1f, 1f, 1f, 0.6f) }
            };

            GUI.Label(new Rect(graphX, graphY - 2, 60, 14), $"{maxReward:F1}", labelStyle);
            GUI.Label(new Rect(graphX, graphY + graphHeight - 12, 60, 14), $"{minReward:F1}", labelStyle);
        }

        private void DrawRect(Rect rect, Color color)
        {
            RLMovieIMGUI.DrawRect(rect, color);
        }
#endif

        private void RecalculateRewardBounds()
        {
            if (rewardHistory.Count == 0)
            {
                minReward = -1f;
                maxReward = 1f;
                return;
            }

            minReward = rewardHistory[0];
            maxReward = rewardHistory[0];

            for (int i = 1; i < rewardHistory.Count; i++)
            {
                float reward = rewardHistory[i];
                if (reward > maxReward)
                {
                    maxReward = reward;
                }

                if (reward < minReward)
                {
                    minReward = reward;
                }
            }
        }
    }
}
