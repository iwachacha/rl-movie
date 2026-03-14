using RLMovie.Common;
using RLMovie.Environments.RollerBall;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RLMovie.Editor
{
    /// <summary>
    /// Builds the RollerBall sample scene with the shared Golden Spine wiring.
    /// </summary>
    public static class RollerBallSceneBuilder
    {
        private const string ScenePath = "Assets/_RLMovie/Environments/RollerBall/Scenes/RollerBall.unity";
        private const string MaterialsFolder = "Assets/_RLMovie/Environments/RollerBall/Materials";

        [MenuItem("RLMovie/Create RollerBall Scene")]
        public static void CreateScene()
        {
            CreateSceneSilently();

            EditorUtility.DisplayDialog(
                "RollerBall Scene",
                "RollerBall scene created.\n\nUse Play mode to verify heuristic movement before training.",
                "OK");
        }

        public static string CreateSceneSilently()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject environmentRoot = new GameObject("EnvironmentRoot");
            var goldenSpine = environmentRoot.AddComponent<ScenarioGoldenSpine>();

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(environmentRoot.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = Vector3.one;
            floor.AddComponent<FloorVisual>();

            var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMaterial.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.2f));
            floorMaterial.SetFloat("_Smoothness", 0.8f);
            floorMaterial.SetFloat("_Metallic", 0.3f);
            var floorRenderer = floor.GetComponent<Renderer>();
            floorRenderer.material = floorMaterial;

            var wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wallMaterial.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.35f));
            wallMaterial.SetFloat("_Smoothness", 0.5f);

            var wallRenderers = new[]
            {
                CreateWall(environmentRoot.transform, wallMaterial, "WallN", new Vector3(0f, 0.5f, 5f), new Vector3(10f, 1f, 0.2f)),
                CreateWall(environmentRoot.transform, wallMaterial, "WallS", new Vector3(0f, 0.5f, -5f), new Vector3(10f, 1f, 0.2f)),
                CreateWall(environmentRoot.transform, wallMaterial, "WallE", new Vector3(5f, 0.5f, 0f), new Vector3(0.2f, 1f, 10f)),
                CreateWall(environmentRoot.transform, wallMaterial, "WallW", new Vector3(-5f, 0.5f, 0f), new Vector3(0.2f, 1f, 10f))
            };

            GameObject agentObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            agentObject.name = "RollerAgent";
            agentObject.transform.SetParent(environmentRoot.transform);
            agentObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            var agentMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            agentMaterial.SetColor("_BaseColor", new Color(0.2f, 0.4f, 1f));
            agentMaterial.SetFloat("_Smoothness", 0.9f);
            agentMaterial.SetFloat("_Metallic", 0.5f);
            agentMaterial.EnableKeyword("_EMISSION");
            agentMaterial.SetColor("_EmissionColor", new Color(0.1f, 0.2f, 0.5f) * 1.5f);
            var agentRenderer = agentObject.GetComponent<Renderer>();
            agentRenderer.material = agentMaterial;

            var rigidbody = agentObject.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.angularDamping = 0.5f;

            var rollerAgent = agentObject.AddComponent<RollerBallAgent>();
            var decisionRequester = agentObject.AddComponent<DecisionRequester>();
            decisionRequester.DecisionPeriod = 10;

            var behaviorParameters = agentObject.GetComponent<BehaviorParameters>();
            if (behaviorParameters == null)
            {
                behaviorParameters = agentObject.AddComponent<BehaviorParameters>();
            }

            behaviorParameters.BehaviorName = nameof(RollerBallAgent);
            behaviorParameters.BehaviorType = BehaviorType.HeuristicOnly;
            behaviorParameters.BrainParameters.VectorObservationSize = 13;
            behaviorParameters.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(2);

            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = "Target";
            targetObject.transform.SetParent(environmentRoot.transform);
            targetObject.transform.localPosition = new Vector3(3f, 0.5f, 3f);
            targetObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            Object.DestroyImmediate(targetObject.GetComponent<BoxCollider>());

            var targetMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            targetMaterial.SetColor("_BaseColor", new Color(0.2f, 1f, 0.4f));
            targetMaterial.SetFloat("_Smoothness", 0.9f);
            targetMaterial.EnableKeyword("_EMISSION");
            targetMaterial.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.4f) * 2f);
            var targetRenderer = targetObject.GetComponent<Renderer>();
            targetRenderer.material = targetMaterial;
            targetObject.AddComponent<TargetVisual>();

            GameObject environmentManagerObject = new GameObject("EnvironmentManager");
            environmentManagerObject.transform.SetParent(environmentRoot.transform);
            var environmentManager = environmentManagerObject.AddComponent<EnvironmentManager>();

            var agentSo = new SerializedObject(rollerAgent);
            agentSo.FindProperty("target").objectReferenceValue = targetObject.transform;
            agentSo.FindProperty("envManager").objectReferenceValue = environmentManager;
            agentSo.FindProperty("moveForce").floatValue = 1.0f;
            agentSo.FindProperty("goalDistance").floatValue = 1.42f;
            agentSo.FindProperty("startPosition").vector3Value = new Vector3(0f, 0.5f, 0f);
            agentSo.ApplyModifiedProperties();

            Transform defaultView = null;
            Transform recordLeft = null;
            Transform recordRight = null;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 12f, -8f);
                mainCamera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
                mainCamera.backgroundColor = new Color(0.05f, 0.05f, 0.1f);

                GameObject cameraRig = new GameObject("CameraRig");
                cameraRig.transform.SetParent(environmentRoot.transform);
                defaultView = CreateCameraAnchor(cameraRig.transform, "DefaultView", mainCamera.transform.position, mainCamera.transform.rotation);
                recordLeft = CreateCameraAnchor(cameraRig.transform, "RecordWideLeft", new Vector3(-7f, 7f, -7f), Quaternion.Euler(35f, 45f, 0f));
                recordRight = CreateCameraAnchor(cameraRig.transform, "RecordWideRight", new Vector3(7f, 7f, -7f), Quaternion.Euler(35f, -45f, 0f));
            }

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional)
                {
                    continue;
                }

                light.color = new Color(1f, 0.95f, 0.9f);
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            GameObject visualizerObject = new GameObject("TrainingVisualizer");
            visualizerObject.transform.SetParent(environmentRoot.transform);
            var visualizer = visualizerObject.AddComponent<TrainingVisualizer>();
            var visualizerSo = new SerializedObject(visualizer);
            visualizerSo.FindProperty("targetAgent").objectReferenceValue = rollerAgent;
            visualizerSo.ApplyModifiedProperties();

            GameObject recordingObject = new GameObject("RecordingHelper");
            recordingObject.transform.SetParent(environmentRoot.transform);
            var recordingHelper = recordingObject.AddComponent<RecordingHelper>();
            var recordingSo = new SerializedObject(recordingHelper);
            var cameraPositions = recordingSo.FindProperty("cameraPositions");
            cameraPositions.arraySize = 2;
            cameraPositions.GetArrayElementAtIndex(0).objectReferenceValue = recordLeft;
            cameraPositions.GetArrayElementAtIndex(1).objectReferenceValue = recordRight;
            recordingSo.FindProperty("enableCameraSwitching").boolValue = true;
            recordingSo.FindProperty("hideUIWhenRecording").boolValue = true;
            recordingSo.ApplyModifiedProperties();

            var spineSo = new SerializedObject(goldenSpine);
            spineSo.FindProperty("environmentRoot").objectReferenceValue = environmentRoot.transform;
            spineSo.FindProperty("primaryAgent").objectReferenceValue = rollerAgent;
            spineSo.FindProperty("primaryGoal").objectReferenceValue = targetObject.transform;
            spineSo.FindProperty("environmentManager").objectReferenceValue = environmentManager;
            spineSo.FindProperty("trainingVisualizer").objectReferenceValue = visualizer;
            spineSo.FindProperty("recordingHelper").objectReferenceValue = recordingHelper;
            spineSo.FindProperty("defaultCameraView").objectReferenceValue = defaultView;
            var recordingViews = spineSo.FindProperty("recordingCameraViews");
            recordingViews.arraySize = 2;
            recordingViews.GetArrayElementAtIndex(0).objectReferenceValue = recordLeft;
            recordingViews.GetArrayElementAtIndex(1).objectReferenceValue = recordRight;
            spineSo.ApplyModifiedProperties();

            EnsureAssetFolder("Assets/_RLMovie");
            EnsureAssetFolder("Assets/_RLMovie/Environments");
            EnsureAssetFolder("Assets/_RLMovie/Environments/RollerBall");
            EnsureAssetFolder("Assets/_RLMovie/Environments/RollerBall/Scenes");
            EnsureAssetFolder(MaterialsFolder);

            EditorSceneManager.SaveScene(scene, ScenePath);

            floorMaterial = CreateOrReplaceAsset(floorMaterial, $"{MaterialsFolder}/FloorMat.mat");
            wallMaterial = CreateOrReplaceAsset(wallMaterial, $"{MaterialsFolder}/WallMat.mat");
            agentMaterial = CreateOrReplaceAsset(agentMaterial, $"{MaterialsFolder}/AgentMat.mat");
            targetMaterial = CreateOrReplaceAsset(targetMaterial, $"{MaterialsFolder}/TargetMat.mat");
            floorRenderer.sharedMaterial = floorMaterial;
            agentRenderer.sharedMaterial = agentMaterial;
            targetRenderer.sharedMaterial = targetMaterial;
            foreach (var wallRenderer in wallRenderers)
            {
                wallRenderer.sharedMaterial = wallMaterial;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"RollerBall scene created successfully at: {ScenePath}");
            return ScenePath;
        }

        private static Renderer CreateWall(Transform parent, Material wallMaterial, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            var wallRenderer = wall.GetComponent<Renderer>();
            wallRenderer.material = wallMaterial;
            return wallRenderer;
        }

        private static Transform CreateCameraAnchor(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent);
            anchor.transform.position = position;
            anchor.transform.rotation = rotation;
            return anchor.transform;
        }

        private static Material CreateOrReplaceAsset(Material material, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(material, existing);
                Object.DestroyImmediate(material);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            int separator = assetPath.LastIndexOf('/');
            string parentPath = assetPath.Substring(0, separator);
            string folderName = assetPath.Substring(separator + 1);

            EnsureAssetFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
