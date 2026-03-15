using System;
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
    /// Creates the shared scene backbone used by new scenarios.
    /// Scenario-specific builders can layer visuals and task logic on top.
    /// </summary>
    public static class GoldenScenarioSceneBuilder
    {
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

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var context = BuildSharedBackbone<TAgent>(scene, behaviorName, vectorObservationSize, actionSpec);

            configureScenario?.Invoke(context, (TAgent)context.Agent);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return context;
        }

        private static GoldenScenarioSceneContext BuildSharedBackbone<TAgent>(
            Scene scene,
            string behaviorName,
            int vectorObservationSize,
            ActionSpec actionSpec)
            where TAgent : BaseRLAgent
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                throw new InvalidOperationException("Starter scene requires a Main Camera.");
            }

            mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;

            GameObject environmentRoot = new GameObject("EnvironmentRoot");
            var environmentManager = environmentRoot.AddComponent<EnvironmentManager>();
            var goldenSpine = environmentRoot.AddComponent<ScenarioGoldenSpine>();

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(environmentRoot.transform);
            floor.transform.localPosition = Vector3.zero;

            GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goal.name = "Goal";
            goal.transform.SetParent(environmentRoot.transform);
            goal.transform.localPosition = new Vector3(3f, 0.5f, 3f);
            goal.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            GameObject agentObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            agentObject.name = "Agent";
            agentObject.transform.SetParent(environmentRoot.transform);
            agentObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            var agentRigidbody = agentObject.AddComponent<Rigidbody>();
            agentRigidbody.mass = 1f;
            agentRigidbody.angularDamping = 0.5f;

            var agent = agentObject.AddComponent<TAgent>();
            var decisionRequester = agentObject.GetComponent<DecisionRequester>() ?? agentObject.AddComponent<DecisionRequester>();
            decisionRequester.DecisionPeriod = 5;

            // ML-Agents Agent may auto-add BehaviorParameters, so reuse it instead of creating duplicates.
            var behaviorParameters = agentObject.GetComponent<BehaviorParameters>() ?? agentObject.AddComponent<BehaviorParameters>();
            behaviorParameters.BehaviorName = behaviorName;
            behaviorParameters.BehaviorType = BehaviorType.Default;
            behaviorParameters.BrainParameters.VectorObservationSize = vectorObservationSize;
            behaviorParameters.BrainParameters.ActionSpec = actionSpec;

            GameObject cameraRig = new GameObject("CameraRig");
            cameraRig.transform.SetParent(environmentRoot.transform);

            Transform defaultCameraView = CreateCameraAnchor(cameraRig.transform, "DefaultView", new Vector3(0f, 11f, -8f), Quaternion.Euler(50f, 0f, 0f));
            Transform recordLeft = CreateCameraAnchor(cameraRig.transform, "RecordWideLeft", new Vector3(-7f, 7f, -7f), Quaternion.Euler(35f, 45f, 0f));
            Transform recordRight = CreateCameraAnchor(cameraRig.transform, "RecordWideRight", new Vector3(7f, 7f, -7f), Quaternion.Euler(35f, -45f, 0f));

            mainCamera.transform.position = defaultCameraView.position;
            mainCamera.transform.rotation = defaultCameraView.rotation;

            GameObject visualizerObject = new GameObject("TrainingVisualizer");
            visualizerObject.transform.SetParent(environmentRoot.transform);
            var trainingVisualizer = visualizerObject.AddComponent<TrainingVisualizer>();

            GameObject recordingObject = new GameObject("RecordingHelper");
            recordingObject.transform.SetParent(environmentRoot.transform);
            var recordingHelper = recordingObject.AddComponent<RecordingHelper>();

            AssignObjectReference(trainingVisualizer, "targetAgent", agent);
            AssignObjectReference(recordingHelper, "cameraPositions", new[] { recordLeft, recordRight });
            AssignBool(recordingHelper, "enableCameraSwitching", true);
            AssignBool(recordingHelper, "hideUIWhenRecording", true);

            AssignObjectReference(goldenSpine, "environmentRoot", environmentRoot.transform);
            AssignObjectReference(goldenSpine, "primaryAgent", agent);
            AssignObjectReference(goldenSpine, "primaryGoal", goal.transform);
            AssignObjectReference(goldenSpine, "environmentManager", environmentManager);
            AssignObjectReference(goldenSpine, "trainingVisualizer", trainingVisualizer);
            AssignObjectReference(goldenSpine, "recordingHelper", recordingHelper);
            AssignObjectReference(goldenSpine, "defaultCameraView", defaultCameraView);
            AssignObjectReference(goldenSpine, "recordingCameraViews", new[] { recordLeft, recordRight });

            return new GoldenScenarioSceneContext(
                scene,
                goldenSpine,
                environmentRoot,
                environmentManager,
                agent,
                goal.transform,
                mainCamera,
                trainingVisualizer,
                recordingHelper,
                defaultCameraView,
                new[] { recordLeft, recordRight });
        }

        private static Transform CreateCameraAnchor(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent);
            anchor.transform.position = position;
            anchor.transform.rotation = rotation;
            return anchor.transform;
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
    }

    public sealed class GoldenScenarioSceneContext
    {
        public GoldenScenarioSceneContext(
            Scene scene,
            ScenarioGoldenSpine goldenSpine,
            GameObject environmentRoot,
            EnvironmentManager environmentManager,
            BaseRLAgent agent,
            Transform goal,
            Camera mainCamera,
            TrainingVisualizer trainingVisualizer,
            RecordingHelper recordingHelper,
            Transform defaultCameraView,
            Transform[] recordingCameraViews)
        {
            Scene = scene;
            GoldenSpine = goldenSpine;
            EnvironmentRoot = environmentRoot;
            EnvironmentManager = environmentManager;
            Agent = agent;
            Goal = goal;
            MainCamera = mainCamera;
            TrainingVisualizer = trainingVisualizer;
            RecordingHelper = recordingHelper;
            DefaultCameraView = defaultCameraView;
            RecordingCameraViews = recordingCameraViews;
        }

        public Scene Scene { get; }

        public ScenarioGoldenSpine GoldenSpine { get; }

        public GameObject EnvironmentRoot { get; }

        public EnvironmentManager EnvironmentManager { get; }

        public BaseRLAgent Agent { get; }

        public Transform Goal { get; }

        public Camera MainCamera { get; }

        public TrainingVisualizer TrainingVisualizer { get; }

        public RecordingHelper RecordingHelper { get; }

        public Transform DefaultCameraView { get; }

        public Transform[] RecordingCameraViews { get; }
    }
}
