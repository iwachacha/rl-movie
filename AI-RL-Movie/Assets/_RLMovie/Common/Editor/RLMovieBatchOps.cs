using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RLMovie.Editor
{
    /// <summary>
    /// Small batchmode entry points for scene validation and build automation.
    /// </summary>
    public static class RLMovieBatchOps
    {
        public static void ValidateSceneFromArgs()
        {
            string scenePath = GetRequiredArg("scenePath");
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var report = ScenarioValidator.ValidateCurrentScene(true);
            if (!report.IsValid)
            {
                throw new Exception($"Validation failed for {scenePath}: {report.Errors.Count} errors, {report.Warnings.Count} warnings.");
            }

            UnityEngine.Debug.Log($"[RLMovieBatchOps] Validation passed for {scenePath}");
        }

        public static void BuildSceneFromArgs()
        {
            string scenePath = GetRequiredArg("scenePath");
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var result = BuildForColab.BuildCurrentSceneSilent();
            if (!result.Success)
            {
                throw new Exception($"Build failed for {scenePath}: {result.Message}");
            }

            UnityEngine.Debug.Log($"[RLMovieBatchOps] Build succeeded for {scenePath}: {result.ZipPath}");
        }

        public static void ValidateAndBuildSceneFromArgs()
        {
            ValidateSceneFromArgs();
            BuildSceneFromArgs();
        }

        private static string GetRequiredArg(string key)
        {
            string value = TryGetArgValue(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Missing required command line argument --{key}");
            }

            return value;
        }

        private static string TryGetArgValue(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            string longKey = $"--{key}";

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, longKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        return args[i + 1];
                    }

                    break;
                }

                string prefix = $"{longKey}=";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(prefix.Length);
                }
            }

            return null;
        }
    }
}
