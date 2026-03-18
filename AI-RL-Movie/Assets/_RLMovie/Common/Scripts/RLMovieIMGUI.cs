using UnityEngine;

namespace RLMovie.Common
{
#if !UNITY_SERVER
    /// <summary>
    /// Shared IMGUI drawing primitives used by video-facing overlays.
    /// Compiled out in server builds.
    /// </summary>
    public static class RLMovieIMGUI
    {
        private static Texture2D _whiteTexture;

        public static Texture2D WhiteTexture
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

        public static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = previous;
        }

        public static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
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
    }
#endif
}
