using UnityEditor;
using UnityEngine;

namespace RLMovie.Editor
{
    /// <summary>
    /// Builds the starter scene for GoldenSpineSmoke after the generated files compile.
    /// </summary>
    public static class GoldenSpineSmokeSceneBuilder
    {
        [MenuItem("RLMovie/Create GoldenSpineSmoke Scene")]
        public static void CreateScene()
        {
            CreateSceneSilently();

            EditorUtility.DisplayDialog(
                "GoldenSpineSmoke Scene",
                "Starter scene created.\n\nNext step:\nCustomize the generated agent, scene visuals, and manifest for the real scenario contract.",
                "OK");
        }

        public static string CreateSceneSilently()
        {
            GoldenScenarioSceneBuilder.CreateStarterScene<RLMovie.Environments.GoldenSpineSmoke.GoldenSpineSmokeAgent>(
                "Assets/_RLMovie/Environments/GoldenSpineSmoke/Scenes/GoldenSpineSmoke.unity",
                "GoldenSpineSmokeAgent",
                13,
                2,
                (context, agent) =>
                {
                    var agentSo = new SerializedObject(agent);
                    agentSo.FindProperty("goal").objectReferenceValue = context.Goal;
                    agentSo.FindProperty("envManager").objectReferenceValue = context.EnvironmentManager;
                    agentSo.FindProperty("moveForce").floatValue = 1.0f;
                    agentSo.FindProperty("goalDistance").floatValue = 1.25f;
                    agentSo.FindProperty("startPosition").vector3Value = new Vector3(0f, 0.5f, 0f);
                    agentSo.ApplyModifiedProperties();
                });

            Debug.Log("Created the golden starter scene for GoldenSpineSmoke at Assets/_RLMovie/Environments/GoldenSpineSmoke/Scenes/GoldenSpineSmoke.unity");
            return "Assets/_RLMovie/Environments/GoldenSpineSmoke/Scenes/GoldenSpineSmoke.unity";
        }
    }
}
