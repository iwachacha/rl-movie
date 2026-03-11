using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace RLMovie.Editor
{
    /// <summary>
    /// Colab 用 Linux ビルドを自動作成するエディタメニュー。
    /// メニュー: RLMovie > Build for Colab
    /// 
    /// 以下をワンクリックで実行:
    /// 1. 現在のシーンを Linux Standalone としてビルド
    /// 2. 対応する YAML 設定ファイルをビルドフォルダにコピー
    /// 3. すべてを ZIP にパッケージング
    /// 4. Google Drive アップロード用フォルダに配置
    /// </summary>
    public static class BuildForColab
    {
        private const string BuildRootDir = "ColabBuilds";
        private const string DriveUploadDir = "ColabBuilds/_ReadyToUpload";

        [MenuItem("RLMovie/Build for Colab (Current Scene)")]
        public static void BuildCurrentScene()
        {
            // 現在のシーンを取得
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                EditorUtility.DisplayDialog("Error", "シーンが保存されていません。先にシーンを保存してください。", "OK");
                return;
            }

            string sceneName = activeScene.name;
            Build(sceneName, new string[] { activeScene.path });
        }

        [MenuItem("RLMovie/Build All Scenes for Colab")]
        public static void BuildAllScenes()
        {
            // _RLMovie/Environments 以下のすべてのシーンを検索
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_RLMovie/Environments" });
            if (sceneGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Assets/_RLMovie/Environments にシーンが見つかりません。", "OK");
                return;
            }

            foreach (var guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                Build(sceneName, new string[] { scenePath });
            }

            EditorUtility.DisplayDialog("Build Complete",
                $"{sceneGuids.Length} 個のシーンをビルドしました。\n\n" +
                $"出力先: {Path.GetFullPath(DriveUploadDir)}\n\n" +
                "ZIP ファイルを Google Drive の RL-Movie/Builds/ にアップロードしてください。",
                "OK");
        }

        private static void Build(string sceneName, string[] scenePaths)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            // ビルド出力先
            string buildDir = Path.Combine(projectRoot, BuildRootDir, sceneName);
            string executablePath = Path.Combine(buildDir, $"{sceneName}.x86_64");

            // 既存ビルドをクリーン
            if (Directory.Exists(buildDir))
                Directory.Delete(buildDir, true);
            Directory.CreateDirectory(buildDir);

            // ビルド設定
            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = executablePath,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };

            Debug.Log($"🔨 Building {sceneName} for Linux...");
            EditorUtility.DisplayProgressBar("Building for Colab", $"Building {sceneName}...", 0.3f);

            // ビルド実行
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Build Failed",
                    $"ビルドに失敗しました: {report.summary.result}\n\n" +
                    "Console ログを確認してください。",
                    "OK");
                return;
            }

            Debug.Log($"✅ Build succeeded: {executablePath}");

            // YAML 設定ファイルをコピー
            EditorUtility.DisplayProgressBar("Building for Colab", "Copying config files...", 0.7f);
            CopyConfigFiles(sceneName, buildDir);

            // ZIP にパッケージング
            EditorUtility.DisplayProgressBar("Building for Colab", "Creating ZIP package...", 0.9f);
            string uploadDir = Path.Combine(projectRoot, DriveUploadDir);
            Directory.CreateDirectory(uploadDir);

            string zipPath = Path.Combine(uploadDir, $"{sceneName}.zip");
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(buildDir, zipPath);

            EditorUtility.ClearProgressBar();

            long zipSizeMB = new FileInfo(zipPath).Length / (1024 * 1024);
            Debug.Log($"📦 ZIP created: {zipPath} ({zipSizeMB} MB)");
            Debug.Log($"📁 Upload this ZIP to Google Drive: RL-Movie/Builds/");

            EditorUtility.DisplayDialog("Build Complete",
                $"✅ {sceneName} のビルドが完了しました！\n\n" +
                $"📦 ZIP: {Path.GetFullPath(zipPath)} ({zipSizeMB} MB)\n\n" +
                "次のステップ:\n" +
                "1. この ZIP を Google Drive の RL-Movie/Builds/ にアップロード\n" +
                "2. Colab ノートブックを開いて実行",
                "OK");
        }

        private static void CopyConfigFiles(string sceneName, string buildDir)
        {
            string configDir = Path.Combine(buildDir, "Config");
            Directory.CreateDirectory(configDir);

            // シナリオ固有の設定を検索
            string[] configSearchPaths = new[]
            {
                $"Assets/_RLMovie/Environments/{sceneName}/Config",
                $"Assets/_RLMovie/Environments/{sceneName}",
            };

            bool configFound = false;
            foreach (var searchPath in configSearchPaths)
            {
                string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), searchPath);
                if (!Directory.Exists(fullPath)) continue;

                var yamlFiles = Directory.GetFiles(fullPath, "*.yaml");
                foreach (var yamlFile in yamlFiles)
                {
                    string destFile = Path.Combine(configDir, Path.GetFileName(yamlFile));
                    File.Copy(yamlFile, destFile, true);
                    configFound = true;
                    Debug.Log($"📋 Config copied: {Path.GetFileName(yamlFile)}");
                }
            }

            if (!configFound)
            {
                // テンプレートをコピー
                string templateConfig = Path.Combine(
                    Path.GetDirectoryName(Application.dataPath),
                    "Assets/_RLMovie/Environments/_Template/Config/template_config.yaml");

                if (File.Exists(templateConfig))
                {
                    string destFile = Path.Combine(configDir, $"{sceneName}_config.yaml");
                    File.Copy(templateConfig, destFile, true);
                    Debug.Log($"📋 Template config copied as {sceneName}_config.yaml");
                }
            }
        }
    }
}
