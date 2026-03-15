using System;
using System.IO;
using RLMovie.Environments.ReactorCoreDelivery;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RLMovie.Editor
{
    internal static class ReactorCoreDeliveryRenderProbe
    {
        private const string ScenePath = "Assets/_RLMovie/Environments/ReactorCoreDelivery/Scenes/ReactorCoreDelivery.unity";
        private const string OutputDirectory = @"C:\rl-movie\Temp\ReactorCoreDeliveryRenderProbe";

        private static readonly ProbeView[] Views =
        {
            new ProbeView(
                "Overview",
                new Vector3(-7.4f, 4.3f, -12.6f),
                Quaternion.Euler(15f, 25f, 0f),
                50f),
            new ProbeView(
                "StartBay",
                new Vector3(0f, 1.6f, -6.8f),
                Quaternion.Euler(6f, 180f, 0f),
                48f),
            new ProbeView(
                "AgentProfile",
                new Vector3(1.65f, 1.45f, -8.1f),
                Quaternion.Euler(8f, 225f, 0f),
                42f),
            new ProbeView(
                "MidCourse",
                new Vector3(0f, 2.0f, 5.4f),
                Quaternion.Euler(8f, 180f, 0f),
                55f),
            new ProbeView(
                "GoalLane",
                new Vector3(0f, 1.9f, 13.4f),
                Quaternion.Euler(8f, 0f, 0f),
                48f)
        };

        public static void RenderCurrentScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RenderLoadedScene();
        }

        public static void RenderAfterRebuild()
        {
            ReactorCoreDeliverySceneBuilder.CreateSceneSilently();
            RenderLoadedScene();
        }

        private static void RenderLoadedScene()
        {
            Directory.CreateDirectory(OutputDirectory);

            var tempCameraObject = new GameObject("ReactorCoreDeliveryRenderProbeCamera");
            try
            {
                PoseAgentForProbe();

                Camera renderCamera = tempCameraObject.AddComponent<Camera>();
                renderCamera.clearFlags = CameraClearFlags.SolidColor;
                renderCamera.backgroundColor = new Color(0.02f, 0.03f, 0.04f);
                renderCamera.nearClipPlane = 0.03f;
                renderCamera.farClipPlane = 200f;
                renderCamera.allowHDR = true;

                foreach (ProbeView view in Views)
                {
                    renderCamera.transform.SetPositionAndRotation(view.Position, view.Rotation);
                    renderCamera.fieldOfView = view.FieldOfView;
                    string path = Path.Combine(OutputDirectory, $"RCD_{view.Name}.png");
                    RenderToPng(renderCamera, path, 1600, 900);
                    Debug.Log($"[ReactorCoreDeliveryRenderProbe] Rendered {path}");
                }

                WriteSceneFacts(Path.Combine(OutputDirectory, "RCD_SceneFacts.txt"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempCameraObject);
            }

            AssetDatabase.Refresh();
        }

        private static void RenderToPng(Camera renderCamera, string outputPath, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture previousTarget = renderCamera.targetTexture;

            try
            {
                renderCamera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                renderCamera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                renderCamera.targetTexture = previousTarget;
                RenderTexture.active = previous;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void WriteSceneFacts(string outputPath)
        {
            ReactorCoreDeliveryAgent agent = UnityEngine.Object.FindFirstObjectByType<ReactorCoreDeliveryAgent>();
            GameObject shellRoot = GameObject.Find("ShellRoot");
            GameObject goal = GameObject.Find("Goal");
            Transform visualRoot = agent != null ? agent.transform.Find("VisualRoot") : null;
            Transform courierVisual = visualRoot != null ? visualRoot.Find("CourierVisual") : null;

            Bounds? agentBounds = GetCombinedBounds(agent != null ? agent.gameObject : null);
            Bounds? visualBounds = GetCombinedBounds(courierVisual != null ? courierVisual.gameObject : null);
            Bounds? shellBounds = GetCombinedBounds(shellRoot);

            string[] lines =
            {
                $"scene: {SceneManager.GetActiveScene().path}",
                $"agent_position: {FormatVector(agent != null ? agent.transform.position : Vector3.zero)}",
                $"visual_root_local_position: {FormatVector(visualRoot != null ? visualRoot.localPosition : Vector3.zero)}",
                $"courier_visual_local_position: {FormatVector(courierVisual != null ? courierVisual.localPosition : Vector3.zero)}",
                $"goal_position: {FormatVector(goal != null ? goal.transform.position : Vector3.zero)}",
                $"agent_bounds: {FormatBounds(agentBounds)}",
                $"visual_bounds: {FormatBounds(visualBounds)}",
                $"visual_floor_clearance: {FormatClearance(visualBounds)}",
                $"shell_bounds: {FormatBounds(shellBounds)}"
            };

            File.WriteAllLines(outputPath, lines);
        }

        private static void PoseAgentForProbe()
        {
            ReactorCoreDeliveryAgent agent = UnityEngine.Object.FindFirstObjectByType<ReactorCoreDeliveryAgent>();
            if (agent == null)
            {
                return;
            }

            agent.transform.position = new Vector3(0f, 0.5f, -10f);
            agent.transform.rotation = Quaternion.identity;

            Rigidbody agentBody = agent.GetComponent<Rigidbody>();
            if (agentBody != null)
            {
                agentBody.linearVelocity = Vector3.zero;
                agentBody.angularVelocity = Vector3.zero;
            }
        }

        private static Bounds? GetCombinedBounds(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return null;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:F3}, {value.y:F3}, {value.z:F3})";
        }

        private static string FormatBounds(Bounds? bounds)
        {
            if (!bounds.HasValue)
            {
                return "none";
            }

            Bounds value = bounds.Value;
            return $"center={FormatVector(value.center)} size={FormatVector(value.size)} min={FormatVector(value.min)} max={FormatVector(value.max)}";
        }

        private static string FormatClearance(Bounds? bounds)
        {
            if (!bounds.HasValue)
            {
                return "none";
            }

            float floorTopY = 0.06f;
            return (bounds.Value.min.y - floorTopY).ToString("F3");
        }

        private readonly struct ProbeView
        {
            public ProbeView(string name, Vector3 position, Quaternion rotation, float fieldOfView)
            {
                Name = name;
                Position = position;
                Rotation = rotation;
                FieldOfView = fieldOfView;
            }

            public string Name { get; }

            public Vector3 Position { get; }

            public Quaternion Rotation { get; }

            public float FieldOfView { get; }
        }
    }
}
