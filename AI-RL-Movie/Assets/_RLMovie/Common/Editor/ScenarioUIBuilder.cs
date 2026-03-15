using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using RLMovie.UI;

namespace RLMovie.Editor
{
    public static class ScenarioUIBuilder
    {
        private const string PREFAB_PATH = "Assets/_RLMovie/Common/Prefabs/ScenarioInfoOverlay.prefab";

        [MenuItem("RLMovie/Build Scenario UI Prefab")]
        public static void BuildUIPrefab()
        {
            // 1. Create Root Canvas
            GameObject root = new GameObject("ScenarioInfoOverlay");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            ScenarioUIController controller = root.AddComponent<ScenarioUIController>();

            // 2. Create Background Panel (Top Right)
            GameObject panel = new GameObject("BackgroundPanel");
            panel.transform.SetParent(root.transform, false);
            Image bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.85f); // 少し青みがかった濃いグレー

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-30, -30); // マージンを少し広く
            panelRect.sizeDelta = new Vector2(450, 260); // パネルを少し大きく

            // 3. Helper to create Text
            Text CreateText(string name, string initText, Vector2 pos, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
            {
                GameObject txtObj = new GameObject(name);
                txtObj.transform.SetParent(panel.transform, false);
                Text textComponent = txtObj.AddComponent<Text>();
                textComponent.text = initText;
                textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textComponent.fontSize = fontSize;
                textComponent.fontStyle = fontStyle;
                textComponent.color = color;
                textComponent.alignment = alignment;

                RectTransform rect = txtObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.anchoredPosition = pos;
                rect.sizeDelta = new Vector2(-40, 40); // 20px padding left/right

                return textComponent;
            }

            // Title
            CreateText("TitleText", "AI学習リアルタイム解析", new Vector2(0, -25), 26, FontStyle.Bold, new Color(0.2f, 0.8f, 1f, 1f), TextAnchor.MiddleCenter);

            // Helper for value texts to make them pop out more
            Color labelColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            Color valueColor = Color.white;
            int labelSize = 22;

            // Episode
            controller.episodeText = CreateText("EpisodeText", "試行数: 0", new Vector2(0, -75), labelSize, FontStyle.Bold, valueColor, TextAnchor.MiddleLeft);

            // Success Rate
            controller.successRateText = CreateText("SuccessRateText", "目標達成率: 0.0%", new Vector2(0, -110), labelSize, FontStyle.Bold, valueColor, TextAnchor.MiddleLeft);

            // Reward
            controller.rewardText = CreateText("RewardText", "合計スコア: 0.00", new Vector2(0, -145), labelSize, FontStyle.Bold, valueColor, TextAnchor.MiddleLeft);

            // 4. Progress Bar (Step Count)
            GameObject pbg = new GameObject("ProgressBarBG");
            pbg.transform.SetParent(panel.transform, false);
            Image pbgImage = pbg.AddComponent<Image>();
            pbgImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            RectTransform pbgRect = pbg.GetComponent<RectTransform>();
            pbgRect.anchorMin = new Vector2(0, 1);
            pbgRect.anchorMax = new Vector2(1, 1);
            pbgRect.pivot = new Vector2(0.5f, 1);
            pbgRect.anchoredPosition = new Vector2(0, -195);
            pbgRect.sizeDelta = new Vector2(-60, 36);

            GameObject pfill = new GameObject("ProgressBarFill");
            pfill.transform.SetParent(pbg.transform, false);
            Image pfillImage = pfill.AddComponent<Image>();
            pfillImage.color = new Color(0.2f, 0.9f, 0.4f, 1f); // より鮮やかな緑
            pfillImage.type = Image.Type.Filled;
            pfillImage.fillMethod = Image.FillMethod.Horizontal;
            pfillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            pfillImage.fillAmount = 0f;

            RectTransform pfillRect = pfill.GetComponent<RectTransform>();
            pfillRect.anchorMin = new Vector2(0, 0);
            pfillRect.anchorMax = new Vector2(1, 1);
            pfillRect.pivot = new Vector2(0.5f, 0.5f);
            pfillRect.anchoredPosition = Vector2.zero;
            pfillRect.sizeDelta = Vector2.zero;

            controller.progressBarFill = pfillImage;

            // Step Text (Overlay on progress bar)
            controller.stepCountText = CreateText("StepCountText", "残り時間: 100.0%", new Vector2(0, -195), 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform stepRect = controller.stepCountText.GetComponent<RectTransform>();
            stepRect.sizeDelta = new Vector2(-60, 36); // match progress bar height
            stepRect.anchoredPosition = new Vector2(0, -195); // same Y as progress bar

            // 5. Ensure Prefabs folder exists and Save
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PREFAB_PATH));

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PREFAB_PATH, InteractionMode.AutomatedAction);

            // Destroy the temporary object from the scene
            GameObject.DestroyImmediate(root);

            Debug.Log($"[RLMovie] Successfully built and saved UI Prefab to {PREFAB_PATH}");
            
            // Refresh asset database to ensure Unity sees it immediately
            AssetDatabase.Refresh();
        }
    }
}
