using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RLMovie.Editor
{
    /// <summary>
    /// 現在シーンの RL シナリオ契約と必須コンポーネントを検証するバリデータ。
    /// メニュー: RLMovie > Validate Current Scenario
    /// </summary>
    public static class ScenarioValidator
    {
        private static readonly string[] RequiredManifestKeys =
        {
            "scenario_name",
            "scene_name",
            "agent_class",
            "behavior_name",
            "training_config",
            "learning_goal",
            "success_conditions",
            "failure_conditions",
            "observation_contract",
            "action_contract",
            "reward_rules",
            "randomization_knobs",
            "difficulty_stages",
            "visual_theme",
            "camera_plan",
            "acceptance_criteria",
            "baseline_run",
            "spec_version"
        };

        [MenuItem("RLMovie/Validate Current Scenario")]
        public static void ValidateCurrentSceneMenu()
        {
            var report = ValidateCurrentScene(true);
            string title = report.IsValid ? "Scenario Validation" : "Scenario Validation Failed";
            string message = report.IsValid
                ? $"✅ Validation passed\n\nWarnings: {report.Warnings.Count}\nSee Console for details."
                : $"❌ Validation failed\n\nErrors: {report.Errors.Count}\nWarnings: {report.Warnings.Count}\nSee Console for details.";

            EditorUtility.DisplayDialog(title, message, "OK");
        }

        public static ScenarioValidationReport ValidateCurrentScene(bool logToConsole)
        {
            var report = new ScenarioValidationReport();
            var activeScene = SceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(activeScene.path))
            {
                report.AddError("シーンが未保存です。保存してから検証してください。");
                LogReport(report, logToConsole);
                return report;
            }

            if (activeScene.isDirty)
            {
                report.AddWarning("シーンに未保存変更があります。保存後に再検証することを推奨します。");
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string scenarioName = activeScene.name;
            string scenarioDir = Path.Combine(projectRoot, "Assets/_RLMovie/Environments", scenarioName);
            string configDir = Path.Combine(scenarioDir, "Config");
            string manifestPath = Path.Combine(configDir, "scenario_manifest.yaml");

            if (!Directory.Exists(scenarioDir))
            {
                report.AddError($"シナリオフォルダが見つかりません: {scenarioDir}");
                LogReport(report, logToConsole);
                return report;
            }

            if (!Directory.Exists(configDir))
            {
                report.AddError($"Config フォルダが見つかりません: {configDir}");
            }

            if (!File.Exists(manifestPath))
            {
                report.AddError("scenario_manifest.yaml が見つかりません。");
            }

            string[] trainingConfigs = Directory.Exists(configDir)
                ? Directory.GetFiles(configDir, "*.yaml")
                    .Where(path => !string.Equals(Path.GetFileName(path), "scenario_manifest.yaml", StringComparison.OrdinalIgnoreCase))
                    .Where(path => !string.Equals(Path.GetFileName(path), "template_config.yaml", StringComparison.OrdinalIgnoreCase))
                    .ToArray()
                : Array.Empty<string>();

            if (trainingConfigs.Length == 0)
            {
                report.AddError("学習用 YAML が見つかりません。Config フォルダに scenario_manifest.yaml 以外の .yaml を配置してください。");
            }

            string selectedTrainingConfigPath = null;
            if (File.Exists(manifestPath))
            {
                ValidateManifest(manifestPath, scenarioName, configDir, trainingConfigs, report);
                selectedTrainingConfigPath = ResolveTrainingConfigPath(manifestPath, configDir, trainingConfigs);
            }

            var agents = UnityEngine.Object.FindObjectsByType<RLMovie.Common.BaseRLAgent>(FindObjectsSortMode.None);
            if (agents.Length == 0)
            {
                report.AddError("BaseRLAgent を継承した Agent がシーン内に見つかりません。");
            }

            string[] behaviorNamesFromConfig = string.IsNullOrEmpty(selectedTrainingConfigPath)
                ? trainingConfigs
                    .SelectMany(ReadBehaviorNames)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
                : ReadBehaviorNames(selectedTrainingConfigPath)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

            foreach (var agent in agents)
            {
                ValidateAgent(agent, behaviorNamesFromConfig, manifestPath, report);
            }

            ValidateTrainingVisualizers(agents, report);
            ValidateRecordingHelpers(report);
            ValidateGoldenSpine(agents, report);

            LogReport(report, logToConsole);
            return report;
        }

        private static void ValidateManifest(
            string manifestPath,
            string scenarioName,
            string configDir,
            string[] trainingConfigs,
            ScenarioValidationReport report)
        {
            var manifestKeys = ReadTopLevelKeys(manifestPath);
            foreach (string key in RequiredManifestKeys)
            {
                if (!manifestKeys.Contains(key))
                {
                    report.AddError($"manifest に必須キー `{key}` がありません。");
                }
            }

            ValidateManifestScalar(manifestPath, "scenario_name", scenarioName, report);
            ValidateManifestScalar(manifestPath, "scene_name", scenarioName, report);

            string specVersion = ReadTopLevelScalar(manifestPath, "spec_version");
            if (string.IsNullOrWhiteSpace(specVersion))
            {
                report.AddError("manifest の `spec_version` が空です。");
            }

            string trainingConfig = ReadTopLevelScalar(manifestPath, "training_config");
            if (string.IsNullOrWhiteSpace(trainingConfig))
            {
                report.AddError("manifest の `training_config` が空です。");
                return;
            }

            if (!string.Equals(Path.GetFileName(trainingConfig), trainingConfig, StringComparison.Ordinal))
            {
                report.AddError("manifest の `training_config` は Config 直下のファイル名だけを指定してください。");
                return;
            }

            string selectedTrainingConfigPath = Path.Combine(configDir, trainingConfig);
            if (!trainingConfigs.Contains(selectedTrainingConfigPath, StringComparer.OrdinalIgnoreCase))
            {
                report.AddError($"manifest の `training_config` が Config 内の学習 YAML を指していません。現在値: `{trainingConfig}`");
            }
        }

        private static string ResolveTrainingConfigPath(string manifestPath, string configDir, string[] trainingConfigs)
        {
            string trainingConfig = ReadTopLevelScalar(manifestPath, "training_config");
            if (string.IsNullOrWhiteSpace(trainingConfig))
            {
                return null;
            }

            string selectedTrainingConfigPath = Path.Combine(configDir, trainingConfig);
            return trainingConfigs.Contains(selectedTrainingConfigPath, StringComparer.OrdinalIgnoreCase)
                ? selectedTrainingConfigPath
                : null;
        }

        private static void ValidateManifestScalar(string manifestPath, string key, string expectedValue, ScenarioValidationReport report)
        {
            string actual = ReadTopLevelScalar(manifestPath, key);
            if (!string.Equals(actual, expectedValue, StringComparison.Ordinal))
            {
                report.AddError($"manifest の `{key}` が `{expectedValue}` と一致しません。現在値: `{actual}`");
            }
        }

        private static void ValidateAgent(
            RLMovie.Common.BaseRLAgent agent,
            string[] behaviorNamesFromConfig,
            string manifestPath,
            ScenarioValidationReport report)
        {
            Type agentType = agent.GetType();
            string agentName = agent.gameObject.name;

            if (!typeof(RLMovie.Common.BaseRLAgent).IsAssignableFrom(agentType))
            {
                report.AddError($"{agentName}: BaseRLAgent 継承ではありません。");
            }

            var behaviorParameters = agent.GetComponent<BehaviorParameters>();
            if (behaviorParameters == null)
            {
                report.AddError($"{agentName}: BehaviorParameters がありません。");
            }
            else
            {
                if (!string.Equals(behaviorParameters.BehaviorName, agentType.Name, StringComparison.Ordinal))
                {
                    report.AddError($"{agentName}: Behavior Name `{behaviorParameters.BehaviorName}` が Agent クラス名 `{agentType.Name}` と一致しません。");
                }

                if (behaviorNamesFromConfig.Length > 0 && !behaviorNamesFromConfig.Contains(behaviorParameters.BehaviorName, StringComparer.Ordinal))
                {
                    report.AddError($"{agentName}: Behavior Name `{behaviorParameters.BehaviorName}` が学習 YAML の behaviors キーに存在しません。");
                }
            }

            if (agent.GetComponent<DecisionRequester>() == null)
            {
                report.AddError($"{agentName}: DecisionRequester がありません。");
            }

            if (File.Exists(manifestPath))
            {
                string manifestAgentClass = ReadTopLevelScalar(manifestPath, "agent_class");
                string manifestBehaviorName = ReadTopLevelScalar(manifestPath, "behavior_name");

                if (!string.Equals(manifestAgentClass, agentType.Name, StringComparison.Ordinal))
                {
                    report.AddError($"{agentName}: manifest の `agent_class` が `{agentType.Name}` と一致しません。現在値: `{manifestAgentClass}`");
                }

                if (behaviorParameters != null && !string.Equals(manifestBehaviorName, behaviorParameters.BehaviorName, StringComparison.Ordinal))
                {
                    report.AddError($"{agentName}: manifest の `behavior_name` が `{behaviorParameters.BehaviorName}` と一致しません。現在値: `{manifestBehaviorName}`");
                }
            }

            foreach (FieldInfo field in GetSerializableObjectReferenceFields(agentType))
            {
                if (field.FieldType.IsArray)
                {
                    continue;
                }

                var value = field.GetValue(agent) as UnityEngine.Object;
                if (value == null)
                {
                    report.AddError($"{agentName}: SerializeField `{field.Name}` が未設定です。");
                }
            }
        }

        private static void ValidateTrainingVisualizers(RLMovie.Common.BaseRLAgent[] agents, ScenarioValidationReport report)
        {
            var visualizers = UnityEngine.Object.FindObjectsByType<RLMovie.Common.TrainingVisualizer>(FindObjectsSortMode.None);
            if (visualizers.Length == 0)
            {
                report.AddError("TrainingVisualizer がシーン内に見つかりません。");
                return;
            }

            foreach (var visualizer in visualizers)
            {
                var serializedObject = new SerializedObject(visualizer);
                var targetAgentProperty = serializedObject.FindProperty("targetAgent");
                var targetAgent = targetAgentProperty?.objectReferenceValue as RLMovie.Common.BaseRLAgent;

                if (targetAgent == null)
                {
                    report.AddError($"{visualizer.gameObject.name}: targetAgent が未設定です。");
                    continue;
                }

                if (!agents.Contains(targetAgent))
                {
                    report.AddWarning($"{visualizer.gameObject.name}: targetAgent が現在シーンの Agent 一覧に含まれていません。");
                }
            }
        }

        private static void ValidateRecordingHelpers(ScenarioValidationReport report)
        {
            var helpers = UnityEngine.Object.FindObjectsByType<RLMovie.Common.RecordingHelper>(FindObjectsSortMode.None);
            if (helpers.Length == 0)
            {
                report.AddError("RecordingHelper がシーン内に見つかりません。");
                return;
            }

            foreach (var helper in helpers)
            {
                var serializedObject = new SerializedObject(helper);
                var cameraSwitchingProperty = serializedObject.FindProperty("enableCameraSwitching");
                var cameraPositionsProperty = serializedObject.FindProperty("cameraPositions");

                bool enableCameraSwitching = cameraSwitchingProperty != null && cameraSwitchingProperty.boolValue;
                int cameraCount = cameraPositionsProperty != null ? cameraPositionsProperty.arraySize : 0;

                if (enableCameraSwitching && cameraCount < 2)
                {
                    report.AddWarning($"{helper.gameObject.name}: enableCameraSwitching が有効ですが cameraPositions が 2 個未満です。");
                }
            }
        }

        private static void ValidateGoldenSpine(RLMovie.Common.BaseRLAgent[] agents, ScenarioValidationReport report)
        {
            var spines = UnityEngine.Object.FindObjectsByType<RLMovie.Common.ScenarioGoldenSpine>(FindObjectsSortMode.None);
            if (spines.Length == 0)
            {
                report.AddWarning("ScenarioGoldenSpine がありません。新規シナリオでは Golden Spine の利用を推奨します。");
                return;
            }

            if (spines.Length > 1)
            {
                report.AddError("ScenarioGoldenSpine は 1 シーンにつき 1 つにしてください。");
                return;
            }

            var spine = spines[0];
            var serializedObject = new SerializedObject(spine);

            var environmentRoot = serializedObject.FindProperty("environmentRoot")?.objectReferenceValue as Transform;
            var primaryAgent = serializedObject.FindProperty("primaryAgent")?.objectReferenceValue as RLMovie.Common.BaseRLAgent;
            var primaryGoal = serializedObject.FindProperty("primaryGoal")?.objectReferenceValue as Transform;
            var environmentManager = serializedObject.FindProperty("environmentManager")?.objectReferenceValue as RLMovie.Common.EnvironmentManager;
            var trainingVisualizer = serializedObject.FindProperty("trainingVisualizer")?.objectReferenceValue as RLMovie.Common.TrainingVisualizer;
            var recordingHelper = serializedObject.FindProperty("recordingHelper")?.objectReferenceValue as RLMovie.Common.RecordingHelper;
            var defaultCameraView = serializedObject.FindProperty("defaultCameraView")?.objectReferenceValue as Transform;
            var recordingCameraViews = serializedObject.FindProperty("recordingCameraViews");

            if (environmentRoot == null)
            {
                report.AddError("ScenarioGoldenSpine: environmentRoot が未設定です。");
            }
            else if (!string.Equals(environmentRoot.name, "EnvironmentRoot", StringComparison.Ordinal))
            {
                report.AddWarning($"ScenarioGoldenSpine: environmentRoot 名は `EnvironmentRoot` を推奨します。現在値: `{environmentRoot.name}`");
            }
            else if (environmentRoot != spine.transform)
            {
                report.AddWarning("ScenarioGoldenSpine: environmentRoot は ScenarioGoldenSpine を持つ GameObject 自身を参照することを推奨します。");
            }

            if (primaryAgent == null)
            {
                report.AddError("ScenarioGoldenSpine: primaryAgent が未設定です。");
            }
            else if (!agents.Contains(primaryAgent))
            {
                report.AddError("ScenarioGoldenSpine: primaryAgent が現在シーンの Agent 一覧に含まれていません。");
            }
            else if (environmentRoot != null && !primaryAgent.transform.IsChildOf(environmentRoot))
            {
                report.AddWarning("ScenarioGoldenSpine: primaryAgent が environmentRoot 配下にありません。");
            }

            if (primaryGoal == null)
            {
                report.AddError("ScenarioGoldenSpine: primaryGoal が未設定です。");
            }
            else if (environmentRoot != null && !primaryGoal.IsChildOf(environmentRoot))
            {
                report.AddWarning("ScenarioGoldenSpine: primaryGoal が environmentRoot 配下にありません。");
            }

            if (environmentManager == null)
            {
                report.AddError("ScenarioGoldenSpine: environmentManager が未設定です。");
            }

            if (trainingVisualizer == null)
            {
                report.AddError("ScenarioGoldenSpine: trainingVisualizer が未設定です。");
            }
            else
            {
                var visualizerSo = new SerializedObject(trainingVisualizer);
                var targetAgent = visualizerSo.FindProperty("targetAgent")?.objectReferenceValue as RLMovie.Common.BaseRLAgent;
                if (primaryAgent != null && targetAgent != primaryAgent)
                {
                    report.AddError("ScenarioGoldenSpine: trainingVisualizer.targetAgent が primaryAgent と一致しません。");
                }
            }

            if (recordingHelper == null)
            {
                report.AddError("ScenarioGoldenSpine: recordingHelper が未設定です。");
            }
            else
            {
                var helperSo = new SerializedObject(recordingHelper);
                var hideUiWhenRecording = helperSo.FindProperty("hideUIWhenRecording");
                var helperCameraPositions = helperSo.FindProperty("cameraPositions");

                if (defaultCameraView == null)
                {
                    report.AddWarning("ScenarioGoldenSpine: defaultCameraView が未設定です。");
                }
                else if (environmentRoot != null && !defaultCameraView.IsChildOf(environmentRoot))
                {
                    report.AddWarning("ScenarioGoldenSpine: defaultCameraView が environmentRoot 配下にありません。");
                }

                int spineCameraCount = recordingCameraViews != null ? recordingCameraViews.arraySize : 0;
                if (spineCameraCount == 0)
                {
                    report.AddWarning("ScenarioGoldenSpine: recordingCameraViews が空です。録画カメラ基準点の設定を推奨します。");
                }

                if (helperCameraPositions != null && spineCameraCount != helperCameraPositions.arraySize)
                {
                    report.AddWarning("ScenarioGoldenSpine: recordingCameraViews と RecordingHelper.cameraPositions の数が一致しません。");
                }

                if (hideUiWhenRecording != null && !hideUiWhenRecording.boolValue)
                {
                    report.AddWarning("ScenarioGoldenSpine: RecordingHelper.hideUIWhenRecording は true を推奨します。");
                }

                int cameraCountToCheck = helperCameraPositions != null
                    ? Math.Min(spineCameraCount, helperCameraPositions.arraySize)
                    : spineCameraCount;

                for (int i = 0; i < cameraCountToCheck; i++)
                {
                    var spineCamera = recordingCameraViews.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    var helperCamera = helperCameraPositions?.GetArrayElementAtIndex(i).objectReferenceValue as Transform;

                    if (spineCamera == null)
                    {
                        report.AddWarning($"ScenarioGoldenSpine: recordingCameraViews[{i}] が未設定です。");
                        continue;
                    }

                    if (environmentRoot != null && !spineCamera.IsChildOf(environmentRoot))
                    {
                        report.AddWarning($"ScenarioGoldenSpine: recordingCameraViews[{i}] が environmentRoot 配下にありません。");
                    }

                    if (helperCamera != spineCamera)
                    {
                        report.AddWarning($"ScenarioGoldenSpine: recordingCameraViews[{i}] と RecordingHelper.cameraPositions[{i}] が一致しません。");
                    }
                }
            }
        }

        private static IEnumerable<FieldInfo> GetSerializableObjectReferenceFields(Type type)
        {
            while (type != null && typeof(RLMovie.Common.BaseRLAgent).IsAssignableFrom(type))
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    bool isSerialized = (field.IsPublic && !field.IsDefined(typeof(NonSerializedAttribute), true))
                        || field.IsDefined(typeof(SerializeField), true);

                    if (!isSerialized)
                    {
                        continue;
                    }

                    if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    {
                        continue;
                    }

                    yield return field;
                }

                type = type.BaseType;
            }
        }

        private static HashSet<string> ReadTopLevelKeys(string filePath)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) || char.IsWhiteSpace(line[0]))
                {
                    continue;
                }

                string trimmed = line.Trim();
                if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith("-", StringComparison.Ordinal))
                {
                    continue;
                }

                int separatorIndex = trimmed.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                keys.Add(trimmed.Substring(0, separatorIndex).Trim());
            }

            return keys;
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

        private static IEnumerable<string> ReadBehaviorNames(string configPath)
        {
            bool insideBehaviors = false;

            foreach (string line in File.ReadLines(configPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string trimmed = line.Trim();
                if (trimmed.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!insideBehaviors)
                {
                    if (!char.IsWhiteSpace(line[0]) && string.Equals(trimmed, "behaviors:", StringComparison.Ordinal))
                    {
                        insideBehaviors = true;
                    }

                    continue;
                }

                if (!char.IsWhiteSpace(line[0]))
                {
                    yield break;
                }

                int indent = line.TakeWhile(char.IsWhiteSpace).Count();
                if (indent == 2 && trimmed.EndsWith(":", StringComparison.Ordinal))
                {
                    yield return trimmed.TrimEnd(':').Trim();
                }
            }
        }

        private static void LogReport(ScenarioValidationReport report, bool logToConsole)
        {
            if (!logToConsole)
            {
                return;
            }

            foreach (string error in report.Errors)
            {
                Debug.LogError($"[ScenarioValidator] ❌ {error}");
            }

            foreach (string warning in report.Warnings)
            {
                Debug.LogWarning($"[ScenarioValidator] ⚠️ {warning}");
            }

            if (report.IsValid)
            {
                Debug.Log($"[ScenarioValidator] ✅ Validation passed with {report.Warnings.Count} warning(s)");
            }
            else
            {
                Debug.LogError($"[ScenarioValidator] ❌ Validation failed with {report.Errors.Count} error(s) and {report.Warnings.Count} warning(s)");
            }
        }
    }

    public sealed class ScenarioValidationReport
    {
        public List<string> Errors { get; } = new List<string>();

        public List<string> Warnings { get; } = new List<string>();

        public bool IsValid => Errors.Count == 0;

        public void AddError(string message)
        {
            Errors.Add(message);
        }

        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }
    }
}



