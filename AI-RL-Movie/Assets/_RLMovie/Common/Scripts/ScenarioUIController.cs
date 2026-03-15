using UnityEngine;
using UnityEngine.UI;
using RLMovie.Common;

namespace RLMovie.UI
{
    /// <summary>
    /// UI Controller for the AI Warehouse-style Scenario Info Overlay.
    /// Automatically finds the BaseRLAgent in the scene and displays its training metrics.
    /// </summary>
    public class ScenarioUIController : MonoBehaviour
    {
        [Header("UI References")]
        public Text episodeText;
        public Text stepCountText;
        public Text rewardText;
        public Text successRateText;
        public Image progressBarFill;

        [Header("Settings")]
        [Tooltip("If null, the controller will automatically find the first BaseRLAgent in the scene.")]
        public BaseRLAgent targetAgent;

        [Header("Visual Feedback")]
        public float lerpSpeed = 5f;
        public Color lowSuccessColor = new Color(0.9f, 0.3f, 0.3f); // Reddish
        public Color midSuccessColor = new Color(0.9f, 0.9f, 0.3f); // Yellowish
        public Color highSuccessColor = new Color(0.3f, 0.9f, 0.3f); // Greenish

        private float _displayReward;
        private float _displaySuccessRate;
        private float _displayStepFill;

        private void Start()
        {
            if (targetAgent == null)
            {
                targetAgent = UnityEngine.Object.FindFirstObjectByType<BaseRLAgent>();
            }

            if (targetAgent == null)
            {
                Debug.LogWarning("[ScenarioUIController] No BaseRLAgent found in the scene.");
            }
        }

        private void Update()
        {
            if (targetAgent == null) return;

            float dt = Time.deltaTime;

            // Smooth Lerping for "Juicy" UI
            _displayReward = Mathf.Lerp(_displayReward, targetAgent.CurrentEpisodeReward, dt * lerpSpeed);
            _displaySuccessRate = Mathf.Lerp(_displaySuccessRate, targetAgent.SuccessRate, dt * lerpSpeed);
            float targetStepFill = (targetAgent.MaxStep > 0) ? (float)targetAgent.StepCount / targetAgent.MaxStep : 0f;
            _displayStepFill = Mathf.Lerp(_displayStepFill, targetStepFill, dt * lerpSpeed * 2f);

            // Update Texts
            if (episodeText != null)
            {
                episodeText.text = $"試行数: <color=white>{targetAgent.CurrentEpisodeNumber}</color>";
            }

            if (stepCountText != null)
            {
                // 残り時間をパーセントで計算 (100% -> 0%)
                float remainingPercent = (targetAgent.MaxStep > 0) 
                    ? (1.0f - (float)targetAgent.StepCount / targetAgent.MaxStep) 
                    : 1.0f;
                stepCountText.text = $"残り時間: <color=white>{remainingPercent:P1}</color>";
            }

            if (rewardText != null)
            {
                // Slightly pulse color if reward is positive
                string rewardColor = _displayReward >= 0 ? "#50FF50" : "#FF5050";
                rewardText.text = $"合計スコア: <color={rewardColor}>{_displayReward:F2}</color>";
            }

            if (successRateText != null)
            {
                // Dynamic Success Rate Color
                Color color = lowSuccessColor;
                if (targetAgent.SuccessRate >= 0.7f) color = highSuccessColor;
                else if (targetAgent.SuccessRate >= 0.3f) color = midSuccessColor;
                
                string hexColor = ColorUtility.ToHtmlStringRGB(color);
                successRateText.text = $"目標達成率: <color=#{hexColor}>{_displaySuccessRate:P1}</color>";
            }

            // Update Progress Bar (Battery Style)
            if (progressBarFill != null)
            {
                // バーは右から左へ減るように（FillAmountを残り時間に合わせる）
                float remainingPercent = (targetAgent.MaxStep > 0) 
                    ? (1.0f - (float)targetAgent.StepCount / targetAgent.MaxStep) 
                    : 1.0f;
                
                progressBarFill.fillAmount = remainingPercent;
                
                // Color & Flashing Logic
                if (remainingPercent <= 0.15f)
                {
                    // 15%以下: 赤点滅
                    float lerp = Mathf.PingPong(Time.time * 8f, 1.0f);
                    progressBarFill.color = Color.Lerp(new Color(1f, 0.1f, 0.1f), new Color(0.3f, 0f, 0f), lerp);
                }
                else if (remainingPercent <= 0.3f)
                {
                    // 30%以下: 赤
                    progressBarFill.color = new Color(1f, 0.3f, 0.3f);
                }
                else if (remainingPercent <= 0.5f)
                {
                    // 50%以下: 黄色
                    progressBarFill.color = new Color(1f, 0.9f, 0.3f);
                }
                else
                {
                    // 通常時: 緑
                    progressBarFill.color = new Color(0.2f, 0.9f, 0.4f);
                }
            }
        }
    }
}
