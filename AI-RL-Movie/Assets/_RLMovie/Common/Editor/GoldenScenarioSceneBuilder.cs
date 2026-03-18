using System;
using System.Collections.Generic;
using System.IO;
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
    /// Creates the V2 shared scene backbone used by new scenarios.
    /// Theme-specific builders should only layer specialized gameplay on top.
    /// </summary>
    public static class GoldenScenarioSceneBuilder
    {
        private const string CommonMaterialsRoot = "Assets/_RLMovie/Common/Materials";

        public static GoldenScenarioSceneContext CreateStarterScene<TAgent>(
            string scenePath,
            string behaviorName,
            int vectorObservationSize,
            int continuousActionSize,
            Action<GoldenScenarioSceneContext, TAgent> configureScenario = null)
            where TAgent : BaseRLAgent
        {
            return CreateStarterScene(
                scenePath,
                behaviorName,
                vectorObservationSize,
                ActionSpec.MakeContinuous(Mathf.Max(1, continuousActionSize)),
                configureScenario);
        }

        public static GoldenScenarioSceneContext CreateStarterScene<TAgent>(
            string scenePath,
            string behaviorName,
            int vectorObservationSize,
            ActionSpec actionSpec,
            Action<GoldenScenarioSceneContext, TAgent> configureScenario = null)
            where TAgent : BaseRLAgent
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException("scenePath is required.", nameof(scenePath));
            }

            EnsureAssetFoldersForScene(scenePath);

            ScenarioConfigPaths paths = ScenarioConfigIO.ResolveScenarioPaths(scenePath);
            ScenarioManifestData manifest = ScenarioConfigIO.LoadManifest(paths.ManifestPath);
            ScenarioBlueprintData blueprint = ScenarioConfigIO.LoadBlueprint(paths.ConfigDirectory, manifest);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GoldenScenarioSceneContext context = BuildSharedBackbone<TAgent>(
                scene,
                scenePath,
                behaviorName,
                vectorObservationSize,
                actionSpec,
                manifest,
                blueprint);

            configureScenario?.Invoke(context, (TAgent)context.Agent);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return context;
        }

        private static GoldenScenarioSceneContext BuildSharedBackbone<TAgent>(
            Scene scene,
            string scenePath,
            string behaviorName,
            int vectorObservationSize,
            ActionSpec actionSpec,
            ScenarioManifestData manifest,
            ScenarioBlueprintData blueprint)
            where TAgent : BaseRLAgent
        {
            V2ReadabilityKitBuilder.EnsureAssets();

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                throw new InvalidOperationException("Starter scene requires a Main Camera.");
            }

            ConfigureMainCamera(mainCamera);
            ConfigureDefaultDirectionalLight();

            ScenarioAgentBlueprintData primaryAgentBlueprint = blueprint.ResolvePrimaryAgent(manifest)
                ?? throw new InvalidOperationException("Starter scene requires a primary agent definition.");

            string arenaRootName = FirstNonEmpty(blueprint.SceneRoles.ArenaRoot, "EnvironmentRoot");
            GameObject environmentRoot = new GameObject(arenaRootName);
            var environmentManager = environmentRoot.AddComponent<EnvironmentManager>();
            var goldenSpine = environmentRoot.AddComponent<ScenarioGoldenSpine>();

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(environmentRoot.transform);
            floor.transform.localPosition = Vector3.zero;

            string heroName = ToDisplayName(primaryAgentBlueprint.Role, "Hero");
            GameObject heroObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            heroObject.name = heroName;
            heroObject.transform.SetParent(environmentRoot.transform);
            heroObject.transform.localPosition = new Vector3(0f, 1f, 0f);

            string primaryTargetName = FirstNonEmpty(blueprint.SceneRoles.PrimaryTarget, "PrimaryTarget");
            GameObject primaryTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primaryTarget.name = primaryTargetName;
            primaryTarget.transform.SetParent(environmentRoot.transform);
            primaryTarget.transform.localPosition = new Vector3(3f, 0.6f, 3f);
            primaryTarget.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

            GameObject primaryHazard = null;
            if (!string.IsNullOrWhiteSpace(blueprint.SceneRoles.PrimaryHazard))
            {
                primaryHazard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                primaryHazard.name = blueprint.SceneRoles.PrimaryHazard;
                primaryHazard.transform.SetParent(environmentRoot.transform);
                primaryHazard.transform.localPosition = new Vector3(-3.5f, 0.75f, 2.5f);
                primaryHazard.transform.localScale = new Vector3(0.65f, 0.75f, 0.65f);
            }

            var heroRigidbody = heroObject.AddComponent<Rigidbody>();
            heroRigidbody.mass = 1f;
            heroRigidbody.angularDamping = 0.5f;

            var agent = heroObject.AddComponent<TAgent>();
            var decisionRequester = heroObject.GetComponent<DecisionRequester>() ?? heroObject.AddComponent<DecisionRequester>();
            decisionRequester.DecisionPeriod = 5;

            var behaviorParameters = heroObject.GetComponent<BehaviorParameters>() ?? heroObject.AddComponent<BehaviorParameters>();
            behaviorParameters.BehaviorName = behaviorName;
            behaviorParameters.BehaviorType = BehaviorType.Default;
            behaviorParameters.BrainParameters.VectorObservationSize = vectorObservationSize;
            behaviorParameters.BrainParameters.ActionSpec = actionSpec;

            GameObject cameraRig = new GameObject("CameraRig");
            cameraRig.transform.SetParent(environmentRoot.transform);

            Dictionary<string, Transform> cameraAnchors = CreateCameraAnchors(cameraRig.transform, blueprint);
            Transform defaultCameraView = ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.DefaultCamera, blueprint.CameraRoles.Explain);
            if (defaultCameraView != null)
            {
                mainCamera.transform.SetPositionAndRotation(defaultCameraView.position, defaultCameraView.rotation);
            }

            GameObject visualizerObject = new GameObject("TrainingVisualizer");
            visualizerObject.transform.SetParent(environmentRoot.transform);
            var trainingVisualizer = visualizerObject.AddComponent<TrainingVisualizer>();

            Transform overlayAnchor = EnsureSceneRoleAnchor(
                environmentRoot.transform,
                FirstNonEmpty(blueprint.SceneRoles.OverlayAnchor, "OverlayAnchor"));

            GameObject recordingObject = new GameObject("RecordingHelper");
            recordingObject.transform.SetParent(environmentRoot.transform);
            var recordingHelper = recordingObject.AddComponent<RecordingHelper>();

            GameObject highlightObject = new GameObject("HighlightTracker");
            highlightObject.transform.SetParent(environmentRoot.transform);
            var highlightTracker = highlightObject.AddComponent<ScenarioHighlightTracker>();

            GameObject overlayObject = new GameObject("BroadcastOverlay");
            overlayObject.transform.SetParent(overlayAnchor, false);
            var broadcastOverlay = overlayObject.AddComponent<ScenarioBroadcastOverlay>();

            ConfigureRecordingHelper(recordingHelper, goldenSpine, cameraAnchors, manifest, blueprint, agent);
            ConfigureHighlightTracker(highlightTracker, manifest, blueprint);
            ConfigureOverlay(broadcastOverlay, manifest, blueprint);
            ConfigureTrainingVisualizer(trainingVisualizer, agent);
            ApplyReadabilityKit(floor, heroObject, primaryTarget, primaryHazard, blueprint.VisualDefaults);

            ScenarioGoldenSpine.SceneRoleBinding[] sceneRoles =
            {
                new ScenarioGoldenSpine.SceneRoleBinding { role = "arena_root", target = environmentRoot.transform },
                new ScenarioGoldenSpine.SceneRoleBinding { role = "primary_target", target = primaryTarget.transform },
                new ScenarioGoldenSpine.SceneRoleBinding { role = "overlay_anchor", target = overlayAnchor },
                new ScenarioGoldenSpine.SceneRoleBinding { role = "primary_hazard", target = primaryHazard != null ? primaryHazard.transform : null }
            };

            ScenarioGoldenSpine.AgentRoleBinding[] agentRoles =
            {
                new ScenarioGoldenSpine.AgentRoleBinding
                {
                    role = primaryAgentBlueprint.Role,
                    team = FirstNonEmpty(primaryAgentBlueprint.Team, "solo"),
                    primary = primaryAgentBlueprint.Primary,
                    agent = agent
                }
            };

            ScenarioGoldenSpine.TeamRoleBinding[] teamRoles =
            {
                new ScenarioGoldenSpine.TeamRoleBinding
                {
                    team = FirstNonEmpty(primaryAgentBlueprint.Team, "solo"),
                    primaryAgent = agent,
                    agents = new[] { agent }
                }
            };

            List<ScenarioGoldenSpine.CameraRoleBinding> cameraRoles = new List<ScenarioGoldenSpine.CameraRoleBinding>();
            AddCameraRoleBinding(cameraRoles, "default_camera", defaultCameraView);
            AddCameraRoleBinding(cameraRoles, "explain", ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.Explain));
            AddCameraRoleBinding(cameraRoles, "wide_a", ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.WideA));
            AddCameraRoleBinding(cameraRoles, "wide_b", ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.WideB));
            AddCameraRoleBinding(cameraRoles, "follow_optional", ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.FollowOptional));
            AddCameraRoleBinding(cameraRoles, "comparison_optional", ResolveCameraAnchor(cameraAnchors, blueprint.CameraRoles.ComparisonOptional));

            goldenSpine.Configure(
                environmentRoot.transform,
                environmentManager,
                trainingVisualizer,
                recordingHelper,
                broadcastOverlay,
                highlightTracker,
                sceneRoles,
                agentRoles,
                teamRoles,
                cameraRoles.ToArray());

            return new GoldenScenarioSceneContext(
                scene,
                goldenSpine,
                environmentRoot,
                environmentManager,
                agent,
                primaryTarget.transform,
                primaryHazard != null ? primaryHazard.transform : null,
                mainCamera,
                trainingVisualizer,
                recordingHelper,
                broadcastOverlay,
                highlightTracker,
                defaultCameraView,
                goldenSpine.RecordingCameraViews,
                manifest,
                blueprint);
        }

        private static void ConfigureMainCamera(Camera mainCamera)
        {
            mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void ConfigureDefaultDirectionalLight()
        {
            Light mainLight = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(light => light != null && light.type == LightType.Directional);
            if (mainLight == null)
            {
                return;
            }

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
            IDictionary<string, Transform> anchors,
            Transform parent,
            string anchorName,
            Vector3 position,
            Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(anchorName) || anchors.ContainsKey(anchorName))
            {
                return;
            }

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
                string name = names[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (anchors.TryGetValue(name, out Transform anchor))
                {
                    return anchor;
                }
            }

            return null;
        }

        private static Transform EnsureSceneRoleAnchor(Transform parent, string anchorName)
        {
            if (parent == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(anchorName) || string.Equals(anchorName, parent.name, StringComparison.Ordinal))
            {
                return parent;
            }

            Transform existing = parent.Find(anchorName);
            if (existing != null)
            {
                return existing;
            }

            GameObject anchor = new GameObject(anchorName);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            return anchor.transform;
        }

        private static void ConfigureTrainingVisualizer(TrainingVisualizer visualizer, BaseRLAgent agent)
        {
            AssignObjectReference(visualizer, "targetAgent", agent);
        }

        private static void ConfigureRecordingHelper(
            RecordingHelper helper,
            ScenarioGoldenSpine spine,
            IDictionary<string, Transform> cameraAnchors,
            ScenarioManifestData manifest,
            ScenarioBlueprintData blueprint,
            BaseRLAgent heroAgent)
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
            AssignObjectReference(helper, "followTarget", heroAgent.transform);
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

                if (anchor != null)
                {
                    ordered.Add(anchor);
                }
            }

            return ordered.ToArray();
        }

        private static void ConfigureHighlightTracker(
            ScenarioHighlightTracker tracker,
            ScenarioManifestData manifest,
            ScenarioBlueprintData blueprint)
        {
            AssignString(tracker, "trackedAgentRole", FirstNonEmpty(blueprint.HighlightBindings.TrackedAgentRole, blueprint.ResolvePrimaryAgentRole(manifest), "hero"));
            AssignStringArray(tracker, "trackedAgentRoles", ScenarioConfigIO.ResolveHighlightTargetRoles(blueprint, manifest));
            AssignString(tracker, "trackedAgentTeam", blueprint.HighlightBindings.TrackedAgentTeam);
            AssignString(tracker, "scenarioLabel", manifest.ScenarioName);
            AssignBool(tracker, "exportHighlightsToJsonl", blueprint.HighlightBindings.ExportHighlightsToJsonl);
            AssignBool(tracker, "exportSnapshotsToJsonl", blueprint.HighlightBindings.ExportSnapshotsToJsonl);
        }

        private static void ConfigureOverlay(
            ScenarioBroadcastOverlay overlay,
            ScenarioManifestData manifest,
            ScenarioBlueprintData blueprint)
        {
            AssignString(overlay, "targetAgentRole", FirstNonEmpty(blueprint.OverlayBindings.TargetAgentRole, blueprint.ResolvePrimaryAgentRole(manifest), "hero"));
            AssignStringArray(overlay, "targetAgentRoles", ScenarioConfigIO.ResolveOverlayTargetRoles(blueprint, manifest));
            AssignString(overlay, "targetAgentTeam", blueprint.OverlayBindings.TargetAgentTeam);
            AssignString(overlay, "scenarioLabel", ScenarioConfigIO.ResolveOverlayLabel(manifest, blueprint));
            AssignString(overlay, "goalDescription", ScenarioConfigIO.ResolveOverlayObjective(manifest, blueprint));
        }

        private static void ApplyReadabilityKit(
            GameObject floor,
            GameObject heroObject,
            GameObject primaryTarget,
            GameObject primaryHazard,
            VisualDefaultsData visualDefaults)
        {
            if (visualDefaults == null || !visualDefaults.ApplyReadabilityKit)
            {
                return;
            }

            ApplyMaterialOrFallback(floor, visualDefaults.FloorMaterial, new Color(0.17f, 0.21f, 0.26f));
            ApplyMaterialOrFallback(heroObject, visualDefaults.HeroMaterial, new Color(0.92f, 0.83f, 0.28f));
            ApplyMaterialOrFallback(primaryTarget, visualDefaults.TargetMaterial, new Color(0.22f, 0.92f, 0.56f));
            if (primaryHazard != null)
            {
                ApplyMaterialOrFallback(primaryHazard, visualDefaults.HazardMaterial, new Color(0.93f, 0.34f, 0.22f));
            }
        }

        private static void ApplyMaterialOrFallback(GameObject targetObject, string materialName, Color fallbackColor)
        {
            if (targetObject == null)
            {
                return;
            }

            Renderer renderer = targetObject.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material material = TryLoadMaterial(materialName);
            if (material != null)
            {
                renderer.sharedMaterial = material;
                return;
            }

            renderer.sharedMaterial.color = fallbackColor;
        }

        private static Material TryLoadMaterial(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return null;
            }

            string materialPath = $"{CommonMaterialsRoot}/{materialName}.mat";
            return AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }

        private static void AddCameraRoleBinding(ICollection<ScenarioGoldenSpine.CameraRoleBinding> bindings, string role, Transform anchor)
        {
            if (string.IsNullOrWhiteSpace(role) || anchor == null)
            {
                return;
            }

            bindings.Add(new ScenarioGoldenSpine.CameraRoleBinding
            {
                role = role,
                anchor = anchor
            });
        }

        private static string FirstNonEmpty(params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    return values[i];
                }
            }

            return string.Empty;
        }

        private static string ToDisplayName(string role, string fallback)
        {
            string source = FirstNonEmpty(role, fallback);
            if (string.IsNullOrWhiteSpace(source))
            {
                return fallback;
            }

            string[] parts = source.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return fallback;
            }

            return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
        }

        private static void EnsureAssetFoldersForScene(string scenePath)
        {
            string normalizedPath = scenePath.Replace('\\', '/');
            string folderPath = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/');
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
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }

        private static void AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignString(UnityEngine.Object target, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignStringArray(UnityEngine.Object target, string propertyName, IReadOnlyList<string> values)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public sealed class GoldenScenarioSceneContext
    {
        public GoldenScenarioSceneContext(
            Scene scene,
            ScenarioGoldenSpine goldenSpine,
            GameObject environmentRoot,
            EnvironmentManager environmentManager,
            BaseRLAgent agent,
            Transform primaryTarget,
            Transform primaryHazard,
            Camera mainCamera,
            TrainingVisualizer trainingVisualizer,
            RecordingHelper recordingHelper,
            ScenarioBroadcastOverlay broadcastOverlay,
            ScenarioHighlightTracker highlightTracker,
            Transform defaultCameraView,
            Transform[] recordingCameraViews,
            ScenarioManifestData manifest,
            ScenarioBlueprintData blueprint)
        {
            Scene = scene;
            GoldenSpine = goldenSpine;
            EnvironmentRoot = environmentRoot;
            EnvironmentManager = environmentManager;
            Agent = agent;
            PrimaryTarget = primaryTarget;
            PrimaryHazard = primaryHazard;
            MainCamera = mainCamera;
            TrainingVisualizer = trainingVisualizer;
            RecordingHelper = recordingHelper;
            BroadcastOverlay = broadcastOverlay;
            HighlightTracker = highlightTracker;
            DefaultCameraView = defaultCameraView;
            RecordingCameraViews = recordingCameraViews;
            Manifest = manifest;
            Blueprint = blueprint;
        }

        public Scene Scene { get; }

        public ScenarioGoldenSpine GoldenSpine { get; }

        public GameObject EnvironmentRoot { get; }

        public EnvironmentManager EnvironmentManager { get; }

        public BaseRLAgent Agent { get; }

        public Transform PrimaryTarget { get; }

        public Transform PrimaryHazard { get; }

        public Camera MainCamera { get; }

        public TrainingVisualizer TrainingVisualizer { get; }

        public RecordingHelper RecordingHelper { get; }

        public ScenarioBroadcastOverlay BroadcastOverlay { get; }

        public ScenarioHighlightTracker HighlightTracker { get; }

        public Transform DefaultCameraView { get; }

        public Transform[] RecordingCameraViews { get; }

        public ScenarioManifestData Manifest { get; }

        public ScenarioBlueprintData Blueprint { get; }
    }
}
