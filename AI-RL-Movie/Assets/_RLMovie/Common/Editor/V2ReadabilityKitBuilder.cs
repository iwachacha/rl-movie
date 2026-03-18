using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace RLMovie.Editor
{
    /// <summary>
    /// Creates the minimum shared readability assets for the V2 common starter.
    /// </summary>
    public static class V2ReadabilityKitBuilder
    {
        private const string CommonRoot = "Assets/_RLMovie/Common";
        private const string MaterialsRoot = CommonRoot + "/Materials";
        private const string PrefabsRoot = CommonRoot + "/Prefabs";
        private const string RecorderSettingsRoot = "Assets/_RLMovie/Recording/RecorderSettings";

        [MenuItem("RLMovie/Create or Refresh V2 Readability Kit")]
        public static void CreateOrRefreshMenu()
        {
            EnsureAssets();
            EditorUtility.DisplayDialog(
                "V2 Readability Kit",
                "Created or refreshed the shared V2 readability materials and prefabs.",
                "OK");
        }

        [MenuItem("RLMovie/Internal/Refresh V2 Readability Kit")]
        public static void CreateOrRefreshSilently()
        {
            EnsureAssets();
        }

        [InitializeOnLoadMethod]
        private static void EnsureOnEditorLoad()
        {
            EditorApplication.delayCall += EnsureAssetsIfMissing;
        }

        internal static void EnsureAssets()
        {
            EnsureFolder(MaterialsRoot);
            EnsureFolder(PrefabsRoot);
            EnsureFolder(RecorderSettingsRoot);

            CreateOrUpdateMaterial("V2HeroReadable", new Color(0.94f, 0.80f, 0.22f), new Color(0.24f, 0.18f, 0.03f));
            CreateOrUpdateMaterial("V2TargetReadable", new Color(0.20f, 0.88f, 0.48f), new Color(0.03f, 0.22f, 0.10f));
            CreateOrUpdateMaterial("V2HazardReadable", new Color(0.93f, 0.32f, 0.22f), new Color(0.26f, 0.06f, 0.03f));
            CreateOrUpdateMaterial("V2FloorReadable", new Color(0.18f, 0.22f, 0.28f), Color.black);

            CreateOrUpdateLightRigPrefab();
            CreateOrUpdateCameraAnchorMarkerPrefab();
            CreateOrUpdateRecorderPreset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureAssetsIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (File.Exists($"{RecorderSettingsRoot}/V2GameplayRecorder.preset") &&
                File.Exists($"{MaterialsRoot}/V2HeroReadable.mat") &&
                File.Exists($"{PrefabsRoot}/V2BasicLightRig.prefab"))
            {
                return;
            }

            EnsureAssets();
        }

        private static void CreateOrUpdateMaterial(string name, Color baseColor, Color emissionColor)
        {
            string assetPath = $"{MaterialsRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }

            EditorUtility.SetDirty(material);
        }

        private static void CreateOrUpdateLightRigPrefab()
        {
            string prefabPath = $"{PrefabsRoot}/V2BasicLightRig.prefab";
            GameObject root = new GameObject("V2BasicLightRig");

            GameObject keyLightObject = new GameObject("KeyLight");
            keyLightObject.transform.SetParent(root.transform);
            keyLightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            Light keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.1f;
            keyLight.color = new Color(1f, 0.97f, 0.92f);

            GameObject fillLightObject = new GameObject("FillLight");
            fillLightObject.transform.SetParent(root.transform);
            fillLightObject.transform.position = new Vector3(0f, 4.5f, 0f);
            Light fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 18f;
            fillLight.intensity = 0.35f;
            fillLight.color = new Color(0.62f, 0.72f, 0.92f);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateOrUpdateCameraAnchorMarkerPrefab()
        {
            string prefabPath = $"{PrefabsRoot}/V2CameraAnchorMarker.prefab";
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "V2CameraAnchorMarker";
            marker.transform.localScale = Vector3.one * 0.2f;

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Material markerMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsRoot}/V2TargetReadable.mat");
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null && markerMaterial != null)
            {
                renderer.sharedMaterial = markerMaterial;
            }

            PrefabUtility.SaveAsPrefabAsset(marker, prefabPath);
            Object.DestroyImmediate(marker);
        }

        private static void CreateOrUpdateRecorderPreset()
        {
            string presetPath = $"{RecorderSettingsRoot}/V2GameplayRecorder.preset";

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieSettings.name = "V2GameplayRecorder";
            movieSettings.Enabled = true;
            movieSettings.EncoderSettings = new CoreEncoderSettings
            {
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
                Codec = CoreEncoderSettings.OutputCodec.MP4
            };
            movieSettings.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080
            };
            movieSettings.OutputFile = "Recordings/V2/v2_gameplay_capture";

            controllerSettings.AddRecorderSettings(movieSettings);
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 60f;

            RecorderControllerSettingsPreset.SaveAtPath(controllerSettings, presetPath);

            Object.DestroyImmediate(movieSettings);
            Object.DestroyImmediate(controllerSettings);
        }

        private static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
