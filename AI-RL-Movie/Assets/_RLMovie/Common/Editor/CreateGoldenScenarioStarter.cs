using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace RLMovie.Editor
{
    /// <summary>
    /// Generates the files for a new scenario that follows the shared V2 backbone.
    /// After Unity recompiles, the generated scene builder can create the starter scene.
    /// </summary>
    public static class CreateGoldenScenarioStarter
    {
        private const string EnvironmentsRoot = "Assets/_RLMovie/Environments";

        public sealed class StarterFilesResult
        {
            public bool Success { get; set; }

            public string ScenarioName { get; set; }

            public string ScenePath { get; set; }

            public string AgentPath { get; set; }

            public string BuilderPath { get; set; }

            public string ManifestPath { get; set; }

            public string BlueprintPath { get; set; }

            public string TrainingConfigPath { get; set; }

            public string Message { get; set; }
        }

        [MenuItem("RLMovie/Create Scenario Starter Files")]
        public static void CreateStarterFiles()
        {
            string selectedScenePath = EditorUtility.SaveFilePanelInProject(
                "Create Scenario Starter",
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

        [MenuItem("RLMovie/Create Golden Scenario Starter Files")]
        public static void CreateStarterFilesLegacyAlias()
        {
            Debug.LogWarning("[CreateScenarioStarter] 'RLMovie/Create Golden Scenario Starter Files' is deprecated. Use 'RLMovie/Create Scenario Starter Files' instead.");
            CreateStarterFiles();
        }

        public static StarterFilesResult CreateStarterFilesForScenario(string scenarioName, bool overwriteExisting = false)
        {
            return CreateStarterFilesForScenario(scenarioName, CoreScenarioStarterDefinition.StarterKindCore, overwriteExisting);
        }

        public static StarterFilesResult CreateStarterFilesForScenario(string scenarioName, string starterKind, bool overwriteExisting = false)
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

            ScenarioStarterDefinition starterDefinition;
            try
            {
                starterDefinition = ScenarioStarterRegistry.GetRequired(starterKind);
            }
            catch (Exception ex)
            {
                return new StarterFilesResult
                {
                    Success = false,
                    ScenarioName = scenarioName,
                    Message = ex.Message
                };
            }

            ScenarioStarterScaffold scaffold = starterDefinition.CreateScaffold(scenarioName);

            bool starterExists = AssetPathExists(scaffold.AgentPath)
                || AssetPathExists(scaffold.BuilderPath)
                || AssetPathExists(scaffold.ManifestPath)
                || AssetPathExists(scaffold.BlueprintPath)
                || AssetPathExists(scaffold.TrainingConfigPath);

            if (starterExists && !overwriteExisting)
            {
                return new StarterFilesResult
                {
                    Success = false,
                    ScenarioName = scenarioName,
                    ScenePath = scaffold.ScenePath,
                    AgentPath = scaffold.AgentPath,
                    BuilderPath = scaffold.BuilderPath,
                    ManifestPath = scaffold.ManifestPath,
                    BlueprintPath = scaffold.BlueprintPath,
                    TrainingConfigPath = scaffold.TrainingConfigPath,
                    Message = $"A starter for {scenarioName} already exists. Choose a different scenario name or remove the existing files first."
                };
            }

            EnsureFolder($"{scaffold.ScenarioRootAssetPath}/Scenes");
            EnsureFolder($"{scaffold.ScenarioRootAssetPath}/Scripts");
            EnsureFolder($"{scaffold.ScenarioRootAssetPath}/Prefabs");
            EnsureFolder($"{scaffold.ScenarioRootAssetPath}/Config");
            EnsureFolder($"{scaffold.ScenarioRootAssetPath}/Editor");

            Dictionary<string, string> files = starterDefinition.CreateDefaultFiles(scaffold);
            foreach (KeyValuePair<string, string> file in files)
            {
                WriteAssetText(file.Key, file.Value);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();

            string mode = starterExists ? "Updated" : "Created";
            Debug.Log($"[CreateScenarioStarter] {mode} {starterDefinition.Kind} starter files for {scenarioName}.");

            return new StarterFilesResult
            {
                Success = true,
                ScenarioName = scenarioName,
                ScenePath = scaffold.ScenePath,
                AgentPath = scaffold.AgentPath,
                BuilderPath = scaffold.BuilderPath,
                ManifestPath = scaffold.ManifestPath,
                BlueprintPath = scaffold.BlueprintPath,
                TrainingConfigPath = scaffold.TrainingConfigPath,
                Message = starterDefinition.BuildStarterFilesMessage(scaffold, starterExists)
            };
        }

        private static bool IsValidScenarioName(string scenarioName)
        {
            return Regex.IsMatch(scenarioName, "^[A-Z][A-Za-z0-9]*$");
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
