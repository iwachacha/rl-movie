using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RLMovie.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RLMovie.Editor
{
    internal sealed class ScenarioStarterScaffold
    {
        public string StarterKind { get; set; } = string.Empty;
        public string ScenarioName { get; set; } = string.Empty;
        public string ScenarioRootAssetPath { get; set; } = string.Empty;
        public string ScenePath { get; set; } = string.Empty;
        public string AgentPath { get; set; } = string.Empty;
        public string BuilderPath { get; set; } = string.Empty;
        public string ManifestPath { get; set; } = string.Empty;
        public string BlueprintPath { get; set; } = string.Empty;
        public string TrainingConfigName { get; set; } = string.Empty;
        public string TrainingConfigPath { get; set; } = string.Empty;
        public string AgentClassName { get; set; } = string.Empty;
        public string BehaviorName { get; set; } = string.Empty;
    }

    internal sealed class ScenarioStarterValidationContext
    {
        public Scene ActiveScene { get; set; }
        public ScenarioConfigPaths Paths { get; set; }
        public ScenarioManifestData Manifest { get; set; }
        public ScenarioBlueprintData Blueprint { get; set; }
        public BaseRLAgent[] Agents { get; set; } = Array.Empty<BaseRLAgent>();
        public ScenarioGoldenSpine Spine { get; set; }
    }

    /// <summary>
    /// Abstract base for starter definitions.
    /// Extend this to create new starter kinds (e.g. multi_agent, navigation, avoidance).
    /// </summary>
    internal abstract class ScenarioStarterDefinition
    {
        protected const string EnvironmentsRoot = "Assets/_RLMovie/Environments";

        public abstract string Kind { get; }

        public abstract string DisplayName { get; }

        public abstract string TemplateRoot { get; }

        public virtual string BuildStarterFilesMessage(ScenarioStarterScaffold scaffold, bool updated)
        {
            string mode = updated ? "Updated" : "Created";
            return $"{mode} the {DisplayName} starter files for {scaffold.ScenarioName}.\n\nNext step:\n1. Wait for Unity to finish compiling.\n2. Run RLMovie/Create {scaffold.ScenarioName} Scene.";
        }

        public virtual ScenarioStarterScaffold CreateScaffold(string scenarioName)
        {
            string scenarioRoot = $"{EnvironmentsRoot}/{scenarioName}";
            string agentClassName = $"{scenarioName}Agent";
            string behaviorName = agentClassName;
            string trainingConfigName = ToSnakeCase(scenarioName) + "_config.yaml";

            return new ScenarioStarterScaffold
            {
                StarterKind = Kind,
                ScenarioName = scenarioName,
                ScenarioRootAssetPath = scenarioRoot,
                ScenePath = $"{scenarioRoot}/Scenes/{scenarioName}.unity",
                AgentPath = $"{scenarioRoot}/Scripts/{scenarioName}Agent.cs",
                BuilderPath = $"{scenarioRoot}/Editor/{scenarioName}SceneBuilder.cs",
                ManifestPath = $"{scenarioRoot}/Config/scenario_manifest.yaml",
                BlueprintPath = $"{scenarioRoot}/Config/scenario_blueprint.yaml",
                TrainingConfigName = trainingConfigName,
                TrainingConfigPath = $"{scenarioRoot}/Config/{trainingConfigName}",
                AgentClassName = agentClassName,
                BehaviorName = behaviorName
            };
        }

        public abstract Dictionary<string, string> CreateDefaultFiles(ScenarioStarterScaffold scaffold);

        /// <summary>
        /// Override to add starter-kind-specific validation rules.
        /// </summary>
        public virtual void ValidateStarterSpecificRules(ScenarioStarterValidationContext context, ScenarioValidationReport report)
        {
        }

        protected string ReadTemplate(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            return File.ReadAllText(fullPath);
        }

        protected string TransformTemplate(string template, ScenarioStarterScaffold scaffold)
        {
            return template
                .Replace("__SCENARIO_NAME__", scaffold.ScenarioName)
                .Replace("__AGENT_CLASS__", scaffold.AgentClassName)
                .Replace("__BEHAVIOR_NAME__", scaffold.BehaviorName)
                .Replace("__SCENE_PATH__", scaffold.ScenePath.Replace('\\', '/'))
                .Replace("__TRAINING_CONFIG_NAME__", scaffold.TrainingConfigName)
                .Replace("__STARTER_KIND__", scaffold.StarterKind);
        }

        private static string ToSnakeCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
        }
    }

    /// <summary>
    /// Default starter: a single hero agent with target-reaching gameplay.
    /// Scene construction is handled entirely by the generated SceneBuilder template.
    /// </summary>
    internal sealed class CoreScenarioStarterDefinition : ScenarioStarterDefinition
    {
        public const string StarterKindCore = "core";

        public override string Kind => StarterKindCore;

        public override string DisplayName => "core";

        public override string TemplateRoot => "Assets/_RLMovie/Environments/_Template";

        public override Dictionary<string, string> CreateDefaultFiles(ScenarioStarterScaffold scaffold)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [scaffold.AgentPath] = TransformTemplate(ReadTemplate($"{TemplateRoot}/Scripts/TemplateAgent.cs.txt"), scaffold),
                [scaffold.BuilderPath] = TransformTemplate(ReadTemplate($"{TemplateRoot}/Scripts/TemplateSceneBuilder.cs.txt"), scaffold),
                [scaffold.ManifestPath] = TransformTemplate(ReadTemplate($"{TemplateRoot}/Config/scenario_manifest.yaml"), scaffold),
                [scaffold.BlueprintPath] = TransformTemplate(ReadTemplate($"{TemplateRoot}/Config/scenario_blueprint.yaml"), scaffold),
                [scaffold.TrainingConfigPath] = TransformTemplate(ReadTemplate($"{TemplateRoot}/Config/template_config.yaml"), scaffold)
            };
        }

        public override void ValidateStarterSpecificRules(ScenarioStarterValidationContext context, ScenarioValidationReport report)
        {
            if (context.Spine == null)
            {
                report.AddError("core starter requires a ScenarioGoldenSpine in the scene.");
                return;
            }

            string[] requiredCameraRoles = { "explain", "wide_a", "wide_b" };
            for (int i = 0; i < requiredCameraRoles.Length; i++)
            {
                string role = requiredCameraRoles[i];
                if (!context.Spine.TryGetCameraRole(role, out Transform anchor) || anchor == null)
                {
                    report.AddError($"core starter requires the `{role}` camera role to resolve.");
                }
            }
        }
    }

    /// <summary>
    /// Registry of starter kinds. To add a new starter kind:
    /// 1. Create a class extending ScenarioStarterDefinition
    /// 2. Register it in the Definitions dictionary below
    /// 3. Create template files in a new template directory
    /// </summary>
    internal static class ScenarioStarterRegistry
    {
        private static readonly Dictionary<string, ScenarioStarterDefinition> Definitions =
            new Dictionary<string, ScenarioStarterDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                [CoreScenarioStarterDefinition.StarterKindCore] = new CoreScenarioStarterDefinition()
                // Add new starter kinds here:
                // ["multi_agent"] = new MultiAgentStarterDefinition(),
                // ["navigation"] = new NavigationStarterDefinition(),
            };

        public static ScenarioStarterDefinition GetDefault()
        {
            return Definitions[CoreScenarioStarterDefinition.StarterKindCore];
        }

        public static IReadOnlyList<string> GetRegisteredKinds()
        {
            return Definitions.Keys
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static bool TryGet(string kind, out ScenarioStarterDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(kind))
            {
                definition = GetDefault();
                return true;
            }

            return Definitions.TryGetValue(kind, out definition);
        }

        public static ScenarioStarterDefinition GetRequired(string kind)
        {
            if (TryGet(kind, out ScenarioStarterDefinition definition))
            {
                return definition;
            }

            string available = string.Join(", ", GetRegisteredKinds());
            throw new InvalidOperationException($"Unknown starter_kind `{kind}`. Available starter kinds: {available}");
        }
    }
}
