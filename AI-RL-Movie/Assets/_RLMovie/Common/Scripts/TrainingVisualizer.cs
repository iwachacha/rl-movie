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
        private int lastEpisodeCount;
        private bool runtimeUiEnabled = true;

#if !UNITY_SERVER
        private readonly float panelWidth = 320f;
        private readonly float panelHeight = 200f;
        private readonly float panelMargin = 10f;
        private readonly float graphHeight = 100f;
        private Texture2D whiteTexture;

        private Texture2D WhiteTexture
        {
            get
            {
                if (whiteTexture == null)
                {
                    whiteTexture = new Texture2D(1, 1);
                    whiteTexture.SetPixel(0, 0, Color.white);
                    whiteTexture.Apply();
                }

                return whiteTexture;
            }
        }
#endif

        private void Awake()
        {
            runtimeUiEnabled = !IsHeadlessRuntime();
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

            if (targetAgent.TotalEpisodes <= lastEpisodeCount)
            {
                return;
            }

            lastEpisodeCount = targetAgent.TotalEpisodes;
            rewardHistory.Add(targetAgent.CurrentEpisodeReward);

            if (rewardHistory.Count > maxDataPoints)
            {
                rewardHistory.RemoveAt(0);
            }

            foreach (float reward in rewardHistory)
            {
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
                $"Episodes: {targetAgent.TotalEpisodes}", statStyle);

            GUI.Label(new Rect(statX + 150, statY, panelWidth, 18),
                $"Success: {targetAgent.SuccessRate:P1}", statStyle);

            GUI.Label(new Rect(statX, statY + 18, panelWidth, 18),
                $"Current Reward: {targetAgent.CurrentEpisodeReward:F2}", statStyle);

            float graphX = x + 10;
            float graphY = statY + 42;
            float graphW = panelWidth - 20;

            DrawRect(new Rect(graphX, graphY, graphW, graphHeight), new Color(0.1f, 0.1f, 0.1f, 0.8f));

            if (rewardHistory.Count > 1)
            {
                float range = Mathf.Max(maxReward - minReward, 0.01f);

                for (int i = 1; i < rewardHistory.Count; i++)
                {
                    float x1 = graphX + (float)(i - 1) / (maxDataPoints - 1) * graphW;
                    float x2 = graphX + (float)i / (maxDataPoints - 1) * graphW;
                    float y1 = graphY + graphHeight - ((rewardHistory[i - 1] - minReward) / range) * graphHeight;
                    float y2 = graphY + graphHeight - ((rewardHistory[i] - minReward) / range) * graphHeight;

                    DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), graphColor, 2f);
                }

                float zeroY = graphY + graphHeight - ((0f - minReward) / range) * graphHeight;
                if (zeroY > graphY && zeroY < graphY + graphHeight)
                {
                    DrawLine(
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
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = previous;
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Color previous = GUI.color;
            GUI.color = color;

            Vector2 delta = b - a;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - width / 2f, delta.magnitude, width), WhiteTexture);
            GUI.matrix = matrixBackup;

            GUI.color = previous;
        }
#endif

        private static bool IsHeadlessRuntime()
        {
            return Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }
    }
}
