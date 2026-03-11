using UnityEngine;
using UnityEditor;
using Unity.MLAgents.Policies;
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
    /// 2. 現在のシーンのエージェントの Behavior Type を Inference Only に変更
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

            string modelFileName = Path.GetFileName(modelPath);

            // StreamingAssets にコピー
            string streamingAssetsDir = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsDir))
                Directory.CreateDirectory(streamingAssetsDir);

            string destPath = Path.Combine(streamingAssetsDir, modelFileName);
            File.Copy(modelPath, destPath, true);
            Debug.Log($"📥 Model copied to: {destPath}");

            // Unity のアセットとしてリフレッシュ
            AssetDatabase.Refresh();

            string assetPath = "Assets/StreamingAssets/" + modelFileName;
            Debug.Log($"✅ Model imported: {assetPath}");

            // シーン内のエージェントを検索
            var behaviorParams = Object.FindObjectsByType<BehaviorParameters>(FindObjectsSortMode.None);

            if (behaviorParams.Length == 0)
            {
                EditorUtility.DisplayDialog("Import Complete",
                    $"✅ モデルを StreamingAssets にコピーしました。\n\n" +
                    $"ファイル: {modelFileName}\n\n" +
                    "シーンにエージェントが見つかりませんでした。\n" +
                    "手動でエージェントの Behavior Parameters > Model にセットしてください。",
                    "OK");
                return;
            }

            // すべてのエージェントを Inference Only に切替
            foreach (var bp in behaviorParams)
            {
                var so = new SerializedObject(bp);
                so.FindProperty("m_BehaviorType").intValue = (int)BehaviorType.InferenceOnly;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(bp);
                Debug.Log($"🧠 {bp.gameObject.name}: Behavior Type → Inference Only");
            }

            string agentNames = string.Join(", ", behaviorParams.Select(bp => bp.gameObject.name));

            EditorUtility.DisplayDialog("Import Complete",
                $"✅ モデルのインポートが完了しました！\n\n" +
                $"📄 モデル: {modelFileName}\n" +
                $"🤖 エージェント: {agentNames}\n" +
                $"   → Behavior Type を Inference Only に変更済み\n\n" +
                "⚠️ 残りの手動作業:\n" +
                "  Inspector の Behavior Parameters > Model に\n" +
                $"  {modelFileName} をドラッグ&ドロップしてください。\n\n" +
                "その後 Play ボタンで動作確認 → Unity Recorder で録画！",
                "OK");
        }
    }
}
