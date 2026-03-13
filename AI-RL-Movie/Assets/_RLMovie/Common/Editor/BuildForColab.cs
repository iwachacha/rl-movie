using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace RLMovie.Editor
{
    /// <summary>
    /// Colab 用 Linux ビルドを自動作成するエディタメニュー。
    /// メニュー: RLMovie > Build for Colab
    ///
    /// 現在シーンでは以下を行う:
    /// 1. Validator を通す
    /// 2. Linux Standalone としてビルド
    /// 3. Config 配下の YAML と manifest を同梱
    /// 4. ZIP にまとめてアップロード用フォルダへ出力
    /// </summary>
    public static class BuildForColab
    {
        private const string BuildRootDir = "ColabBuilds";
        private const string DriveUploadDir = "ColabBuilds/_ReadyToUpload";

        [MenuItem("RLMovie/Build for Colab (Current Scene)")]
        public static void BuildCurrentScene()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                EditorUtility.DisplayDialog("Error", "シーンが未保存です。先にシーンを保存してください。", "OK");
                return;
            }

            var validationReport = ScenarioValidator.ValidateCurrentScene(true);
            if (!validationReport.IsValid)
            {
                EditorUtility.DisplayDialog(
                    "Build Blocked",
                    $"Validator で {validationReport.Errors.Count} 件のエラーが見つかりました。\nConsole を確認して修正してから再実行してください。",
                    "OK");
                return;
            }

            string sceneName = activeScene.name;
            Build(sceneName, new[] { activeScene.path });
        }

        [MenuItem("RLMovie/Build All Scenes for Colab")]
        public static void BuildAllScenes()
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_RLMovie/Environments" });
            if (sceneGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Assets/_RLMovie/Environments にシーンが見つかりません。", "OK");
                return;
            }

            int builtCount = 0;
            int skippedCount = 0;

            foreach (var guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                bool success = Build(sceneName, new[] { scenePath });
                if (success)
                {
                    builtCount++;
                }
                else
                {
                    skippedCount++;
                }
            }

            EditorUtility.DisplayDialog(
                "Build Complete",
                $"成功: {builtCount} シーン\nスキップ: {skippedCount} シーン\n\n出力先: {Path.GetFullPath(DriveUploadDir)}",
                "OK");
        }

        private static bool Build(string sceneName, string[] scenePaths)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!TryResolveScenarioFiles(projectRoot, sceneName, out string configDir, out string manifestPath, out string[] yamlFiles, out string errorMessage))
            {
                Debug.LogError($"[BuildForColab] ❌ {sceneName}: {errorMessage}");
                EditorUtility.DisplayDialog("Build Blocked", errorMessage, "OK");
                return false;
            }

            string buildDir = Path.Combine(projectRoot, BuildRootDir, sceneName);
            string executablePath = Path.Combine(buildDir, $"{sceneName}.x86_64");

            if (Directory.Exists(buildDir))
            {
                Directory.Delete(buildDir, true);
            }
            Directory.CreateDirectory(buildDir);

            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = executablePath,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildForColab] 🔨 Building {sceneName} for Linux...");
            EditorUtility.DisplayProgressBar("Building for Colab", $"Building {sceneName}...", 0.3f);

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "Build Failed",
                    $"ビルドに失敗しました: {report.summary.result}\nConsole を確認してください。",
                    "OK");
                return false;
            }

            Debug.Log($"[BuildForColab] ✅ Build succeeded: {executablePath}");

            EditorUtility.DisplayProgressBar("Building for Colab", "Copying scenario files...", 0.7f);
            CopyScenarioFiles(sceneName, buildDir, configDir, manifestPath, yamlFiles);

            EditorUtility.DisplayProgressBar("Building for Colab", "Creating ZIP package...", 0.9f);
            string uploadDir = Path.Combine(projectRoot, DriveUploadDir);
            Directory.CreateDirectory(uploadDir);

            string zipPath = Path.Combine(uploadDir, $"{sceneName}.zip");
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(buildDir, zipPath);
            EditorUtility.ClearProgressBar();

            long zipSizeMB = new FileInfo(zipPath).Length / (1024 * 1024);
            Debug.Log($"[BuildForColab] 📦 ZIP created: {zipPath} ({zipSizeMB} MB)");
            Debug.Log($"[BuildForColab] 📁 Upload this ZIP to Google Drive: RL-Movie/Builds/");

            EditorUtility.DisplayDialog(
                "Build Complete",
                $"✅ {sceneName} のビルドが完了しました\n\nZIP: {Path.GetFullPath(zipPath)} ({zipSizeMB} MB)\nmanifest: {Path.GetFileName(manifestPath)}\nYAML: {string.Join(", ", yamlFiles.Select(Path.GetFileName))}",
                "OK");

            return true;
        }

        private static bool TryResolveScenarioFiles(
            string projectRoot,
            string sceneName,
            out string configDir,
            out string manifestPath,
            out string[] yamlFiles,
            out string errorMessage)
        {
            string scenarioDir = Path.Combine(projectRoot, "Assets/_RLMovie/Environments", sceneName);
            configDir = Path.Combine(scenarioDir, "Config");
            manifestPath = Path.Combine(configDir, "scenario_manifest.yaml");
            yamlFiles = Array.Empty<string>();
            errorMessage = string.Empty;

            if (!Directory.Exists(configDir))
            {
                errorMessage = $"Config フォルダが見つかりません: {configDir}";
                return false;
            }

            if (!File.Exists(manifestPath))
            {
                errorMessage = $"scenario_manifest.yaml が見つかりません: {manifestPath}";
                return false;
            }

            yamlFiles = Directory.GetFiles(configDir, "*.yaml")
                .Where(path => !string.Equals(Path.GetFileName(path), "scenario_manifest.yaml", StringComparison.OrdinalIgnoreCase))
                .Where(path => !string.Equals(Path.GetFileName(path), "template_config.yaml", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (yamlFiles.Length == 0)
            {
                errorMessage = "学習用 YAML が見つかりません。Config フォルダに scenario_manifest.yaml 以外の .yaml を配置してください。";
                return false;
            }

            return true;
        }

        private static void CopyScenarioFiles(string sceneName, string buildDir, string configDir, string manifestPath, string[] yamlFiles)
        {
            string targetConfigDir = Path.Combine(buildDir, "Config");
            Directory.CreateDirectory(targetConfigDir);

            foreach (string yamlFile in yamlFiles)
            {
                string destFile = Path.Combine(targetConfigDir, Path.GetFileName(yamlFile));
                File.Copy(yamlFile, destFile, true);
                Debug.Log($"[BuildForColab] 📋 Config copied: {Path.GetFileName(yamlFile)}");
            }

            string manifestDest = Path.Combine(targetConfigDir, Path.GetFileName(manifestPath));
            File.Copy(manifestPath, manifestDest, true);
            Debug.Log($"[BuildForColab] 📋 Manifest copied: {Path.GetFileName(manifestPath)}");

            string readmePath = Path.Combine(targetConfigDir, "README.txt");
            File.WriteAllText(
                readmePath,
                $"Scenario: {sceneName}{Environment.NewLine}Manifest: {Path.GetFileName(manifestPath)}{Environment.NewLine}Configs: {string.Join(", ", yamlFiles.Select(Path.GetFileName))}{Environment.NewLine}");
        }
    }
}
