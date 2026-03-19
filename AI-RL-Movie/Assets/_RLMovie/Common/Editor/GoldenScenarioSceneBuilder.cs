using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RLMovie.Common;

namespace RLMovie.Editor
{
    /// <summary>
    /// Builds the shared scenario backbone used by all scenarios.
    /// Creates only infrastructure (environment root, spine, cameras, overlays, recording).
    /// Gameplay objects (agents, targets, hazards) are added by scenario-specific builders.
    /// </summary>
    public static class GoldenScenarioSceneBuilder
    {
        private const string CommonMaterialsRoot = "Assets/_RLMovie/Common/Materials";

        /// <summary>
        /// Builds the scenario backbone from manifest/blueprint at the given scene path.
        /// </summary>
        public static ScenarioBackboneContext BuildScenarioBackbone(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException("scenePath is required.", nameof(scenePath));
            }

            EnsureAssetFoldersForScene(scenePath);

            ScenarioConfigPaths paths = ScenarioConfigIO.ResolveScenarioPaths(scenePath);
            ScenarioManifestData manifest = ScenarioConfigIO.LoadManifest(paths.ManifestPath);
            ScenarioBlueprintData blueprint = ScenarioConfigIO.LoadBlueprint(paths.ConfigDirectory, manifest);

            return BuildScenarioBackbone(scenePath, manifest, blueprint);
        }

