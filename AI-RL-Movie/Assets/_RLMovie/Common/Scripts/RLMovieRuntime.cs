using UnityEngine;
using UnityEngine.Rendering;

namespace RLMovie.Common
{
    /// <summary>
    /// Shared runtime utilities for RL Movie.
    /// </summary>
    public static class RLMovieRuntime
    {
        /// <summary>
        /// True when running headless (batch mode or null graphics device).
        /// </summary>
        public static bool IsHeadless { get; } =
            Application.isBatchMode ||
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    }
}
