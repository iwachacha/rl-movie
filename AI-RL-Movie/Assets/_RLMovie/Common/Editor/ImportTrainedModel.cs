using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Unity.InferenceEngine;
using Unity.MLAgents.Policies;
using System;
using System.IO;
using System.Linq;

namespace RLMovie.Editor
{
    /// <summary>
    /// 学習済みモデルを Unity に取り込むエディタメニュー。
    /// メニュー: RLMovie > Import Trained Model
    /// 
    /// Google Drive からダウンロードした .onnx ファイルを選択すると:
    /// 1. StreamingAssets にコピー
    /// 2. 現在シーンの manifest `behavior_name` と一致する BehaviorParameters にモデルを割り当てる
    /// 3. 対象エージェントの Behavior Type を Inference Only に変更する
    /// </summary>
    public static class ImportTrainedModel
    {
        [MenuItem("RLMovie/Import Trained Model")]
        public static void Import()
        {
            // ファイル選択ダイアログ
            string modelPath = EditorUtility.OpenFilePanel(
                "学習済みモデル (.onnx) を選択",
                "",
                "onnx");

            if (string.IsNullOrEmpty(modelPath))
                return;

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

            // StreamingAssets にコピー
            string streamingAssetsDir = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsDir))
                Directory.CreateDirectory(streamingAssetsDir);

            string destPath = Path.Combine(streamingAssetsDir, modelFileName);
            File.Copy(modelPath, destPath, true);
            Debug.Log($"📥 Model copied to: {destPath}");

            // Unity のアセットとしてリフレッシュ
            string assetPath = "Assets/StreamingAssets/" + modelFileName;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            var modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(assetPath);
            if (modelAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Import Failed",
                    $"モデルアセットの読み込みに失敗しました。\n\nAsset: {assetPath}",
                    "OK");
                return;
            }

            Debug.Log($"✅ Model imported: {assetPath}");

            foreach (var bp in targetBehaviorParams)
            {
                bp.Model = modelAsset;
                bp.BehaviorType = BehaviorType.InferenceOnly;
                EditorUtility.SetDirty(bp);
                Debug.Log($"🧠 {bp.gameObject.name}: Model → {modelFileName}, Behavior Type → Inference Only");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Selection.activeObject = modelAsset;

            string agentNames = string.Join(", ", targetBehaviorParams.Select(bp => bp.gameObject.name));

            EditorUtility.DisplayDialog("Import Complete",
                $"✅ モデルのインポートが完了しました！\n\n" +
                $"🎬 シーン: {sceneName}\n" +
                $"🏷️ Behavior: {behaviorName}\n" +
                $"📄 モデル: {modelFileName}\n" +
                $"🤖 エージェント: {agentNames}\n" +
                $"   → Model を割り当て、Behavior Type を Inference Only に変更済み\n\n" +
                "シーンは変更済みで dirty 状態です。内容を確認して保存したあと、Play で動作確認してください。",
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
                errorMessage = "シーンが未保存です。Import Trained Model の前にシーンを保存してください。";
                return false;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string manifestPath = Path.Combine(projectRoot, "Assets/_RLMovie/Environments", sceneName, "Config", "scenario_manifest.yaml");
            if (!File.Exists(manifestPath))
            {
                errorMessage =
                    "現在シーンの scenario_manifest.yaml が見つかりません。\n\n"
                    + $"Expected: {manifestPath}";
                return false;
            }

            behaviorName = ReadTopLevelScalar(manifestPath, "behavior_name");
            if (string.IsNullOrWhiteSpace(behaviorName))
            {
                errorMessage = "manifest の `behavior_name` が空です。対象エージェントを特定できません。";
                return false;
            }

            string targetBehaviorName = behaviorName;

            targetBehaviorParams = UnityEngine.Object.FindObjectsByType<BehaviorParameters>(FindObjectsSortMode.None)
                .Where(bp => bp.gameObject.scene == activeScene)
                .Where(bp => bp.GetComponent<RLMovie.Common.BaseRLAgent>() != null)
                .Where(bp => string.Equals(bp.BehaviorName, targetBehaviorName, StringComparison.Ordinal))
                .ToArray();

            if (targetBehaviorParams.Length == 0)
            {
                string availableBehaviorNames = string.Join(
                    ", ",
                    UnityEngine.Object.FindObjectsByType<BehaviorParameters>(FindObjectsSortMode.None)
                        .Where(bp => bp.gameObject.scene == activeScene)
                        .Where(bp => bp.GetComponent<RLMovie.Common.BaseRLAgent>() != null)
                        .Select(bp => bp.BehaviorName)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(name => name, StringComparer.Ordinal));

                errorMessage =
                    "manifest の `behavior_name` と一致する BehaviorParameters が現在シーンに見つかりません。\n\n"
                    + $"Scene: {sceneName}\n"
                    + $"behavior_name: {behaviorName}\n"
                    + $"Available: {availableBehaviorNames}";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static string ReadTopLevelScalar(string filePath, string key)
        {
            foreach (string line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) || char.IsWhiteSpace(line[0]))
                {
                    continue;
                }

                string trimmed = line.Trim();
                if (!trimmed.StartsWith($"{key}:", StringComparison.Ordinal))
                {
                    continue;
                }

                string value = trimmed.Substring(key.Length + 1).Trim();
                return value.Trim('"', '\'');
            }

            return string.Empty;
        }
    }
}
