using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RLMovie.Editor
{
    [Serializable]
    public sealed class ScenarioManifestData
    {
        public string ScenarioName { get; set; } = string.Empty;
        public string SceneName { get; set; } = string.Empty;
        public string StarterKind { get; set; } = CoreScenarioStarterDefinition.StarterKindCore;
        public string AgentClass { get; set; } = string.Empty;
        public string BehaviorName { get; set; } = string.Empty;
        public string TrainingConfig { get; set; } = string.Empty;
        public string RuntimeBlueprint { get; set; } = "scenario_blueprint.yaml";
        public string ViewerPromise { get; set; } = string.Empty;
        public string LearningGoal { get; set; } = string.Empty;
        public List<string> SuccessConditions { get; set; } = new List<string>();
        public List<string> FailureConditions { get; set; } = new List<string>();
        public ObservationContractData ObservationContract { get; set; } = new ObservationContractData();
        public ActionContractData ActionContract { get; set; } = new ActionContractData();
        public RewardRulesData RewardRules { get; set; } = new RewardRulesData();
        public List<RandomizationKnobData> RandomizationKnobs { get; set; } = new List<RandomizationKnobData>();
        public List<DifficultyStageData> DifficultyStages { get; set; } = new List<DifficultyStageData>();
        public VisualThemeData VisualTheme { get; set; } = new VisualThemeData();
        public List<string> VisualHooks { get; set; } = new List<string>();
        public string ThumbnailMoment { get; set; } = string.Empty;
        public CameraPlanData CameraPlan { get; set; } = new CameraPlanData();
        public List<string> AcceptanceCriteria { get; set; } = new List<string>();
        public string BaselineRun { get; set; } = string.Empty;
        public int SpecVersion { get; set; } = 3;
    }

    [Serializable]
    public sealed class ObservationContractData
    {
        public int VectorSize { get; set; }
        public List<string> Items { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class ActionContractData
    {
        public string Type { get; set; } = "continuous";
        public int Size { get; set; } = 2;
        public List<string> Mapping { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class RewardRulesData
    {
        public List<string> Terminal { get; set; } = new List<string>();
        public List<string> Dense { get; set; } = new List<string>();
        public List<string> Penalties { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class RandomizationKnobData
    {
        public string Name { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class DifficultyStageData
    {
        public string Name { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class VisualThemeData
    {
        public string Style { get; set; } = string.Empty;
        public string Readability { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class CameraPlanData
    {
        public string DefaultView { get; set; } = "ExplainView";
        public List<string> RecordingCuts { get; set; } = new List<string>();
        public string ThumbnailReference { get; set; } = "ExplainView";
    }

    [Serializable]
    public sealed class ScenarioAgentBlueprintData
    {
        public string Role { get; set; } = "hero";
        public string ClassName { get; set; } = string.Empty;
        public string BehaviorName { get; set; } = string.Empty;
        public string Team { get; set; } = "solo";
        public bool Primary { get; set; } = true;
    }

    [Serializable]
    public sealed class ScenarioBlueprintData
    {
        public int Version { get; set; } = 3;
        public List<ScenarioAgentBlueprintData> Agents { get; set; } = new List<ScenarioAgentBlueprintData>();
        public SceneRolesData SceneRoles { get; set; } = new SceneRolesData();
        public CameraRolesData CameraRoles { get; set; } = new CameraRolesData();
        public RecordingDefaultsData RecordingDefaults { get; set; } = new RecordingDefaultsData();
        public OverlayBindingsData OverlayBindings { get; set; } = new OverlayBindingsData();
        public HighlightBindingsData HighlightBindings { get; set; } = new HighlightBindingsData();
        public VisualDefaultsData VisualDefaults { get; set; } = new VisualDefaultsData();

        public IReadOnlyList<ScenarioAgentBlueprintData> EnumerateAgents(ScenarioManifestData manifest)
        {
            if (Agents != null && Agents.Count > 0)
            {
                return Agents.Select(agent => NormalizeAgent(agent, manifest)).ToArray();
            }

            return new[]
            {
                NormalizeAgent(new ScenarioAgentBlueprintData
                {
                    Role = "hero",
                    ClassName = manifest?.AgentClass ?? string.Empty,
                    BehaviorName = manifest?.BehaviorName ?? string.Empty,
                    Team = "solo",
                    Primary = true
                }, manifest)
            };
        }

        public ScenarioAgentBlueprintData ResolvePrimaryAgent(ScenarioManifestData manifest)
        {
            IReadOnlyList<ScenarioAgentBlueprintData> agents = EnumerateAgents(manifest);
            return agents.FirstOrDefault(agent => agent.Primary) ?? agents.FirstOrDefault();
        }

        public ScenarioAgentBlueprintData ResolveAgentByRole(string role, ScenarioManifestData manifest)
        {
            return EnumerateAgents(manifest).FirstOrDefault(agent => string.Equals(agent.Role, role, StringComparison.Ordinal));
        }

        public string ResolvePrimaryAgentRole(ScenarioManifestData manifest)
        {
            return ResolvePrimaryAgent(manifest)?.Role ?? "hero";
        }

        public string ResolvePrimaryAgentTeam(ScenarioManifestData manifest)
        {
            return ResolvePrimaryAgent(manifest)?.Team ?? "solo";
        }

        private static ScenarioAgentBlueprintData NormalizeAgent(ScenarioAgentBlueprintData agent, ScenarioManifestData manifest)
        {
            return new ScenarioAgentBlueprintData
            {
                Role = string.IsNullOrWhiteSpace(agent?.Role) ? "hero" : agent.Role.Trim(),
                ClassName = string.IsNullOrWhiteSpace(agent?.ClassName) ? manifest?.AgentClass ?? string.Empty : agent.ClassName.Trim(),
                BehaviorName = string.IsNullOrWhiteSpace(agent?.BehaviorName) ? manifest?.BehaviorName ?? string.Empty : agent.BehaviorName.Trim(),
                Team = string.IsNullOrWhiteSpace(agent?.Team) ? "solo" : agent.Team.Trim(),
                Primary = agent?.Primary ?? false
            };
        }
    }

    [Serializable]
    public sealed class SceneRolesData
    {
        public string ArenaRoot { get; set; } = "EnvironmentRoot";
        public string PrimaryTarget { get; set; } = "PrimaryTarget";
        public string PrimaryHazard { get; set; } = string.Empty;
        public string OverlayAnchor { get; set; } = "OverlayAnchor";
    }

    [Serializable]
    public sealed class CameraRolesData
    {
        public string DefaultCamera { get; set; } = "ExplainView";
        public string Explain { get; set; } = "ExplainView";
        public string WideA { get; set; } = "WideA";
        public string WideB { get; set; } = "WideB";
        public string FollowOptional { get; set; } = "FollowView";
        public string ComparisonOptional { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class RecordingDefaultsData
    {
        public bool HideTrainingUi { get; set; } = true;
        public bool EnableCameraSwitching { get; set; } = true;
        public float CameraSwitchInterval { get; set; } = 9f;
        public List<string> CameraCycleRoles { get; set; } = new List<string>();
        public string FollowCameraRole { get; set; } = "follow_optional";
        public string FollowTargetRole { get; set; } = "hero";
        public string FollowTargetTeam { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class OverlayBindingsData
    {
        public string TargetAgentRole { get; set; } = "hero";
        public List<string> TargetAgentRoles { get; set; } = new List<string>();
        public string TargetAgentTeam { get; set; } = string.Empty;
        public string ObjectiveSource { get; set; } = "viewer_promise";
        public string ScenarioLabelSource { get; set; } = "scenario_name";
    }

    [Serializable]
    public sealed class HighlightBindingsData
    {
        public string TrackedAgentRole { get; set; } = "hero";
        public List<string> TrackedAgentRoles { get; set; } = new List<string>();
        public string TrackedAgentTeam { get; set; } = string.Empty;
        public bool ExportHighlightsToJsonl { get; set; } = true;
        public bool ExportSnapshotsToJsonl { get; set; } = true;
    }

    [Serializable]
    public sealed class VisualDefaultsData
    {
        public bool ApplyReadabilityKit { get; set; } = true;
        public string HeroMaterial { get; set; } = "V2HeroReadable";
        public string TargetMaterial { get; set; } = "V2TargetReadable";
        public string HazardMaterial { get; set; } = "V2HazardReadable";
        public string FloorMaterial { get; set; } = "V2FloorReadable";
    }

    internal sealed class ScenarioConfigPaths
    {
        public string ProjectRoot { get; set; } = string.Empty;
        public string ScenePath { get; set; } = string.Empty;
        public string SceneName { get; set; } = string.Empty;
        public string ScenarioRootAssetPath { get; set; } = string.Empty;
        public string ScenarioRootFullPath { get; set; } = string.Empty;
        public string ConfigDirectory { get; set; } = string.Empty;
        public string ManifestPath { get; set; } = string.Empty;

        public string ResolveConfigPath(string fileName)
        {
            return Path.Combine(ConfigDirectory, fileName);
        }

        public string ResolveConfigAssetPath(string fileName)
        {
            return $"{ScenarioRootAssetPath}/Config/{fileName}".Replace('\\', '/');
        }
    }

    internal static class ScenarioConfigIO
    {
        internal static ScenarioManifestData LoadManifest(string manifestPath)
        {
            IDictionary<string, object> root = LoadDocumentRoot(manifestPath, "scenario manifest");
            return BindManifest(root);
        }

        internal static ScenarioBlueprintData LoadBlueprint(string configDirectory, ScenarioManifestData manifest)
        {
            string blueprintPath = ResolveBlueprintPath(configDirectory, manifest);
            if (string.IsNullOrWhiteSpace(blueprintPath))
            {
                throw new InvalidDataException("manifest.runtime_blueprint is required.");
            }

            IDictionary<string, object> root = LoadDocumentRoot(blueprintPath, "scenario blueprint");
            return BindBlueprint(root, manifest);
        }

        internal static string ResolveBlueprintPath(string configDirectory, ScenarioManifestData manifest)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.RuntimeBlueprint))
            {
                return string.Empty;
            }

            return Path.Combine(configDirectory, manifest.RuntimeBlueprint);
        }

        internal static ScenarioConfigPaths ResolveScenarioPaths(Scene scene)
        {
            return ResolveScenarioPaths(scene.path, scene.name);
        }

        internal static ScenarioConfigPaths ResolveScenarioPaths(string scenePath, string sceneName = null)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new InvalidOperationException("Scene path is required to resolve scenario config paths.");
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string normalizedScenePath = scenePath.Replace('\\', '/');
            string scenesDirAsset = Path.GetDirectoryName(normalizedScenePath)?.Replace('\\', '/');
            string scenarioDirAsset = Path.GetDirectoryName(scenesDirAsset ?? string.Empty)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(scenarioDirAsset))
            {
                throw new InvalidOperationException($"Could not resolve the scenario root from scene path `{scenePath}`.");
            }

            return new ScenarioConfigPaths
            {
                ProjectRoot = projectRoot,
                ScenePath = normalizedScenePath,
                SceneName = string.IsNullOrWhiteSpace(sceneName) ? Path.GetFileNameWithoutExtension(normalizedScenePath) : sceneName,
                ScenarioRootAssetPath = scenarioDirAsset,
                ScenarioRootFullPath = Path.GetFullPath(Path.Combine(projectRoot, scenarioDirAsset)),
                ConfigDirectory = Path.GetFullPath(Path.Combine(projectRoot, scenarioDirAsset, "Config")),
                ManifestPath = Path.Combine(projectRoot, scenarioDirAsset, "Config", "scenario_manifest.yaml")
            };
        }

        internal static string[] GetCandidateTrainingConfigPaths(ScenarioConfigPaths paths)
        {
            if (paths == null || !Directory.Exists(paths.ConfigDirectory))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(paths.ConfigDirectory, "*.yaml")
                .Where(path => !string.Equals(Path.GetFileName(path), "scenario_manifest.yaml", StringComparison.OrdinalIgnoreCase))
                .Where(path => !string.Equals(Path.GetFileName(path), "scenario_blueprint.yaml", StringComparison.OrdinalIgnoreCase))
                .Where(path => !string.Equals(Path.GetFileName(path), "template_config.yaml", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        internal static string ResolveSelectedTrainingConfigPath(ScenarioConfigPaths paths, ScenarioManifestData manifest)
        {
            if (paths == null || manifest == null || string.IsNullOrWhiteSpace(manifest.TrainingConfig))
            {
                return string.Empty;
            }

            return paths.ResolveConfigPath(manifest.TrainingConfig);
        }

        internal static string ResolveOverlayLabel(ScenarioManifestData manifest, ScenarioBlueprintData blueprint)
        {
            string source = blueprint?.OverlayBindings?.ScenarioLabelSource ?? "scenario_name";
            return source switch
            {
                "scene_name" => manifest?.SceneName ?? string.Empty,
                _ => manifest?.ScenarioName ?? string.Empty
            };
        }

        internal static string ResolveOverlayObjective(ScenarioManifestData manifest, ScenarioBlueprintData blueprint)
        {
            string source = blueprint?.OverlayBindings?.ObjectiveSource ?? "viewer_promise";
            return source switch
            {
                "learning_goal" => manifest?.LearningGoal ?? string.Empty,
                "thumbnail_moment" => manifest?.ThumbnailMoment ?? string.Empty,
                _ => manifest?.ViewerPromise ?? string.Empty
            };
        }

        internal static IReadOnlyList<string> ResolveOverlayTargetRoles(ScenarioBlueprintData blueprint, ScenarioManifestData manifest)
        {
            if (blueprint?.OverlayBindings?.TargetAgentRoles != null && blueprint.OverlayBindings.TargetAgentRoles.Count > 0)
            {
                return blueprint.OverlayBindings.TargetAgentRoles.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            }

            string singleRole = blueprint?.OverlayBindings?.TargetAgentRole;
            if (!string.IsNullOrWhiteSpace(singleRole))
            {
                return new[] { singleRole };
            }

            return new[] { blueprint?.ResolvePrimaryAgentRole(manifest) ?? "hero" };
        }

        internal static IReadOnlyList<string> ResolveHighlightTargetRoles(ScenarioBlueprintData blueprint, ScenarioManifestData manifest)
        {
            if (blueprint?.HighlightBindings?.TrackedAgentRoles != null && blueprint.HighlightBindings.TrackedAgentRoles.Count > 0)
            {
                return blueprint.HighlightBindings.TrackedAgentRoles.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            }

            string singleRole = blueprint?.HighlightBindings?.TrackedAgentRole;
            if (!string.IsNullOrWhiteSpace(singleRole))
            {
                return new[] { singleRole };
            }

            return new[] { blueprint?.ResolvePrimaryAgentRole(manifest) ?? "hero" };
        }

        internal static bool IsPlaceholder(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            string trimmed = value.Trim();
            return trimmed.IndexOf("describe", StringComparison.OrdinalIgnoreCase) >= 0
                || trimmed.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0
                || trimmed.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0
                || trimmed.IndexOf("todo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IDictionary<string, object> LoadDocumentRoot(string path, string label)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"{label} was not found.", path);
            }

            string contents = File.ReadAllText(path);
            try
            {
                object parsed = ParseStructuredDocument(contents);
                return AsMap(parsed, $"{label} root");
            }
            catch (Exception ex) when (ex is VendoredYamlParseException || ex is InvalidDataException)
            {
                throw new InvalidDataException($"{label} could not be parsed: {ex.Message}", ex);
            }
        }

        private static object ParseStructuredDocument(string contents)
        {
            try
            {
                return VendoredYamlParser.Parse(contents);
            }
            catch (VendoredYamlParseException yamlEx)
            {
                try
                {
                    JToken token = JToken.Parse(contents);
                    return ConvertJsonToken(token);
                }
                catch (Exception jsonEx)
                {
                    throw new InvalidDataException($"YAML parse failed ({yamlEx.Message}) and legacy JSON-compatible parse also failed ({jsonEx.Message}).", jsonEx);
                }
            }
        }

        private static object ConvertJsonToken(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Object => ((JObject)token).Properties().ToDictionary(property => property.Name, property => ConvertJsonToken(property.Value), StringComparer.Ordinal),
                JTokenType.Array => ((JArray)token).Select(ConvertJsonToken).ToList(),
                JTokenType.Integer => token.Value<int>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Null => null,
                _ => token.ToString()
            };
        }

        private static ScenarioManifestData BindManifest(IDictionary<string, object> root)
        {
            return new ScenarioManifestData
            {
                ScenarioName = ReadString(root, "scenario_name"),
                SceneName = ReadString(root, "scene_name"),
                StarterKind = ReadString(root, "starter_kind", defaultValue: CoreScenarioStarterDefinition.StarterKindCore),
                AgentClass = ReadString(root, "agent_class"),
                BehaviorName = ReadString(root, "behavior_name"),
                TrainingConfig = ReadString(root, "training_config"),
                RuntimeBlueprint = ReadString(root, "runtime_blueprint", defaultValue: "scenario_blueprint.yaml"),
                ViewerPromise = ReadString(root, "viewer_promise"),
                LearningGoal = ReadString(root, "learning_goal"),
                SuccessConditions = ReadStringList(root, "success_conditions"),
                FailureConditions = ReadStringList(root, "failure_conditions"),
                ObservationContract = BindObservationContract(GetMap(root, "observation_contract")),
                ActionContract = BindActionContract(GetMap(root, "action_contract")),
                RewardRules = BindRewardRules(GetMap(root, "reward_rules")),
                RandomizationKnobs = BindRandomizationKnobs(GetList(root, "randomization_knobs")),
                DifficultyStages = BindDifficultyStages(GetList(root, "difficulty_stages")),
                VisualTheme = BindVisualTheme(GetMap(root, "visual_theme")),
                VisualHooks = ReadStringList(root, "visual_hooks"),
                ThumbnailMoment = ReadString(root, "thumbnail_moment"),
                CameraPlan = BindCameraPlan(GetMap(root, "camera_plan")),
                AcceptanceCriteria = ReadStringList(root, "acceptance_criteria"),
                BaselineRun = ReadString(root, "baseline_run"),
                SpecVersion = ReadInt(root, "spec_version", defaultValue: 3)
            };
        }

        private static ScenarioBlueprintData BindBlueprint(IDictionary<string, object> root, ScenarioManifestData manifest)
        {
            var blueprint = new ScenarioBlueprintData
            {
                Version = ReadInt(root, "version", defaultValue: 3),
                SceneRoles = BindSceneRoles(GetMap(root, "scene_roles")),
                CameraRoles = BindCameraRoles(GetMap(root, "camera_roles")),
                RecordingDefaults = BindRecordingDefaults(GetMap(root, "recording_defaults")),
                OverlayBindings = BindOverlayBindings(GetMap(root, "overlay_bindings")),
                HighlightBindings = BindHighlightBindings(GetMap(root, "highlight_bindings"), GetMap(root, "highlight_defaults")),
                VisualDefaults = BindVisualDefaults(GetMap(root, "visual_defaults"))
            };

            List<object> agents = GetList(root, "agents").ToList();
            blueprint.Agents = agents.Count > 0
                ? agents.Select(entry => BindAgentBlueprint(AsMap(entry, "agents[]"))).ToList()
                : new List<ScenarioAgentBlueprintData>();

            if (blueprint.Agents.Count == 0)
            {
                blueprint.Agents.Add(new ScenarioAgentBlueprintData
                {
                    Role = "hero",
                    ClassName = manifest?.AgentClass ?? string.Empty,
                    BehaviorName = manifest?.BehaviorName ?? string.Empty,
                    Team = "solo",
                    Primary = true
                });
            }

            return blueprint;
        }

        private static ObservationContractData BindObservationContract(IDictionary<string, object> map)
        {
            return new ObservationContractData
            {
                VectorSize = ReadInt(map, "vector_size"),
                Items = ReadStringList(map, "items")
            };
        }

        private static ActionContractData BindActionContract(IDictionary<string, object> map)
        {
            return new ActionContractData
            {
                Type = ReadString(map, "type", defaultValue: "continuous"),
                Size = ReadInt(map, "size", defaultValue: 2),
                Mapping = ReadStringList(map, "mapping")
            };
        }

        private static RewardRulesData BindRewardRules(IDictionary<string, object> map)
        {
            return new RewardRulesData
            {
                Terminal = ReadStringList(map, "terminal"),
                Dense = ReadStringList(map, "dense"),
                Penalties = ReadStringList(map, "penalties")
            };
        }

        private static List<RandomizationKnobData> BindRandomizationKnobs(IReadOnlyList<object> values)
        {
            return values.Select(value =>
            {
                IDictionary<string, object> map = AsMap(value, "randomization_knobs[]");
                return new RandomizationKnobData
                {
                    Name = ReadString(map, "name"),
                    Purpose = ReadString(map, "purpose"),
                    DefaultValue = ReadString(map, "default")
                };
            }).ToList();
        }

        private static List<DifficultyStageData> BindDifficultyStages(IReadOnlyList<object> values)
        {
            return values.Select(value =>
            {
                IDictionary<string, object> map = AsMap(value, "difficulty_stages[]");
                return new DifficultyStageData
                {
                    Name = ReadString(map, "name"),
                    Intent = ReadString(map, "intent")
                };
            }).ToList();
        }

        private static VisualThemeData BindVisualTheme(IDictionary<string, object> map)
        {
            return new VisualThemeData
            {
                Style = ReadString(map, "style"),
                Readability = ReadString(map, "readability")
            };
        }

        private static CameraPlanData BindCameraPlan(IDictionary<string, object> map)
        {
            return new CameraPlanData
            {
                DefaultView = ReadString(map, "default_view", defaultValue: "ExplainView"),
                RecordingCuts = ReadStringList(map, "recording_cuts"),
                ThumbnailReference = ReadString(map, "thumbnail_reference", defaultValue: "ExplainView")
            };
        }

        private static ScenarioAgentBlueprintData BindAgentBlueprint(IDictionary<string, object> map)
        {
            return new ScenarioAgentBlueprintData
            {
                Role = ReadString(map, "role", defaultValue: "hero"),
                ClassName = ReadString(map, "class"),
                BehaviorName = ReadString(map, "behavior"),
                Team = ReadString(map, "team", defaultValue: "solo"),
                Primary = ReadBool(map, "primary")
            };
        }

        private static SceneRolesData BindSceneRoles(IDictionary<string, object> map)
        {
            return new SceneRolesData
            {
                ArenaRoot = ReadString(map, "arena_root", defaultValue: "EnvironmentRoot"),
                PrimaryTarget = ReadString(map, "primary_target", defaultValue: "PrimaryTarget"),
                PrimaryHazard = ReadString(map, "primary_hazard"),
                OverlayAnchor = ReadString(map, "overlay_anchor", defaultValue: "OverlayAnchor")
            };
        }

        private static CameraRolesData BindCameraRoles(IDictionary<string, object> map)
        {
            return new CameraRolesData
            {
                DefaultCamera = ReadString(map, "default_camera", defaultValue: "ExplainView"),
                Explain = ReadString(map, "explain", defaultValue: "ExplainView"),
                WideA = ReadString(map, "wide_a", defaultValue: "WideA"),
                WideB = ReadString(map, "wide_b", defaultValue: "WideB"),
                FollowOptional = ReadString(map, "follow_optional", defaultValue: "FollowView"),
                ComparisonOptional = ReadString(map, "comparison_optional")
            };
        }

        private static RecordingDefaultsData BindRecordingDefaults(IDictionary<string, object> map)
        {
            return new RecordingDefaultsData
            {
                HideTrainingUi = ReadBool(map, "hide_training_ui", defaultValue: true),
                EnableCameraSwitching = ReadBool(map, "enable_camera_switching", defaultValue: true),
                CameraSwitchInterval = ReadFloat(map, "camera_switch_interval", defaultValue: 9f),
                CameraCycleRoles = ReadStringList(map, "camera_cycle_roles"),
                FollowCameraRole = ReadString(map, "follow_camera_role", defaultValue: "follow_optional"),
                FollowTargetRole = ReadString(map, "follow_target_role", defaultValue: "hero"),
                FollowTargetTeam = ReadString(map, "follow_target_team")
            };
        }

        private static OverlayBindingsData BindOverlayBindings(IDictionary<string, object> map)
        {
            return new OverlayBindingsData
            {
                TargetAgentRole = ReadString(map, "target_agent_role", defaultValue: "hero"),
                TargetAgentRoles = ReadStringList(map, "target_agent_roles"),
                TargetAgentTeam = ReadString(map, "target_agent_team"),
                ObjectiveSource = ReadString(map, "objective_source", defaultValue: "viewer_promise"),
                ScenarioLabelSource = ReadString(map, "scenario_label_source", defaultValue: "scenario_name")
            };
        }

        private static HighlightBindingsData BindHighlightBindings(IDictionary<string, object> preferred, IDictionary<string, object> legacy)
        {
            IDictionary<string, object> map = preferred.Count > 0 ? preferred : legacy;
            return new HighlightBindingsData
            {
                TrackedAgentRole = ReadString(map, "tracked_agent_role", defaultValue: "hero"),
                TrackedAgentRoles = ReadStringList(map, "tracked_agent_roles"),
                TrackedAgentTeam = ReadString(map, "tracked_agent_team"),
                ExportHighlightsToJsonl = ReadBool(map, "export_highlights_to_jsonl", defaultValue: true),
                ExportSnapshotsToJsonl = ReadBool(map, "export_snapshots_to_jsonl", defaultValue: true)
            };
        }

        private static VisualDefaultsData BindVisualDefaults(IDictionary<string, object> map)
        {
            return new VisualDefaultsData
            {
                ApplyReadabilityKit = ReadBool(map, "apply_readability_kit", defaultValue: true),
                HeroMaterial = ReadString(map, "hero_material", defaultValue: "V2HeroReadable"),
                TargetMaterial = ReadString(map, "target_material", defaultValue: "V2TargetReadable"),
                HazardMaterial = ReadString(map, "hazard_material", defaultValue: "V2HazardReadable"),
                FloorMaterial = ReadString(map, "floor_material", defaultValue: "V2FloorReadable")
            };
        }

        private static IDictionary<string, object> AsMap(object value, string label)
        {
            if (value == null)
            {
                return new Dictionary<string, object>(StringComparer.Ordinal);
            }

            if (value is IDictionary<string, object> map)
            {
                return map;
            }

            throw new InvalidDataException($"{label} must be a mapping.");
        }

        private static IReadOnlyList<object> GetList(IDictionary<string, object> map, params string[] keys)
        {
            object value = GetValue(map, keys);
            if (value == null)
            {
                return Array.Empty<object>();
            }

            if (value is IReadOnlyList<object> list)
            {
                return list;
            }

            if (value is IEnumerable<object> enumerable)
            {
                return enumerable.ToArray();
            }

            throw new InvalidDataException($"{keys[0]} must be a sequence.");
        }

        private static IDictionary<string, object> GetMap(IDictionary<string, object> map, params string[] keys)
        {
            object value = GetValue(map, keys);
            return value == null ? new Dictionary<string, object>(StringComparer.Ordinal) : AsMap(value, keys[0]);
        }

        private static object GetValue(IDictionary<string, object> map, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key) || map == null)
                {
                    continue;
                }

                if (map.TryGetValue(key, out object value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string ReadString(IDictionary<string, object> map, string key, string defaultValue = "")
        {
            object value = GetValue(map, key);
            return value == null ? defaultValue : Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? defaultValue;
        }

        private static int ReadInt(IDictionary<string, object> map, string key, int defaultValue = 0)
        {
            object value = GetValue(map, key);
            if (value == null)
            {
                return defaultValue;
            }

            return value switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                double doubleValue => Convert.ToInt32(doubleValue),
                _ when int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) => parsed,
                _ => defaultValue
            };
        }

        private static float ReadFloat(IDictionary<string, object> map, string key, float defaultValue = 0f)
        {
            object value = GetValue(map, key);
            if (value == null)
            {
                return defaultValue;
            }

            return value switch
            {
                float floatValue => floatValue,
                double doubleValue => (float)doubleValue,
                int intValue => intValue,
                long longValue => longValue,
                _ when float.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) => parsed,
                _ => defaultValue
            };
        }

        private static bool ReadBool(IDictionary<string, object> map, string key, bool defaultValue = false)
        {
            object value = GetValue(map, key);
            if (value == null)
            {
                return defaultValue;
            }

            return value switch
            {
                bool boolValue => boolValue,
                _ when bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out bool parsed) => parsed,
                _ => defaultValue
            };
        }

        private static List<string> ReadStringList(IDictionary<string, object> map, string key)
        {
            return GetList(map, key)
                .Where(item => item != null)
                .Select(item => Convert.ToString(item, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty)
                .ToList();
        }
    }
}
