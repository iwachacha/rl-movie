using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.MLAgents.Policies;

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
        private const string TrainingRequirementsRelativePath = "Notebooks/mlagents-training-requirements.txt";
        private static readonly string[] ColabBuildDefines = { "APP_UI_EDITOR_ONLY" };
        // Colab training is always headless. Require the dedicated server subtarget
        // so the exported Linux build does not depend on desktop UI libraries that
        // are often unavailable in Colab runtimes.
        private const StandaloneBuildSubtarget ColabSubtarget = StandaloneBuildSubtarget.Server;

        public sealed class ColabBuildResult
        {
            public bool Success { get; set; }

            public string SceneName { get; set; }

            public string BuildDir { get; set; }

            public string ExecutablePath { get; set; }

            public string ZipPath { get; set; }

            public string ManifestPath { get; set; }

            public string[] YamlFiles { get; set; } = Array.Empty<string>();

            public StandaloneBuildSubtarget UsedSubtarget { get; set; }

            public string Message { get; set; }
        }

        [MenuItem("RLMovie/Build for Colab (Current Scene)")]
        public static void BuildCurrentScene()
        {
            ColabBuildResult result = BuildCurrentSceneInternal(interactive: true);
            if (!result.Success)
            {
                return;
            }
        }

        public static ColabBuildResult BuildCurrentSceneSilent()
        {
            return BuildCurrentSceneInternal(interactive: false);
        }

        private static ColabBuildResult BuildCurrentSceneInternal(bool interactive)
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                return Fail("シーンが未保存です。先にシーンを保存してください。", interactive);
            }

            var validationReport = ScenarioValidator.ValidateCurrentScene(true);
            if (!validationReport.IsValid)
            {
                return Fail(
                    $"Validator で {validationReport.Errors.Count} 件のエラーが見つかりました。\nConsole を確認して修正してから再実行してください。",
                    interactive);
            }

            string behaviorTypeError = ValidateTrainingBehaviorTypesForCurrentScene();
            if (!string.IsNullOrEmpty(behaviorTypeError))
            {
                return Fail(behaviorTypeError, interactive);
            }

            string sceneName = activeScene.name;
            return Build(sceneName, new[] { activeScene.path }, interactive);
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
                ColabBuildResult result = Build(sceneName, new[] { scenePath }, interactive: false);
                if (result.Success)
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

        private static ColabBuildResult Build(string sceneName, string[] scenePaths, bool interactive)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!TryResolveScenarioFiles(projectRoot, sceneName, out string configDir, out string manifestPath, out string[] yamlFiles, out string errorMessage))
            {
                Debug.LogError($"[BuildForColab] ❌ {sceneName}: {errorMessage}");
                return Fail(errorMessage, interactive, sceneName, manifestPath, yamlFiles);
            }

            string buildDir = Path.Combine(projectRoot, BuildRootDir, sceneName);
            string executablePath = Path.Combine(buildDir, $"{sceneName}.x86_64");
            string uploadDir = Path.Combine(projectRoot, DriveUploadDir);
            string zipPath = Path.Combine(uploadDir, $"{sceneName}.zip");
            if (!TryResolveTrainingRequirementsPath(projectRoot, out string trainingRequirementsPath, out string trainingRequirementsError))
            {
                return Fail(
                    trainingRequirementsError,
                    interactive,
                    sceneName,
                    manifestPath,
                    yamlFiles);
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
            {
                return Fail(
                    "Linux build support is not installed in this Unity Editor. Add Linux Build Support and, if available in your Unity version, Linux Dedicated Server Build Support in Unity Hub, then retry.",
                    interactive,
                    sceneName,
                    manifestPath,
                    yamlFiles);
            }

            StandaloneBuildSubtarget previousSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
            try
            {
                BuildReport report = BuildDedicatedServerPlayer(
                    sceneName,
                    scenePaths,
                    buildDir,
                    executablePath);

                if (report.summary.result != BuildResult.Succeeded)
                {
                    string buildFailureDetail = GetLatestBuildFailureDetail();
                    if (IsMissingDedicatedServerSupportError(buildFailureDetail))
                    {
                        return Fail(
                            "Build for Colab requires Linux Dedicated Server Build Support in this Unity Editor.\n\nRegular Linux player builds can succeed locally but crash in Colab headless runtimes because desktop UI libraries are missing.\n\nOpen Unity Hub > Installs > Add modules and install both Linux Build Support (IL2CPP/Mono as needed) and Linux Dedicated Server Build Support, then rebuild.",
                            interactive,
                            sceneName,
                            manifestPath,
                            yamlFiles);
                    }

                    return Fail(
                        $"ビルドに失敗しました: {report.summary.result}\n{buildFailureDetail}",
                        interactive,
                        sceneName,
                        manifestPath,
                        yamlFiles);
                }

                PruneHeadlessIncompatibleArtifacts(buildDir);

                if (!TryValidateHeadlessBuildOutputs(buildDir, out string headlessBuildError))
                {
                    return Fail(
                        headlessBuildError,
                        interactive,
                        sceneName,
                        manifestPath,
                        yamlFiles);
                }

                Debug.Log($"[BuildForColab] ✅ Build succeeded: {executablePath}");
                Debug.Log($"[BuildForColab] Build subtarget: {ColabSubtarget}");

                EditorUtility.DisplayProgressBar("Building for Colab", "Copying scenario files...", 0.7f);
                CopyScenarioFiles(sceneName, buildDir, configDir, manifestPath, yamlFiles, trainingRequirementsPath);

                EditorUtility.DisplayProgressBar("Building for Colab", "Creating ZIP package...", 0.9f);
                Directory.CreateDirectory(uploadDir);

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                ZipFile.CreateFromDirectory(buildDir, zipPath);
            }
            finally
            {
                if (EditorUserBuildSettings.standaloneBuildSubtarget != previousSubtarget)
                {
                    EditorUserBuildSettings.standaloneBuildSubtarget = previousSubtarget;
                    Debug.Log($"[BuildForColab] Restored standalone subtarget to {previousSubtarget}.");
                }

                EditorUtility.ClearProgressBar();
            }

            long zipSizeMB = new FileInfo(zipPath).Length / (1024 * 1024);
            Debug.Log($"[BuildForColab] 📦 ZIP created: {zipPath} ({zipSizeMB} MB)");
            Debug.Log($"[BuildForColab] 📁 Upload this ZIP to Google Drive: RL-Movie/Builds/");

            string modeSummary = "Dedicated Server";
            string message = $"✅ {sceneName} のビルドが完了しました\n\nMode: {modeSummary}\nZIP: {Path.GetFullPath(zipPath)} ({zipSizeMB} MB)\nmanifest: {Path.GetFileName(manifestPath)}\nYAML: {string.Join(", ", yamlFiles.Select(Path.GetFileName))}";
            if (interactive)
            {
                EditorUtility.DisplayDialog("Build Complete", message, "OK");
            }

            return new ColabBuildResult
            {
                Success = true,
                SceneName = sceneName,
                BuildDir = buildDir,
                ExecutablePath = executablePath,
                ZipPath = zipPath,
                ManifestPath = manifestPath,
                YamlFiles = yamlFiles,
                UsedSubtarget = ColabSubtarget,
                Message = message
            };
        }

        private static BuildReport BuildDedicatedServerPlayer(
            string sceneName,
            string[] scenePaths,
            string buildDir,
            string executablePath)
        {
            PrepareBuildDirectory(buildDir);
            Debug.Log($"[BuildForColab] 🔨 Building {sceneName} for Linux ({ColabSubtarget})...");

            if (EditorUserBuildSettings.standaloneBuildSubtarget != ColabSubtarget)
            {
                Debug.Log(
                    $"[BuildForColab] Forcing standalone subtarget from {EditorUserBuildSettings.standaloneBuildSubtarget} to {ColabSubtarget} for Colab build.");
                EditorUserBuildSettings.standaloneBuildSubtarget = ColabSubtarget;
            }

            EditorUtility.DisplayProgressBar("Building for Colab", $"Building {sceneName} ({ColabSubtarget})...", 0.3f);

            return BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = executablePath,
                targetGroup = BuildTargetGroup.Standalone,
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)ColabSubtarget,
                extraScriptingDefines = ColabBuildDefines,
                options = BuildOptions.None
            });
        }

        private static void PrepareBuildDirectory(string buildDir)
        {
            if (Directory.Exists(buildDir))
            {
                Directory.Delete(buildDir, true);
            }

            Directory.CreateDirectory(buildDir);
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

        private static void CopyScenarioFiles(
            string sceneName,
            string buildDir,
            string configDir,
            string manifestPath,
            string[] yamlFiles,
            string trainingRequirementsPath)
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

            string requirementsDest = Path.Combine(targetConfigDir, "training_requirements.txt");
            File.Copy(trainingRequirementsPath, requirementsDest, true);
            Debug.Log($"[BuildForColab] 📋 Training requirements copied: {Path.GetFileName(requirementsDest)}");

            string readmePath = Path.Combine(targetConfigDir, "README.txt");
            File.WriteAllText(
                readmePath,
                $"Scenario: {sceneName}{Environment.NewLine}Manifest: {Path.GetFileName(manifestPath)}{Environment.NewLine}Configs: {string.Join(", ", yamlFiles.Select(Path.GetFileName))}{Environment.NewLine}Training requirements: training_requirements.txt{Environment.NewLine}");
        }

        private static ColabBuildResult Fail(string message, bool interactive, string sceneName = null, string manifestPath = null, string[] yamlFiles = null)
        {
            if (interactive)
            {
                EditorUtility.DisplayDialog("Build Blocked", message, "OK");
            }

            return new ColabBuildResult
            {
                Success = false,
                SceneName = sceneName,
                ManifestPath = manifestPath,
                YamlFiles = yamlFiles ?? Array.Empty<string>(),
                Message = message
            };
        }

        private static string GetLatestBuildFailureDetail()
        {
            try
            {
                foreach (string logPath in GetCandidateLogPaths())
                {
                    if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
                    {
                        continue;
                    }

                    string[] lines = ReadAllLinesShared(logPath);
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        string line = lines[i].Trim();
                        if (line.StartsWith("Error building Player:", StringComparison.Ordinal))
                        {
                            return line;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildForColab] Failed to read Editor.log for build failure details: {ex.Message}");
            }

            return "Console または Editor.log を確認してください。";
        }

        private static bool IsMissingDedicatedServerSupportError(string buildFailureDetail)
        {
            if (string.IsNullOrWhiteSpace(buildFailureDetail))
            {
                return false;
            }

            string normalized = buildFailureDetail.ToLowerInvariant();
            return normalized.Contains("dedicated server support")
                && normalized.Contains("linux")
                && normalized.Contains("not installed");
        }

        private static bool TryResolveTrainingRequirementsPath(
            string projectRoot,
            out string trainingRequirementsPath,
            out string errorMessage)
        {
            string[] candidates = new[]
            {
                Path.Combine(projectRoot, TrainingRequirementsRelativePath),
                Path.GetFullPath(Path.Combine(projectRoot, "..", TrainingRequirementsRelativePath))
            }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    trainingRequirementsPath = candidate;
                    errorMessage = string.Empty;
                    return true;
                }
            }

            trainingRequirementsPath = null;
            errorMessage =
                "Pinned training requirements file was not found. Colab builds must include training_requirements.txt so the notebook can verify package compatibility.\n\n"
                + $"Expected one of:{Environment.NewLine}"
                + string.Join(Environment.NewLine, candidates.Select(Path.GetFullPath));
            return false;
        }

        private static string ValidateTrainingBehaviorTypesForCurrentScene()
        {
            var invalidAgents = UnityEngine.Object.FindObjectsByType<RLMovie.Common.BaseRLAgent>(FindObjectsSortMode.None)
                .Select(agent => new
                {
                    AgentName = agent.gameObject.name,
                    BehaviorParameters = agent.GetComponent<BehaviorParameters>()
                })
                .Where(entry => entry.BehaviorParameters != null && entry.BehaviorParameters.BehaviorType != BehaviorType.Default)
                .Select(entry => $"{entry.AgentName}: {entry.BehaviorParameters.BehaviorType}")
                .ToArray();

            if (invalidAgents.Length == 0)
            {
                return string.Empty;
            }

            return
                "Build for Colab requires every training agent to use Behavior Type = Default.\n\n"
                + "Default still supports heuristic play when no trainer or model is attached, so it is the safe baseline for both manual checks and Colab training.\n\n"
                + "Agents with invalid Behavior Type:\n"
                + string.Join(Environment.NewLine, invalidAgents);
        }

        private static bool TryValidateHeadlessBuildOutputs(string buildDir, out string errorMessage)
        {
            bool hasForbiddenRuntimeFiles =
                Directory.GetFiles(buildDir, "Unity.AppUI*.dll", SearchOption.AllDirectories).Any()
                || Directory.GetFiles(buildDir, "libAppUINativePlugin.so", SearchOption.AllDirectories).Any();

            if (!hasForbiddenRuntimeFiles)
            {
                errorMessage = string.Empty;
                return true;
            }

            string[] found = Directory.GetFiles(buildDir, "Unity.AppUI*.dll", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(buildDir, "libAppUINativePlugin.so", SearchOption.AllDirectories))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            errorMessage =
                "Colab headless build still contains App UI runtime binaries after compilation.\n\n"
                + "These files trigger libgtk-dependent native initialization in Colab and will crash or hang training.\n\n"
                + "Found files:\n"
                + string.Join(Environment.NewLine, found)
                + "\n\nExpected build define:\n"
                + string.Join(", ", ColabBuildDefines);
            return false;
        }

        private static void PruneHeadlessIncompatibleArtifacts(string buildDir)
        {
            string[] appUiAssemblies = Directory.GetFiles(buildDir, "Unity.AppUI*.dll", SearchOption.AllDirectories);
            string[] nativePlugins = Directory.GetFiles(buildDir, "libAppUINativePlugin.so", SearchOption.AllDirectories);

            if (nativePlugins.Length == 0)
            {
                return;
            }

            // Dedicated server Colab builds should never ship the Linux App UI native plugin.
            // If the managed App UI runtime has already been excluded by APP_UI_EDITOR_ONLY,
            // we can safely strip the orphaned native binary from the exported player.
            if (appUiAssemblies.Length == 0)
            {
                foreach (string pluginPath in nativePlugins)
                {
                    File.Delete(pluginPath);
                    Debug.Log($"[BuildForColab] Removed headless-incompatible native plugin: {pluginPath}");
                }

                return;
            }

            Debug.LogWarning(
                "[BuildForColab] App UI managed assemblies are still present in the exported build, so libAppUINativePlugin.so was not auto-removed.");
        }

        private static string[] GetCandidateLogPaths()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return new[]
            {
                Application.consoleLogPath,
                string.IsNullOrWhiteSpace(localAppData) ? null : Path.Combine(localAppData, "Unity", "Editor", "Editor.log")
            };
        }

        private static string[] ReadAllLinesShared(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            string contents = reader.ReadToEnd();
            return contents.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

    }
}
