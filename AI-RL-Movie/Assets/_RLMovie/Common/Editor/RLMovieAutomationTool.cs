using System;
using System.Linq;
using System.Reflection;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace RLMovie.Editor
{
    [McpForUnityTool("rl_movie_automation", Description = "Runs RL Movie setup and verification steps without Unity modal dialogs.")]
    public static class RLMovieAutomationTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action to perform: create_golden_starter, create_scenario_scene, validate_current_scene, build_current_scene_for_colab.")]
            public string action { get; set; }

            [ToolParameter("Scenario name for starter creation or scene creation, such as MyScenario.", Required = false)]
            public string scenario_name { get; set; }

            [ToolParameter("When true, overwrite generated starter files if they already exist.", Required = false, DefaultValue = "false")]
            public bool? overwrite_existing { get; set; }

            [ToolParameter("When true, validation logs are also written to the Unity Console.", Required = false, DefaultValue = "true")]
            public bool? log_to_console { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            string action = @params?["action"]?.ToString()?.Trim().ToLowerInvariant();
            string scenarioName = @params?["scenario_name"]?.ToString()?.Trim();
            bool overwriteExisting = @params?["overwrite_existing"]?.ToObject<bool?>() ?? false;
            bool logToConsole = @params?["log_to_console"]?.ToObject<bool?>() ?? true;

            if (string.IsNullOrWhiteSpace(action))
            {
                return new ErrorResponse("Required parameter 'action' is missing.");
            }

            try
            {
                switch (action)
                {
                    case "create_golden_starter":
                        return CreateGoldenStarter(scenarioName, overwriteExisting);
                    case "create_scenario_scene":
                        return CreateScenarioScene(scenarioName);
                    case "validate_current_scene":
                        return ValidateCurrentScene(logToConsole);
                    case "build_current_scene_for_colab":
                        return BuildCurrentSceneForColab();
                    default:
                        return new ErrorResponse($"Unknown action '{action}'.");
                }
            }
            catch (TargetInvocationException ex)
            {
                return new ErrorResponse(ex.InnerException?.Message ?? ex.Message);
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message);
            }
        }

        private static object CreateGoldenStarter(string scenarioName, bool overwriteExisting)
        {
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                return new ErrorResponse("Parameter 'scenario_name' is required for create_golden_starter.");
            }

            var result = CreateGoldenScenarioStarter.CreateStarterFilesForScenario(scenarioName, overwriteExisting);
            if (!result.Success)
            {
                return new ErrorResponse(result.Message, result);
            }

            return new SuccessResponse(result.Message, result);
        }

        private static object CreateScenarioScene(string scenarioName)
        {
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                return new ErrorResponse("Parameter 'scenario_name' is required for create_scenario_scene.");
            }

            Type builderType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(type =>
                    string.Equals(type.Namespace, "RLMovie.Editor", StringComparison.Ordinal)
                    && string.Equals(type.Name, $"{scenarioName}SceneBuilder", StringComparison.Ordinal));

            if (builderType == null)
            {
                return new ErrorResponse($"Scene builder type '{scenarioName}SceneBuilder' was not found. Wait for Unity compilation to finish and try again.");
            }

            MethodInfo silentMethod = builderType.GetMethod("CreateSceneSilently", BindingFlags.Public | BindingFlags.Static);
            if (silentMethod == null)
            {
                return new ErrorResponse($"Scene builder '{builderType.Name}' does not expose CreateSceneSilently().");
            }

            string scenePath = silentMethod.Invoke(null, null) as string;
            return new SuccessResponse(
                $"Created scene for {scenarioName} without modal dialogs.",
                new
                {
                    scenarioName,
                    scenePath,
                    activeScene = SceneManager.GetActiveScene().path
                });
        }

        private static object ValidateCurrentScene(bool logToConsole)
        {
            var report = ScenarioValidator.ValidateCurrentScene(logToConsole);
            var data = new
            {
                isValid = report.IsValid,
                errorCount = report.Errors.Count,
                warningCount = report.Warnings.Count,
                errors = report.Errors.ToArray(),
                warnings = report.Warnings.ToArray(),
                activeScene = SceneManager.GetActiveScene().path
            };

            if (!report.IsValid)
            {
                return new ErrorResponse("Scenario validation failed.", data);
            }

            return new SuccessResponse("Scenario validation passed.", data);
        }

        private static object BuildCurrentSceneForColab()
        {
            var result = BuildForColab.BuildCurrentSceneSilent();
            if (!result.Success)
            {
                return new ErrorResponse(result.Message, result);
            }

            return new SuccessResponse(result.Message, result);
        }

        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).ToArray();
            }
        }
    }
}
