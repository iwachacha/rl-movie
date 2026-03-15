using UnityEngine;

namespace RLMovie.Environments.ReactorCoreDelivery
{
    /// <summary>
    /// Keeps countdown readability high without covering the whole frame until the run turns critical.
    /// </summary>
    public sealed class ReactorCoreDeliveryHud : MonoBehaviour
    {
        [SerializeField] private ReactorCoreDeliveryCourse course;
        [SerializeField] private bool hideWhenStable;

#if !UNITY_SERVER
        private GUIStyle _titleStyle;
        private GUIStyle _timerStyle;
        private GUIStyle _subStyle;
        private GUIStyle _bannerStyle;

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

            bool stable = course.IsCoreDocked && !course.IsMeltdownTriggered;
            if (hideWhenStable && stable)
            {
                return;
            }

            EnsureStyles();

            float remaining01 = course.TimeRemaining01;
            bool critical = !course.IsMeltdownTriggered && remaining01 <= 0.22f;
            float urgency01 = course.IsMeltdownTriggered ? 1f : 1f - remaining01;
            float pulse01 = 0.45f + Mathf.Abs(Mathf.Sin(Time.time * Mathf.Lerp(4f, 10f, urgency01))) * 0.55f;

            Color accent = stable
                ? new Color(0.20f, 0.92f, 0.68f, 0.96f)
                : course.IsMeltdownTriggered
                    ? new Color(1.00f, 0.28f, 0.22f, 0.98f)
                    : Color.Lerp(new Color(0.96f, 0.76f, 0.28f, 0.94f), new Color(1.00f, 0.30f, 0.22f, 0.98f), Mathf.InverseLerp(0.55f, 0.05f, remaining01));

            DrawCornerBadge(accent, pulse01, stable);

            if (!critical && !course.IsMeltdownTriggered)
            {
                return;
            }

            float overlayAlpha = course.IsMeltdownTriggered ? 0.16f : 0.08f + (pulse01 * 0.08f);
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.12f, 0.01f, 0.01f, overlayAlpha));

            float stripeAlpha = course.IsMeltdownTriggered ? 0.42f : 0.22f + (pulse01 * 0.12f);
            DrawRect(new Rect(0f, 0f, Screen.width, 16f), new Color(accent.r, accent.g, accent.b, stripeAlpha));
            DrawRect(new Rect(0f, Screen.height - 16f, Screen.width, 16f), new Color(accent.r, accent.g, accent.b, stripeAlpha));

            string bannerText = course.IsMeltdownTriggered
                ? "REACTOR MELTDOWN"
                : "CRITICAL DELIVERY WINDOW";

            float bannerWidth = Mathf.Min(520f, Screen.width * 0.74f);
            Rect bannerRect = new Rect((Screen.width - bannerWidth) * 0.5f, 28f, bannerWidth, 54f);
            DrawRect(bannerRect, new Color(0.06f, 0.02f, 0.02f, 0.90f));
            DrawBorder(bannerRect, 3f, accent);
            _bannerStyle.normal.textColor = Color.Lerp(Color.white, accent, 0.30f);
            GUI.Label(bannerRect, bannerText, _bannerStyle);
        }

        private void DrawCornerBadge(Color accent, float pulse01, bool stable)
        {
            float width = Mathf.Min(250f, Screen.width * 0.34f);
            Rect badgeRect = new Rect(Screen.width - width - 20f, 20f, width, 84f);
            DrawRect(badgeRect, new Color(0.03f, 0.04f, 0.06f, 0.88f));
            DrawBorder(badgeRect, 3f + (pulse01 * 1.5f), accent);

            string title = stable
                ? "CORE STATUS"
                : course.IsHoldingCore
                    ? "CORE CARRIED"
                    : "REACTOR TIMER";
            string timeText = stable
                ? "SAFE"
                : course.IsMeltdownTriggered
                    ? "00"
                    : $"{Mathf.Clamp(Mathf.CeilToInt(course.TimeRemainingSeconds), 0, 99):00}";
            string subText = stable
                ? "SOCKET LOCKED"
                : course.IsHoldingCore
                    ? "GRAB ACTIVE"
                    : course.GrabCooldown01 > 0f
                        ? "GRAB COOLING"
                        : "ROLL OR CARRY";

            _titleStyle.normal.textColor = Color.Lerp(Color.white, accent, 0.40f);
            _timerStyle.normal.textColor = Color.Lerp(Color.white, accent, 0.18f);
            _subStyle.normal.textColor = Color.Lerp(Color.white, accent, 0.42f);

            GUI.Label(new Rect(badgeRect.x + 14f, badgeRect.y + 8f, badgeRect.width - 28f, 18f), title, _titleStyle);
            GUI.Label(new Rect(badgeRect.x + 14f, badgeRect.y + 22f, badgeRect.width - 28f, 36f), timeText, _timerStyle);
            GUI.Label(new Rect(badgeRect.x + 14f, badgeRect.y + 58f, badgeRect.width - 28f, 16f), subText, _subStyle);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            _timerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 34,
                fontStyle = FontStyle.Bold
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            _bannerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
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
#endif
    }
}
