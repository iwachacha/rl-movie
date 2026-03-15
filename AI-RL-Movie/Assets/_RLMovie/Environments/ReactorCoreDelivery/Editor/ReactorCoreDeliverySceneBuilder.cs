using System;
using System.Collections.Generic;
using System.IO;
using RLMovie.Common;
using RLMovie.Environments.ReactorCoreDelivery;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RLMovie.Editor
{
    /// <summary>
    /// Creates the full Reactor Core Delivery V1 scene, scenario-side prefab variants, materials, and animator assets.
    /// </summary>
    public static class ReactorCoreDeliverySceneBuilder
    {
        private const string ScenarioRoot = "Assets/_RLMovie/Environments/ReactorCoreDelivery";
        private const string ScenePath = ScenarioRoot + "/Scenes/ReactorCoreDelivery.unity";
        private const string VariantRoot = ScenarioRoot + "/Prefabs/Variants";
        private const string MaterialsRoot = ScenarioRoot + "/Materials";
        private const string AnimationRoot = ScenarioRoot + "/Animations";

        private const string AgentVisualSource = "Assets/ThirdParty/ithappy/Creative_Characters_FREE/Prefabs/Base_Mesh.prefab";
        private const string AgentFaceSource = "Assets/ThirdParty/ithappy/Creative_Characters_FREE/Prefabs/Faces/Male_emotion_usual_001.prefab";
        private const string AgentGlassesSource = "Assets/ThirdParty/ithappy/Creative_Characters_FREE/Prefabs/Glasses/Glasses_004.prefab";
        private const string AgentMustacheSource = "Assets/ThirdParty/ithappy/Creative_Characters_FREE/Prefabs/Face Accessories/Mustache_011.prefab";
        private const string AgentHelmetSource = "Assets/ThirdParty/ithappy/Creative_Characters_FREE/Prefabs/Hat Single/Hat_Single_008.prefab";
        private const string AgentPantsSource = "Assets/ThirdParty/ithappy/Creative_Characters_FREE/Prefabs/Pants/Pants_010.prefab";
        private const string AgentShoesSource = "Assets/ThirdParty/ithappy/Creative_Characters_FREE/Prefabs/Shoes/Shoe_Sneakers_009.prefab";
        private const string CosmicLockerSource = "Assets/ThirdParty/Cosmic_Retro_Station_Props_FREE/Prefabs/CR_Locker_Emergency_Red.prefab";
        private const string CosmicMonitorSource = "Assets/ThirdParty/Cosmic_Retro_Station_Props_FREE/Prefabs/CR_Monitor_Small_1.prefab";
        private const string CosmicCrateSource = "Assets/ThirdParty/Cosmic_Retro_Station_Props_FREE/Prefabs/CR_StorageCrate_Large_1.prefab";
        private const string CosmicPanelSource = "Assets/ThirdParty/Cosmic_Retro_Station_Props_FREE/Prefabs/CR_Computer_PanelOnly.prefab";
        private const string SteamFxSource = "Assets/ThirdParty/SimpleFX/Prefabs/FX_Steam.prefab";
        private const string GoalFxSource = "Assets/ThirdParty/SimpleFX/Prefabs/FX_Lightray.prefab";
        private const string ExplosionFxSource = "Assets/ThirdParty/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab";
        private const string FireExplosionFxSource = "Assets/ThirdParty/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR3 Fire Explosion B.prefab";

        private const string IdleClipSource = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx";
        private const string WalkClipSource = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx";
        private const string RunClipSource = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx";
        private const string ShockClipSource = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Female/Movement/Jump/HumanF@Fall01.fbx";

        [MenuItem("RLMovie/Create ReactorCoreDelivery Scene")]
        public static void CreateSceneMenu()
        {
            CreateSceneInternal(validateAfterBuild: true);
        }

        public static void CreateSceneAndValidate()
        {
            CreateSceneInternal(validateAfterBuild: true);
        }

        public static string CreateSceneSilently()
        {
            return CreateSceneInternal(validateAfterBuild: false);
        }

        private static string CreateSceneInternal(bool validateAfterBuild)
        {
            EnsureScenarioFolders();
            GeneratedAssets assets = EnsureGeneratedAssets();

            GoldenScenarioSceneBuilder.CreateStarterScene<ReactorCoreDeliveryAgent>(
                ScenePath,
                "ReactorCoreDeliveryAgent",
                ReactorCoreDeliveryCourse.ObservationSize,
                2,
                (context, agent) => ConfigureScenario(context, agent, assets));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (validateAfterBuild)
            {
                ScenarioValidationReport report = ScenarioValidator.ValidateCurrentScene(true);
                if (!report.IsValid)
                {
                    throw new InvalidOperationException(
                        $"ReactorCoreDelivery validation failed with {report.Errors.Count} error(s).");
                }
            }

            Debug.Log("[ReactorCoreDeliverySceneBuilder] Scene build complete.");
            return ScenePath;
        }

        private static void ConfigureScenario(
            GoldenScenarioSceneContext context,
            ReactorCoreDeliveryAgent agent,
            GeneratedAssets assets)
        {
            ConfigureEnvironmentManager(context.EnvironmentManager);
            ConfigureCameraAnchors(context);

            Transform environmentRoot = context.EnvironmentRoot.transform;
            GameObject floor = environmentRoot.Find("Floor")?.gameObject;
            Transform defaultGoalRoot = context.Goal;

            if (floor != null)
            {
                ConfigureBaseFloor(floor, assets);
            }

            SetupAgent(agent, context.EnvironmentManager, assets);

            Transform courseRoot = CreateChild(environmentRoot, "CourseRoot").transform;
            Transform shellRoot = CreateChild(environmentRoot, "ShellRoot").transform;
            Transform propsRoot = CreateChild(environmentRoot, "PropsRoot").transform;

            Transform startAnchor = CreateAnchor(courseRoot, "StartAnchor", new Vector3(0f, 0.5f, -10f));
            Transform mergeAnchor = CreateAnchor(courseRoot, "MergeAnchor", new Vector3(0f, 0.5f, 9f));

            BuildBoundaryGeometry(courseRoot, assets);
            BuildCourseReadability(courseRoot, assets);
            BuildStaticShell(shellRoot, assets);
            SupportPropRig supportPropRig = BuildPropDressing(propsRoot, assets);

            SweepingLaserHazard laserHazard = BuildLaserHazard(courseRoot, assets);
            ShockFloorHazard shockFloorHazard = BuildShockFloor(courseRoot, assets);
            BlastDoorHazard blastDoorHazard = BuildBlastDoor(courseRoot, assets);
            PickupVisuals pickupVisuals = BuildPickupCoreDock(courseRoot, assets);
            GoalVisuals goalVisuals = BuildGoalSocket(defaultGoalRoot, courseRoot, assets);

            ReactorCoreDeliveryCourse course = CreateCourseRoot(courseRoot);
            WireCourse(course, startAnchor, mergeAnchor, pickupVisuals, defaultGoalRoot, laserHazard, shockFloorHazard, blastDoorHazard, goalVisuals, supportPropRig);
            WireAgent(agent, context.EnvironmentManager, context.RecordingHelper, course, startAnchor);
            WireHud(context.EnvironmentRoot, course);
            WireGoldenSpine(context.GoldenSpine, context.EnvironmentRoot.transform, agent, defaultGoalRoot, context.EnvironmentManager);
        }

        private static GeneratedAssets EnsureGeneratedAssets()
        {
            var logs = new List<string>();
            var assets = new GeneratedAssets
            {
                DarkFloorMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdFloorDark.mat", new Color(0.08f, 0.10f, 0.12f), new Color(0f, 0f, 0f), 0.15f, 0.6f),
                BoundaryMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdBoundary.mat", new Color(0.16f, 0.18f, 0.20f), new Color(0.05f, 0.05f, 0.05f), 0.25f, 0.55f),
                LaserMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdLaser.mat", new Color(0.70f, 0.05f, 0.05f), new Color(1.25f, 0.18f, 0.18f), 0.05f, 0.25f),
                ShockOnMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdShockOn.mat", new Color(0.24f, 0.70f, 0.90f), new Color(0.25f, 1.25f, 1.65f), 0f, 0.45f),
                ShockOffMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdShockOff.mat", new Color(0.11f, 0.15f, 0.20f), new Color(0.00f, 0.05f, 0.08f), 0f, 0.45f),
                GoalMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdGoal.mat", new Color(0.10f, 0.24f, 0.28f), new Color(0.05f, 0.18f, 0.22f), 0f, 0.5f),
                CoreMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdCore.mat", new Color(0.20f, 0.80f, 1.00f), new Color(0.18f, 0.95f, 1.35f), 0f, 0.35f),
                DoorPanelMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdDoor.mat", new Color(0.15f, 0.16f, 0.18f), new Color(0.02f, 0.02f, 0.03f), 0.2f, 0.55f),
                AgentSkinMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdAgentSkin.mat", new Color(0.76f, 0.61f, 0.50f), new Color(0f, 0f, 0f), 0f, 0.42f),
                AgentSuitMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdAgentSuit.mat", new Color(0.84f, 0.86f, 0.88f), new Color(0.00f, 0.00f, 0.00f), 0.08f, 0.52f),
                AgentShirtMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdAgentShirt.mat", new Color(0.18f, 0.27f, 0.33f), new Color(0.00f, 0.00f, 0.00f), 0.04f, 0.45f),
                AgentPantMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdAgentPants.mat", new Color(0.20f, 0.23f, 0.28f), new Color(0.00f, 0.00f, 0.00f), 0.06f, 0.40f),
                AgentBootMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdAgentBoots.mat", new Color(0.18f, 0.22f, 0.27f), new Color(0f, 0f, 0f), 0.18f, 0.34f),
                AgentHairMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdAgentHair.mat", new Color(0.19f, 0.17f, 0.14f), new Color(0f, 0f, 0f), 0f, 0.28f),
                AgentHelmetMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdAgentHelmet.mat", new Color(0.92f, 0.70f, 0.16f), new Color(0.00f, 0.00f, 0.00f), 0.14f, 0.32f),
                AgentVisorMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdAgentVisor.mat", new Color(0.10f, 0.12f, 0.14f), new Color(0f, 0f, 0f), 0.12f, 0.28f),
                AgentAccentMaterial = CreateOrUpdateLitMaterial(MaterialsRoot + "/M_RcdAgentAccent.mat", new Color(0.14f, 0.62f, 0.78f), new Color(0.08f, 0.35f, 0.46f), 0f, 0.44f),
                CorePhysicsMaterial = CreateOrUpdatePhysicsMaterial(MaterialsRoot + "/PM_RcdCore.physicsMaterial", 0.12f, 0.16f, 0.28f),
                PropPhysicsMaterial = CreateOrUpdatePhysicsMaterial(MaterialsRoot + "/PM_RcdSupportProp.physicsMaterial", 0.72f, 0.80f, 0.02f)
            };

            assets.MovementController = CreateOrUpdateMovementController(AnimationRoot + "/ReactorCoreDeliveryMovement.controller");

            assets.AgentVisualPrefab = CreateOrUpdateVariant(AgentVisualSource, VariantRoot + "/RCD_AgentVisual.prefab", false, logs);
            assets.CosmicLockerPrefab = CreateOrUpdateVariant(CosmicLockerSource, VariantRoot + "/RCD_Locker.prefab", false, logs);
            assets.CosmicMonitorPrefab = CreateOrUpdateVariant(CosmicMonitorSource, VariantRoot + "/RCD_Monitor.prefab", false, logs);
            assets.CosmicCratePrefab = CreateOrUpdateVariant(CosmicCrateSource, VariantRoot + "/RCD_Crate.prefab", false, logs);
            assets.CosmicPanelPrefab = CreateOrUpdateVariant(CosmicPanelSource, VariantRoot + "/RCD_ControlPanel.prefab", false, logs);
            assets.SteamFxPrefab = CreateOrUpdateVariant(SteamFxSource, VariantRoot + "/RCD_SteamFx.prefab", false, logs);
            assets.GoalFxPrefab = CreateOrUpdateVariant(GoalFxSource, VariantRoot + "/RCD_GoalFx.prefab", false, logs);
            assets.ExplosionFxPrefab = CreateOrUpdateVariant(ExplosionFxSource, VariantRoot + "/RCD_ExplosionFx.prefab", false, logs);
            assets.FireExplosionFxPrefab = CreateOrUpdateVariant(FireExplosionFxSource, VariantRoot + "/RCD_FireExplosionFx.prefab", false, logs);

            // _Creepy_Cat is treated as URP-incompatible for V1. Remove any previously generated variants and
            // keep the corridor shell fully scenario-owned so hazard readability stays stable.
            DeleteGeneratedAsset(VariantRoot + "/RCD_CreepyWall.prefab", logs);
            DeleteGeneratedAsset(VariantRoot + "/RCD_CreepyFloor.prefab", logs);
            DeleteGeneratedAsset(VariantRoot + "/RCD_CreepyDoor.prefab", logs);
            DeleteGeneratedAsset(VariantRoot + "/RCD_CreepySign.prefab", logs);

            foreach (string log in logs)
            {
                Debug.Log(log);
            }

            return assets;
        }

        private static RuntimeAnimatorController CreateOrUpdateMovementController(string assetPath)
        {
            AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if (existingController != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Shock", AnimatorControllerParameterType.Trigger);

            BlendTree locomotionTree = new BlendTree
            {
                name = "Locomotion",
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(locomotionTree, controller);

            AnimationClip idleClip = LoadAnimationClip(IdleClipSource);
            AnimationClip walkClip = LoadAnimationClip(WalkClipSource);
            AnimationClip runClip = LoadAnimationClip(RunClipSource);
            AnimationClip shockClip = LoadAnimationClip(ShockClipSource);

            if (idleClip != null) locomotionTree.AddChild(idleClip, 0f);
            if (walkClip != null) locomotionTree.AddChild(walkClip, 0.45f);
            if (runClip != null) locomotionTree.AddChild(runClip, 1.0f);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState locomotionState = stateMachine.AddState("Locomotion");
            locomotionState.motion = locomotionTree;
            stateMachine.defaultState = locomotionState;

            if (shockClip != null)
            {
                AnimatorState shockState = stateMachine.AddState("Shock");
                shockState.motion = shockClip;

                AnimatorStateTransition anyToShock = stateMachine.AddAnyStateTransition(shockState);
                anyToShock.hasExitTime = false;
                anyToShock.duration = 0.05f;
                anyToShock.AddCondition(AnimatorConditionMode.If, 0f, "Shock");

                AnimatorStateTransition shockToLocomotion = shockState.AddTransition(locomotionState);
                shockToLocomotion.hasExitTime = true;
                shockToLocomotion.exitTime = 0.9f;
                shockToLocomotion.duration = 0.08f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static GameObject CreateOrUpdateVariant(string sourcePath, string targetPath, bool strictRenderCheck, IList<string> logs)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                logs?.Add($"[ReactorCoreDeliverySceneBuilder] Missing source prefab: {sourcePath}");
                return null;
            }

            if (strictRenderCheck && !HasRenderableSanity(sourcePrefab, out string reason))
            {
                logs?.Add($"[ReactorCoreDeliverySceneBuilder] Skipped variant for {sourcePrefab.name}: {reason}");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                logs?.Add($"[ReactorCoreDeliverySceneBuilder] Failed to instantiate {sourcePath}");
                return null;
            }

            instance.name = Path.GetFileNameWithoutExtension(targetPath);
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            Object.DestroyImmediate(instance);
            logs?.Add($"[ReactorCoreDeliverySceneBuilder] Prepared variant: {targetPath}");
            return savedPrefab;
        }

        private static void DeleteGeneratedAsset(string assetPath, IList<string> logs)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            {
                return;
            }

            if (AssetDatabase.DeleteAsset(assetPath))
            {
                logs?.Add($"[ReactorCoreDeliverySceneBuilder] Removed legacy variant: {assetPath}");
            }
        }

        private static bool HasRenderableSanity(GameObject prefab, out string reason)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                reason = "no renderers found";
                return false;
            }

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        reason = $"{renderer.name} has a missing material";
                        return false;
                    }

                    if (material.shader == null)
                    {
                        reason = $"{renderer.name} has a missing shader";
                        return false;
                    }

                    string shaderName = material.shader.name ?? string.Empty;
                    if (shaderName.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        reason = $"{renderer.name} resolved to an error shader";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        private static Material CreateOrUpdateLitMaterial(string assetPath, Color baseColor, Color emissionColor, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static PhysicsMaterial CreateOrUpdatePhysicsMaterial(string assetPath, float dynamicFriction, float staticFriction, float bounciness)
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(assetPath);
            if (material == null)
            {
                material = new PhysicsMaterial(Path.GetFileNameWithoutExtension(assetPath));
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.dynamicFriction = dynamicFriction;
            material.staticFriction = staticFriction;
            material.bounciness = bounciness;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Maximum;

            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimationClip LoadAnimationClip(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        }

        private static void EnsureScenarioFolders()
        {
            EnsureFolder(ScenarioRoot + "/Scenes");
            EnsureFolder(ScenarioRoot + "/Scripts");
            EnsureFolder(ScenarioRoot + "/Config");
            EnsureFolder(ScenarioRoot + "/Prefabs");
            EnsureFolder(ScenarioRoot + "/Editor");
            EnsureFolder(VariantRoot);
            EnsureFolder(MaterialsRoot);
            EnsureFolder(AnimationRoot);
        }

        private static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
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

        private static void ConfigureEnvironmentManager(EnvironmentManager environmentManager)
        {
            SerializedObject serializedObject = new SerializedObject(environmentManager);
            serializedObject.FindProperty("areaRadius").floatValue = 22f;
            serializedObject.FindProperty("fallThreshold").floatValue = -2f;
            serializedObject.FindProperty("randomizePositions").boolValue = false;
            serializedObject.FindProperty("randomizationStrength").floatValue = 0f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCameraAnchors(GoldenScenarioSceneContext context)
        {
            Transform cameraRig = FindDeepChild(context.EnvironmentRoot.transform, "CameraRig");
            DeleteChildIfExists(cameraRig, "RecordWideLeft");
            DeleteChildIfExists(cameraRig, "RecordWideRight");
            DeleteChildIfExists(cameraRig, "RecordMergeRun");
            DeleteChildIfExists(cameraRig, "RecordStartReveal");
            DeleteChildIfExists(cameraRig, "RecordAgentFollow");
            DeleteChildIfExists(cameraRig, "RecordHazardSweep");
            DeleteChildIfExists(cameraRig, "RecordGoalFront");
            Transform recordRearFollow = EnsureCameraAnchor(cameraRig, "RecordAgentRearFollow");
            Transform recordFrontFollow = EnsureCameraAnchor(cameraRig, "RecordAgentFrontFollow");

            if (context.DefaultCameraView != null)
            {
                context.DefaultCameraView.position = new Vector3(-2.35f, 2.55f, -15.2f);
                context.DefaultCameraView.rotation = Quaternion.Euler(11f, 18f, 0f);
            }

            if (recordRearFollow != null)
            {
                recordRearFollow.position = new Vector3(-2.75f, 2.45f, -15.7f);
                recordRearFollow.rotation = Quaternion.Euler(11f, 18f, 0f);
            }

            if (recordFrontFollow != null)
            {
                recordFrontFollow.position = new Vector3(2.45f, 2.35f, -4.4f);
                recordFrontFollow.rotation = Quaternion.Euler(10f, 198f, 0f);
            }

            if (context.MainCamera != null && context.DefaultCameraView != null)
            {
                context.MainCamera.transform.position = context.DefaultCameraView.position;
                context.MainCamera.transform.rotation = context.DefaultCameraView.rotation;
            }

            Transform[] recordingViews = { recordRearFollow, recordFrontFollow };
            AssignObjectReferenceArray(context.RecordingHelper, "cameraPositions", recordingViews);
            AssignObjectReferenceArray(context.GoldenSpine, "recordingCameraViews", recordingViews);
            AssignFloat(context.RecordingHelper, "cameraSwitchInterval", 6f);
            AssignBool(context.RecordingHelper, "enableCameraSwitching", true);
            AssignBool(context.RecordingHelper, "hideUIWhenRecording", true);
            AssignBool(context.RecordingHelper, "autoPreviewCamerasInPlayMode", true);
            AssignBool(context.RecordingHelper, "keepCameraAlignedWhileRecording", true);
            AssignBool(context.RecordingHelper, "enableTemporaryBlackoutCuts", true);
            AssignInt(context.RecordingHelper, "followCameraIndex", -1);
            AssignObjectReference(context.RecordingHelper, "followTarget", context.Agent != null ? context.Agent.transform : null);
            AssignVector3(context.RecordingHelper, "followPositionOffset", new Vector3(-0.45f, 2.25f, -5.8f));
            AssignVector3(context.RecordingHelper, "followLookAtOffset", new Vector3(0.10f, 1.20f, 3.05f));
            AssignFloat(context.RecordingHelper, "followPositionSharpness", 4.2f);
            AssignFloat(context.RecordingHelper, "followRotationSharpness", 6.0f);
            AssignBool(context.RecordingHelper, "enableOccluderGhosting", true);
            AssignFloat(context.RecordingHelper, "occluderGhostAlpha", 0.16f);
            AssignFloat(context.RecordingHelper, "occluderProbeRadius", 0.36f);
            AssignFollowCameraProfiles(
                context.RecordingHelper,
                new[]
                {
                    FollowCameraProfileData.Create(0, new Vector3(-0.45f, 2.25f, -5.8f), new Vector3(0.10f, 1.20f, 3.05f), 4.2f, 6.0f),
                    FollowCameraProfileData.Create(1, new Vector3(0.38f, 2.15f, 5.45f), new Vector3(0f, 1.18f, 0.75f), 4.2f, 6.4f)
                });
        }

        private static void ConfigureBaseFloor(GameObject floor, GeneratedAssets assets)
        {
            floor.name = "BaseGround";
            floor.transform.localPosition = new Vector3(0f, -0.05f, 4f);
            floor.transform.localScale = new Vector3(1.65f, 1f, 3.45f);

            MeshRenderer renderer = floor.GetComponent<MeshRenderer>();
            if (renderer != null && assets.DarkFloorMaterial != null)
            {
                renderer.sharedMaterial = assets.DarkFloorMaterial;
            }
        }

        private static void SetupAgent(ReactorCoreDeliveryAgent agent, EnvironmentManager environmentManager, GeneratedAssets assets)
        {
            agent.name = "ReactorCourierAgent";

            SphereCollider sphereCollider = agent.GetComponent<SphereCollider>();
            MeshFilter meshFilter = agent.GetComponent<MeshFilter>();
            if (meshFilter != null) Object.DestroyImmediate(meshFilter);
            MeshRenderer meshRenderer = agent.GetComponent<MeshRenderer>();
            if (meshRenderer != null) Object.DestroyImmediate(meshRenderer);

            CapsuleCollider capsuleCollider = GetOrAddComponent<CapsuleCollider>(agent.gameObject);
            capsuleCollider.center = new Vector3(0f, 0.9f, 0f);
            capsuleCollider.height = 1.8f;
            capsuleCollider.radius = 0.36f;

            if (sphereCollider != null)
            {
                Object.DestroyImmediate(sphereCollider);
            }

            Rigidbody rigidbody = GetOrAddComponent<Rigidbody>(agent.gameObject);
            rigidbody.mass = 1f;
            rigidbody.angularDamping = 0.8f;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            agent.MaxStep = 1200;

            GameObject visualRoot = CreateChild(agent.transform, "VisualRoot");
            visualRoot.transform.localPosition = new Vector3(0f, -0.24f, 0f);
            GameObject visualInstance = null;
            Animator animator = null;

            if (assets.AgentVisualPrefab != null)
            {
                visualInstance = PrefabUtility.InstantiatePrefab(assets.AgentVisualPrefab) as GameObject;
                if (visualInstance != null)
                {
                    visualInstance.name = "CourierVisual";
                    visualInstance.transform.SetParent(visualRoot.transform, false);
                    visualInstance.transform.localPosition = Vector3.zero;
                    visualInstance.transform.localRotation = Quaternion.identity;
                    visualInstance.transform.localScale = Vector3.one * 0.92f;

                    animator = visualInstance.GetComponentInChildren<Animator>();
                    if (animator != null && assets.MovementController != null)
                    {
                        animator.runtimeAnimatorController = assets.MovementController;
                    }

                    StripDecorativePhysics(visualInstance, stripRigidbodies: true);
                    StyleCourierVisual(visualInstance, assets);
                }
            }

            if (visualInstance == null)
            {
                visualInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visualInstance.name = "CourierVisualFallback";
                visualInstance.transform.SetParent(visualRoot.transform, false);
                visualInstance.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                visualInstance.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                Object.DestroyImmediate(visualInstance.GetComponent<Collider>());
                Renderer fallbackRenderer = visualInstance.GetComponent<Renderer>();
                if (fallbackRenderer != null && assets.BoundaryMaterial != null)
                {
                    fallbackRenderer.sharedMaterial = assets.BoundaryMaterial;
                }
            }

            ReactorCoreDeliveryVisualController visualController = visualRoot.AddComponent<ReactorCoreDeliveryVisualController>();
            SerializedObject visualControllerSo = new SerializedObject(visualController);
            visualControllerSo.FindProperty("animator").objectReferenceValue = animator;
            visualControllerSo.FindProperty("modelRoot").objectReferenceValue = visualInstance.transform;
            visualControllerSo.ApplyModifiedPropertiesWithoutUndo();

            BuildAgentViewSensor(agent.transform);

            SerializedObject agentSo = new SerializedObject(agent);
            agentSo.FindProperty("showDebugInfo").boolValue = false;
            agentSo.FindProperty("autoResetPosition").boolValue = false;
            agentSo.FindProperty("startPosition").vector3Value = new Vector3(0f, 0.5f, -10f);
            agentSo.FindProperty("startRotation").quaternionValue = Quaternion.identity;
            agentSo.FindProperty("environmentManager").objectReferenceValue = environmentManager;
            agentSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildAgentViewSensor(Transform parent)
        {
            DeleteChildIfExists(parent, "AgentViewSensor");

            GameObject sensorRoot = CreateChild(parent, "AgentViewSensor");
            sensorRoot.transform.localPosition = new Vector3(0f, 1.34f, 0.22f);
            sensorRoot.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);

            Camera sensorCamera = sensorRoot.AddComponent<Camera>();
            sensorCamera.enabled = false;
            sensorCamera.clearFlags = CameraClearFlags.SolidColor;
            sensorCamera.backgroundColor = Color.black;
            sensorCamera.fieldOfView = 76f;
            sensorCamera.nearClipPlane = 0.03f;
            sensorCamera.farClipPlane = 22f;
            sensorCamera.allowHDR = false;
            sensorCamera.allowMSAA = false;

            FlareLayer flareLayer = sensorRoot.GetComponent<FlareLayer>();
            if (flareLayer != null)
            {
                Object.DestroyImmediate(flareLayer);
            }

            AudioListener listener = sensorRoot.GetComponent<AudioListener>();
            if (listener != null)
            {
                Object.DestroyImmediate(listener);
            }

            CameraSensorComponent sensor = sensorRoot.AddComponent<CameraSensorComponent>();
            sensor.Camera = sensorCamera;
            sensor.SensorName = "CourierView";
            sensor.Width = 64;
            sensor.Height = 36;
            sensor.Grayscale = true;
            sensor.ObservationStacks = 1;
            sensor.ObservationType = ObservationType.Default;
            sensor.CompressionType = SensorCompressionType.PNG;
            sensor.RuntimeCameraEnable = false;
        }

        private static void WireAgent(ReactorCoreDeliveryAgent agent, EnvironmentManager environmentManager, RecordingHelper recordingHelper, ReactorCoreDeliveryCourse course, Transform startAnchor)
        {
            SerializedObject agentSo = new SerializedObject(agent);
            agentSo.FindProperty("course").objectReferenceValue = course;
            agentSo.FindProperty("environmentManager").objectReferenceValue = environmentManager;
            agentSo.FindProperty("recordingHelper").objectReferenceValue = recordingHelper;
            agentSo.FindProperty("startPosition").vector3Value = startAnchor.position;
            agentSo.FindProperty("meltdownFailDelay").floatValue = 0.42f;
            agentSo.FindProperty("meltdownCameraCutDelay").floatValue = 0.20f;
            agentSo.FindProperty("meltdownCameraCutDuration").floatValue = 0.18f;
            agentSo.ApplyModifiedPropertiesWithoutUndo();

            // Keep the serialized scene view aligned with the actual episode spawn so the agent
            // is readable in edit mode and does not appear embedded in the lane divider.
            agent.transform.position = startAnchor.position;
            agent.transform.rotation = startAnchor.rotation;

            Rigidbody rigidbody = agent.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private static void BuildBoundaryGeometry(Transform parent, GeneratedAssets assets)
        {
            const float shellWallHeight = 2.64f;
            const float shellHeaderY = 2.86f;
            float shellWallCenterY = shellWallHeight * 0.5f;

            CreateBlock(parent, "LeftBoundary", new Vector3(-4.8f, shellWallCenterY, 4f), new Vector3(0.35f, shellWallHeight, 28f), assets.BoundaryMaterial);
            CreateBlock(parent, "RightBoundary", new Vector3(4.8f, shellWallCenterY, 4f), new Vector3(0.35f, shellWallHeight, 28f), assets.BoundaryMaterial);
            CreateBlock(parent, "StartBackWall", new Vector3(0f, shellWallCenterY, -10.8f), new Vector3(8.8f, shellWallHeight, 0.35f), assets.BoundaryMaterial);
            CreateBlock(parent, "GoalBackWall", new Vector3(0f, shellWallCenterY, 20.5f), new Vector3(8.8f, shellWallHeight, 0.35f), assets.BoundaryMaterial);
            CreateBlock(parent, "StartHeaderFill", new Vector3(0f, shellHeaderY, -10.55f), new Vector3(8.8f, 0.36f, 0.24f), assets.BoundaryMaterial);
            CreateBlock(parent, "GoalHeaderFill", new Vector3(0f, shellHeaderY, 20.25f), new Vector3(8.8f, 0.36f, 0.24f), assets.BoundaryMaterial);
            CreateBlock(parent, "LaneDivider", new Vector3(0f, 0.8f, 2f), new Vector3(0.30f, 1.6f, 10f), assets.BoundaryMaterial);
            CreateBlock(parent, "LaserLaneRail", new Vector3(-2.4f, 0.3f, 2f), new Vector3(2.2f, 0.6f, 0.18f), assets.BoundaryMaterial);
            CreateBlock(parent, "ShockLaneRail", new Vector3(2.4f, 0.3f, 2f), new Vector3(2.2f, 0.6f, 0.18f), assets.BoundaryMaterial);
        }

        private static void BuildCourseReadability(Transform parent, GeneratedAssets assets)
        {
            BuildStartBay(parent, assets);
            BuildMergeGuide(parent, assets);
            BuildMergeChicane(parent, assets);
            BuildLaserWarningFloor(parent, assets);
            BuildShockWarningFloor(parent, assets);
            BuildDoorThresholdGuide(parent, assets);
            BuildGoalRunway(parent, assets);
            BuildZoneFrames(parent, assets);
            BuildGoalChamber(parent, assets);
        }

        private static void BuildStartBay(Transform parent, GeneratedAssets assets)
        {
            CreateBlock(parent, "StartPad", new Vector3(0f, 0.04f, -9.8f), new Vector3(2.35f, 0.04f, 1.65f), assets.BoundaryMaterial);
            CreateBlock(parent, "StartPadAccent", new Vector3(0f, 0.06f, -9.8f), new Vector3(1.75f, 0.02f, 1.05f), assets.GoalMaterial);
            CreateBlock(parent, "StartGuideLeft", new Vector3(-1.15f, 0.08f, -8.35f), new Vector3(0.16f, 0.02f, 1.05f), assets.GoalMaterial);
            CreateBlock(parent, "StartGuideRight", new Vector3(1.15f, 0.08f, -8.35f), new Vector3(0.16f, 0.02f, 1.05f), assets.GoalMaterial);
        }

        private static void BuildMergeGuide(Transform parent, GeneratedAssets assets)
        {
            CreateBlock(parent, "MergeSpineA", new Vector3(0f, 0.045f, 7.4f), new Vector3(0.42f, 0.03f, 2.2f), assets.GoalMaterial);
            CreateBlock(parent, "MergeSpineB", new Vector3(0f, 0.045f, 10.8f), new Vector3(0.42f, 0.03f, 1.8f), assets.GoalMaterial);
            CreateBlock(parent, "MergeChevronLeft", new Vector3(-0.70f, 0.045f, 7.0f), new Vector3(0.18f, 0.03f, 1.7f), assets.GoalMaterial, Quaternion.Euler(0f, 28f, 0f));
            CreateBlock(parent, "MergeChevronRight", new Vector3(0.70f, 0.045f, 7.0f), new Vector3(0.18f, 0.03f, 1.7f), assets.GoalMaterial, Quaternion.Euler(0f, -28f, 0f));
        }

        private static void BuildMergeChicane(Transform parent, GeneratedAssets assets)
        {
            GameObject leftBarrier = CreateBlock(parent, "MergeChicaneLeft", new Vector3(-1.55f, 0.34f, 7.95f), new Vector3(1.36f, 0.68f, 0.26f), assets.BoundaryMaterial);
            GameObject leftAccent = CreateBlock(parent, "MergeChicaneLeftAccent", new Vector3(-1.55f, 0.72f, 7.95f), new Vector3(1.02f, 0.08f, 0.08f), assets.GoalMaterial);
            GameObject rightBarrier = CreateBlock(parent, "MergeChicaneRight", new Vector3(1.55f, 0.34f, 9.55f), new Vector3(1.36f, 0.68f, 0.26f), assets.BoundaryMaterial);
            GameObject rightAccent = CreateBlock(parent, "MergeChicaneRightAccent", new Vector3(1.55f, 0.72f, 9.55f), new Vector3(1.02f, 0.08f, 0.08f), assets.LaserMaterial);
            DestroyCollider(leftAccent);
            DestroyCollider(rightAccent);
        }

        private static void BuildLaserWarningFloor(Transform parent, GeneratedAssets assets)
        {
            CreateBlock(parent, "LaserHoldPad", new Vector3(-2.35f, 0.04f, -0.7f), new Vector3(1.55f, 0.03f, 1.25f), assets.BoundaryMaterial);
            CreateBlock(parent, "LaserHoldPadAccent", new Vector3(-2.35f, 0.06f, -0.7f), new Vector3(1.15f, 0.02f, 0.82f), assets.LaserMaterial);

            float[] warningSlices = { -0.3f, 0.5f, 1.3f, 2.1f, 2.9f };
            foreach (float z in warningSlices)
            {
                CreateBlock(parent, $"LaserWarning_{z:+0.0;-0.0;0.0}", new Vector3(-2.35f, 0.05f, z), new Vector3(1.55f, 0.025f, 0.18f), assets.LaserMaterial);
            }
        }

        private static void BuildShockWarningFloor(Transform parent, GeneratedAssets assets)
        {
            CreateBlock(parent, "ShockHoldPad", new Vector3(2.35f, 0.04f, -0.2f), new Vector3(1.55f, 0.03f, 1.45f), assets.BoundaryMaterial);
            CreateBlock(parent, "ShockHoldPadAccent", new Vector3(2.35f, 0.06f, -0.2f), new Vector3(1.15f, 0.02f, 0.95f), assets.ShockOffMaterial);
            CreateBlock(parent, "ShockSideGuideLeft", new Vector3(1.30f, 0.055f, 1.8f), new Vector3(0.12f, 0.02f, 3.55f), assets.ShockOffMaterial);
            CreateBlock(parent, "ShockSideGuideRight", new Vector3(3.40f, 0.055f, 1.8f), new Vector3(0.12f, 0.02f, 3.55f), assets.ShockOffMaterial);
        }

        private static void BuildDoorThresholdGuide(Transform parent, GeneratedAssets assets)
        {
            float[] thresholdSlices = { 10.9f, 11.45f, 12.0f, 12.55f };
            for (int i = 0; i < thresholdSlices.Length; i++)
            {
                Material material = i % 2 == 0 ? assets.LaserMaterial : assets.DoorPanelMaterial;
                CreateBlock(parent, $"DoorThreshold_{i}", new Vector3(0f, 0.05f, thresholdSlices[i]), new Vector3(2.95f, 0.025f, 0.16f), material);
            }
        }

        private static void BuildGoalRunway(Transform parent, GeneratedAssets assets)
        {
            CreateBlock(parent, "GoalRunwayBase", new Vector3(0f, 0.04f, 16.3f), new Vector3(2.65f, 0.03f, 3.45f), assets.BoundaryMaterial);
            CreateBlock(parent, "GoalRunwayCenter", new Vector3(0f, 0.06f, 16.7f), new Vector3(0.50f, 0.02f, 2.45f), assets.GoalMaterial);
            CreateBlock(parent, "GoalRunwayLeft", new Vector3(-1.05f, 0.06f, 16.7f), new Vector3(0.18f, 0.02f, 2.05f), assets.GoalMaterial);
            CreateBlock(parent, "GoalRunwayRight", new Vector3(1.05f, 0.06f, 16.7f), new Vector3(0.18f, 0.02f, 2.05f), assets.GoalMaterial);
            CreateBlock(parent, "GoalDockRingA", new Vector3(0f, 0.07f, 18f), new Vector3(1.85f, 0.02f, 0.18f), assets.GoalMaterial);
            CreateBlock(parent, "GoalDockRingB", new Vector3(0f, 0.07f, 18f), new Vector3(0.18f, 0.02f, 1.85f), assets.GoalMaterial);
        }

        private static void BuildZoneFrames(Transform parent, GeneratedAssets assets)
        {
            BuildZoneFrame(parent, "LaserZoneFrame", new Vector3(-2.35f, 0f, 1.3f), 2.35f, 2.72f, 4.0f, assets.BoundaryMaterial, assets.LaserMaterial);
            BuildZoneFrame(parent, "ShockZoneFrame", new Vector3(2.35f, 0f, 1.8f), 2.35f, 2.72f, 7.6f, assets.BoundaryMaterial, assets.ShockOnMaterial);
            BuildZoneFrame(parent, "DoorZoneFrame", new Vector3(0f, 0f, 11.8f), 3.45f, 2.88f, 1.3f, assets.BoundaryMaterial, assets.LaserMaterial);
        }

        private static void BuildGoalChamber(Transform parent, GeneratedAssets assets)
        {
            CreateBlock(parent, "GoalPillarLeft", new Vector3(-2.15f, 1.45f, 18f), new Vector3(0.38f, 2.9f, 0.42f), assets.BoundaryMaterial);
            CreateBlock(parent, "GoalPillarRight", new Vector3(2.15f, 1.45f, 18f), new Vector3(0.38f, 2.9f, 0.42f), assets.BoundaryMaterial);
            CreateBlock(parent, "GoalLintel", new Vector3(0f, 2.78f, 18f), new Vector3(4.5f, 0.18f, 0.42f), assets.BoundaryMaterial);
            CreateBlock(parent, "GoalLightBar", new Vector3(0f, 2.48f, 18.18f), new Vector3(3.2f, 0.10f, 0.06f), assets.GoalMaterial);
            CreateBlock(parent, "GoalFrameFloorLeft", new Vector3(-1.6f, 0.045f, 17.85f), new Vector3(0.18f, 0.02f, 1.45f), assets.GoalMaterial);
            CreateBlock(parent, "GoalFrameFloorRight", new Vector3(1.6f, 0.045f, 17.85f), new Vector3(0.18f, 0.02f, 1.45f), assets.GoalMaterial);
            CreateBlock(parent, "GoalFrameRear", new Vector3(0f, 0.045f, 19.15f), new Vector3(1.75f, 0.02f, 0.18f), assets.GoalMaterial);

            CreateBlock(parent, "GoalEnergyPostLeft", new Vector3(-1.3f, 0.8f, 16.9f), new Vector3(0.18f, 1.45f, 0.18f), assets.BoundaryMaterial);
            CreateBlock(parent, "GoalEnergyPostRight", new Vector3(1.3f, 0.8f, 16.9f), new Vector3(0.18f, 1.45f, 0.18f), assets.BoundaryMaterial);
            CreateBlock(parent, "GoalEnergyCoreLeft", new Vector3(-1.3f, 1.62f, 16.9f), new Vector3(0.22f, 0.22f, 0.22f), assets.CoreMaterial);
            CreateBlock(parent, "GoalEnergyCoreRight", new Vector3(1.3f, 1.62f, 16.9f), new Vector3(0.22f, 0.22f, 0.22f), assets.CoreMaterial);
            CreateBlock(parent, "GoalCanopyLeft", new Vector3(-1.3f, 2.92f, 17.55f), new Vector3(0.18f, 0.10f, 1.2f), assets.GoalMaterial);
            CreateBlock(parent, "GoalCanopyRight", new Vector3(1.3f, 2.92f, 17.55f), new Vector3(0.18f, 0.10f, 1.2f), assets.GoalMaterial);
            CreateBlock(parent, "GoalCanopyBridge", new Vector3(0f, 3.05f, 17.55f), new Vector3(2.75f, 0.08f, 0.18f), assets.BoundaryMaterial);
        }

        private static void BuildStaticShell(Transform parent, GeneratedAssets assets)
        {
            const float shellCeilingY = 3.08f;
            const float shellCeilingPlateWidth = 1.82f;
            const float shellCeilingPlateHeight = 0.18f;
            const float shellPlateX = 3.38f;
            const float shellSkylightFrameY = 2.96f;
            const float shellSkylightFrameX = 2.05f;
            const float shellInsetY = 1.35f;
            const float shellInsetHeight = 2.45f;
            const float shellAccentY = 1.88f;
            const float shellLightStripY = 2.12f;
            const float shellRibY = 2.84f;
            const float shellRibX = 2.55f;
            const float shellSpineY = 2.94f;
            const float shellButtressY = 1.35f;
            const float shellButtressHeight = 2.7f;

            float[] zSlices = { -8f, -2f, 4f, 10f, 16f };
            for (int i = 0; i < zSlices.Length; i++)
            {
                float z = zSlices[i];
                Material accentMaterial = i % 2 == 0 ? assets.GoalMaterial : assets.DoorPanelMaterial;

                CreateBlock(parent, $"ShellFloorPlate_{i}", new Vector3(0f, 0.03f, z), new Vector3(8.7f, 0.06f, 5.6f), assets.DarkFloorMaterial);
                CreateBlock(parent, $"ShellCeilingPlateLeft_{i}", new Vector3(-shellPlateX, shellCeilingY, z), new Vector3(shellCeilingPlateWidth, shellCeilingPlateHeight, 6.4f), assets.BoundaryMaterial);
                CreateBlock(parent, $"ShellCeilingPlateRight_{i}", new Vector3(shellPlateX, shellCeilingY, z), new Vector3(shellCeilingPlateWidth, shellCeilingPlateHeight, 6.4f), assets.BoundaryMaterial);
                CreateBlock(parent, $"ShellSkylightFrameLeft_{i}", new Vector3(-shellSkylightFrameX, shellSkylightFrameY, z), new Vector3(0.12f, 0.12f, 5.95f), accentMaterial);
                CreateBlock(parent, $"ShellSkylightFrameRight_{i}", new Vector3(shellSkylightFrameX, shellSkylightFrameY, z), new Vector3(0.12f, 0.12f, 5.95f), accentMaterial);
                CreateBlock(parent, $"ShellLeftInset_{i}", new Vector3(-4.55f, shellInsetY, z), new Vector3(0.20f, shellInsetHeight, 5.35f), assets.DarkFloorMaterial);
                CreateBlock(parent, $"ShellRightInset_{i}", new Vector3(4.55f, shellInsetY, z), new Vector3(0.20f, shellInsetHeight, 5.35f), assets.DarkFloorMaterial);
                CreateBlock(parent, $"ShellLeftAccent_{i}", new Vector3(-4.32f, shellAccentY, z), new Vector3(0.05f, 0.22f, 4.85f), accentMaterial);
                CreateBlock(parent, $"ShellRightAccent_{i}", new Vector3(4.32f, shellAccentY, z), new Vector3(0.05f, 0.22f, 4.85f), accentMaterial);
                CreateBlock(parent, $"ShellLightStripLeft_{i}", new Vector3(-4.18f, shellLightStripY, z), new Vector3(0.04f, 0.10f, 5.0f), accentMaterial);
                CreateBlock(parent, $"ShellLightStripRight_{i}", new Vector3(4.18f, shellLightStripY, z), new Vector3(0.04f, 0.10f, 5.0f), accentMaterial);
                CreateBlock(parent, $"ShellCeilingRibLeft_{i}", new Vector3(-shellRibX, shellRibY, z), new Vector3(0.12f, 0.22f, 5.8f), assets.BoundaryMaterial);
                CreateBlock(parent, $"ShellCeilingRibRight_{i}", new Vector3(shellRibX, shellRibY, z), new Vector3(0.12f, 0.22f, 5.8f), assets.BoundaryMaterial);
                CreateBlock(parent, $"ShellCeilingSpine_{i}", new Vector3(0f, shellSpineY, z), new Vector3(0.10f, 0.08f, 5.4f), accentMaterial);
                CreateBlock(parent, $"ShellServiceRailLeft_{i}", new Vector3(-1.22f, 2.74f, z), new Vector3(0.10f, 0.10f, 5.55f), accentMaterial);
                CreateBlock(parent, $"ShellServiceRailRight_{i}", new Vector3(1.22f, 2.74f, z), new Vector3(0.10f, 0.10f, 5.55f), accentMaterial);
                CreateBlock(parent, $"ShellConduitLeft_{i}", new Vector3(-0.84f, 2.62f, z), new Vector3(0.16f, 0.14f, 5.2f), assets.DarkFloorMaterial);
                CreateBlock(parent, $"ShellConduitRight_{i}", new Vector3(0.84f, 2.62f, z), new Vector3(0.16f, 0.14f, 5.2f), assets.DarkFloorMaterial);
                CreateBlock(parent, $"ShellButtressLeft_{i}", new Vector3(-3.95f, shellButtressY, z + 2.35f), new Vector3(0.18f, shellButtressHeight, 0.16f), assets.BoundaryMaterial);
                CreateBlock(parent, $"ShellButtressRight_{i}", new Vector3(3.95f, shellButtressY, z - 2.35f), new Vector3(0.18f, shellButtressHeight, 0.16f), assets.BoundaryMaterial);
            }

            CreateBlock(parent, "ShellUpperFasciaLeft", new Vector3(-4.46f, 2.74f, 4f), new Vector3(0.34f, 0.44f, 29.2f), assets.BoundaryMaterial);
            CreateBlock(parent, "ShellUpperFasciaRight", new Vector3(4.46f, 2.74f, 4f), new Vector3(0.34f, 0.44f, 29.2f), assets.BoundaryMaterial);
            CreateBlock(parent, "ShellInnerCableTrayLeft", new Vector3(-1.6f, 2.52f, 4f), new Vector3(0.18f, 0.12f, 29.0f), assets.BoundaryMaterial);
            CreateBlock(parent, "ShellInnerCableTrayRight", new Vector3(1.6f, 2.52f, 4f), new Vector3(0.18f, 0.12f, 29.0f), assets.BoundaryMaterial);

            float[] seamCenters = { -5f, 1f, 7f, 13f };
            for (int i = 0; i < seamCenters.Length; i++)
            {
                CreateBlock(parent, $"ShellCeilingBridge_{i}", new Vector3(0f, 2.98f, seamCenters[i]), new Vector3(1.85f, 0.10f, 0.92f), assets.BoundaryMaterial);
                CreateBlock(parent, $"ShellCeilingBridgeAccent_{i}", new Vector3(0f, 2.91f, seamCenters[i]), new Vector3(0.92f, 0.04f, 0.74f), assets.GoalMaterial);
            }

            CreateBlock(parent, "ShellFrontCap", new Vector3(0f, 2.86f, -10.55f), new Vector3(8.7f, 0.40f, 0.26f), assets.BoundaryMaterial);
            CreateBlock(parent, "ShellRearCap", new Vector3(0f, 2.86f, 18.55f), new Vector3(8.7f, 0.40f, 0.26f), assets.BoundaryMaterial);

            BuildStatusPanel(parent, "HazardPanelLeft", new Vector3(-4.2f, 2.02f, 0.9f), Quaternion.Euler(0f, 90f, 0f), assets);
            BuildStatusPanel(parent, "HazardPanelRight", new Vector3(4.2f, 2.02f, 1.9f), Quaternion.Euler(0f, -90f, 0f), assets);
            BuildStatusPanel(parent, "DoorPanel", new Vector3(4.2f, 2.02f, 10.9f), Quaternion.Euler(0f, -90f, 0f), assets);
            BuildEmergencyBeacon(parent, "LaserAlarmLeft", new Vector3(-3.85f, 2.42f, 1.2f), assets.LaserMaterial, assets.BoundaryMaterial);
            BuildEmergencyBeacon(parent, "LaserAlarmRight", new Vector3(3.85f, 2.42f, 1.2f), assets.LaserMaterial, assets.BoundaryMaterial);
            BuildEmergencyBeacon(parent, "ShockAlarmLeft", new Vector3(-3.85f, 2.42f, 3.1f), assets.ShockOnMaterial, assets.BoundaryMaterial);
            BuildEmergencyBeacon(parent, "ShockAlarmRight", new Vector3(3.85f, 2.42f, 3.1f), assets.ShockOnMaterial, assets.BoundaryMaterial);
            BuildEmergencyBeacon(parent, "DoorAlarmLeft", new Vector3(-3.85f, 2.42f, 11.8f), assets.LaserMaterial, assets.BoundaryMaterial);
            BuildEmergencyBeacon(parent, "DoorAlarmRight", new Vector3(3.85f, 2.42f, 11.8f), assets.LaserMaterial, assets.BoundaryMaterial);
            BuildEmergencyBeacon(parent, "GoalAlarmLeft", new Vector3(-3.85f, 2.42f, 17.6f), assets.GoalMaterial, assets.BoundaryMaterial);
            BuildEmergencyBeacon(parent, "GoalAlarmRight", new Vector3(3.85f, 2.42f, 17.6f), assets.GoalMaterial, assets.BoundaryMaterial);
        }

        private static SupportPropRig BuildPropDressing(Transform parent, GeneratedAssets assets)
        {
            BuildStartMissionRack(parent, assets);
            PlaceOptionalPrefab(assets.CosmicLockerPrefab, parent, new Vector3(-3.8f, 0f, -8.2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            PlaceOptionalPrefab(assets.CosmicMonitorPrefab, parent, new Vector3(3.1f, 0f, -8.5f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            PlaceOptionalPrefab(assets.CosmicPanelPrefab, parent, new Vector3(-3.2f, 0f, -6.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            PlaceOptionalPrefab(assets.CosmicCratePrefab, parent, new Vector3(3.0f, 0f, -6.2f), Quaternion.identity, Vector3.one * 0.9f);

            PlaceOptionalPrefab(assets.CosmicPanelPrefab, parent, new Vector3(-3.5f, 0f, 1.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.95f);
            PlaceOptionalPrefab(assets.CosmicCratePrefab, parent, new Vector3(3.18f, 0f, 2.55f), Quaternion.identity, Vector3.one * 0.72f);
            PlaceOptionalPrefab(assets.CosmicCratePrefab, parent, new Vector3(-3.0f, 0f, 8.8f), Quaternion.identity, Vector3.one * 0.82f);
            BuildDoorServiceBay(parent, assets);

            PlaceOptionalPrefab(assets.CosmicLockerPrefab, parent, new Vector3(-3.9f, 0f, 15.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            PlaceOptionalPrefab(assets.CosmicPanelPrefab, parent, new Vector3(3.1f, 0f, 14.7f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            PlaceOptionalPrefab(assets.CosmicMonitorPrefab, parent, new Vector3(-2.8f, 0f, 17.0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            PlaceOptionalPrefab(assets.CosmicCratePrefab, parent, new Vector3(2.9f, 0f, 17.2f), Quaternion.identity, Vector3.one * 0.9f);
            PlaceOptionalPrefab(assets.CosmicCratePrefab, parent, new Vector3(-3.35f, 0f, 4.9f), Quaternion.identity, Vector3.one * 0.74f);
            PlaceOptionalPrefab(assets.CosmicLockerPrefab, parent, new Vector3(3.85f, 0f, 6.1f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 0.96f);
            PlaceOptionalPrefab(assets.CosmicMonitorPrefab, parent, new Vector3(-3.2f, 0f, 12.9f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.92f);
            return BuildSupportProps(parent, assets);
        }

        private static SweepingLaserHazard BuildLaserHazard(Transform parent, GeneratedAssets assets)
        {
            GameObject root = CreateChild(parent, "LaserGate");
            root.transform.position = new Vector3(-2.35f, 0f, 1.3f);

            CreateBlock(root.transform, "LaserPostLeft", new Vector3(-0.9f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 0.18f), assets.BoundaryMaterial);
            CreateBlock(root.transform, "LaserPostRight", new Vector3(0.9f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 0.18f), assets.BoundaryMaterial);

            Transform pivot = CreateChild(root.transform, "LaserPivot").transform;
            pivot.localPosition = new Vector3(0f, 1.25f, 0f);

            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "LaserBeam";
            beam.transform.SetParent(pivot, false);
            beam.transform.localPosition = Vector3.zero;
            beam.transform.localScale = new Vector3(0.12f, 0.12f, 3.4f);

            Renderer beamRenderer = beam.GetComponent<Renderer>();
            if (beamRenderer != null && assets.LaserMaterial != null)
            {
                beamRenderer.sharedMaterial = assets.LaserMaterial;
            }

            BoxCollider trigger = beam.GetComponent<BoxCollider>();
            trigger.isTrigger = true;

            SweepingLaserHazard laserHazard = beam.AddComponent<SweepingLaserHazard>();
            SerializedObject laserSo = new SerializedObject(laserHazard);
            laserSo.FindProperty("pivot").objectReferenceValue = pivot;
            laserSo.FindProperty("beamRenderer").objectReferenceValue = beamRenderer;
            laserSo.FindProperty("sweepAngle").floatValue = 78f;
            laserSo.FindProperty("cycleDuration").floatValue = 2.15f;
            laserSo.ApplyModifiedPropertiesWithoutUndo();

            return laserHazard;
        }

        private static ShockFloorHazard BuildShockFloor(Transform parent, GeneratedAssets assets)
        {
            GameObject root = CreateChild(parent, "ShockFloor");
            root.transform.position = new Vector3(1.52f, 0.02f, 1.8f);

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(1.28f, 0.62f, 0f);
            trigger.size = new Vector3(2.35f, 1.26f, 7.2f);

            var panels = new List<Renderer>();
            var indicators = new List<Renderer>();
            var ramHeads = new List<Transform>();
            GameObject housing = CreateBlock(root.transform, "PusherHousing", new Vector3(0.02f, 1.02f, 0f), new Vector3(0.30f, 2.04f, 7.38f), assets.BoundaryMaterial);
            GameObject housingCap = CreateBlock(root.transform, "PusherHousingCap", new Vector3(0.12f, 2.18f, 0f), new Vector3(0.58f, 0.18f, 7.38f), assets.BoundaryMaterial);
            GameObject guideRail = CreateBlock(root.transform, "PusherGuideRail", new Vector3(0.40f, 1.10f, 0f), new Vector3(0.08f, 1.62f, 7.12f), assets.DoorPanelMaterial);
            DestroyCollider(housing);
            DestroyCollider(housingCap);
            DestroyCollider(guideRail);

            float[] floorOffsets = { -2.8f, -1.4f, 0f, 1.4f, 2.8f };
            foreach (float panelOffset in floorOffsets)
            {
                GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = $"ShockPanel_{panelOffset:+0.0;-0.0;0.0}";
                panel.transform.SetParent(root.transform, false);
                panel.transform.localPosition = new Vector3(1.34f, 0.02f, panelOffset);
                panel.transform.localScale = new Vector3(1.34f, 0.12f, 1.05f);
                Object.DestroyImmediate(panel.GetComponent<BoxCollider>());

                Renderer renderer = panel.GetComponent<Renderer>();
                if (renderer != null && assets.ShockOffMaterial != null)
                {
                    renderer.sharedMaterial = assets.ShockOffMaterial;
                }

                panels.Add(renderer);
            }

            float[] pusherOffsets = { -2.2f, 0f, 2.2f, 3.25f };
            foreach (float pusherOffset in pusherOffsets)
            {
                GameObject frame = CreateBlock(root.transform, $"PusherFrame_{pusherOffset:+0.0;-0.0;0.0}", new Vector3(0.50f, 0.98f, pusherOffset), new Vector3(0.26f, 1.30f, 1.22f), assets.DoorPanelMaterial);
                GameObject ramHead = CreateBlock(root.transform, $"PusherHead_{pusherOffset:+0.0;-0.0;0.0}", new Vector3(0.76f, 0.98f, pusherOffset), new Vector3(0.44f, 0.62f, 0.86f), assets.AgentHelmetMaterial);
                GameObject ramFace = CreateBlock(root.transform, $"PusherFace_{pusherOffset:+0.0;-0.0;0.0}", new Vector3(0.98f, 0.98f, pusherOffset), new Vector3(0.10f, 0.38f, 0.56f), assets.LaserMaterial);
                DestroyCollider(frame);
                DestroyCollider(ramHead);
                DestroyCollider(ramFace);
                ramHeads.Add(ramHead.transform);
                panels.Add(ramFace.GetComponent<Renderer>());
            }

            if (assets.SteamFxPrefab != null)
            {
                PlaceOptionalPrefab(assets.SteamFxPrefab, root.transform, new Vector3(0.22f, 0.05f, -2.8f), Quaternion.identity, Vector3.one * 0.36f);
                PlaceOptionalPrefab(assets.SteamFxPrefab, root.transform, new Vector3(0.22f, 0.05f, 2.4f), Quaternion.identity, Vector3.one * 0.36f);
            }

            ShockFloorHazard shockFloorHazard = root.AddComponent<ShockFloorHazard>();
            SerializedObject shockSo = new SerializedObject(shockFloorHazard);
            shockSo.FindProperty("panelRenderers").arraySize = panels.Count;
            for (int i = 0; i < panels.Count; i++)
            {
                shockSo.FindProperty("panelRenderers").GetArrayElementAtIndex(i).objectReferenceValue = panels[i];
            }

            shockSo.FindProperty("indicatorRenderers").arraySize = indicators.Count;
            for (int i = 0; i < indicators.Count; i++)
            {
                shockSo.FindProperty("indicatorRenderers").GetArrayElementAtIndex(i).objectReferenceValue = indicators[i];
            }

            shockSo.FindProperty("ramHeads").arraySize = ramHeads.Count;
            for (int i = 0; i < ramHeads.Count; i++)
            {
                shockSo.FindProperty("ramHeads").GetArrayElementAtIndex(i).objectReferenceValue = ramHeads[i];
            }

            shockSo.FindProperty("cycleDuration").floatValue = 2.1f;
            shockSo.FindProperty("activeDuration").floatValue = 0.68f;
            shockSo.FindProperty("ramTravelDistance").floatValue = 0.82f;
            shockSo.FindProperty("ramDirectionSign").floatValue = 1f;
            shockSo.ApplyModifiedPropertiesWithoutUndo();

            return shockFloorHazard;
        }

        private static BlastDoorHazard BuildBlastDoor(Transform parent, GeneratedAssets assets)
        {
            GameObject root = CreateChild(parent, "BlastDoor");
            root.transform.position = new Vector3(0f, 0f, 11.8f);

            CreateBlock(root.transform, "DoorPillarLeft", new Vector3(-1.5f, 1.45f, 0f), new Vector3(0.40f, 2.9f, 0.45f), assets.BoundaryMaterial);
            CreateBlock(root.transform, "DoorPillarRight", new Vector3(1.5f, 1.45f, 0f), new Vector3(0.40f, 2.9f, 0.45f), assets.BoundaryMaterial);
            CreateBlock(root.transform, "DoorHeader", new Vector3(0f, 2.72f, 0f), new Vector3(3.5f, 0.36f, 0.45f), assets.BoundaryMaterial);
            CreateBlock(root.transform, "DoorLintelAccent", new Vector3(0f, 2.40f, 0.24f), new Vector3(2.8f, 0.10f, 0.04f), assets.GoalMaterial);

            GameObject leftPanel = CreateBlock(root.transform, "LeftDoorPanel", new Vector3(-0.55f, 1.18f, 0f), new Vector3(0.65f, 2.36f, 0.28f), assets.DoorPanelMaterial);
            GameObject rightPanel = CreateBlock(root.transform, "RightDoorPanel", new Vector3(0.55f, 1.18f, 0f), new Vector3(0.65f, 2.36f, 0.28f), assets.DoorPanelMaterial);

            GameObject leftIndicator = CreateBlock(root.transform, "DoorIndicatorLeft", new Vector3(-1.15f, 2.22f, 0.25f), new Vector3(0.20f, 0.20f, 0.05f), assets.DoorPanelMaterial);
            GameObject rightIndicator = CreateBlock(root.transform, "DoorIndicatorRight", new Vector3(1.15f, 2.22f, 0.25f), new Vector3(0.20f, 0.20f, 0.05f), assets.DoorPanelMaterial);

            BlastDoorHazard blastDoorHazard = root.AddComponent<BlastDoorHazard>();
            SerializedObject blastDoorSo = new SerializedObject(blastDoorHazard);
            blastDoorSo.FindProperty("leftPanel").objectReferenceValue = leftPanel.transform;
            blastDoorSo.FindProperty("rightPanel").objectReferenceValue = rightPanel.transform;
            blastDoorSo.FindProperty("indicatorRenderers").arraySize = 2;
            blastDoorSo.FindProperty("indicatorRenderers").GetArrayElementAtIndex(0).objectReferenceValue = leftIndicator.GetComponent<Renderer>();
            blastDoorSo.FindProperty("indicatorRenderers").GetArrayElementAtIndex(1).objectReferenceValue = rightIndicator.GetComponent<Renderer>();
            blastDoorSo.FindProperty("cycleDuration").floatValue = 3.0f;
            blastDoorSo.FindProperty("openDuration").floatValue = 1.05f;
            blastDoorSo.FindProperty("travelDistance").floatValue = 1.35f;
            blastDoorSo.ApplyModifiedPropertiesWithoutUndo();

            return blastDoorHazard;
        }

        private static PickupVisuals BuildPickupCoreDock(Transform parent, GeneratedAssets assets)
        {
            GameObject root = CreateChild(parent, "PickupDock");
            root.transform.localPosition = new Vector3(0f, 0f, -8.9f);

            CreateBlock(root.transform, "PickupBase", new Vector3(0f, 0.06f, 0f), new Vector3(1.35f, 0.12f, 1.1f), assets.BoundaryMaterial);
            CreateBlock(root.transform, "PickupRingOuter", new Vector3(0f, 0.12f, 0f), new Vector3(0.92f, 0.05f, 0.92f), assets.GoalMaterial);
            CreateBlock(root.transform, "PickupRingInner", new Vector3(0f, 0.15f, 0f), new Vector3(0.42f, 0.04f, 0.42f), assets.BoundaryMaterial);

            Transform coreSpawnAnchor = CreateChild(root.transform, "PickupCoreSpawn").transform;
            coreSpawnAnchor.localPosition = new Vector3(0f, 0.38f, 0f);

            GameObject pickupCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickupCore.name = "PickupCore";
            pickupCore.transform.SetParent(root.transform, false);
            pickupCore.transform.localPosition = coreSpawnAnchor.localPosition;
            pickupCore.transform.localScale = Vector3.one * 0.34f;

            SphereCollider pickupCollider = GetOrAddComponent<SphereCollider>(pickupCore);
            if (assets.CorePhysicsMaterial != null)
            {
                pickupCollider.sharedMaterial = assets.CorePhysicsMaterial;
            }

            Rigidbody pickupBody = GetOrAddComponent<Rigidbody>(pickupCore);
            pickupBody.mass = 0.75f;
            pickupBody.linearDamping = 0.18f;
            pickupBody.angularDamping = 0.22f;
            pickupBody.interpolation = RigidbodyInterpolation.Interpolate;
            pickupBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            pickupBody.maxAngularVelocity = 20f;

            Renderer pickupCoreRenderer = pickupCore.GetComponent<Renderer>();
            if (pickupCoreRenderer != null && assets.CoreMaterial != null)
            {
                pickupCoreRenderer.sharedMaterial = assets.CoreMaterial;
            }

            return new PickupVisuals
            {
                CoreSpawnAnchor = coreSpawnAnchor,
                ObjectiveCore = pickupBody,
                CoreRenderer = pickupCoreRenderer
            };
        }

        private static GoalVisuals BuildGoalSocket(Transform goalRoot, Transform parent, GeneratedAssets assets)
        {
            goalRoot.name = "ReactorSocket";
            goalRoot.SetParent(parent, true);
            goalRoot.position = new Vector3(0f, 0f, 18f);
            goalRoot.rotation = Quaternion.identity;
            goalRoot.localScale = Vector3.one;

            BoxCollider existingCollider = goalRoot.GetComponent<BoxCollider>();
            if (existingCollider != null) Object.DestroyImmediate(existingCollider);
            MeshFilter existingFilter = goalRoot.GetComponent<MeshFilter>();
            if (existingFilter != null) Object.DestroyImmediate(existingFilter);
            MeshRenderer existingRenderer = goalRoot.GetComponent<MeshRenderer>();
            if (existingRenderer != null) Object.DestroyImmediate(existingRenderer);

            SphereCollider trigger = GetOrAddComponent<SphereCollider>(goalRoot.gameObject);
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            trigger.center = new Vector3(0f, 0.22f, 0f);

            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "SocketPedestal";
            pedestal.transform.SetParent(goalRoot, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            pedestal.transform.localScale = new Vector3(1.08f, 0.04f, 1.08f);
            Renderer pedestalRenderer = pedestal.GetComponent<Renderer>();
            if (pedestalRenderer != null && assets.BoundaryMaterial != null)
            {
                pedestalRenderer.sharedMaterial = assets.BoundaryMaterial;
            }

            GameObject ringA = CreateBlock(goalRoot, "SocketRingA", new Vector3(0f, 0.045f, 0f), new Vector3(1.95f, 0.025f, 0.12f), assets.GoalMaterial);
            GameObject ringB = CreateBlock(goalRoot, "SocketRingB", new Vector3(0f, 0.045f, 0f), new Vector3(0.12f, 0.025f, 1.95f), assets.GoalMaterial);
            GameObject ringC = CreateBlock(goalRoot, "SocketRingC", new Vector3(0f, 0.055f, 0f), new Vector3(1.25f, 0.02f, 0.10f), assets.CoreMaterial);
            GameObject ringD = CreateBlock(goalRoot, "SocketRingD", new Vector3(0f, 0.055f, 0f), new Vector3(0.10f, 0.02f, 1.25f), assets.CoreMaterial);
            DestroyCollider(ringA);
            DestroyCollider(ringB);
            DestroyCollider(ringC);
            DestroyCollider(ringD);

            GameObject reactorGlow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            reactorGlow.name = "ReactorGlow";
            reactorGlow.transform.SetParent(goalRoot, false);
            reactorGlow.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            reactorGlow.transform.localScale = new Vector3(0.48f, 0.08f, 0.48f);
            Renderer glowRenderer = reactorGlow.GetComponent<Renderer>();
            if (glowRenderer != null && assets.GoalMaterial != null)
            {
                glowRenderer.sharedMaterial = assets.GoalMaterial;
            }
            DestroyCollider(reactorGlow);

            ParticleSystem ignitionFx = null;
            if (assets.GoalFxPrefab != null)
            {
                GameObject fx = PlaceOptionalPrefab(assets.GoalFxPrefab, goalRoot, new Vector3(0f, 0.24f, 0f), Quaternion.identity, Vector3.one * 0.9f);
                if (fx != null)
                {
                    ignitionFx = fx.GetComponentInChildren<ParticleSystem>();
                    StopAllParticleSystems(fx);
                }
            }

            if (ignitionFx == null)
            {
                ignitionFx = CreateGoalIgnitionFallback(goalRoot);
            }

            ParticleSystem shockFx = null;
            if (assets.SteamFxPrefab != null)
            {
                GameObject fx = PlaceOptionalPrefab(assets.SteamFxPrefab, goalRoot, new Vector3(0f, 0.08f, -0.3f), Quaternion.identity, Vector3.one * 0.65f);
                if (fx != null)
                {
                    shockFx = fx.GetComponentInChildren<ParticleSystem>();
                    StopAllParticleSystems(fx);
                }
            }

            GoalAlertRig alertRig = BuildGoalAlertRig(goalRoot, assets);

            return new GoalVisuals
            {
                GlowRenderer = glowRenderer,
                IgnitionFx = ignitionFx,
                ShockFx = shockFx,
                AlertBeaconRenderers = alertRig.AlertBeaconRenderers,
                CountdownFillRenderer = alertRig.CountdownFillRenderer,
                CountdownFill = alertRig.CountdownFill,
                CountdownDigitRenderers = alertRig.CountdownDigitRenderers,
                MeltdownFx = alertRig.MeltdownFx,
                MeltdownDebrisFx = alertRig.MeltdownDebrisFx,
                MeltdownOrigin = alertRig.MeltdownOrigin
            };
        }

        private static ReactorCoreDeliveryCourse CreateCourseRoot(Transform parent)
        {
            GameObject courseObject = CreateChild(parent, "ReactorCoreCourse");
            return courseObject.AddComponent<ReactorCoreDeliveryCourse>();
        }

        private static void WireCourse(ReactorCoreDeliveryCourse course, Transform startAnchor, Transform mergeAnchor, PickupVisuals pickupVisuals, Transform goalSocket, SweepingLaserHazard laserHazard, ShockFloorHazard shockFloorHazard, BlastDoorHazard blastDoorHazard, GoalVisuals goalVisuals, SupportPropRig supportPropRig)
        {
            SerializedObject courseSo = new SerializedObject(course);
            courseSo.FindProperty("startAnchor").objectReferenceValue = startAnchor;
            courseSo.FindProperty("mergeAnchor").objectReferenceValue = mergeAnchor;
            courseSo.FindProperty("coreSpawnAnchor").objectReferenceValue = pickupVisuals != null ? pickupVisuals.CoreSpawnAnchor : null;
            courseSo.FindProperty("objectiveCore").objectReferenceValue = pickupVisuals != null ? pickupVisuals.ObjectiveCore : null;
            courseSo.FindProperty("goalSocket").objectReferenceValue = goalSocket;
            courseSo.FindProperty("laserHazard").objectReferenceValue = laserHazard;
            courseSo.FindProperty("shockFloorHazard").objectReferenceValue = shockFloorHazard;
            courseSo.FindProperty("blastDoorHazard").objectReferenceValue = blastDoorHazard;
            courseSo.FindProperty("coreRenderer").objectReferenceValue = pickupVisuals != null ? pickupVisuals.CoreRenderer : null;
            courseSo.FindProperty("reactorGlowRenderer").objectReferenceValue = goalVisuals.GlowRenderer;
            courseSo.FindProperty("ignitionFx").objectReferenceValue = goalVisuals.IgnitionFx;
            courseSo.FindProperty("shockFx").objectReferenceValue = goalVisuals.ShockFx;
            courseSo.FindProperty("countdownFillRenderer").objectReferenceValue = goalVisuals.CountdownFillRenderer;
            courseSo.FindProperty("countdownFill").objectReferenceValue = goalVisuals.CountdownFill;
            courseSo.FindProperty("meltdownFx").objectReferenceValue = goalVisuals.MeltdownFx;
            courseSo.FindProperty("meltdownDebrisFx").objectReferenceValue = goalVisuals.MeltdownDebrisFx;
            courseSo.FindProperty("meltdownOrigin").objectReferenceValue = goalVisuals.MeltdownOrigin;
            courseSo.FindProperty("alertBeaconRenderers").arraySize = goalVisuals.AlertBeaconRenderers != null ? goalVisuals.AlertBeaconRenderers.Length : 0;
            for (int i = 0; i < courseSo.FindProperty("alertBeaconRenderers").arraySize; i++)
            {
                courseSo.FindProperty("alertBeaconRenderers").GetArrayElementAtIndex(i).objectReferenceValue = goalVisuals.AlertBeaconRenderers[i];
            }

            courseSo.FindProperty("countdownDigitRenderers").arraySize = goalVisuals.CountdownDigitRenderers != null ? goalVisuals.CountdownDigitRenderers.Length : 0;
            for (int i = 0; i < courseSo.FindProperty("countdownDigitRenderers").arraySize; i++)
            {
                courseSo.FindProperty("countdownDigitRenderers").GetArrayElementAtIndex(i).objectReferenceValue = goalVisuals.CountdownDigitRenderers[i];
            }

            Rigidbody[] supportBodies = supportPropRig != null ? supportPropRig.Bodies : Array.Empty<Rigidbody>();
            Transform[] supportAnchors = supportPropRig != null ? supportPropRig.SpawnAnchors : Array.Empty<Transform>();
            courseSo.FindProperty("supportProps").arraySize = supportBodies.Length;
            for (int i = 0; i < supportBodies.Length; i++)
            {
                courseSo.FindProperty("supportProps").GetArrayElementAtIndex(i).objectReferenceValue = supportBodies[i];
            }

            courseSo.FindProperty("supportPropSpawnAnchors").arraySize = supportAnchors.Length;
            for (int i = 0; i < supportAnchors.Length; i++)
            {
                courseSo.FindProperty("supportPropSpawnAnchors").GetArrayElementAtIndex(i).objectReferenceValue = supportAnchors[i];
            }

            courseSo.FindProperty("coreDockSettleTime").floatValue = 0.35f;
            courseSo.FindProperty("coreDockMaxPlanarSpeed").floatValue = 0.45f;
            courseSo.FindProperty("courseHalfWidth").floatValue = 4.75f;
            courseSo.FindProperty("progressStartZ").floatValue = -10f;
            courseSo.FindProperty("progressEndZ").floatValue = 18f;
            courseSo.FindProperty("coreOutOfBoundsPadding").floatValue = 1.0f;
            courseSo.FindProperty("supportPropSpawnJitterX").floatValue = 0.42f;
            courseSo.FindProperty("supportPropSpawnJitterZ").floatValue = 0.24f;
            courseSo.FindProperty("baseDeliveryDeadline").floatValue = 21.5f;
            courseSo.FindProperty("minDeliveryDeadline").floatValue = 12f;
            courseSo.FindProperty("meltdownCoreBlastForce").floatValue = 8.8f;
            courseSo.FindProperty("meltdownCoreLiftForce").floatValue = 6.4f;
            courseSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GoalAlertRig BuildGoalAlertRig(Transform goalRoot, GeneratedAssets assets)
        {
            GameObject rigRoot = CreateChild(goalRoot, "GoalAlertRig");
            rigRoot.transform.localPosition = new Vector3(0f, 2.15f, -1.65f);

            CreateBlock(rigRoot.transform, "AlertCrossbar", new Vector3(0f, 0.06f, 0f), new Vector3(4.10f, 0.18f, 0.18f), assets.BoundaryMaterial);
            CreateBlock(rigRoot.transform, "AlertSupportLeft", new Vector3(-1.75f, -0.82f, 0f), new Vector3(0.18f, 1.82f, 0.18f), assets.BoundaryMaterial);
            CreateBlock(rigRoot.transform, "AlertSupportRight", new Vector3(1.75f, -0.82f, 0f), new Vector3(0.18f, 1.82f, 0.18f), assets.BoundaryMaterial);

            List<Renderer> beaconRenderers = new List<Renderer>();
            beaconRenderers.AddRange(BuildAlarmBeacon(rigRoot.transform, "AlertBeaconLeft", new Vector3(-1.75f, 0.32f, 0.18f), assets));
            beaconRenderers.AddRange(BuildAlarmBeacon(rigRoot.transform, "AlertBeaconRight", new Vector3(1.75f, 0.32f, 0.18f), assets));

            GameObject displayRoot = CreateChild(rigRoot.transform, "CountdownDisplay");
            displayRoot.transform.localPosition = new Vector3(0f, 0.34f, 0.10f);
            CreateBlock(displayRoot.transform, "DisplayFrame", Vector3.zero, new Vector3(1.85f, 0.82f, 0.12f), assets.BoundaryMaterial);
            CreateBlock(displayRoot.transform, "DisplayScreen", new Vector3(0f, 0f, 0.04f), new Vector3(1.58f, 0.60f, 0.04f), assets.DoorPanelMaterial);

            Renderer[] tensSegments = BuildSevenSegmentDigit(displayRoot.transform, "TensDigit", new Vector3(-0.42f, 0f, 0.06f), assets.LaserMaterial);
            Renderer[] onesSegments = BuildSevenSegmentDigit(displayRoot.transform, "OnesDigit", new Vector3(0.42f, 0f, 0.06f), assets.LaserMaterial);
            Renderer[] digitRenderers = new Renderer[tensSegments.Length + onesSegments.Length];
            tensSegments.CopyTo(digitRenderers, 0);
            onesSegments.CopyTo(digitRenderers, tensSegments.Length);

            GameObject countdownBarRoot = CreateChild(rigRoot.transform, "CountdownBar");
            countdownBarRoot.transform.localPosition = new Vector3(0f, -0.22f, 0.08f);
            GameObject barFrame = CreateBlock(countdownBarRoot.transform, "BarFrame", Vector3.zero, new Vector3(2.40f, 0.18f, 0.10f), assets.BoundaryMaterial);
            GameObject barTrack = CreateBlock(countdownBarRoot.transform, "BarTrack", new Vector3(0f, 0f, 0.03f), new Vector3(2.14f, 0.08f, 0.04f), assets.DoorPanelMaterial);
            GameObject barFill = CreateBlock(countdownBarRoot.transform, "BarFill", new Vector3(-1.07f, 0f, 0.04f), new Vector3(2.14f, 0.08f, 0.03f), assets.LaserMaterial);
            DestroyCollider(barFrame);
            DestroyCollider(barTrack);
            DestroyCollider(barFill);

            Transform meltdownOrigin = CreateChild(goalRoot, "MeltdownOrigin").transform;
            meltdownOrigin.localPosition = new Vector3(0f, 0.42f, 0f);

            GameObject blastCluster = CreateChild(meltdownOrigin, "MeltdownBlastCluster");
            ParticleSystem meltdownFx = CreateMeltdownExplosionFallback(blastCluster.transform, "BlastController", new Color(1.0f, 0.46f, 0.10f, 0.95f), 96, 0.62f);
            PlaceOptionalPrefab(assets.FireExplosionFxPrefab, blastCluster.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.95f);
            PlaceOptionalPrefab(assets.FireExplosionFxPrefab, blastCluster.transform, new Vector3(1.55f, 0.35f, -0.65f), Quaternion.identity, Vector3.one * 0.78f);
            PlaceOptionalPrefab(assets.FireExplosionFxPrefab, blastCluster.transform, new Vector3(-1.55f, 0.45f, -0.55f), Quaternion.identity, Vector3.one * 0.78f);
            PlaceOptionalPrefab(assets.FireExplosionFxPrefab, blastCluster.transform, new Vector3(0f, 1.10f, 0.85f), Quaternion.identity, Vector3.one * 0.72f);
            StopAllParticleSystems(blastCluster);

            GameObject debrisCluster = CreateChild(meltdownOrigin, "MeltdownDebrisCluster");
            ParticleSystem meltdownDebrisFx = CreateMeltdownExplosionFallback(debrisCluster.transform, "DebrisController", new Color(0.95f, 0.84f, 0.28f, 0.9f), 128, 0.95f);
            PlaceOptionalPrefab(assets.ExplosionFxPrefab, debrisCluster.transform, Vector3.zero, Quaternion.identity, Vector3.one * 1.20f);
            PlaceOptionalPrefab(assets.ExplosionFxPrefab, debrisCluster.transform, new Vector3(2.20f, 0.22f, -0.35f), Quaternion.identity, Vector3.one * 0.96f);
            PlaceOptionalPrefab(assets.ExplosionFxPrefab, debrisCluster.transform, new Vector3(-2.20f, 0.20f, -0.35f), Quaternion.identity, Vector3.one * 0.96f);
            PlaceOptionalPrefab(assets.ExplosionFxPrefab, debrisCluster.transform, new Vector3(0f, 0.18f, 1.55f), Quaternion.identity, Vector3.one * 0.92f);
            StopAllParticleSystems(debrisCluster);

            return new GoalAlertRig
            {
                AlertBeaconRenderers = beaconRenderers.ToArray(),
                CountdownFillRenderer = barFill.GetComponent<Renderer>(),
                CountdownFill = barFill.transform,
                CountdownDigitRenderers = digitRenderers,
                MeltdownFx = meltdownFx,
                MeltdownDebrisFx = meltdownDebrisFx,
                MeltdownOrigin = meltdownOrigin
            };
        }

        private static void WireGoldenSpine(ScenarioGoldenSpine goldenSpine, Transform environmentRoot, ReactorCoreDeliveryAgent agent, Transform goalSocket, EnvironmentManager environmentManager)
        {
            SerializedObject spineSo = new SerializedObject(goldenSpine);
            spineSo.FindProperty("environmentRoot").objectReferenceValue = environmentRoot;
            spineSo.FindProperty("primaryAgent").objectReferenceValue = agent;
            spineSo.FindProperty("primaryGoal").objectReferenceValue = goalSocket;
            spineSo.FindProperty("environmentManager").objectReferenceValue = environmentManager;
            spineSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireHud(GameObject environmentRoot, ReactorCoreDeliveryCourse course)
        {
            if (environmentRoot == null)
            {
                return;
            }

            ReactorCoreDeliveryHud hud = GetOrAddComponent<ReactorCoreDeliveryHud>(environmentRoot);
            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("course").objectReferenceValue = course;
            hudSo.FindProperty("hideWhenStable").boolValue = false;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            return CreateBlock(parent, name, localPosition, localScale, material, Quaternion.identity);
        }

        private static ParticleSystem CreateGoalIgnitionFallback(Transform parent)
        {
            GameObject fxRoot = CreateChild(parent, "GoalIgnitionFallback");
            fxRoot.transform.localPosition = new Vector3(0f, 0.22f, 0f);

            ParticleSystem particleSystem = GetOrAddComponent<ParticleSystem>(fxRoot);

            var main = particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 1.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
            main.startColor = new Color(0.40f, 0.95f, 1.0f, 0.95f);
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24, 32) });

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.22f;

            var velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.radial = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.32f, 0.92f, 1.0f), 0f),
                    new GradientColorKey(new Color(0.16f, 0.54f, 0.95f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.95f, 0.15f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 0f));

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particleSystem;
        }

        private static ParticleSystem CreateMeltdownExplosionFallback(Transform parent, string name, Color startColor, int burstCount, float radius)
        {
            GameObject fxRoot = CreateChild(parent, name);
            fxRoot.transform.localPosition = Vector3.zero;

            ParticleSystem particleSystem = GetOrAddComponent<ParticleSystem>(fxRoot);

            var main = particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.90f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 6.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
            main.startColor = startColor;
            main.maxParticles = Mathf.Max(64, burstCount + 24);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.15f;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.0f, 0.92f, 0.54f), 0f),
                    new GradientColorKey(new Color(1.0f, 0.42f, 0.12f), 0.30f),
                    new GradientColorKey(new Color(0.18f, 0.12f, 0.12f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.95f, 0.08f),
                    new GradientAlphaKey(0.5f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.45f));

            var noise = particleSystem.noise;
            noise.enabled = true;
            noise.strength = 0.45f;
            noise.frequency = 0.6f;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particleSystem;
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localRotation = localRotation;
            block.transform.localScale = localScale;

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return block;
        }

        private static void DestroyCollider(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void ApplyPrimaryColliderMaterial(GameObject target, PhysicsMaterial material)
        {
            if (target == null || material == null)
            {
                return;
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                collider.sharedMaterial = material;
            }
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector3 position)
        {
            GameObject anchor = CreateChild(parent, name);
            anchor.transform.position = position;
            return anchor.transform;
        }

        private static GameObject PlaceOptionalPrefab(GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                return null;
            }

            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            StripDecorativePhysics(instance, stripRigidbodies: true);
            return instance;
        }

        private static Renderer[] BuildAlarmBeacon(Transform parent, string name, Vector3 localPosition, GeneratedAssets assets)
        {
            GameObject root = CreateChild(parent, name);
            root.transform.localPosition = localPosition;

            GameObject housing = CreateBlock(root.transform, "Housing", Vector3.zero, new Vector3(0.44f, 0.18f, 0.28f), assets.BoundaryMaterial);
            GameObject lensA = CreateBlock(root.transform, "LensA", new Vector3(-0.10f, 0f, 0.10f), new Vector3(0.14f, 0.10f, 0.10f), assets.LaserMaterial);
            GameObject lensB = CreateBlock(root.transform, "LensB", new Vector3(0.10f, 0f, 0.10f), new Vector3(0.14f, 0.10f, 0.10f), assets.LaserMaterial);
            DestroyCollider(housing);
            DestroyCollider(lensA);
            DestroyCollider(lensB);

            return new[]
            {
                lensA.GetComponent<Renderer>(),
                lensB.GetComponent<Renderer>()
            };
        }

        private static Renderer[] BuildSevenSegmentDigit(Transform parent, string name, Vector3 localPosition, Material material)
        {
            GameObject root = CreateChild(parent, name);
            root.transform.localPosition = localPosition;

            return new[]
            {
                CreateSegment(root.transform, "Top", new Vector3(0f, 0.20f, 0f), new Vector3(0.22f, 0.05f, 0.02f), material),
                CreateSegment(root.transform, "UpperLeft", new Vector3(-0.12f, 0.10f, 0f), new Vector3(0.05f, 0.18f, 0.02f), material),
                CreateSegment(root.transform, "UpperRight", new Vector3(0.12f, 0.10f, 0f), new Vector3(0.05f, 0.18f, 0.02f), material),
                CreateSegment(root.transform, "Middle", new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.05f, 0.02f), material),
                CreateSegment(root.transform, "LowerLeft", new Vector3(-0.12f, -0.10f, 0f), new Vector3(0.05f, 0.18f, 0.02f), material),
                CreateSegment(root.transform, "LowerRight", new Vector3(0.12f, -0.10f, 0f), new Vector3(0.05f, 0.18f, 0.02f), material),
                CreateSegment(root.transform, "Bottom", new Vector3(0f, -0.20f, 0f), new Vector3(0.22f, 0.05f, 0.02f), material)
            };
        }

        private static Renderer CreateSegment(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject segment = CreateBlock(parent, name, localPosition, localScale, material);
            DestroyCollider(segment);
            return segment.GetComponent<Renderer>();
        }

        private static void BuildStatusPanel(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, GeneratedAssets assets)
        {
            GameObject root = CreateChild(parent, name);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = localRotation;

            CreateBlock(root.transform, "Backplate", Vector3.zero, new Vector3(1.10f, 0.60f, 0.08f), assets.BoundaryMaterial);
            CreateBlock(root.transform, "StripeTop", new Vector3(0f, 0.12f, 0.05f), new Vector3(0.82f, 0.10f, 0.03f), assets.LaserMaterial);
            CreateBlock(root.transform, "StripeBottom", new Vector3(0f, -0.12f, 0.05f), new Vector3(0.82f, 0.10f, 0.03f), assets.GoalMaterial);
            CreateBlock(root.transform, "Indicator", new Vector3(0.34f, 0f, 0.05f), new Vector3(0.16f, 0.16f, 0.03f), assets.GoalMaterial);
        }

        private static void BuildZoneFrame(
            Transform parent,
            string name,
            Vector3 center,
            float width,
            float height,
            float depth,
            Material frameMaterial,
            Material accentMaterial)
        {
            GameObject root = CreateChild(parent, name);
            root.transform.localPosition = center;

            float halfWidth = width * 0.5f;
            CreateBlock(root.transform, "LeftPost", new Vector3(-halfWidth, height * 0.5f, 0f), new Vector3(0.16f, height, depth), frameMaterial);
            CreateBlock(root.transform, "RightPost", new Vector3(halfWidth, height * 0.5f, 0f), new Vector3(0.16f, height, depth), frameMaterial);
            CreateBlock(root.transform, "TopBeam", new Vector3(0f, height, 0f), new Vector3(width + 0.18f, 0.14f, depth), frameMaterial);
            CreateBlock(root.transform, "AccentBar", new Vector3(0f, height - 0.18f, 0.02f), new Vector3(width - 0.25f, 0.06f, depth - 0.18f), accentMaterial);
        }

        private static void BuildEmergencyBeacon(Transform parent, string name, Vector3 localPosition, Material glowMaterial, Material housingMaterial)
        {
            GameObject root = CreateChild(parent, name);
            root.transform.localPosition = localPosition;

            CreateBlock(root.transform, "Housing", Vector3.zero, new Vector3(0.32f, 0.16f, 0.24f), housingMaterial);
            CreateBlock(root.transform, "Light", new Vector3(0f, 0f, 0.08f), new Vector3(0.18f, 0.10f, 0.08f), glowMaterial);
        }

        private static void BuildStartMissionRack(Transform parent, GeneratedAssets assets)
        {
            GameObject root = CreateChild(parent, "StartMissionRack");
            root.transform.localPosition = new Vector3(-2.65f, 0f, -9.6f);

            CreateBlock(root.transform, "RackBase", new Vector3(0f, 0.12f, 0f), new Vector3(1.15f, 0.24f, 0.85f), assets.BoundaryMaterial);
            CreateBlock(root.transform, "RackTray", new Vector3(0f, 0.42f, 0f), new Vector3(0.78f, 0.08f, 0.54f), assets.DarkFloorMaterial);
            CreateBlock(root.transform, "RackAccent", new Vector3(0f, 0.54f, 0f), new Vector3(0.42f, 0.12f, 0.42f), assets.CoreMaterial);
            CreateBlock(root.transform, "RackSignal", new Vector3(0f, 0.92f, 0f), new Vector3(0.14f, 0.34f, 0.14f), assets.GoalMaterial);
        }

        private static void BuildDoorServiceBay(Transform parent, GeneratedAssets assets)
        {
            GameObject root = CreateChild(parent, "DoorServiceBay");
            root.transform.localPosition = new Vector3(3.62f, 0f, 13.05f);

            GameObject bayDeck = CreateBlock(root.transform, "BayDeck", new Vector3(0f, 0.06f, 0f), new Vector3(0.86f, 0.12f, 1.68f), assets.BoundaryMaterial);
            GameObject bayKick = CreateBlock(root.transform, "BayKick", new Vector3(0.22f, 0.34f, 0f), new Vector3(0.34f, 0.52f, 1.62f), assets.DoorPanelMaterial);
            GameObject baySignal = CreateBlock(root.transform, "BaySignal", new Vector3(-0.18f, 1.12f, -0.42f), new Vector3(0.16f, 0.28f, 0.16f), assets.GoalMaterial);
            DestroyCollider(bayDeck);
            DestroyCollider(bayKick);
            DestroyCollider(baySignal);

            PlaceOptionalPrefab(assets.CosmicPanelPrefab, root.transform, new Vector3(-0.06f, 0f, -0.34f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 0.78f);
        }

        private static SupportPropRig BuildSupportProps(Transform parent, GeneratedAssets assets)
        {
            var anchors = new List<Transform>();
            var bodies = new List<Rigidbody>();

            bodies.Add(CreateMovableSupportCart(parent, "SupportCart_MergeLeft", new Vector3(-2.05f, 0.16f, 6.45f), 1.20f, assets, out Transform mergeAnchor));
            anchors.Add(mergeAnchor);

            bodies.Add(CreateMovableSupportCart(parent, "SupportCart_DoorRight", new Vector3(2.35f, 0.16f, 12.65f), 1.35f, assets, out Transform doorAnchor));
            anchors.Add(doorAnchor);

            bodies.Add(CreateMovableSupportCart(parent, "SupportCart_GoalLeft", new Vector3(-1.45f, 0.16f, 15.55f), 1.25f, assets, out Transform goalAnchor));
            anchors.Add(goalAnchor);

            return new SupportPropRig
            {
                Bodies = bodies.ToArray(),
                SpawnAnchors = anchors.ToArray()
            };
        }

        private static Rigidbody CreateMovableSupportCart(
            Transform parent,
            string name,
            Vector3 localPosition,
            float mass,
            GeneratedAssets assets,
            out Transform spawnAnchor)
        {
            spawnAnchor = CreateAnchor(parent, $"{name}_Spawn", parent.TransformPoint(localPosition));

            GameObject cart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cart.name = name;
            cart.transform.SetParent(parent, false);
            cart.transform.localPosition = localPosition;
            cart.transform.localScale = new Vector3(0.86f, 0.28f, 0.54f);

            Renderer cartRenderer = cart.GetComponent<Renderer>();
            if (cartRenderer != null && assets.DoorPanelMaterial != null)
            {
                cartRenderer.sharedMaterial = assets.DoorPanelMaterial;
            }

            ApplyPrimaryColliderMaterial(cart, assets.PropPhysicsMaterial);

            Rigidbody rigidbody = GetOrAddComponent<Rigidbody>(cart);
            rigidbody.mass = mass;
            rigidbody.linearDamping = 0.42f;
            rigidbody.angularDamping = 4.5f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.constraints =
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationY |
                RigidbodyConstraints.FreezeRotationZ;

            GameObject topPlate = CreateBlock(cart.transform, "TopPlate", new Vector3(0f, 0.19f, 0f), new Vector3(0.72f, 0.06f, 0.42f), assets.BoundaryMaterial);
            GameObject lightStrip = CreateBlock(cart.transform, "LightStrip", new Vector3(0f, 0.05f, 0.28f), new Vector3(0.56f, 0.08f, 0.04f), assets.GoalMaterial);
            GameObject railLeft = CreateBlock(cart.transform, "RailLeft", new Vector3(-0.34f, 0.08f, 0f), new Vector3(0.05f, 0.16f, 0.42f), assets.BoundaryMaterial);
            GameObject railRight = CreateBlock(cart.transform, "RailRight", new Vector3(0.34f, 0.08f, 0f), new Vector3(0.05f, 0.16f, 0.42f), assets.BoundaryMaterial);
            DestroyCollider(topPlate);
            DestroyCollider(lightStrip);
            DestroyCollider(railLeft);
            DestroyCollider(railRight);

            return rigidbody;
        }

        private static void StyleCourierVisual(GameObject visualInstance, GeneratedAssets assets)
        {
            SetRendererActive(visualInstance.transform, "Accessories", false);
            SetRendererActive(visualInstance.transform, "Hat", true);
            SetRendererActive(visualInstance.transform, "Mustache", true);
            SetRendererActive(visualInstance.transform, "Full_body", false);
            SetRendererActive(visualInstance.transform, "Gloves", false);
            SetRendererActive(visualInstance.transform, "Glasses", true);
            SetRendererActive(visualInstance.transform, "Outerwear", false);
            SetRendererActive(visualInstance.transform, "T_Shirt", false);
            SetRendererActive(visualInstance.transform, "Pants", true);
            SetRendererActive(visualInstance.transform, "Shoes", true);
            SetRendererActive(visualInstance.transform, "Hairstyle", false);

            ApplySkinnedSlotFromPrefab(visualInstance.transform, "Faces", AgentFaceSource);
            ApplySkinnedSlotFromPrefab(visualInstance.transform, "Glasses", AgentGlassesSource);
            ApplySkinnedSlotFromPrefab(visualInstance.transform, "Mustache", AgentMustacheSource);
            ApplySkinnedSlotFromPrefab(visualInstance.transform, "Hat", AgentHelmetSource);
            ApplySkinnedSlotFromPrefab(visualInstance.transform, "Pants", AgentPantsSource);
            ApplySkinnedSlotFromPrefab(visualInstance.transform, "Shoes", AgentShoesSource);

            AssignRendererMaterial(visualInstance.transform, "Body", assets.AgentSkinMaterial);
            AssignRendererMaterial(visualInstance.transform, "Pants", assets.AgentPantMaterial);
            AssignRendererMaterial(visualInstance.transform, "Shoes", assets.AgentBootMaterial);
            AssignRendererMaterial(visualInstance.transform, "Glasses", assets.AgentVisorMaterial);
            AssignRendererMaterial(visualInstance.transform, "Mustache", assets.AgentHairMaterial);
            AssignRendererMaterial(visualInstance.transform, "Hat", assets.AgentHelmetMaterial);
        }

        private static void ApplySkinnedSlotFromPrefab(Transform visualRoot, string slotName, string prefabPath)
        {
            if (visualRoot == null || string.IsNullOrWhiteSpace(slotName) || string.IsNullOrWhiteSpace(prefabPath))
            {
                return;
            }

            Transform slot = FindDeepChild(visualRoot, slotName);
            SkinnedMeshRenderer targetRenderer = slot != null ? slot.GetComponent<SkinnedMeshRenderer>() : null;
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            SkinnedMeshRenderer sourceRenderer = sourcePrefab != null ? sourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            if (targetRenderer == null || sourceRenderer == null || sourceRenderer.sharedMesh == null)
            {
                return;
            }

            targetRenderer.sharedMesh = sourceRenderer.sharedMesh;
            targetRenderer.sharedMaterials = sourceRenderer.sharedMaterials;

            if (sourceRenderer.rootBone != null)
            {
                Transform mappedRootBone = FindDeepChild(visualRoot, sourceRenderer.rootBone.name);
                if (mappedRootBone != null)
                {
                    targetRenderer.rootBone = mappedRootBone;
                }
            }

            if (sourceRenderer.bones == null || sourceRenderer.bones.Length == 0)
            {
                return;
            }

            var mappedBones = new Transform[sourceRenderer.bones.Length];
            for (int index = 0; index < sourceRenderer.bones.Length; index++)
            {
                Transform sourceBone = sourceRenderer.bones[index];
                Transform mappedBone = sourceBone != null ? FindDeepChild(visualRoot, sourceBone.name) : null;
                if (mappedBone == null)
                {
                    return;
                }

                mappedBones[index] = mappedBone;
            }

            targetRenderer.bones = mappedBones;
        }

        private static void AssignRendererMaterial(Transform root, string childName, Material material)
        {
            if (material == null)
            {
                return;
            }

            Transform child = FindDeepChild(root, childName);
            Renderer renderer = child != null ? child.GetComponent<Renderer>() : null;
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Transform EnsureCameraAnchor(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            return CreateChild(parent, name).transform;
        }

        private static void DeleteChildIfExists(Transform parent, string name)
        {
            if (parent == null)
            {
                return;
            }

            Transform child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void AssignObjectReferenceArray(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            if (target == null)
            {
                return;
            }

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

        private static void AssignFloat(UnityEngine.Object target, string propertyName, float value)
        {
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignInt(UnityEngine.Object target, string propertyName, int value)
        {
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignVector3(UnityEngine.Object target, string propertyName, Vector3 value)
        {
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.vector3Value = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignFollowCameraProfiles(RecordingHelper recordingHelper, FollowCameraProfileData[] profiles)
        {
            if (recordingHelper == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(recordingHelper);
            SerializedProperty property = serializedObject.FindProperty("followCameraProfiles");
            if (property == null)
            {
                throw new InvalidOperationException("Property `followCameraProfiles` was not found on RecordingHelper.");
            }

            property.arraySize = profiles != null ? profiles.Length : 0;

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("cameraIndex").intValue = profiles[i].CameraIndex;
                element.FindPropertyRelative("positionOffset").vector3Value = profiles[i].PositionOffset;
                element.FindPropertyRelative("lookAtOffset").vector3Value = profiles[i].LookAtOffset;
                element.FindPropertyRelative("positionSharpness").floatValue = profiles[i].PositionSharpness;
                element.FindPropertyRelative("rotationSharpness").floatValue = profiles[i].RotationSharpness;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignBool(UnityEngine.Object target, string propertyName, bool value)
        {
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property `{propertyName}` was not found on {target.GetType().Name}.");
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRendererActive(Transform root, string childName, bool isActive)
        {
            Transform child = FindDeepChild(root, childName);
            if (child != null)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static void CreateCourierRig(Transform spine, Material suitMaterial, Material glowMaterial, Material housingMaterial)
        {
            if (spine == null || suitMaterial == null || glowMaterial == null || housingMaterial == null || spine.Find("CourierRig") != null)
            {
                return;
            }

            GameObject rigRoot = CreateChild(spine, "CourierRig");
            rigRoot.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            rigRoot.transform.localRotation = Quaternion.identity;

            GameObject backpack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backpack.name = "Backpack";
            backpack.transform.SetParent(rigRoot.transform, false);
            backpack.transform.localPosition = new Vector3(0f, 0.14f, -0.14f);
            backpack.transform.localScale = new Vector3(0.16f, 0.20f, 0.08f);
            Object.DestroyImmediate(backpack.GetComponent<Collider>());
            backpack.GetComponent<Renderer>().sharedMaterial = housingMaterial;

            GameObject packCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            packCore.name = "PackCore";
            packCore.transform.SetParent(rigRoot.transform, false);
            packCore.transform.localPosition = new Vector3(0f, 0.14f, -0.19f);
            packCore.transform.localScale = new Vector3(0.08f, 0.10f, 0.018f);
            Object.DestroyImmediate(packCore.GetComponent<Collider>());
            packCore.GetComponent<Renderer>().sharedMaterial = glowMaterial;

            GameObject lanyardLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lanyardLeft.name = "LanyardLeft";
            lanyardLeft.transform.SetParent(rigRoot.transform, false);
            lanyardLeft.transform.localPosition = new Vector3(-0.024f, 0.14f, 0.14f);
            lanyardLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            lanyardLeft.transform.localScale = new Vector3(0.012f, 0.16f, 0.01f);
            Object.DestroyImmediate(lanyardLeft.GetComponent<Collider>());
            lanyardLeft.GetComponent<Renderer>().sharedMaterial = housingMaterial;

            GameObject lanyardRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lanyardRight.name = "LanyardRight";
            lanyardRight.transform.SetParent(rigRoot.transform, false);
            lanyardRight.transform.localPosition = new Vector3(0.024f, 0.14f, 0.14f);
            lanyardRight.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            lanyardRight.transform.localScale = new Vector3(0.012f, 0.16f, 0.01f);
            Object.DestroyImmediate(lanyardRight.GetComponent<Collider>());
            lanyardRight.GetComponent<Renderer>().sharedMaterial = housingMaterial;

            GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "ResearchBadge";
            badge.transform.SetParent(rigRoot.transform, false);
            badge.transform.localPosition = new Vector3(0f, 0.02f, 0.155f);
            badge.transform.localScale = new Vector3(0.08f, 0.11f, 0.018f);
            Object.DestroyImmediate(badge.GetComponent<Collider>());
            badge.GetComponent<Renderer>().sharedMaterial = housingMaterial;

            GameObject badgeLight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badgeLight.name = "BadgeLight";
            badgeLight.transform.SetParent(rigRoot.transform, false);
            badgeLight.transform.localPosition = new Vector3(0f, 0.05f, 0.168f);
            badgeLight.transform.localScale = new Vector3(0.035f, 0.035f, 0.008f);
            Object.DestroyImmediate(badgeLight.GetComponent<Collider>());
            badgeLight.GetComponent<Renderer>().sharedMaterial = glowMaterial;

            GameObject penA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            penA.name = "PocketPenA";
            penA.transform.SetParent(rigRoot.transform, false);
            penA.transform.localPosition = new Vector3(-0.07f, 0.07f, 0.148f);
            penA.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            penA.transform.localScale = new Vector3(0.01f, 0.08f, 0.01f);
            Object.DestroyImmediate(penA.GetComponent<Collider>());
            penA.GetComponent<Renderer>().sharedMaterial = glowMaterial;

            GameObject penB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            penB.name = "PocketPenB";
            penB.transform.SetParent(rigRoot.transform, false);
            penB.transform.localPosition = new Vector3(-0.05f, 0.065f, 0.146f);
            penB.transform.localRotation = Quaternion.Euler(0f, 0f, 3f);
            penB.transform.localScale = new Vector3(0.01f, 0.07f, 0.01f);
            Object.DestroyImmediate(penB.GetComponent<Collider>());
            penB.GetComponent<Renderer>().sharedMaterial = suitMaterial;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Failed to get or add {typeof(T).Name} on `{target.name}`.");
            }

            return component;
        }

        private static void StopAllParticleSystems(GameObject root)
        {
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in systems)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static void StripDecorativePhysics(GameObject root, bool stripRigidbodies)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                Object.DestroyImmediate(collider);
            }

            if (!stripRigidbodies)
            {
                return;
            }

            Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody rigidbody in rigidbodies)
            {
                Object.DestroyImmediate(rigidbody);
            }
        }

        private sealed class GeneratedAssets
        {
            public Material DarkFloorMaterial { get; set; }
            public Material BoundaryMaterial { get; set; }
            public Material LaserMaterial { get; set; }
            public Material ShockOnMaterial { get; set; }
            public Material ShockOffMaterial { get; set; }
            public Material GoalMaterial { get; set; }
            public Material CoreMaterial { get; set; }
            public Material DoorPanelMaterial { get; set; }
            public Material AgentSkinMaterial { get; set; }
            public Material AgentSuitMaterial { get; set; }
            public Material AgentShirtMaterial { get; set; }
            public Material AgentPantMaterial { get; set; }
            public Material AgentBootMaterial { get; set; }
            public Material AgentHairMaterial { get; set; }
            public Material AgentHelmetMaterial { get; set; }
            public Material AgentVisorMaterial { get; set; }
            public Material AgentAccentMaterial { get; set; }
            public PhysicsMaterial CorePhysicsMaterial { get; set; }
            public PhysicsMaterial PropPhysicsMaterial { get; set; }
            public RuntimeAnimatorController MovementController { get; set; }
            public GameObject AgentVisualPrefab { get; set; }
            public GameObject CosmicLockerPrefab { get; set; }
            public GameObject CosmicMonitorPrefab { get; set; }
            public GameObject CosmicCratePrefab { get; set; }
            public GameObject CosmicPanelPrefab { get; set; }
            public GameObject SteamFxPrefab { get; set; }
            public GameObject GoalFxPrefab { get; set; }
            public GameObject ExplosionFxPrefab { get; set; }
            public GameObject FireExplosionFxPrefab { get; set; }
        }

        private sealed class PickupVisuals
        {
            public Transform CoreSpawnAnchor { get; set; }
            public Rigidbody ObjectiveCore { get; set; }
            public Renderer CoreRenderer { get; set; }
        }

        private sealed class SupportPropRig
        {
            public Rigidbody[] Bodies { get; set; }
            public Transform[] SpawnAnchors { get; set; }
        }

        private sealed class GoalVisuals
        {
            public Renderer GlowRenderer { get; set; }
            public ParticleSystem IgnitionFx { get; set; }
            public ParticleSystem ShockFx { get; set; }
            public Renderer[] AlertBeaconRenderers { get; set; }
            public Renderer CountdownFillRenderer { get; set; }
            public Transform CountdownFill { get; set; }
            public Renderer[] CountdownDigitRenderers { get; set; }
            public ParticleSystem MeltdownFx { get; set; }
            public ParticleSystem MeltdownDebrisFx { get; set; }
            public Transform MeltdownOrigin { get; set; }
        }

        private sealed class GoalAlertRig
        {
            public Renderer[] AlertBeaconRenderers { get; set; }
            public Renderer CountdownFillRenderer { get; set; }
            public Transform CountdownFill { get; set; }
            public Renderer[] CountdownDigitRenderers { get; set; }
            public ParticleSystem MeltdownFx { get; set; }
            public ParticleSystem MeltdownDebrisFx { get; set; }
            public Transform MeltdownOrigin { get; set; }
        }

        private readonly struct FollowCameraProfileData
        {
            private FollowCameraProfileData(int cameraIndex, Vector3 positionOffset, Vector3 lookAtOffset, float positionSharpness, float rotationSharpness)
            {
                CameraIndex = cameraIndex;
                PositionOffset = positionOffset;
                LookAtOffset = lookAtOffset;
                PositionSharpness = positionSharpness;
                RotationSharpness = rotationSharpness;
            }

            public int CameraIndex { get; }
            public Vector3 PositionOffset { get; }
            public Vector3 LookAtOffset { get; }
            public float PositionSharpness { get; }
            public float RotationSharpness { get; }

            public static FollowCameraProfileData Create(int cameraIndex, Vector3 positionOffset, Vector3 lookAtOffset, float positionSharpness, float rotationSharpness)
            {
                return new FollowCameraProfileData(cameraIndex, positionOffset, lookAtOffset, positionSharpness, rotationSharpness);
            }
        }
    }
}
