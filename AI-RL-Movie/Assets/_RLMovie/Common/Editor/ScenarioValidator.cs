using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RLMovie.Common;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RLMovie.Editor
{
    public static class ScenarioValidator
    {
        private static readonly string[] DefaultCameraCycleRoles =
        {
            "explain",
            "wide_a",
            "wide_b",
            "follow_optional"
        };

        private static readonly HashSet<string> AllowedScenarioLabelSources = new HashSet<string>(StringComparer.Ordinal)
        {
            "scenario_name",
            "scene_name"
        };

        private static readonly HashSet<string> AllowedObjectiveSources = new HashSet<string>(StringComparer.Ordinal)
        {
            "viewer_promise",
            "learning_goal",
            "thumbnail_moment"
        };

        private static readonly HashSet<string> BuiltInSceneRoles = new HashSet<string>(StringComparer.Ordinal)
        {
            "arena_root",
            "primary_target",
            "primary_hazard",
            "overlay_anchor"
        };

        [MenuItem("RLMovie/Validate Current Scenario")]
        public static void ValidateCurrentSceneMenu()
        {
            ScenarioValidationReport report = ValidateCurrentScene(true);
            string title = report.IsValid ? "Scenario Validation" : "Scenario Validation Failed";
            string message = report.IsValid
                ? $"Validation passed.\n\nWarnings: {report.Warnings.Count}\nSee Console for details."
                : $"Validation failed.\n\nErrors: {report.Errors.Count}\nWarnings: {report.Warnings.Count}\nSee Console for details.";

            EditorStatus.ShowNonBlockingMessage(title, message, isError: !report.IsValid);
        }

        public static ScenarioValidationReport ValidateCurrentScene(bool logToConsole)
        {
            var report = new ScenarioValidationReport();
            Scene activeScene = SceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(activeScene.path))
            {
                report.AddError("The active scene is unsaved. Save the scene before validation.");
                LogReport(report, logToConsole);
                return report;
            }

            if (activeScene.isDirty)
            {
                report.AddWarning("The active scene has unsaved changes. Save before training or recording.");
            }

            ValidateCurrentSceneInternal(activeScene, report);
            LogReport(report, logToConsole);
            return report;
        }

        private static void ValidateCurrentSceneInternal(Scene activeScene, ScenarioValidationReport report)
        {
            ScenarioConfigPaths paths;
            try
            {
                paths = ScenarioConfigIO.ResolveScenarioPaths(activeScene);
            }
            catch (Exception ex)
            {
                report.AddError(ex.Message);
                return;
            }

            if (!Directory.Exists(paths.ConfigDirectory))
            {
                report.AddError($"Config folder not found: {paths.ConfigDirectory}");
                return;
            }

            if (!File.Exists(paths.ManifestPath))
            {
                report.AddError($"scenario_manifest.yaml not found: {paths.ManifestPath}");
                return;
            }

            string[] trainingConfigCandidates = ScenarioConfigIO.GetCandidateTrainingConfigPaths(paths);
            ScenarioManifestData manifest;
            ScenarioBlueprintData blueprint;
            string blueprintPath;

            try
            {
                manifest = ScenarioConfigIO.LoadManifest(paths.ManifestPath);
                blueprintPath = ScenarioConfigIO.ResolveBlueprintPath(paths.ConfigDirectory, manifest);
                blueprint = ScenarioConfigIO.LoadBlueprint(paths.ConfigDirectory, manifest);
            }
            catch (Exception ex)
            {
                report.AddError(ex.Message);
                return;
            }

            ScenarioStarterDefinition starterDefinition = ValidateStarterKind(manifest, report);
            ValidateManifest(paths, manifest, activeScene.name, trainingConfigCandidates, report);
            ValidateBlueprint(manifest, blueprint, blueprintPath, report);

            string selectedTrainingConfigPath = ScenarioConfigIO.ResolveSelectedTrainingConfigPath(paths, manifest);
            ValidateTrainingConfig(selectedTrainingConfigPath, manifest, report);
            string[] behaviorNamesFromConfig = File.Exists(selectedTrainingConfigPath)
                ? ReadBehaviorNames(selectedTrainingConfigPath).Distinct(StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();

            if (File.Exists(selectedTrainingConfigPath)
                && behaviorNamesFromConfig.Length > 0
                && !behaviorNamesFromConfig.Contains(manifest.BehaviorName, StringComparer.Ordinal))
            {
                report.AddError($"The selected training config `{Path.GetFileName(selectedTrainingConfigPath)}` does not declare behavior `{manifest.BehaviorName}`.");
            }

            BaseRLAgent[] agents = UnityEngine.Object.FindObjectsByType<BaseRLAgent>(FindObjectsSortMode.None)
                .Where(agent => agent != null && agent.gameObject.scene == activeScene)
                .ToArray();

            ValidateAgents(activeScene, agents, manifest, behaviorNamesFromConfig, report);
            ScenarioGoldenSpine spine = ValidateSharedBackbone(activeScene, manifest, blueprint, agents, report);

            if (starterDefinition != null && spine != null)
            {
                starterDefinition.ValidateStarterSpecificRules(
                    new ScenarioStarterValidationContext
                    {
                        ActiveScene = activeScene,
                        Paths = paths,
                        Manifest = manifest,
                        Blueprint = blueprint,
                        Agents = agents,
                        Spine = spine
                    },
                    report);
            }
        }

        private static ScenarioStarterDefinition ValidateStarterKind(ScenarioManifestData manifest, ScenarioValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(manifest?.StarterKind))
            {
                report.AddError("manifest.starter_kind is required.");
                return null;
            }

            if (ScenarioStarterRegistry.TryGet(manifest.StarterKind, out ScenarioStarterDefinition definition))
            {
                return definition;
            }

            report.AddError($"manifest.starter_kind `{manifest.StarterKind}` is not registered. Available starter kinds: {string.Join(", ", ScenarioStarterRegistry.GetRegisteredKinds())}");
            return null;
        }

        private static void ValidateManifest(ScenarioConfigPaths paths, ScenarioManifestData manifest, string activeSceneName, string[] trainingConfigCandidates, ScenarioValidationReport report)
        {
            ValidateRequiredString(manifest.ScenarioName, "manifest.scenario_name", report);
            ValidateRequiredString(manifest.SceneName, "manifest.scene_name", report);
            ValidateRequiredString(manifest.StarterKind, "manifest.starter_kind", report);
            ValidateRequiredString(manifest.AgentClass, "manifest.agent_class", report);
            ValidateRequiredString(manifest.BehaviorName, "manifest.behavior_name", report);
            ValidateRequiredString(manifest.TrainingConfig, "manifest.training_config", report);
            ValidateRequiredString(manifest.RuntimeBlueprint, "manifest.runtime_blueprint", report);
            ValidateViewerFacingString(manifest.ViewerPromise, "manifest.viewer_promise", report);
            ValidateViewerFacingString(manifest.LearningGoal, "manifest.learning_goal", report);
            ValidateNonEmptyList(manifest.VisualHooks, "manifest.visual_hooks", report, placeholderCheck: true);
            ValidateViewerFacingString(manifest.ThumbnailMoment, "manifest.thumbnail_moment", report);
            ValidateNonEmptyList(manifest.AcceptanceCriteria, "manifest.acceptance_criteria", report, placeholderCheck: true);

            if (!string.Equals(manifest.ScenarioName, activeSceneName, StringComparison.Ordinal))
            {
                report.AddError($"manifest.scenario_name must match the active scene name `{activeSceneName}`. Current value: `{manifest.ScenarioName}`.");
            }

            if (!string.Equals(manifest.SceneName, activeSceneName, StringComparison.Ordinal))
            {
                report.AddError($"manifest.scene_name must match the active scene name `{activeSceneName}`. Current value: `{manifest.SceneName}`.");
            }

            if (manifest.SpecVersion < 2)
            {
                report.AddError($"manifest.spec_version must be 2 or higher for the shared backbone. Current value: {manifest.SpecVersion}");
            }
            else if (manifest.SpecVersion < 3)
            {
                report.AddWarning($"manifest.spec_version is {manifest.SpecVersion}. Version 3 is recommended for the extensible core contract.");
            }

            if (manifest.ObservationContract == null || manifest.ObservationContract.VectorSize <= 0)
            {
                report.AddError("manifest.observation_contract.vector_size must be greater than 0.");
            }

            if (manifest.ActionContract == null || manifest.ActionContract.Size <= 0)
            {
                report.AddError("manifest.action_contract.size must be greater than 0.");
            }

            if (manifest.CameraPlan == null)
            {
                report.AddError("manifest.camera_plan is required.");
            }
            else
            {
                ValidateViewerFacingString(manifest.CameraPlan.DefaultView, "manifest.camera_plan.default_view", report);
                ValidateNonEmptyList(manifest.CameraPlan.RecordingCuts, "manifest.camera_plan.recording_cuts", report, placeholderCheck: false);
                ValidateViewerFacingString(manifest.CameraPlan.ThumbnailReference, "manifest.camera_plan.thumbnail_reference", report);
            }

            if (!string.IsNullOrWhiteSpace(manifest.TrainingConfig)
                && !string.Equals(Path.GetFileName(manifest.TrainingConfig), manifest.TrainingConfig, StringComparison.Ordinal))
            {
                report.AddError("manifest.training_config must be a file name under Config/, not a relative path.");
            }

            if (!string.IsNullOrWhiteSpace(manifest.RuntimeBlueprint)
                && !string.Equals(Path.GetFileName(manifest.RuntimeBlueprint), manifest.RuntimeBlueprint, StringComparison.Ordinal))
            {
                report.AddError("manifest.runtime_blueprint must be a file name under Config/, not a relative path.");
            }

            if (!string.IsNullOrWhiteSpace(manifest.TrainingConfig))
            {
                string selectedTrainingConfig = ScenarioConfigIO.ResolveSelectedTrainingConfigPath(paths, manifest);
                if (!File.Exists(selectedTrainingConfig))
                {
                    report.AddError($"manifest.training_config points to `{manifest.TrainingConfig}`, but that file was not found in Config/.");
                }
                else if (!trainingConfigCandidates.Contains(selectedTrainingConfig, StringComparer.OrdinalIgnoreCase))
                {
                    report.AddError($"manifest.training_config points to `{manifest.TrainingConfig}`, but it is not part of the supported training config set. Available configs: {string.Join(", ", trainingConfigCandidates.Select(Path.GetFileName))}");
                }
            }
        }

        private static void ValidateBlueprint(ScenarioManifestData manifest, ScenarioBlueprintData blueprint, string blueprintPath, ScenarioValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(blueprintPath) || !File.Exists(blueprintPath))
            {
                report.AddError("The manifest references a blueprint file that does not exist.");
            }

            if (blueprint.Version < 2)
            {
                report.AddError($"scenario_blueprint.version must be 2 or higher. Current value: {blueprint.Version}");
            }
            else if (blueprint.Version < 3)
            {
                report.AddWarning($"scenario_blueprint.version is {blueprint.Version}. Version 3 is recommended for the extensible core contract.");
            }

            IReadOnlyList<ScenarioAgentBlueprintData> agentBlueprints = blueprint.EnumerateAgents(manifest);
            if (agentBlueprints.Count == 0)
            {
                report.AddError("scenario_blueprint.agents must declare at least one agent role.");
            }

            ValidateUniqueValues(agentBlueprints.Select(agent => agent.Role), "scenario_blueprint.agents.role", report);

            int primaryAgents = agentBlueprints.Count(agent => agent.Primary);
            if (primaryAgents == 0)
            {
                report.AddError("scenario_blueprint.agents must include one primary agent.");
            }
            else if (primaryAgents > 1)
            {
                report.AddError("scenario_blueprint.agents must include only one primary agent.");
            }

            ScenarioAgentBlueprintData primaryBlueprint = blueprint.ResolvePrimaryAgent(manifest);
            if (primaryBlueprint != null)
            {
                if (!string.Equals(primaryBlueprint.ClassName, manifest.AgentClass, StringComparison.Ordinal))
                {
                    report.AddError($"The primary scenario_blueprint agent must use manifest.agent_class `{manifest.AgentClass}`. Current value: `{primaryBlueprint.ClassName}`.");
                }

                if (!string.Equals(primaryBlueprint.BehaviorName, manifest.BehaviorName, StringComparison.Ordinal))
                {
                    report.AddError($"The primary scenario_blueprint agent must use manifest.behavior_name `{manifest.BehaviorName}`. Current value: `{primaryBlueprint.BehaviorName}`.");
                }
            }

            foreach (ScenarioAgentBlueprintData agentBlueprint in agentBlueprints)
            {
                ValidateRequiredString(agentBlueprint.Role, $"scenario_blueprint.agents[{agentBlueprint.Role}].role", report);
                ValidateRequiredString(agentBlueprint.ClassName, $"scenario_blueprint.agents[{agentBlueprint.Role}].class", report);
                ValidateRequiredString(agentBlueprint.BehaviorName, $"scenario_blueprint.agents[{agentBlueprint.Role}].behavior", report);
                ValidateRequiredString(agentBlueprint.Team, $"scenario_blueprint.agents[{agentBlueprint.Role}].team", report);
            }

            ValidateRequiredString(blueprint.SceneRoles?.ArenaRoot, "scenario_blueprint.scene_roles.arena_root", report);
            ValidateRequiredString(blueprint.CameraRoles?.DefaultCamera, "scenario_blueprint.camera_roles.default_camera", report);

            IReadOnlyList<string> cycleRoles = GetRecordingCycleRoles(blueprint);
            if (cycleRoles.Count == 0)
            {
                report.AddError("scenario_blueprint.recording_defaults.camera_cycle_roles must contain at least one camera role.");
            }

            ValidateUniqueValues(cycleRoles, "scenario_blueprint.recording_defaults.camera_cycle_roles", report);
            ValidateKnownValue(blueprint.OverlayBindings?.ScenarioLabelSource, AllowedScenarioLabelSources, "scenario_blueprint.overlay_bindings.scenario_label_source", report);
            ValidateKnownValue(blueprint.OverlayBindings?.ObjectiveSource, AllowedObjectiveSources, "scenario_blueprint.overlay_bindings.objective_source", report);
            ValidateAgentRoleList(agentBlueprints, ScenarioConfigIO.ResolveOverlayTargetRoles(blueprint, manifest), "scenario_blueprint.overlay_bindings.target_agent_roles", report);
            ValidateAgentRoleList(agentBlueprints, ScenarioConfigIO.ResolveHighlightTargetRoles(blueprint, manifest), "scenario_blueprint.highlight_bindings.tracked_agent_roles", report);

            if (!string.IsNullOrWhiteSpace(blueprint.OverlayBindings?.TargetAgentTeam)
                && !agentBlueprints.Any(agent => string.Equals(agent.Team, blueprint.OverlayBindings.TargetAgentTeam, StringComparison.Ordinal)))
            {
                report.AddError($"scenario_blueprint.overlay_bindings.target_agent_team `{blueprint.OverlayBindings.TargetAgentTeam}` does not match any declared agent team.");
            }

            if (!string.IsNullOrWhiteSpace(blueprint.HighlightBindings?.TrackedAgentTeam)
                && !agentBlueprints.Any(agent => string.Equals(agent.Team, blueprint.HighlightBindings.TrackedAgentTeam, StringComparison.Ordinal)))
            {
                report.AddError($"scenario_blueprint.highlight_bindings.tracked_agent_team `{blueprint.HighlightBindings.TrackedAgentTeam}` does not match any declared agent team.");
            }

            if (!string.IsNullOrWhiteSpace(blueprint.RecordingDefaults?.FollowTargetTeam)
                && !agentBlueprints.Any(agent => string.Equals(agent.Team, blueprint.RecordingDefaults.FollowTargetTeam, StringComparison.Ordinal)))
            {
                report.AddError($"scenario_blueprint.recording_defaults.follow_target_team `{blueprint.RecordingDefaults.FollowTargetTeam}` does not match any declared agent team.");
            }

            string followTargetRole = FirstNonEmpty(blueprint.RecordingDefaults?.FollowTargetRole, blueprint.ResolvePrimaryAgentRole(manifest), "hero");
            if (!string.IsNullOrWhiteSpace(followTargetRole)
                && !agentBlueprints.Any(agent => string.Equals(agent.Role, followTargetRole, StringComparison.Ordinal))
                && !BuiltInSceneRoles.Contains(followTargetRole))
            {
                report.AddError($"scenario_blueprint.recording_defaults.follow_target_role `{followTargetRole}` does not match any declared agent role or built-in scene role.");
            }
        }

        private static void ValidateTrainingConfig(string trainingConfigPath, ScenarioManifestData manifest, ScenarioValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(manifest.TrainingConfig))
            {
                report.AddError("manifest.training_config is required.");
                return;
            }

            if (!File.Exists(trainingConfigPath))
            {
                report.AddError($"Training config referenced by manifest was not found: {manifest.TrainingConfig}");
                return;
            }

            if (!ReadBehaviorNames(trainingConfigPath).Any())
            {
                report.AddError($"Training config `{Path.GetFileName(trainingConfigPath)}` does not declare any behaviors.");
            }
        }

        private static void ValidateAgents(Scene activeScene, BaseRLAgent[] agents, ScenarioManifestData manifest, IReadOnlyCollection<string> behaviorNamesFromConfig, ScenarioValidationReport report)
        {
            if (agents.Length == 0)
            {
                report.AddError("No BaseRLAgent instances were found in the active scene.");
                return;
            }

            bool foundManifestAgent = false;
            foreach (BaseRLAgent agent in agents)
            {
                string agentName = agent.gameObject.name;
                Type agentType = agent.GetType();
                var behaviorParameters = agent.GetComponent<BehaviorParameters>();
                if (behaviorParameters == null)
                {
                    report.AddError($"{agentName}: BehaviorParameters is required.");
                }
                else if (behaviorNamesFromConfig.Count > 0 && !behaviorNamesFromConfig.Contains(behaviorParameters.BehaviorName))
                {
                    report.AddError($"{agentName}: Behavior name `{behaviorParameters.BehaviorName}` is not declared in the selected training config.");
                }

                if (agent.GetComponent<DecisionRequester>() == null)
                {
                    report.AddError($"{agentName}: DecisionRequester is required.");
                }

                ValidateRequiredSharedReferences(activeScene, agent, report);

                if (!string.Equals(agentType.Name, manifest.AgentClass, StringComparison.Ordinal))
                {
                    continue;
                }

                foundManifestAgent = true;
                if (behaviorParameters != null && !string.Equals(behaviorParameters.BehaviorName, manifest.BehaviorName, StringComparison.Ordinal))
                {
                    report.AddError($"{agentName}: BehaviorParameters.BehaviorName must match manifest.behavior_name `{manifest.BehaviorName}`. Current value: `{behaviorParameters.BehaviorName}`.");
                }
            }

            if (!foundManifestAgent)
            {
                report.AddError($"No agent of type `{manifest.AgentClass}` was found in the active scene.");
            }
        }

        private static void ValidateRequiredSharedReferences(Scene activeScene, BaseRLAgent agent, ScenarioValidationReport report)
        {
            Component[] components = agent.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    report.AddError($"{agent.gameObject.name}: A required component script is missing.");
                    continue;
                }

                FieldInfo[] fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    FieldInfo field = fields[fieldIndex];
                    RequiredSharedReferenceAttribute attribute = field.GetCustomAttribute<RequiredSharedReferenceAttribute>(inherit: true);
                    if (attribute == null)
                    {
                        continue;
                    }

                    if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    {
                        report.AddError($"{agent.gameObject.name}: {component.GetType().Name}.{field.Name} uses RequiredSharedReference but is not a UnityEngine.Object field.");
                        continue;
                    }

                    var value = field.GetValue(component) as UnityEngine.Object;
                    string label = string.IsNullOrWhiteSpace(attribute.Label) ? $"{component.GetType().Name}.{field.Name}" : attribute.Label;
                    if (value == null)
                    {
                        report.AddError($"{agent.gameObject.name}: Required shared reference `{label}` is not assigned.");
                        continue;
                    }

                    if (value is Component referencedComponent && referencedComponent.gameObject.scene != activeScene)
                    {
                        report.AddError($"{agent.gameObject.name}: Required shared reference `{label}` points outside the active scene.");
                    }
                    else if (value is GameObject referencedGameObject && referencedGameObject.scene != activeScene)
                    {
                        report.AddError($"{agent.gameObject.name}: Required shared reference `{label}` points outside the active scene.");
                    }
                }
            }
        }

        private static ScenarioGoldenSpine ValidateSharedBackbone(Scene activeScene, ScenarioManifestData manifest, ScenarioBlueprintData blueprint, BaseRLAgent[] agents, ScenarioValidationReport report)
        {
            ScenarioGoldenSpine[] spines = UnityEngine.Object.FindObjectsByType<ScenarioGoldenSpine>(FindObjectsSortMode.None)
                .Where(spine => spine != null && spine.gameObject.scene == activeScene)
                .ToArray();

            if (spines.Length == 0)
            {
                report.AddError("ScenarioGoldenSpine is required for the shared backbone.");
                return null;
            }

            if (spines.Length > 1)
            {
                report.AddError("Only one ScenarioGoldenSpine is allowed per scene.");
                return null;
            }

            ScenarioGoldenSpine spine = spines[0];
            if (spine.EnvironmentRoot == null)
            {
                report.AddError("ScenarioGoldenSpine.environmentRoot is required.");
            }

            if (spine.EnvironmentManager == null)
            {
                report.AddError("ScenarioGoldenSpine.environmentManager is required.");
            }

            if (spine.TrainingVisualizer == null)
            {
                report.AddError("ScenarioGoldenSpine.trainingVisualizer is required.");
            }

            if (spine.RecordingHelper == null)
            {
                report.AddError("ScenarioGoldenSpine.recordingHelper is required.");
            }

            if (spine.ScenarioBroadcastOverlay == null)
            {
                report.AddError("ScenarioGoldenSpine.scenarioBroadcastOverlay is required.");
            }

            if (spine.ScenarioHighlightTracker == null)
            {
                report.AddError("ScenarioGoldenSpine.scenarioHighlightTracker is required.");
            }

            ValidateUniqueValues(spine.SceneRoles.Select(binding => binding.role), "ScenarioGoldenSpine.sceneRoles.role", report);
            ValidateUniqueValues(spine.AgentRoles.Select(binding => binding.role), "ScenarioGoldenSpine.agentRoles.role", report);
            ValidateUniqueValues(spine.TeamRoles.Select(binding => binding.team), "ScenarioGoldenSpine.teamRoles.team", report);
            ValidateUniqueValues(spine.CameraRoles.Select(binding => binding.role), "ScenarioGoldenSpine.cameraRoles.role", report);

            IReadOnlyList<ScenarioAgentBlueprintData> agentBlueprints = blueprint.EnumerateAgents(manifest);
            ScenarioAgentBlueprintData primaryBlueprint = blueprint.ResolvePrimaryAgent(manifest);
            BaseRLAgent primaryAgent = null;

            for (int i = 0; i < agentBlueprints.Count; i++)
            {
                ScenarioAgentBlueprintData agentBlueprint = agentBlueprints[i];
                if (!spine.TryGetAgentRole(agentBlueprint.Role, out BaseRLAgent resolvedAgent) || resolvedAgent == null)
                {
                    report.AddError($"ScenarioGoldenSpine must resolve the `{agentBlueprint.Role}` agent role.");
                    continue;
                }

                if (!agents.Contains(resolvedAgent))
                {
                    report.AddError($"ScenarioGoldenSpine agent role `{agentBlueprint.Role}` does not point to an agent in the active scene.");
                }

                if (!string.Equals(resolvedAgent.GetType().Name, agentBlueprint.ClassName, StringComparison.Ordinal))
                {
                    report.AddError($"ScenarioGoldenSpine agent role `{agentBlueprint.Role}` must point to `{agentBlueprint.ClassName}`. Current type: `{resolvedAgent.GetType().Name}`.");
                }

                var behaviorParameters = resolvedAgent.GetComponent<BehaviorParameters>();
                if (behaviorParameters != null && !string.Equals(behaviorParameters.BehaviorName, agentBlueprint.BehaviorName, StringComparison.Ordinal))
                {
                    report.AddError($"ScenarioGoldenSpine agent role `{agentBlueprint.Role}` must use behavior `{agentBlueprint.BehaviorName}`. Current value: `{behaviorParameters.BehaviorName}`.");
                }

                ScenarioGoldenSpine.AgentRoleBinding[] matchingBindings = spine.AgentRoles
                    .Where(candidate => string.Equals(candidate.role, agentBlueprint.Role, StringComparison.Ordinal))
                    .ToArray();
                if (matchingBindings.Length == 0)
                {
                    report.AddError($"ScenarioGoldenSpine.agentRoles is missing metadata for role `{agentBlueprint.Role}`.");
                }
                else
                {
                    ScenarioGoldenSpine.AgentRoleBinding binding = matchingBindings[0];
                    if (!string.Equals(binding.team, agentBlueprint.Team, StringComparison.Ordinal))
                    {
                        report.AddError($"ScenarioGoldenSpine agent role `{agentBlueprint.Role}` must use team `{agentBlueprint.Team}`. Current value: `{binding.team}`.");
                    }

                    if (binding.primary != agentBlueprint.Primary)
                    {
                        report.AddError($"ScenarioGoldenSpine agent role `{agentBlueprint.Role}` primary flag must be `{agentBlueprint.Primary}`. Current value: `{binding.primary}`.");
                    }
                }

                if (primaryBlueprint != null && string.Equals(agentBlueprint.Role, primaryBlueprint.Role, StringComparison.Ordinal))
                {
                    primaryAgent = resolvedAgent;
                }
            }

            if (primaryBlueprint != null && primaryAgent == null)
            {
                report.AddError($"The primary agent role `{primaryBlueprint.Role}` could not be resolved.");
            }
            else if (primaryAgent != null && spine.PrimaryAgent != primaryAgent)
            {
                report.AddError($"ScenarioGoldenSpine.PrimaryAgent must resolve the primary agent role `{primaryBlueprint.Role}`.");
            }

            ValidateTeamBindings(spine, agentBlueprints, report);
            ValidateSceneRole(spine, spine.EnvironmentRoot, "arena_root", blueprint.SceneRoles.ArenaRoot, report, required: true);
            ValidateSceneRole(spine, null, "primary_target", blueprint.SceneRoles.PrimaryTarget, report, required: !string.IsNullOrWhiteSpace(blueprint.SceneRoles.PrimaryTarget));
            ValidateSceneRole(spine, null, "overlay_anchor", blueprint.SceneRoles.OverlayAnchor, report, required: !string.IsNullOrWhiteSpace(blueprint.SceneRoles.OverlayAnchor));
            ValidateSceneRole(spine, null, "primary_hazard", blueprint.SceneRoles.PrimaryHazard, report, required: !string.IsNullOrWhiteSpace(blueprint.SceneRoles.PrimaryHazard));

            ValidateCameraRole(spine, "default_camera", ResolveCameraAnchorName(spine, "default_camera"), report, required: true);
            IReadOnlyList<string> cycleRoles = GetRecordingCycleRoles(blueprint);
            for (int i = 0; i < cycleRoles.Count; i++)
            {
                string cycleRole = cycleRoles[i];
                ValidateCameraRole(spine, cycleRole, ResolveCameraAnchorName(spine, cycleRole), report, required: true);
            }

            string followCameraRole = FirstNonEmpty(blueprint.RecordingDefaults.FollowCameraRole, "follow_optional");
            if (!string.IsNullOrWhiteSpace(followCameraRole))
            {
                ValidateCameraRole(spine, followCameraRole, ResolveCameraAnchorName(spine, followCameraRole), report, required: true);
            }

            if (spine.TrainingVisualizer != null && primaryAgent != null)
            {
                ValidateTrainingVisualizer(spine.TrainingVisualizer, primaryAgent, report);
            }

            if (spine.RecordingHelper != null)
            {
                ValidateRecordingHelper(spine.RecordingHelper, spine, manifest, blueprint, report);
            }

            if (spine.ScenarioBroadcastOverlay != null)
            {
                ValidateOverlay(spine.ScenarioBroadcastOverlay, spine, manifest, blueprint, report);
            }

            if (spine.ScenarioHighlightTracker != null)
            {
                ValidateHighlightTracker(spine.ScenarioHighlightTracker, spine, manifest, blueprint, report);
            }

            if (spine.TryGetSceneRole("overlay_anchor", out Transform overlayAnchor)
                && overlayAnchor != null
                && spine.ScenarioBroadcastOverlay != null
                && !spine.ScenarioBroadcastOverlay.transform.IsChildOf(overlayAnchor))
            {
                report.AddError("ScenarioBroadcastOverlay must be parented under the `overlay_anchor` scene role.");
            }

            ValidateCameraPlan(manifest, blueprint, spine, report);
            return spine;
        }

        private static void ValidateTeamBindings(ScenarioGoldenSpine spine, IReadOnlyList<ScenarioAgentBlueprintData> agentBlueprints, ScenarioValidationReport report)
        {
            string[] teams = agentBlueprints
                .Select(agent => agent.Team)
                .Where(team => !string.IsNullOrWhiteSpace(team))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            for (int i = 0; i < teams.Length; i++)
            {
                string team = teams[i];
                BaseRLAgent[] expectedAgents = agentBlueprints
                    .Where(agent => string.Equals(agent.Team, team, StringComparison.Ordinal))
                    .Select(agent => spine.TryGetAgentRole(agent.Role, out BaseRLAgent resolved) ? resolved : null)
                    .Where(agent => agent != null)
                    .ToArray();

                IReadOnlyList<BaseRLAgent> resolvedAgents = spine.GetAgentsByTeam(team);
                foreach (BaseRLAgent expectedAgent in expectedAgents)
                {
                    if (!resolvedAgents.Contains(expectedAgent))
                    {
                        report.AddError($"ScenarioGoldenSpine team `{team}` does not include agent `{expectedAgent.gameObject.name}`.");
                    }
                }

                ScenarioAgentBlueprintData primaryTeamAgent = agentBlueprints.FirstOrDefault(agent =>
                    string.Equals(agent.Team, team, StringComparison.Ordinal) && agent.Primary);
                if (primaryTeamAgent == null)
                {
                    continue;
                }

                if (!spine.TryGetAgentRole(primaryTeamAgent.Role, out BaseRLAgent expectedPrimary) || expectedPrimary == null)
                {
                    continue;
                }

                if (!spine.TryGetPrimaryAgentForTeam(team, out BaseRLAgent actualPrimary) || actualPrimary == null)
                {
                    report.AddError($"ScenarioGoldenSpine must resolve a primary agent for team `{team}`.");
                }
                else if (actualPrimary != expectedPrimary)
                {
                    report.AddError($"ScenarioGoldenSpine team `{team}` must resolve primary agent role `{primaryTeamAgent.Role}`.");
                }
            }
        }

        private static void ValidateTrainingVisualizer(TrainingVisualizer visualizer, BaseRLAgent expectedTargetAgent, ScenarioValidationReport report)
        {
            var serializedObject = new SerializedObject(visualizer);
            ValidateObjectReferenceProperty(serializedObject, "targetAgent", expectedTargetAgent, "TrainingVisualizer.targetAgent", report);
        }

        private static void ValidateRecordingHelper(RecordingHelper helper, ScenarioGoldenSpine spine, ScenarioManifestData manifest, ScenarioBlueprintData blueprint, ScenarioValidationReport report)
        {
            var serializedObject = new SerializedObject(helper);
            string[] expectedCycleRoles = GetRecordingCycleRoles(blueprint).ToArray();
            string expectedFollowCameraRole = FirstNonEmpty(blueprint.RecordingDefaults.FollowCameraRole, "follow_optional");
            string expectedFollowTargetRole = FirstNonEmpty(blueprint.RecordingDefaults.FollowTargetRole, blueprint.ResolvePrimaryAgentRole(manifest), "hero");
            string expectedFollowTargetTeam = FirstNonEmpty(blueprint.RecordingDefaults.FollowTargetTeam, blueprint.ResolvePrimaryAgentTeam(manifest));

            ValidateObjectReferenceProperty(serializedObject, "scenarioSpine", spine, "RecordingHelper.scenarioSpine", report);
            ValidateBoolProperty(serializedObject, "hideUIWhenRecording", blueprint.RecordingDefaults.HideTrainingUi, "RecordingHelper.hideUIWhenRecording", report);
            ValidateBoolProperty(serializedObject, "enableCameraSwitching", blueprint.RecordingDefaults.EnableCameraSwitching, "RecordingHelper.enableCameraSwitching", report);
            ValidateFloatProperty(serializedObject, "cameraSwitchInterval", blueprint.RecordingDefaults.CameraSwitchInterval, "RecordingHelper.cameraSwitchInterval", report);
            ValidateStringArrayProperty(serializedObject, "cameraCycleRoles", expectedCycleRoles, "RecordingHelper.cameraCycleRoles", report);
            ValidateStringProperty(serializedObject, "defaultCameraRole", "default_camera", "RecordingHelper.defaultCameraRole", report);
            ValidateStringProperty(serializedObject, "followCameraRole", expectedFollowCameraRole, "RecordingHelper.followCameraRole", report);
            ValidateStringProperty(serializedObject, "followTargetRole", expectedFollowTargetRole, "RecordingHelper.followTargetRole", report);
            ValidateStringProperty(serializedObject, "followTargetTeam", expectedFollowTargetTeam, "RecordingHelper.followTargetTeam", report);

            string[] actualKnownCameraAnchors = GetStringArray(serializedObject, "knownCameraRoleNames");
            string[] expectedKnownCameraAnchors = GetKnownCameraAnchorNames(spine);
            string[] missingAnchors = expectedKnownCameraAnchors.Except(actualKnownCameraAnchors, StringComparer.Ordinal).ToArray();
            if (missingAnchors.Length > 0)
            {
                report.AddError($"RecordingHelper.knownCameraRoleNames is missing anchor names: {string.Join(", ", missingAnchors)}");
            }

            Transform explicitFollowTarget = GetObjectReferenceProperty(serializedObject, "followTarget") as Transform;
            Transform expectedFollowTarget = ResolveExpectedFollowTarget(spine, expectedFollowTargetRole, expectedFollowTargetTeam);
            if (expectedFollowTarget == null)
            {
                report.AddError("RecordingHelper follow target configuration does not resolve to a scene role or team primary agent.");
            }
            else if (explicitFollowTarget != null && explicitFollowTarget != expectedFollowTarget)
            {
                report.AddError($"RecordingHelper.followTarget must point to `{expectedFollowTarget.name}` when explicitly assigned. Current value: `{explicitFollowTarget.name}`.");
            }

            Transform[] legacyCameraPositions = GetObjectReferenceArray(serializedObject, "legacyCameraPositions").OfType<Transform>().ToArray();
            string[] expectedLegacyNames = expectedCycleRoles
                .Select(role => ResolveCameraAnchorName(spine, role))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();

            if (legacyCameraPositions.Length != expectedLegacyNames.Length)
            {
                report.AddError($"RecordingHelper.legacyCameraPositions count must match the resolved recording cuts. Expected: {expectedLegacyNames.Length} | Current: {legacyCameraPositions.Length}");
            }
            else
            {
                for (int i = 0; i < expectedLegacyNames.Length; i++)
                {
                    if (legacyCameraPositions[i] == null || !string.Equals(legacyCameraPositions[i].name, expectedLegacyNames[i], StringComparison.Ordinal))
                    {
                        report.AddError($"RecordingHelper.legacyCameraPositions[{i}] must reference `{expectedLegacyNames[i]}`.");
                    }
                }
            }
        }

        private static void ValidateOverlay(ScenarioBroadcastOverlay overlay, ScenarioGoldenSpine spine, ScenarioManifestData manifest, ScenarioBlueprintData blueprint, ScenarioValidationReport report)
        {
            var serializedObject = new SerializedObject(overlay);
            string expectedSingleRole = FirstNonEmpty(blueprint.OverlayBindings.TargetAgentRole, blueprint.ResolvePrimaryAgentRole(manifest), "hero");
            string[] expectedRoles = ScenarioConfigIO.ResolveOverlayTargetRoles(blueprint, manifest).ToArray();
            string expectedTeam = blueprint.OverlayBindings.TargetAgentTeam ?? string.Empty;
            string expectedScenarioLabel = ScenarioConfigIO.ResolveOverlayLabel(manifest, blueprint);
            string expectedGoalDescription = ScenarioConfigIO.ResolveOverlayObjective(manifest, blueprint);

            ValidateStringProperty(serializedObject, "targetAgentRole", expectedSingleRole, "ScenarioBroadcastOverlay.targetAgentRole", report);
            ValidateStringArrayProperty(serializedObject, "targetAgentRoles", expectedRoles, "ScenarioBroadcastOverlay.targetAgentRoles", report);
            ValidateStringProperty(serializedObject, "targetAgentTeam", expectedTeam, "ScenarioBroadcastOverlay.targetAgentTeam", report);
            ValidateStringProperty(serializedObject, "scenarioLabel", expectedScenarioLabel, "ScenarioBroadcastOverlay.scenarioLabel", report);
            ValidateStringProperty(serializedObject, "goalDescription", expectedGoalDescription, "ScenarioBroadcastOverlay.goalDescription", report);

            BaseRLAgent targetAgent = GetObjectReferenceProperty(serializedObject, "targetAgent") as BaseRLAgent;
            BaseRLAgent[] allowedAgents = ResolvePreferredAgents(spine, expectedSingleRole, expectedRoles, expectedTeam);
            if (allowedAgents.Length == 0)
            {
                report.AddError("ScenarioBroadcastOverlay target agent bindings do not resolve to any registered agent.");
            }
            else if (targetAgent != null && !allowedAgents.Contains(targetAgent))
            {
                report.AddError("ScenarioBroadcastOverlay.targetAgent must point to one of the configured target agent roles or teams when explicitly assigned.");
            }

            if (ScenarioConfigIO.IsPlaceholder(expectedScenarioLabel) || ScenarioConfigIO.IsPlaceholder(expectedGoalDescription))
            {
                report.AddError("ScenarioBroadcastOverlay is still sourcing placeholder label or goal text.");
            }
        }

        private static void ValidateHighlightTracker(ScenarioHighlightTracker tracker, ScenarioGoldenSpine spine, ScenarioManifestData manifest, ScenarioBlueprintData blueprint, ScenarioValidationReport report)
        {
            var serializedObject = new SerializedObject(tracker);
            string expectedSingleRole = FirstNonEmpty(blueprint.HighlightBindings.TrackedAgentRole, blueprint.ResolvePrimaryAgentRole(manifest), "hero");
            string[] expectedRoles = ScenarioConfigIO.ResolveHighlightTargetRoles(blueprint, manifest).ToArray();
            string expectedTeam = blueprint.HighlightBindings.TrackedAgentTeam ?? string.Empty;

            ValidateStringProperty(serializedObject, "trackedAgentRole", expectedSingleRole, "ScenarioHighlightTracker.trackedAgentRole", report);
            ValidateStringArrayProperty(serializedObject, "trackedAgentRoles", expectedRoles, "ScenarioHighlightTracker.trackedAgentRoles", report);
            ValidateStringProperty(serializedObject, "trackedAgentTeam", expectedTeam, "ScenarioHighlightTracker.trackedAgentTeam", report);
            ValidateStringProperty(serializedObject, "scenarioLabel", manifest.ScenarioName, "ScenarioHighlightTracker.scenarioLabel", report);
            ValidateBoolProperty(serializedObject, "exportHighlightsToJsonl", blueprint.HighlightBindings.ExportHighlightsToJsonl, "ScenarioHighlightTracker.exportHighlightsToJsonl", report);
            ValidateBoolProperty(serializedObject, "exportSnapshotsToJsonl", blueprint.HighlightBindings.ExportSnapshotsToJsonl, "ScenarioHighlightTracker.exportSnapshotsToJsonl", report);

            BaseRLAgent trackedAgent = GetObjectReferenceProperty(serializedObject, "targetAgent") as BaseRLAgent;
            BaseRLAgent[] allowedAgents = ResolvePreferredAgents(spine, expectedSingleRole, expectedRoles, expectedTeam);
            if (allowedAgents.Length == 0)
            {
                report.AddError("ScenarioHighlightTracker tracked agent bindings do not resolve to any registered agent.");
            }
            else if (trackedAgent != null && !allowedAgents.Contains(trackedAgent))
            {
                report.AddError("ScenarioHighlightTracker.targetAgent must point to one of the configured tracked agent roles or teams when explicitly assigned.");
            }
        }

        private static BaseRLAgent[] ResolvePreferredAgents(ScenarioGoldenSpine spine, string preferredRole, IReadOnlyList<string> roleList, string team)
        {
            if (spine == null)
            {
                return Array.Empty<BaseRLAgent>();
            }

            if (!string.IsNullOrWhiteSpace(preferredRole)
                && spine.TryGetAgentRole(preferredRole, out BaseRLAgent preferredAgent)
                && preferredAgent != null)
            {
                return new[] { preferredAgent };
            }

            if (roleList != null && roleList.Count > 0)
            {
                BaseRLAgent[] resolved = roleList
                    .Select(role => spine.TryGetAgentRole(role, out BaseRLAgent agent) ? agent : null)
                    .Where(agent => agent != null)
                    .Distinct()
                    .ToArray();
                if (resolved.Length > 0)
                {
                    return resolved;
                }
            }

            if (!string.IsNullOrWhiteSpace(team)
                && spine.TryGetPrimaryAgentForTeam(team, out BaseRLAgent teamPrimary)
                && teamPrimary != null)
            {
                return new[] { teamPrimary };
            }

            return spine.PrimaryAgent != null ? new[] { spine.PrimaryAgent } : Array.Empty<BaseRLAgent>();
        }

        private static Transform ResolveExpectedFollowTarget(ScenarioGoldenSpine spine, string role, string team)
        {
            if (spine == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                if (spine.TryGetAgentRole(role, out BaseRLAgent agent) && agent != null)
                {
                    return agent.transform;
                }

                if (spine.TryGetSceneRole(role, out Transform target) && target != null)
                {
                    return target;
                }
            }

            if (!string.IsNullOrWhiteSpace(team)
                && spine.TryGetPrimaryAgentForTeam(team, out BaseRLAgent teamPrimary)
                && teamPrimary != null)
            {
                return teamPrimary.transform;
            }

            return null;
        }

        private static void ValidateCameraPlan(ScenarioManifestData manifest, ScenarioBlueprintData blueprint, ScenarioGoldenSpine spine, ScenarioValidationReport report)
        {
            if (manifest.CameraPlan == null)
            {
                return;
            }

            string expectedDefaultView = ResolveCameraAnchorName(spine, "default_camera");
            if (!string.IsNullOrWhiteSpace(expectedDefaultView)
                && !string.Equals(manifest.CameraPlan.DefaultView, expectedDefaultView, StringComparison.Ordinal))
            {
                report.AddError($"manifest.camera_plan.default_view must match the runtime default camera anchor `{expectedDefaultView}`. Current value: `{manifest.CameraPlan.DefaultView}`.");
            }

            string[] expectedRecordingCuts = GetRecordingCycleRoles(blueprint)
                .Select(role => ResolveCameraAnchorName(spine, role))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();

            if (!manifest.CameraPlan.RecordingCuts.SequenceEqual(expectedRecordingCuts, StringComparer.Ordinal))
            {
                report.AddError($"manifest.camera_plan.recording_cuts must mirror the runtime camera cycle anchors. Expected: {string.Join(", ", expectedRecordingCuts)} | Current: {string.Join(", ", manifest.CameraPlan.RecordingCuts)}");
            }

            string[] knownCameraAnchors = GetKnownCameraAnchorNames(spine);
            if (!knownCameraAnchors.Contains(manifest.CameraPlan.ThumbnailReference, StringComparer.Ordinal))
            {
                report.AddError($"manifest.camera_plan.thumbnail_reference must match a known camera anchor. Current value: `{manifest.CameraPlan.ThumbnailReference}` | Known: {string.Join(", ", knownCameraAnchors)}");
            }
            else if (!manifest.CameraPlan.RecordingCuts.Contains(manifest.CameraPlan.ThumbnailReference, StringComparer.Ordinal)
                     && !string.Equals(manifest.CameraPlan.DefaultView, manifest.CameraPlan.ThumbnailReference, StringComparison.Ordinal))
            {
                report.AddWarning("manifest.camera_plan.thumbnail_reference is valid, but it is not part of the default view or recording cuts.");
            }
        }

        private static void ValidateAgentRoleList(IReadOnlyList<ScenarioAgentBlueprintData> agentBlueprints, IReadOnlyList<string> roles, string label, ScenarioValidationReport report)
        {
            if (roles == null || roles.Count == 0)
            {
                return;
            }

            ValidateUniqueValues(roles, label, report);
            for (int i = 0; i < roles.Count; i++)
            {
                string role = roles[i];
                if (string.IsNullOrWhiteSpace(role))
                {
                    report.AddError($"{label}[{i}] is empty.");
                    continue;
                }

                if (!agentBlueprints.Any(agent => string.Equals(agent.Role, role, StringComparison.Ordinal)))
                {
                    report.AddError($"{label}[{i}] references unknown agent role `{role}`.");
                }
            }
        }

        private static void ValidateKnownValue(string value, IReadOnlyCollection<string> allowedValues, string label, ScenarioValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                report.AddError($"{label} is required.");
                return;
            }

            if (!allowedValues.Contains(value))
            {
                report.AddError($"{label} must be one of: {string.Join(", ", allowedValues)}. Current value: `{value}`.");
            }
        }

        private static void ValidateUniqueValues(IEnumerable<string> values, string label, ScenarioValidationReport report)
        {
            string[] duplicates = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(value => value, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            for (int i = 0; i < duplicates.Length; i++)
            {
                report.AddError($"{label} contains a duplicate entry: `{duplicates[i]}`.");
            }
        }

        private static void ValidateSceneRole(ScenarioGoldenSpine spine, Transform expectedTarget, string role, string expectedName, ScenarioValidationReport report, bool required)
        {
            if (!required && string.IsNullOrWhiteSpace(expectedName))
            {
                return;
            }

            if (!spine.TryGetSceneRole(role, out Transform target) || target == null)
            {
                report.AddError($"ScenarioGoldenSpine must resolve the `{role}` scene role.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(expectedName) && !string.Equals(target.name, expectedName, StringComparison.Ordinal))
            {
                report.AddError($"ScenarioGoldenSpine scene role `{role}` must point to `{expectedName}`. Current value: `{target.name}`.");
            }

            if (expectedTarget != null && target != expectedTarget)
            {
                report.AddError($"ScenarioGoldenSpine scene role `{role}` does not match the expected shared reference.");
            }
        }

        private static void ValidateCameraRole(ScenarioGoldenSpine spine, string role, string expectedName, ScenarioValidationReport report, bool required)
        {
            if (!required && string.IsNullOrWhiteSpace(expectedName))
            {
                return;
            }

            if (!spine.TryGetCameraRole(role, out Transform anchor) || anchor == null)
            {
                report.AddError($"ScenarioGoldenSpine must resolve the `{role}` camera role.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(expectedName) && !string.Equals(anchor.name, expectedName, StringComparison.Ordinal))
            {
                report.AddError($"ScenarioGoldenSpine camera role `{role}` must point to `{expectedName}`. Current value: `{anchor.name}`.");
            }
        }

        private static void ValidateRequiredString(string value, string label, ScenarioValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                report.AddError($"{label} is required.");
            }
        }

        private static void ValidateViewerFacingString(string value, string label, ScenarioValidationReport report)
        {
            ValidateRequiredString(value, label, report);
            if (!string.IsNullOrWhiteSpace(value) && ScenarioConfigIO.IsPlaceholder(value))
            {
                report.AddError($"{label} still contains placeholder text.");
            }
        }

        private static void ValidateNonEmptyList(IReadOnlyList<string> values, string label, ScenarioValidationReport report, bool placeholderCheck)
        {
            if (values == null || values.Count == 0)
            {
                report.AddError($"{label} must contain at least one item.");
                return;
            }

            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i];
                if (string.IsNullOrWhiteSpace(value))
                {
                    report.AddError($"{label}[{i}] is empty.");
                    continue;
                }

                if (placeholderCheck && ScenarioConfigIO.IsPlaceholder(value))
                {
                    report.AddError($"{label}[{i}] still contains placeholder text.");
                }
            }
        }

        private static void ValidateStringProperty(SerializedObject serializedObject, string propertyName, string expectedValue, string label, ScenarioValidationReport report)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                report.AddError($"{label} could not be found.");
                return;
            }

            string actualValue = property.stringValue ?? string.Empty;
            string normalizedExpected = expectedValue ?? string.Empty;
            if (!string.Equals(actualValue, normalizedExpected, StringComparison.Ordinal))
            {
                report.AddError($"{label} must be `{normalizedExpected}`. Current value: `{actualValue}`.");
            }
        }

        private static void ValidateStringArrayProperty(SerializedObject serializedObject, string propertyName, IReadOnlyList<string> expectedValues, string label, ScenarioValidationReport report)
        {
            string[] actualValues = GetStringArray(serializedObject, propertyName);
            string[] normalizedExpectedValues = expectedValues?.ToArray() ?? Array.Empty<string>();
            if (!actualValues.SequenceEqual(normalizedExpectedValues, StringComparer.Ordinal))
            {
                report.AddError($"{label} must be `{string.Join(", ", normalizedExpectedValues)}`. Current value: `{string.Join(", ", actualValues)}`.");
            }
        }

        private static void ValidateBoolProperty(SerializedObject serializedObject, string propertyName, bool expectedValue, string label, ScenarioValidationReport report)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                report.AddError($"{label} could not be found.");
                return;
            }

            if (property.boolValue != expectedValue)
            {
                report.AddError($"{label} must be `{expectedValue}`. Current value: `{property.boolValue}`.");
            }
        }

        private static void ValidateFloatProperty(SerializedObject serializedObject, string propertyName, float expectedValue, string label, ScenarioValidationReport report)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                report.AddError($"{label} could not be found.");
                return;
            }

            if (!Mathf.Approximately(property.floatValue, expectedValue))
            {
                report.AddError($"{label} must be `{expectedValue}`. Current value: `{property.floatValue}`.");
            }
        }

        private static void ValidateObjectReferenceProperty(SerializedObject serializedObject, string propertyName, UnityEngine.Object expectedValue, string label, ScenarioValidationReport report)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                report.AddError($"{label} could not be found.");
                return;
            }

            if (property.objectReferenceValue != expectedValue)
            {
                string actualName = property.objectReferenceValue != null ? property.objectReferenceValue.name : "null";
                string expectedName = expectedValue != null ? expectedValue.name : "null";
                report.AddError($"{label} must reference `{expectedName}`. Current value: `{actualName}`.");
            }
        }

        private static UnityEngine.Object GetObjectReferenceProperty(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property?.objectReferenceValue;
        }

        private static IReadOnlyList<string> GetRecordingCycleRoles(ScenarioBlueprintData blueprint)
        {
            if (blueprint?.RecordingDefaults?.CameraCycleRoles != null && blueprint.RecordingDefaults.CameraCycleRoles.Count > 0)
            {
                return blueprint.RecordingDefaults.CameraCycleRoles;
            }

            return DefaultCameraCycleRoles;
        }

        private static string ResolveCameraAnchorName(ScenarioGoldenSpine spine, string role)
        {
            if (spine == null || string.IsNullOrWhiteSpace(role))
            {
                return string.Empty;
            }

            return spine.TryGetCameraRole(role, out Transform anchor) && anchor != null ? anchor.name : string.Empty;
        }

        private static string[] GetKnownCameraAnchorNames(ScenarioGoldenSpine spine)
        {
            return spine?.CameraRoles?
                .Where(binding => binding.anchor != null)
                .Select(binding => binding.anchor.name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToArray()
                ?? Array.Empty<string>();
        }

        private static string[] GetStringArray(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return Array.Empty<string>();
            }

            string[] values = new string[property.arraySize];
            for (int i = 0; i < property.arraySize; i++)
            {
                values[i] = property.GetArrayElementAtIndex(i).stringValue;
            }

            return values;
        }

        private static UnityEngine.Object[] GetObjectReferenceArray(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return Array.Empty<UnityEngine.Object>();
            }

            UnityEngine.Object[] values = new UnityEngine.Object[property.arraySize];
            for (int i = 0; i < property.arraySize; i++)
            {
                values[i] = property.GetArrayElementAtIndex(i).objectReferenceValue;
            }

            return values;
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

        private static string FirstNonEmpty(params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    return values[i];
                }
            }

            return string.Empty;
        }

        private static void LogReport(ScenarioValidationReport report, bool logToConsole)
        {
            if (!logToConsole)
            {
                return;
            }

            foreach (string error in report.Errors)
            {
                Debug.LogError($"[ScenarioValidator] {error}");
            }

            foreach (string warning in report.Warnings)
            {
                Debug.LogWarning($"[ScenarioValidator] {warning}");
            }

            if (report.IsValid)
            {
                Debug.Log($"[ScenarioValidator] Validation passed with {report.Warnings.Count} warning(s).");
            }
            else
            {
                Debug.LogError($"[ScenarioValidator] Validation failed with {report.Errors.Count} error(s) and {report.Warnings.Count} warning(s).");
            }
        }
    }

    public sealed class ScenarioValidationReport
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;

        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);
    }

    internal static class EditorStatus
    {
        internal static void ShowNonBlockingMessage(string title, string message, bool isError)
        {
            if (isError)
            {
                Debug.LogError($"[{title}] {message}");
            }
            else
            {
                Debug.Log($"[{title}] {message}");
            }

            string summary = GetFirstLine(message);
            EditorWindow window = SceneView.lastActiveSceneView ?? EditorWindow.focusedWindow ?? EditorWindow.mouseOverWindow;
            if (window != null)
            {
                window.ShowNotification(new GUIContent($"{title}: {summary}"));
            }
        }

        private static string GetFirstLine(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            int breakIndex = message.IndexOfAny(new[] { '\r', '\n' });
            return breakIndex < 0 ? message : message.Substring(0, breakIndex);
        }
    }
}