        /// <summary>
        /// Builds the scenario backbone with explicit manifest and blueprint.
        /// </summary>
        public static ScenarioBackboneContext BuildScenarioBackbone(
            string scenePath,
            ScenarioManifestData manifest,
            ScenarioBlueprintData blueprint)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException("scenePath is required.", nameof(scenePath));
            }

            V2ReadabilityKitBuilder.EnsureAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                throw new InvalidOperationException("Starter scene requires a Main Camera.");
            }

            ConfigureMainCamera(mainCamera);
            ConfigureDefaultDirectionalLight();

            string arenaRootName = FirstNonEmpty(blueprint.SceneRoles.ArenaRoot, "EnvironmentRoot");
            GameObject environmentRoot = new GameObject(arenaRootName);
            var environmentManager = environmentRoot.AddComponent<EnvironmentManager>();
            var goldenSpine = environmentRoot.AddComponent<ScenarioGoldenSpine>();

            // Camera rig
            GameObject cameraRig = new GameObject("CameraRig");
            cameraRig.transform.SetParent(environmentRoot.transform);
            Dictionary<string, Transform> cameraAnchors = CreateCameraAnchors(cameraRig.transform, blueprint);
            Transform defaultCameraView = ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.DefaultCamera, blueprint.CameraRoles.Explain);
            if (defaultCameraView != null)
            {
                mainCamera.transform.SetPositionAndRotation(defaultCameraView.position, defaultCameraView.rotation);
            }

            // Training visualizer
            GameObject visualizerObject = new GameObject("TrainingVisualizer");
            visualizerObject.transform.SetParent(environmentRoot.transform);
            var trainingVisualizer = visualizerObject.AddComponent<TrainingVisualizer>();

            // Overlay anchor
            Transform overlayAnchor = EnsureSceneRoleAnchor(
                environmentRoot.transform,
                FirstNonEmpty(blueprint.SceneRoles.OverlayAnchor, "OverlayAnchor"));

            // Recording helper
            GameObject recordingObject = new GameObject("RecordingHelper");
            recordingObject.transform.SetParent(environmentRoot.transform);
            var recordingHelper = recordingObject.AddComponent<RecordingHelper>();

            // Highlight tracker
            GameObject highlightObject = new GameObject("HighlightTracker");
            highlightObject.transform.SetParent(environmentRoot.transform);
            var highlightTracker = highlightObject.AddComponent<ScenarioHighlightTracker>();

            // Broadcast overlay
            GameObject overlayObject = new GameObject("BroadcastOverlay");
            overlayObject.transform.SetParent(overlayAnchor, false);
            var broadcastOverlay = overlayObject.AddComponent<ScenarioBroadcastOverlay>();

            ConfigureRecordingHelper(recordingHelper, goldenSpine, cameraAnchors, manifest, blueprint);
            ConfigureHighlightTracker(highlightTracker, manifest, blueprint);
            ConfigureOverlay(broadcastOverlay, manifest, blueprint);
            ConfigureTrainingVisualizerRoles(trainingVisualizer, manifest, blueprint);

            return new ScenarioBackboneContext(
                scene,
                scenePath,
                goldenSpine,
                environmentRoot,
                environmentManager,
                mainCamera,
                trainingVisualizer,
                recordingHelper,
                broadcastOverlay,
                highlightTracker,
                defaultCameraView,
                cameraAnchors,
                overlayAnchor,
                manifest,
                blueprint);
        }

        /// <summary>
        /// Configures ML-Agents BehaviorParameters and DecisionRequester on a GameObject.
        /// Call from scenario-specific builders after adding the Agent component.
        /// </summary>
        public static void ConfigureAgentBehavior(
            GameObject agentObject,
            string behaviorName,
            int vectorObservationSize,
            ActionSpec actionSpec,
            int decisionPeriod = 5)
        {
            var behaviorParameters = agentObject.GetComponent<BehaviorParameters>() ?? agentObject.AddComponent<BehaviorParameters>();
            behaviorParameters.BehaviorName = behaviorName;
            behaviorParameters.BehaviorType = BehaviorType.Default;
            behaviorParameters.BrainParameters.VectorObservationSize = vectorObservationSize;
            behaviorParameters.BrainParameters.ActionSpec = actionSpec;

            var decisionRequester = agentObject.GetComponent<DecisionRequester>() ?? agentObject.AddComponent<DecisionRequester>();
            decisionRequester.DecisionPeriod = decisionPeriod;
        }

        /// <summary>
        /// Applies a readability material to a GameObject, falling back to a solid color.
        /// Call from scenario-specific builders.
        /// </summary>
        public static void ApplyReadabilityMaterial(GameObject targetObject, string materialName, Color fallbackColor)
        {
            if (targetObject == null) return;

            Renderer renderer = targetObject.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            Material material = TryLoadMaterial(materialName);
            if (material != null)
            {
                renderer.sharedMaterial = material;
                return;
            }

            renderer.sharedMaterial.color = fallbackColor;
        }

        #region Private Helpers

        private static void ConfigureMainCamera(Camera mainCamera)
        {
            mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void ConfigureDefaultDirectionalLight()
        {
            Light mainLight = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(light => light != null && light.type == LightType.Directional);
            if (mainLight == null) return;

            mainLight.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            mainLight.intensity = 1.15f;
            mainLight.color = new Color(1f, 0.96f, 0.9f);
        }

        private static Dictionary<string, Transform> CreateCameraAnchors(Transform parent, ScenarioBlueprintData blueprint)
        {
            var anchors = new Dictionary<string, Transform>(StringComparer.Ordinal);
            CreateOrReuseCameraAnchor(anchors, parent, blueprint.CameraRoles.Explain, new Vector3(0f, 10.5f, -8.5f), Quaternion.Euler(44f, 0f, 0f));
            CreateOrReuseCameraAnchor(anchors, parent, blueprint.CameraRoles.DefaultCamera, new Vector3(0f, 10.5f, -8.5f), Quaternion.Euler(44f, 0f, 0f));
            CreateOrReuseCameraAnchor(anchors, parent, blueprint.CameraRoles.WideA, new Vector3(-8f, 6.6f, -7.5f), Quaternion.Euler(29f, 48f, 0f));
            CreateOrReuseCameraAnchor(anchors, parent, blueprint.CameraRoles.WideB, new Vector3(8f, 6.6f, -7.5f), Quaternion.Euler(29f, -48f, 0f));
            CreateOrReuseCameraAnchor(anchors, parent, blueprint.CameraRoles.FollowOptional, new Vector3(0f, 3.1f, -5.2f), Quaternion.Euler(17f, 0f, 0f));
            CreateOrReuseCameraAnchor(anchors, parent, blueprint.CameraRoles.ComparisonOptional, new Vector3(0f, 8.4f, -10.5f), Quaternion.Euler(34f, 0f, 0f));
            return anchors;
        }

        private static void CreateOrReuseCameraAnchor(
            IDictionary<string, Transform> anchors, Transform parent, string anchorName, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(anchorName) || anchors.ContainsKey(anchorName)) return;

            GameObject anchor = new GameObject(anchorName);
            anchor.transform.SetParent(parent);
            anchor.transform.position = position;
            anchor.transform.rotation = rotation;
            anchors.Add(anchorName, anchor.transform);
        }

        private static Transform ResolveCameraAnchor(IDictionary<string, Transform> anchors, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(names[i]) && anchors.TryGetValue(names[i], out Transform anchor))
                    return anchor;
            }
            return null;
        }

        private static Transform EnsureSceneRoleAnchor(Transform parent, string anchorName)
        {
            if (parent == null) return null;
            if (string.IsNullOrWhiteSpace(anchorName) || string.Equals(anchorName, parent.name, StringComparison.Ordinal))
                return parent;

            Transform existing = parent.Find(anchorName);
            if (existing != null) return existing;

            GameObject anchor = new GameObject(anchorName);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            return anchor.transform;
        }

        private static void ConfigureRecordingHelper(
            RecordingHelper helper,
            ScenarioGoldenSpine spine,
            IDictionary<string, Transform> cameraAnchors,
            ScenarioManifestData manifest,
            ScenarioBlueprintData blueprint)
        {
            AssignObjectReference(helper, "scenarioSpine", spine);
            AssignBool(helper, "hideUIWhenRecording", blueprint.RecordingDefaults.HideTrainingUi);
            AssignBool(helper, "enableCameraSwitching", blueprint.RecordingDefaults.EnableCameraSwitching);
            AssignFloat(helper, "cameraSwitchInterval", blueprint.RecordingDefaults.CameraSwitchInterval);
            AssignStringArray(helper, "cameraCycleRoles", blueprint.RecordingDefaults.CameraCycleRoles.Count > 0
                ? blueprint.RecordingDefaults.CameraCycleRoles
                : new List<string> { "explain", "wide_a", "wide_b", "follow_optional" });
            AssignString(helper, "defaultCameraRole", "default_camera");
            AssignString(helper, "followCameraRole", FirstNonEmpty(blueprint.RecordingDefaults.FollowCameraRole, "follow_optional"));
            AssignString(helper, "followTargetRole", FirstNonEmpty(blueprint.RecordingDefaults.FollowTargetRole, blueprint.ResolvePrimaryAgentRole(manifest), "hero"));
            AssignString(helper, "followTargetTeam", FirstNonEmpty(blueprint.RecordingDefaults.FollowTargetTeam, blueprint.ResolvePrimaryAgentTeam(manifest)));
            AssignStringArray(helper, "knownCameraRoleNames", new List<string>(cameraAnchors.Keys));
            AssignObjectReference(helper, "legacyCameraPositions", BuildLegacyCameraPositions(cameraAnchors, blueprint));
        }

        private static Transform[] BuildLegacyCameraPositions(IDictionary<string, Transform> cameraAnchors, ScenarioBlueprintData blueprint)
        {
            var ordered = new List<Transform>();
            IReadOnlyList<string> cycleRoles = blueprint.RecordingDefaults.CameraCycleRoles.Count > 0
                ? blueprint.RecordingDefaults.CameraCycleRoles
                : new List<string> { "explain", "wide_a", "wide_b", "follow_optional" };

            for (int i = 0; i < cycleRoles.Count; i++)
            {
                string cycleRole = cycleRoles[i];
                Transform anchor = cycleRole switch
                {
                    "explain" => ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.Explain),
                    "wide_a" => ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.WideA),
                    "wide_b" => ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.WideB),
                    "follow_optional" => ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.FollowOptional),
                    "comparison_optional" => ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.ComparisonOptional),
                    _ => ResolveCameraAnchor(cameraAnchors, cycleRole)
                };

                if (anchor != null) ordered.Add(anchor);
            }

            return ordered.ToArray();
        }

        private static void ConfigureHighlightTracker(
            ScenarioHighlightTracker tracker, ScenarioManifestData manifest, ScenarioBlueprintData blueprint)
        {
            AssignString(tracker, "trackedAgentRole", FirstNonEmpty(blueprint.HighlightBindings.TrackedAgentRole, blueprint.ResolvePrimaryAgentRole(manifest), "hero"));
            AssignStringArray(tracker, "trackedAgentRoles", ScenarioConfigIO.ResolveHighlightTargetRoles(blueprint, manifest));
            AssignString(tracker, "trackedAgentTeam", blueprint.HighlightBindings.TrackedAgentTeam);
            AssignString(tracker, "scenarioLabel", manifest.ScenarioName);
            AssignBool(tracker, "exportHighlightsToJsonl", blueprint.HighlightBindings.ExportHighlightsToJsonl);
            AssignBool(tracker, "exportSnapshotsToJsonl", blueprint.HighlightBindings.ExportSnapshotsToJsonl);
        }

        private static void ConfigureTrainingVisualizerRoles(
            TrainingVisualizer visualizer, ScenarioManifestData manifest, ScenarioBlueprintData blueprint)
        {
            string primaryRole = FirstNonEmpty(blueprint.ResolvePrimaryAgentRole(manifest), "hero");
            AssignString(visualizer, "targetAgentRole", primaryRole);
            AssignString(visualizer, "targetAgentTeam", blueprint.OverlayBindings.TargetAgentTeam);
        }

        private static void ConfigureOverlay(
            ScenarioBroadcastOverlay overlay, ScenarioManifestData manifest, ScenarioBlueprintData blueprint)
        {
            AssignString(overlay, "targetAgentRole", FirstNonEmpty(blueprint.OverlayBindings.TargetAgentRole, blueprint.ResolvePrimaryAgentRole(manifest), "hero"));
            AssignStringArray(overlay, "targetAgentRoles", ScenarioConfigIO.ResolveOverlayTargetRoles(blueprint, manifest));
            AssignString(overlay, "targetAgentTeam", blueprint.OverlayBindings.TargetAgentTeam);
            AssignString(overlay, "scenarioLabel", ScenarioConfigIO.ResolveOverlayLabel(manifest, blueprint));
            AssignString(overlay, "goalDescription", ScenarioConfigIO.ResolveOverlayObjective(manifest, blueprint));
        }

        private static Material TryLoadMaterial(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName)) return null;
            return AssetDatabase.LoadAssetAtPath<Material>($"{CommonMaterialsRoot}/{materialName}.mat");
        }

        internal static string FirstNonEmpty(params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i])) return values[i];
            }
            return string.Empty;
        }

        private static void EnsureAssetFoldersForScene(string scenePath)
        {
            string normalizedPath = scenePath.Replace('\\', '/');
            string folderPath = System.IO.Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(folderPath) || !folderPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Scene path must stay under Assets/: {scenePath}", nameof(scenePath));
            }

            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                currentPath = nextPath;
            }
        }

        #endregion

        #region Serialized Property Helpers

        internal static void AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void AssignBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void AssignFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void AssignString(UnityEngine.Object target, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void AssignStringArray(UnityEngine.Object target, string propertyName, IReadOnlyList<string> values)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }

    /// <summary>
    /// Context returned by BuildScenarioBackbone.
    /// Scenario-specific builders use this to add gameplay objects, then call FinalizeSpine and Save.
    /// </summary>
    public sealed class ScenarioBackboneContext
    {
        private readonly string _scenePath;
        private readonly Dictionary<string, Transform> _cameraAnchors;
        private readonly Transform _overlayAnchor;

        public ScenarioBackboneContext(
            Scene scene,
            string scenePath,
            ScenarioGoldenSpine spine,
            GameObject environmentRoot,
            EnvironmentManager environmentManager,
            Camera mainCamera,
            TrainingVisualizer trainingVisualizer,
            RecordingHelper recordingHelper,
            ScenarioBroadcastOverlay broadcastOverlay,
            ScenarioHighlightTracker highlightTracker,
            Transform defaultCameraView,
            Dictionary<string, Transform> cameraAnchors,
            Transform overlayAnchor,
            ScenarioManifestData manifest,
            ScenarioBlueprintData blueprint)
        {
            Scene = scene;
            _scenePath = scenePath;
            Spine = spine;
            EnvironmentRoot = environmentRoot;
            EnvironmentManager = environmentManager;
            MainCamera = mainCamera;
            TrainingVisualizer = trainingVisualizer;
            RecordingHelper = recordingHelper;
            BroadcastOverlay = broadcastOverlay;
            HighlightTracker = highlightTracker;
            DefaultCameraView = defaultCameraView;
            _cameraAnchors = cameraAnchors;
            _overlayAnchor = overlayAnchor;
            Manifest = manifest;
            Blueprint = blueprint;
        }

        public Scene Scene { get; }
        public ScenarioGoldenSpine Spine { get; }
        public GameObject EnvironmentRoot { get; }
        public EnvironmentManager EnvironmentManager { get; }
        public Camera MainCamera { get; }
        public TrainingVisualizer TrainingVisualizer { get; }
        public RecordingHelper RecordingHelper { get; }
        public ScenarioBroadcastOverlay BroadcastOverlay { get; }
        public ScenarioHighlightTracker HighlightTracker { get; }
        public Transform DefaultCameraView { get; }
        public ScenarioManifestData Manifest { get; }
        public ScenarioBlueprintData Blueprint { get; }

        /// <summary>
        /// Wires the spine with scene/agent/team/camera role bindings.
        /// Call after all gameplay objects have been added to the scene.
        /// </summary>
        public void FinalizeSpine(
            ScenarioGoldenSpine.SceneRoleBinding[] sceneRoles,
            ScenarioGoldenSpine.AgentRoleBinding[] agentRoles,
            ScenarioGoldenSpine.TeamRoleBinding[] teamRoles)
        {
            // Merge backbone scene roles with scenario-provided ones
            var allSceneRoles = new List<ScenarioGoldenSpine.SceneRoleBinding>
            {
                new ScenarioGoldenSpine.SceneRoleBinding { role = "arena_root", target = EnvironmentRoot.transform },
                new ScenarioGoldenSpine.SceneRoleBinding { role = "overlay_anchor", target = _overlayAnchor }
            };
            if (sceneRoles != null)
            {
                allSceneRoles.AddRange(sceneRoles);
            }

            // Build camera role bindings from backbone anchors
            var cameraRoles = new List<ScenarioGoldenSpine.CameraRoleBinding>();
            foreach (var kvp in _cameraAnchors)
            {
                if (kvp.Value == null) continue;

                string cameraRole = kvp.Key switch
                {
                    var k when k == Blueprint.CameraRoles.DefaultCamera => "default_camera",
                    var k when k == Blueprint.CameraRoles.Explain => "explain",
                    var k when k == Blueprint.CameraRoles.WideA => "wide_a",
                    var k when k == Blueprint.CameraRoles.WideB => "wide_b",
                    var k when k == Blueprint.CameraRoles.FollowOptional => "follow_optional",
                    var k when k == Blueprint.CameraRoles.ComparisonOptional => "comparison_optional",
                    _ => kvp.Key
                };

                cameraRoles.Add(new ScenarioGoldenSpine.CameraRoleBinding { role = cameraRole, anchor = kvp.Value });
            }

            // Ensure default_camera is present
            if (DefaultCameraView != null && !cameraRoles.Exists(c => c.role == "default_camera"))
            {
                cameraRoles.Insert(0, new ScenarioGoldenSpine.CameraRoleBinding { role = "default_camera", anchor = DefaultCameraView });
            }

            Spine.Configure(
                EnvironmentRoot.transform,
                EnvironmentManager,
                TrainingVisualizer,
                RecordingHelper,
                BroadcastOverlay,
                HighlightTracker,
                allSceneRoles.ToArray(),
                agentRoles ?? Array.Empty<ScenarioGoldenSpine.AgentRoleBinding>(),
                teamRoles ?? Array.Empty<ScenarioGoldenSpine.TeamRoleBinding>(),
                cameraRoles.ToArray());
        }

        /// <summary>
        /// Sets the target agent for the TrainingVisualizer.
        /// Call after adding the agent to the scene.
        /// </summary>
        public void ConfigureTrainingVisualizer(BaseRLAgent agent)
        {
            GoldenScenarioSceneBuilder.AssignObjectReference(TrainingVisualizer, "targetAgent", agent);
        }

        /// <summary>
        /// Saves the scene and refreshes the asset database.
        /// </summary>
        public void Save()
        {
            EditorSceneManager.SaveScene(Scene, _scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
