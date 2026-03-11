using UnityEngine;
using System.Collections.Generic;

namespace RLMovie.Common
{
    /// <summary>
    /// 学習過程をリアルタイムで可視化するUI。
    /// シーンに配置して BaseRLAgent を参照させてください。
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

        private List<float> rewardHistory = new List<float>();
        private float maxReward = 1f;
        private float minReward = -1f;
        private int lastEpisodeCount = 0;

        // UI dimensions
        private readonly float panelWidth = 320f;
        private readonly float panelHeight = 200f;
        private readonly float panelMargin = 10f;
        private readonly float graphHeight = 100f;

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

        private void Update()
        {
            if (targetAgent == null) return;

            // 新しいエピソードが完了したらグラフにデータ追加
            if (targetAgent.TotalEpisodes > lastEpisodeCount)
            {
                lastEpisodeCount = targetAgent.TotalEpisodes;
                rewardHistory.Add(targetAgent.CurrentEpisodeReward);

                if (rewardHistory.Count > maxDataPoints)
                    rewardHistory.RemoveAt(0);

                // グラフ範囲の更新
                foreach (var r in rewardHistory)
                {
                    if (r > maxReward) maxReward = r;
                    if (r < minReward) minReward = r;
                }
            }
        }

        private void OnGUI()
        {
            if (!showUI || targetAgent == null) return;

            float x = Screen.width - panelWidth - panelMargin;
            float y = panelMargin;

            // パネル背景
            DrawRect(new Rect(x, y, panelWidth, panelHeight), bgColor);

            // タイトル
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(x, y + 4, panelWidth, 24), "📊 Training Monitor", titleStyle);

            // 統計情報
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

            // 報酬グラフ
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

                // ゼロライン
                float zeroY = graphY + graphHeight - ((0f - minReward) / range) * graphHeight;
                if (zeroY > graphY && zeroY < graphY + graphHeight)
                {
                    DrawLine(
                        new Vector2(graphX, zeroY),
                        new Vector2(graphX + graphW, zeroY),
                        new Color(1f, 1f, 1f, 0.3f), 1f);
                }
            }

            // グラフラベル
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
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = prev;
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Color prev = GUI.color;
            GUI.color = color;

            Vector2 d = b - a;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

            var matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - width / 2f, d.magnitude, width), WhiteTexture);
            GUI.matrix = matrixBackup;

            GUI.color = prev;
        }
    }
}
