using System;
using System.IO;
using System.Linq;
using Unity.InferenceEngine;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RLMovie.Editor
{
    /// <summary>
    /// Imports a trained ONNX model into StreamingAssets and assigns it to the active scene's target behavior.
    /// Menu: RLMovie > Import Trained Model
    /// </summary>
    public static class ImportTrainedModel
    {
        [MenuItem("RLMovie/Import Trained Model")]
        public static void Import()
        {
            string modelPath = EditorUtility.OpenFilePanel(
                "Import Trained Model (.onnx)",
                string.Empty,
                "onnx");

            if (string.IsNullOrEmpty(modelPath))
            {
                return;
            }

            if (!TryResolveImportTargets(
                    out string sceneName,
                    out string behaviorName,
                    out BehaviorParameters[] targetBehaviorParams,
                    out string resolveError))
            {
                EditorUtility.DisplayDialog("Import Blocked", resolveError, "OK");
                return;
            }

            string modelFileName = Path.GetFileName(modelPath);
            string streamingAssetsDir = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsDir))
            {
                Directory.CreateDirectory(streamingAssetsDir);
            }

            string destinationPath = Path.Combine(streamingAssetsDir, modelFileName);
            File.Copy(modelPath, destinationPath, true);
            Debug.Log($"[ImportTrainedModel] Model copied to: {destinationPath}");

            string assetPath = "Assets/StreamingAssets/" + modelFileName;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            ModelAsset modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(assetPath);
            if (modelAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Import Failed",
                    $"Failed to load the imported model asset.\n\nAsset: {assetPath}",
                    "OK");
                return;
            }

            foreach (BehaviorParameters behaviorParameters in targetBehaviorParams)
            {
                behaviorParameters.Model = modelAsset;
                behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
                EditorUtility.SetDirty(behaviorParameters);
                Debug.Log($"[ImportTrainedModel] {behaviorParameters.gameObject.name}: Model={modelFileName}, BehaviorType=InferenceOnly");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Selection.activeObject = modelAsset;

            string agentNames = string.Join(", ", targetBehaviorParams.Select(bp => bp.gameObject.name));
            EditorUtility.DisplayDialog(
                "Import Complete",
                $"Model import finished.\n\nScene: {sceneName}\nBehavior: {behaviorName}\nModel: {modelFileName}\nAgents: {agentNames}\n\nThe matching agents were switched to Inference Only.",
                "OK");
        }

        private static bool TryResolveImportTargets(
            out string sceneName,
            out string behaviorName,
            out BehaviorParameters[] targetBehaviorParams,
            out string errorMessage)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            sceneName = activeScene.name;
            behaviorName = string.Empty;
            targetBehaviorParams = Array.Empty<BehaviorParameters>();

            if (string.IsNullOrEmpty(activeScene.path))
            {
                errorMessage = "The active scene is unsaved. Save the scene before importing a trained model.";
                return false;
            }

            ScenarioConfigPaths paths;
            try
            {
                paths = ScenarioConfigIO.ResolveScenarioPaths(activeScene);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }

            string manifestPath = paths.ManifestPath;
            if (!File.Exists(manifestPath))
            {
                errorMessage = $"scenario_manifest.yaml was not found for the active scene.\n\nExpected: {manifestPath}";
                return false;
            }

            ScenarioManifestData manifest;
            try
            {
                manifest = ScenarioConfigIO.LoadManifest(manifestPath);
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to read the scenario manifest.\n\n{ex.Message}";
                return false;
            }

            behaviorName = manifest.BehaviorName;
            if (string.IsNullOrWhiteSpace(behaviorName))
            {
                errorMessage = "manifest.behavior_name is empty. Set the target behavior before importing a model.";
                return false;
            }

            string targetBehaviorName = behaviorName;
            targetBehaviorParams = UnityEngine.Object.FindObjectsByType<BehaviorParameters>(FindObjectsSortMode.None)
                .Where(bp => bp != null && bp.gameObject.scene == activeScene)
                .Where(bp => bp.GetComponent<RLMovie.Common.BaseRLAgent>() != null)
                .Where(bp => string.Equals(bp.BehaviorName, targetBehaviorName, StringComparison.Ordinal))
                .ToArray();

            if (targetBehaviorParams.Length == 0)
            {
                string availableBehaviorNames = string.Join(
                    ", ",
                    UnityEngine.Object.FindObjectsByType<BehaviorParameters>(FindObjectsSortMode.None)
                        .Where(bp => bp != null && bp.gameObject.scene == activeScene)
                        .Where(bp => bp.GetComponent<RLMovie.Common.BaseRLAgent>() != null)
                        .Select(bp => bp.BehaviorName)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(name => name, StringComparer.Ordinal));

                errorMessage =
                    "No agent in the active scene matches manifest.behavior_name.\n\n"
                    + $"Scene: {sceneName}\n"
                    + $"behavior_name: {behaviorName}\n"
                    + $"Available: {availableBehaviorNames}";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
