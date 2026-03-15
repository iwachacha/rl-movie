using UnityEngine;

namespace RLMovie.Environments.ReactorCoreDelivery
{
    /// <summary>
    /// Draws a persistent meltdown HUD so the countdown and alert state stay readable from any camera.
    /// </summary>
    public sealed class ReactorCoreDeliveryHud : MonoBehaviour
    {
        [SerializeField] private ReactorCoreDeliveryCourse course;
        [SerializeField] private bool hideWhenStable;

#if !UNITY_SERVER
        private GUIStyle _headlineStyle;
        private GUIStyle _countdownStyle;
        private GUIStyle _subStyle;
        private GUIStyle _cornerLabelStyle;
        private GUIStyle _cornerTimeStyle;

        private void OnGUI()
        {
            if (course == null)
            {
                course = FindFirstObjectByType<ReactorCoreDeliveryCourse>();
            }

            if (course == null || Application.isBatchMode)
            {
                return;
            }

            bool showHud = !hideWhenStable || course.IsAlertActive || course.IsMeltdownTriggered || !course.IsCoreDocked;
            if (!showHud)
            {
                return;
            }

            EnsureStyles();

            float remaining01 = course.TimeRemaining01;
            float urgency01 = course.IsMeltdownTriggered ? 1f : 1f - remaining01;
            float critical01 = course.IsMeltdownTriggered ? 1f : Mathf.InverseLerp(0.32f, 0.04f, remaining01);
            float pulse01 = 0.45f + (Mathf.Abs(Mathf.Sin(Time.time * Mathf.Lerp(4.0f, 11.0f, urgency01))) * 0.55f);

            Color sirenColor = course.IsCoreDocked && !course.IsMeltdownTriggered
                ? new Color(0.18f, 0.95f, 0.62f, 0.92f)
                : Color.Lerp(new Color(0.90f, 0.14f, 0.14f, 0.92f), new Color(1.0f, 0.28f, 0.18f, 0.98f), pulse01 * critical01);
            Color frameColor = new Color(sirenColor.r, sirenColor.g * 0.75f, sirenColor.b * 0.75f, 0.38f + (critical01 * 0.22f));

            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.10f, 0f, 0f, 0.03f + (urgency01 * 0.10f) + (critical01 * pulse01 * 0.10f)));
            DrawSirenBars(sirenColor, pulse01, critical01);
            DrawBorder(new Rect(0f, 0f, Screen.width, Screen.height), 6f + (pulse01 * 10f), frameColor);

            Vector2 jitter = course.IsMeltdownTriggered || critical01 > 0f
                ? new Vector2(Mathf.Sin(Time.time * 35f), Mathf.Cos(Time.time * 29f)) * (critical01 * 9f)
                : Vector2.zero;

            float panelWidth = Mathf.Min(420f, Screen.width * 0.52f);
            Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f + jitter.x, 18f + jitter.y, panelWidth, 116f);
            DrawRect(panelRect, new Color(0.05f, 0.02f, 0.02f, 0.90f));
            DrawBorder(panelRect, 4f + (pulse01 * 4f), sirenColor);

            string headline = course.IsMeltdownTriggered
                ? "REACTOR MELTDOWN"
                : course.IsCoreDocked
                    ? "CORE STABLE"
                    : "EMERGENCY ALERT";
            string countdownText = course.IsCoreDocked && !course.IsMeltdownTriggered
                ? "SAFE"
                : $"{Mathf.Clamp(Mathf.CeilToInt(course.TimeRemainingSeconds), 0, 99):00}";
            string subLabel = course.IsMeltdownTriggered
                ? "RESETTING TRIAL"
                : course.IsCoreDocked
                    ? "SOCKET LOCK ENGAGED"
                    : critical01 > 0.75f
                        ? "DELIVER CORE NOW"
                        : "TIME TO MELTDOWN";

            _headlineStyle.normal.textColor = sirenColor;
            _countdownStyle.normal.textColor = Color.Lerp(Color.white, sirenColor, 0.22f);
            _subStyle.normal.textColor = Color.Lerp(Color.white, sirenColor, 0.40f);

            DrawCornerTimerBadge(sirenColor, critical01, pulse01, countdownText);

            GUI.Label(new Rect(panelRect.x, panelRect.y + 8f, panelRect.width, 24f), headline, _headlineStyle);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 28f, panelRect.width, 58f), countdownText, _countdownStyle);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 84f, panelRect.width, 22f), subLabel, _subStyle);

            if (critical01 > 0.5f || course.IsMeltdownTriggered)
            {
                float messageWidth = Mathf.Min(540f, Screen.width * 0.72f);
                Rect messageRect = new Rect((Screen.width - messageWidth) * 0.5f - jitter.x, Screen.height - 104f - jitter.y, messageWidth, 44f);
                DrawRect(messageRect, new Color(0.22f, 0.02f, 0.02f, 0.70f));
                DrawBorder(messageRect, 3f, sirenColor);
                GUI.Label(messageRect, course.IsMeltdownTriggered ? "CRITICAL FAILURE" : "DELIVER THE CORE BEFORE TOTAL SYSTEM LOSS", _subStyle);
            }
        }

        private void EnsureStyles()
        {
            if (_headlineStyle != null)
            {
                return;
            }

            _headlineStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };

            _countdownStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 48,
                fontStyle = FontStyle.Bold
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };

            _cornerLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            _cornerTimeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 30,
                fontStyle = FontStyle.Bold
            };
        }

        private void DrawSirenBars(Color sirenColor, float pulse01, float critical01)
        {
            float barHeight = 16f + (pulse01 * 8f);
            float gap = Mathf.Max(16f, Screen.width / 12f);
            float alpha = 0.26f + (critical01 * 0.16f);

            for (int i = -1; i < 12; i++)
            {
                float x = i * gap;
                Color bandColor = (i % 2 == 0)
                    ? new Color(sirenColor.r, sirenColor.g, sirenColor.b, alpha)
                    : new Color(0.95f, 0.95f, 0.95f, alpha * 0.32f);

                DrawRect(new Rect(x, 0f, gap * 0.7f, barHeight), bandColor);
                DrawRect(new Rect(x + (gap * 0.15f), Screen.height - barHeight, gap * 0.7f, barHeight), bandColor);
            }
        }

        private static void DrawBorder(Rect rect, float thickness, Color color)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        private void DrawCornerTimerBadge(Color sirenColor, float critical01, float pulse01, string countdownText)
        {
            float badgeWidth = Mathf.Min(220f, Screen.width * 0.32f);
            Rect badgeRect = new Rect(Screen.width - badgeWidth - 18f, 18f, badgeWidth, 74f);

            DrawRect(badgeRect, new Color(0.03f, 0.03f, 0.05f, 0.88f));
            DrawBorder(badgeRect, 3f + (critical01 * pulse01 * 2f), sirenColor);

            _cornerLabelStyle.normal.textColor = Color.Lerp(Color.white, sirenColor, 0.48f);
            _cornerTimeStyle.normal.textColor = Color.Lerp(Color.white, sirenColor, 0.15f);

            GUI.Label(
                new Rect(badgeRect.x + 12f, badgeRect.y + 8f, badgeRect.width - 24f, 20f),
                course.IsCoreDocked && !course.IsMeltdownTriggered ? "STATUS" : "TIME LIMIT",
                _cornerLabelStyle);
            GUI.Label(
                new Rect(badgeRect.x + 12f, badgeRect.y + 22f, badgeRect.width - 24f, 42f),
                countdownText,
                _cornerTimeStyle);
        }
#endif
    }
}
