using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace RLMovie.Editor
{
    /// <summary>
    /// Generates the files for a new scenario that follows the shared golden spine.
    /// After Unity recompiles, the generated scene builder can create the starter scene.
    /// </summary>
    public static class CreateGoldenScenarioStarter
    {
        private const string TemplateRoot = "Assets/_RLMovie/Environments/_Template";
        private const string EnvironmentsRoot = "Assets/_RLMovie/Environments";

        public sealed class StarterFilesResult
        {
            public bool Success { get; set; }

            public string ScenarioName { get; set; }

            public string ScenePath { get; set; }

            public string AgentPath { get; set; }

            public string BuilderPath { get; set; }

            public string ManifestPath { get; set; }

            public string TrainingConfigPath { get; set; }

            public string Message { get; set; }
        }

        [MenuItem("RLMovie/Create Golden Scenario Starter Files")]
        public static void CreateStarterFiles()
        {
            string selectedScenePath = EditorUtility.SaveFilePanelInProject(
                "Create Golden Scenario Starter",
                "NewScenario",
                "unity",
                "Use the scene file name as the scenario name. Files are always created under Assets/_RLMovie/Environments/<Scenario>/.");

            if (string.IsNullOrEmpty(selectedScenePath))
            {
                return;
            }

            string scenarioName = Path.GetFileNameWithoutExtension(selectedScenePath);
            StarterFilesResult result = CreateStarterFilesForScenario(scenarioName);
            if (!result.Success)
            {
                EditorUtility.DisplayDialog(
                    "Starter Creation Failed",
                    result.Message,
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Starter Files Created",
                result.Message,
                "OK");
        }

        public static StarterFilesResult CreateStarterFilesForScenario(string scenarioName, bool overwriteExisting = false)
        {
            if (!IsValidScenarioName(scenarioName))
            {
                return new StarterFilesResult
                {
                    Success = false,
                    ScenarioName = scenarioName,
                    Message = "Scenario names should be PascalCase and contain only letters or digits."
                };
            }

            string scenarioRoot = $"{EnvironmentsRoot}/{scenarioName}";
            string scenePath = $"{scenarioRoot}/Scenes/{scenarioName}.unity";
            string agentPath = $"{scenarioRoot}/Scripts/{scenarioName}Agent.cs";
            string builderPath = $"{scenarioRoot}/Editor/{scenarioName}SceneBuilder.cs";
            string manifestPath = $"{scenarioRoot}/Config/scenario_manifest.yaml";
            string trainingConfigName = $"{ToSnakeCase(scenarioName)}_config.yaml";
            string trainingConfigPath = $"{scenarioRoot}/Config/{trainingConfigName}";

            bool starterExists = AssetPathExists(agentPath)
                || AssetPathExists(builderPath)
                || AssetPathExists(manifestPath)
                || AssetPathExists(trainingConfigPath);

            if (starterExists && !overwriteExisting)
            {
                return new StarterFilesResult
                {
                    Success = false,
                    ScenarioName = scenarioName,
                    ScenePath = scenePath,
                    AgentPath = agentPath,
                    BuilderPath = builderPath,
                    ManifestPath = manifestPath,
                    TrainingConfigPath = trainingConfigPath,
                    Message = $"A starter for {scenarioName} already exists. Choose a different scenario name or remove the existing files first."
                };
            }

            EnsureFolder($"{scenarioRoot}/Scenes");
            EnsureFolder($"{scenarioRoot}/Scripts");
            EnsureFolder($"{scenarioRoot}/Prefabs");
            EnsureFolder($"{scenarioRoot}/Config");
            EnsureFolder($"{scenarioRoot}/Editor");

            string agentClassName = $"{scenarioName}Agent";
            string behaviorName = agentClassName;
            string trainingConfig = TransformTemplate(
                ReadTemplate($"{TemplateRoot}/Config/template_config.yaml"),
                scenarioName,
                agentClassName,
                behaviorName,
                scenePath,
                trainingConfigName);

            string manifest = TransformTemplate(
                ReadTemplate($"{TemplateRoot}/Config/scenario_manifest.yaml"),
                scenarioName,
                agentClassName,
                behaviorName,
                scenePath,
                trainingConfigName);

            string agentScript = TransformTemplate(
                ReadTemplate($"{TemplateRoot}/Scripts/TemplateAgent.cs.txt"),
                scenarioName,
                agentClassName,
                behaviorName,
                scenePath,
                trainingConfigName);

            string builderScript = TransformTemplate(
                ReadTemplate($"{TemplateRoot}/Scripts/TemplateSceneBuilder.cs.txt"),
                scenarioName,
                agentClassName,
                behaviorName,
                scenePath,
                trainingConfigName);

            WriteAssetText(agentPath, agentScript);
            WriteAssetText(builderPath, builderScript);
            WriteAssetText(manifestPath, manifest);
            WriteAssetText(trainingConfigPath, trainingConfig);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();

            string mode = starterExists ? "Updated" : "Created";
            Debug.Log($"[CreateGoldenScenarioStarter] {mode} starter files for {scenarioName}.");

            return new StarterFilesResult
            {
                Success = true,
                ScenarioName = scenarioName,
                ScenePath = scenePath,
                AgentPath = agentPath,
                BuilderPath = builderPath,
                ManifestPath = manifestPath,
                TrainingConfigPath = trainingConfigPath,
                Message = $"{mode} the golden starter files for {scenarioName}.\n\nNext step:\n1. Wait for Unity to finish compiling.\n2. Run RLMovie/Create {scenarioName} Scene."
            };
        }

        private static string ReadTemplate(string assetPath)
        {
            string fullPath = ToFullPath(assetPath);
            return File.ReadAllText(fullPath);
        }

        private static string TransformTemplate(
            string template,
            string scenarioName,
            string agentClassName,
            string behaviorName,
            string scenePath,
            string trainingConfigName)
        {
            return template
                .Replace("TemplateAgent", agentClassName)
                .Replace("scenario_name: Template", $"scenario_name: {scenarioName}")
                .Replace("scene_name: Template", $"scene_name: {scenarioName}")
                .Replace("agent_class: TemplateAgent", $"agent_class: {agentClassName}")
                .Replace("behavior_name: TemplateAgent", $"behavior_name: {behaviorName}")
                .Replace("__SCENARIO_NAME__", scenarioName)
                .Replace("__AGENT_CLASS__", agentClassName)
                .Replace("__BEHAVIOR_NAME__", behaviorName)
                .Replace("__SCENE_PATH__", scenePath.Replace('\\', '/'))
                .Replace("__TRAINING_CONFIG_NAME__", trainingConfigName);
        }

        private static bool IsValidScenarioName(string scenarioName)
        {
            return Regex.IsMatch(scenarioName, "^[A-Z][A-Za-z0-9]*$");
        }

        private static string ToSnakeCase(string input)
        {
            return Regex.Replace(input, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
        }

        private static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
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

        private static bool AssetPathExists(string assetPath)
        {
            return File.Exists(ToFullPath(assetPath));
        }

        private static void WriteAssetText(string assetPath, string contents)
        {
            File.WriteAllText(ToFullPath(assetPath), contents);
        }

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
