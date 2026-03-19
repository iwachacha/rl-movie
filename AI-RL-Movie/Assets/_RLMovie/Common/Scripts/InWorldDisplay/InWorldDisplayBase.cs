using System;
using System.Collections.Generic;
using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// Base class for in-world training info displays.
    /// Renders training data onto a Texture2D applied to any Renderer.
    /// Attach to (or near) a Quad, Plane, or custom mesh to create
    /// environment-blending info displays (monitors, signs, billboards).
    /// </summary>
    public abstract class InWorldDisplayBase : MonoBehaviour
    {
        [Header("=== Display Settings ===")]
        [SerializeField] protected int textureWidth = 256;
        [SerializeField] protected int textureHeight = 128;
        [SerializeField] protected float updateInterval = 0.5f;
        [SerializeField] protected FilterMode filterMode = FilterMode.Point;
        [SerializeField] protected int padding = 8;

        [Header("=== Colors ===")]
        [SerializeField] protected Color backgroundColor = new Color(0.05f, 0.07f, 0.1f, 1f);
        [SerializeField] protected Color primaryColor = new Color(0.88f, 0.93f, 1f, 1f);
        [SerializeField] protected Color accentColor = new Color(0.2f, 0.85f, 0.5f, 1f);
        [SerializeField] protected Color warningColor = new Color(0.95f, 0.35f, 0.22f, 1f);
        [SerializeField] protected Color dimColor = new Color(0.22f, 0.26f, 0.32f, 1f);

        [Header("=== Label (Optional) ===")]
        [SerializeField] protected bool showLabel;
        [SerializeField] protected string labelText = "";
        [SerializeField] protected int labelFontScale = 2;

        [Header("=== Renderer ===")]
        [SerializeField] protected Renderer targetRenderer;
        [SerializeField] protected bool useEmission = true;
        [SerializeField, Range(0f, 3f)] protected float emissionIntensity = 1.2f;

        [Header("=== Agent Binding ===")]
        [SerializeField] protected BaseRLAgent targetAgent;
        [SerializeField] protected string targetAgentRole = "hero";
        [SerializeField] protected string[] targetAgentRoles = Array.Empty<string>();
        [SerializeField] protected string targetAgentTeam = "";

        // Runtime
        protected Texture2D displayTexture;
        protected Color32[] pixels;
        private Material runtimeMaterial;
        private float nextUpdateTime;
        private BaseRLAgent _subscribedTelemetryAgent;
        private bool _wantsTelemetry;

        // Convenience
        protected BaseRLAgent Agent => targetAgent;
        protected int Width => textureWidth;
        protected int Height => textureHeight;

        protected Color32 BgColor32 => backgroundColor;
        protected Color32 PrimaryColor32 => primaryColor;
        protected Color32 AccentColor32 => accentColor;
        protected Color32 WarningColor32 => warningColor;
        protected Color32 DimColor32 => dimColor;

        #region Lifecycle

        protected virtual void Awake()
        {
            if (RLMovieRuntime.IsHeadless)
            {
                enabled = false;
                return;
            }

            InitializeTexture();
            TryAutoResolveTargetAgent();
        }

        protected virtual void OnDestroy()
        {
            UnsubscribeFromAgentTelemetry();
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
            if (displayTexture != null) Destroy(displayTexture);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextUpdateTime) return;
            nextUpdateTime = Time.unscaledTime + updateInterval;

            TryAutoResolveTargetAgent();
            TrySubscribeToResolvedAgent();

            Clear(BgColor32);

            int contentX = padding;
            int contentY = padding;
            int contentW = textureWidth - padding * 2;
            int contentH = textureHeight - padding * 2;

            if (showLabel && !string.IsNullOrEmpty(labelText))
            {
                int labelH = InWorldDisplayFont.CharHeight * labelFontScale + 4;
                DrawText(labelText, contentX, contentY, labelFontScale, DimColor32);
                contentY += labelH;
                contentH -= labelH;
            }

            if (contentW > 0 && contentH > 0)
            {
                RenderContent(contentX, contentY, contentW, contentH);
            }

            displayTexture.SetPixels32(pixels);
            displayTexture.Apply(false);
        }

        #endregion

        #region Abstract

        /// <summary>
        /// Override to draw the display content.
        /// Coordinates are in screen space (0,0 = top-left, y increases downward).
        /// Use the protected drawing methods (DrawText, DrawFilledRect, DrawGraph, etc.).
        /// </summary>
        protected abstract void RenderContent(int x, int y, int w, int h);

        #endregion

        #region Drawing Primitives

        protected void Clear(Color32 color)
        {
            var span = new Color32[pixels.Length];
            for (int i = 0; i < span.Length; i++) span[i] = color;
            Array.Copy(span, pixels, pixels.Length);
        }

        protected void SetPixel(int x, int y, Color32 color)
        {
            if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight) return;
            pixels[(textureHeight - 1 - y) * textureWidth + x] = color;
        }

        protected void DrawFilledRect(int x, int y, int w, int h, Color32 color)
        {
            for (int py = y; py < y + h; py++)
            for (int px = x; px < x + w; px++)
                SetPixel(px, py, color);
        }

        protected void DrawRectOutline(int x, int y, int w, int h, Color32 color, int thickness = 1)
        {
            DrawFilledRect(x, y, w, thickness, color);
            DrawFilledRect(x, y + h - thickness, w, thickness, color);
            DrawFilledRect(x, y, thickness, h, color);
            DrawFilledRect(x + w - thickness, y, thickness, h, color);
        }

        protected void DrawHLine(int x, int y, int length, Color32 color, int thickness = 1)
        {
            DrawFilledRect(x, y, length, thickness, color);
        }

        protected void DrawVLine(int x, int y, int length, Color32 color, int thickness = 1)
        {
            DrawFilledRect(x, y, thickness, length, color);
        }

        protected void DrawLine(int x0, int y0, int x1, int y1, Color32 color, int thickness = 1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int half = thickness / 2;

            while (true)
            {
                for (int oy = -half; oy <= half; oy++)
                for (int ox = -half; ox <= half; ox++)
                    SetPixel(x0 + ox, y0 + oy, color);

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        protected void DrawChar(char c, int x, int y, int scale, Color32 color)
        {
            byte[] glyph = InWorldDisplayFont.GetGlyph(c);
            for (int row = 0; row < InWorldDisplayFont.CharHeight; row++)
            {
                byte rowData = glyph[row];
                for (int col = 0; col < InWorldDisplayFont.CharWidth; col++)
                {
                    if ((rowData & (1 << (InWorldDisplayFont.CharWidth - 1 - col))) != 0)
                    {
                        DrawFilledRect(x + col * scale, y + row * scale, scale, scale, color);
                    }
                }
            }
        }

        protected void DrawText(string text, int x, int y, int scale, Color32 color)
        {
            if (string.IsNullOrEmpty(text)) return;
            int charStep = (InWorldDisplayFont.CharWidth + InWorldDisplayFont.Spacing) * scale;
            for (int i = 0; i < text.Length; i++)
            {
                DrawChar(text[i], x + i * charStep, y, scale, color);
            }
        }

        protected int MeasureText(string text, int scale)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int charStep = (InWorldDisplayFont.CharWidth + InWorldDisplayFont.Spacing) * scale;
            return text.Length * charStep - InWorldDisplayFont.Spacing * scale;
        }

        protected void DrawTextCentered(string text, int y, int scale, Color32 color)
        {
            int w = MeasureText(text, scale);
            DrawText(text, (textureWidth - w) / 2, y, scale, color);
        }

        protected void DrawTextRight(string text, int rightEdge, int y, int scale, Color32 color)
        {
            int w = MeasureText(text, scale);
            DrawText(text, rightEdge - w, y, scale, color);
        }

        protected void DrawProgressBar(float ratio, int x, int y, int w, int h,
            Color32 barColor, Color32 bgColor)
        {
            ratio = Mathf.Clamp01(ratio);
            DrawFilledRect(x, y, w, h, bgColor);
            int fillW = Mathf.RoundToInt(w * ratio);
            if (fillW > 0) DrawFilledRect(x, y, fillW, h, barColor);
        }

        protected void DrawGraph(IReadOnlyList<float> values, int x, int y, int w, int h,
            Color32 lineColor, Color32 fillColor, bool showZeroLine = true)
        {
            if (values == null || values.Count < 2) return;

            float minVal = float.MaxValue, maxVal = float.MinValue;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] < minVal) minVal = values[i];
                if (values[i] > maxVal) maxVal = values[i];
            }
            float range = maxVal - minVal;
            if (range < 0.001f) { minVal -= 0.5f; maxVal += 0.5f; range = 1f; }

            // zero line
            if (showZeroLine && minVal < 0f && maxVal > 0f)
            {
                int zeroY = y + h - Mathf.RoundToInt((0f - minVal) / range * h);
                DrawHLine(x, Mathf.Clamp(zeroY, y, y + h - 1), w, DimColor32);
            }

            // compute pixel positions
            int[] py = new int[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                float normalized = (values[i] - minVal) / range;
                py[i] = y + h - 1 - Mathf.RoundToInt(normalized * (h - 1));
            }

            // fill + line
            for (int i = 0; i < values.Count - 1; i++)
            {
                int px0 = x + Mathf.RoundToInt((float)i / (values.Count - 1) * (w - 1));
                int px1 = x + Mathf.RoundToInt((float)(i + 1) / (values.Count - 1) * (w - 1));

                // vertical fill columns
                for (int px = px0; px <= px1; px++)
                {
                    float t = (px1 == px0) ? 0f : (float)(px - px0) / (px1 - px0);
                    int interpY = Mathf.RoundToInt(Mathf.Lerp(py[i], py[i + 1], t));
                    for (int fy = interpY; fy < y + h; fy++)
                        SetPixel(px, fy, fillColor);
                }

                DrawLine(px0, py[i], px1, py[i + 1], lineColor, 2);
            }
        }

        #endregion

        #region Utility

        protected string FormatNumber(float value, int decimals = 0)
        {
            if (decimals <= 0) return Mathf.RoundToInt(value).ToString();
            return value.ToString($"F{decimals}");
        }

        protected string FormatLargeNumber(int value)
        {
            if (value >= 1000000) return (value / 1000000f).ToString("F1") + "M";
            if (value >= 10000) return (value / 1000f).ToString("F1") + "K";
            return value.ToString();
        }

        #endregion

        #region Internals

        private void InitializeTexture()
        {
            displayTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new Color32[textureWidth * textureHeight];
            Clear(BgColor32);
            displayTexture.SetPixels32(pixels);
            displayTexture.Apply(false);

            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();

            if (targetRenderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Unlit/Texture");
                runtimeMaterial = new Material(shader);
                runtimeMaterial.mainTexture = displayTexture;

                if (useEmission)
                {
                    runtimeMaterial.EnableKeyword("_EMISSION");
                    if (runtimeMaterial.HasProperty("_EmissionMap"))
                        runtimeMaterial.SetTexture("_EmissionMap", displayTexture);
                    if (runtimeMaterial.HasProperty("_EmissionColor"))
                        runtimeMaterial.SetColor("_EmissionColor", Color.white * emissionIntensity);
                }

                targetRenderer.material = runtimeMaterial;
            }
        }

        private void TryAutoResolveTargetAgent()
        {
            if (targetAgent != null) return;
            var spine = GetComponentInParent<ScenarioGoldenSpine>();
            if (spine != null)
                targetAgent = spine.ResolveAgentByPriority(targetAgentRole, targetAgentRoles, targetAgentTeam);
        }

        /// <summary>
        /// Call from subclass Awake to opt into telemetry events.
        /// Once the agent is resolved, OnAgentTelemetryEvent will be called.
        /// </summary>
        protected void SubscribeToAgentTelemetry()
        {
            _wantsTelemetry = true;
            TrySubscribeToResolvedAgent();
        }

        protected void UnsubscribeFromAgentTelemetry()
        {
            if (_subscribedTelemetryAgent != null)
            {
                _subscribedTelemetryAgent.TelemetryEventRecorded -= HandleTelemetryEvent;
                _subscribedTelemetryAgent = null;
            }
        }

        private void TrySubscribeToResolvedAgent()
        {
            if (!_wantsTelemetry || targetAgent == null) return;
            if (_subscribedTelemetryAgent == targetAgent) return;

            UnsubscribeFromAgentTelemetry();
            _subscribedTelemetryAgent = targetAgent;
            _subscribedTelemetryAgent.TelemetryEventRecorded += HandleTelemetryEvent;
        }

        private void HandleTelemetryEvent(AgentTelemetryEvent evt)
        {
            OnAgentTelemetryEvent(evt);
        }

        /// <summary>
        /// Override in subclasses that called SubscribeToAgentTelemetry().
        /// </summary>
        protected virtual void OnAgentTelemetryEvent(AgentTelemetryEvent evt) { }

        #endregion
    }
}