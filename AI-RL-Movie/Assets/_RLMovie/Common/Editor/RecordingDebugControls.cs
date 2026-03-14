using RLMovie.Common;
using UnityEditor;
using UnityEngine;

namespace RLMovie.Editor
{
    /// <summary>
    /// Small menu helpers for starting and checking short debug capture sessions.
    /// </summary>
    public static class RecordingDebugControls
    {
        [MenuItem("RLMovie/Recording/Toggle Debug Capture")]
        public static void ToggleDebugCapture()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[RecordingDebugControls] Enter Play mode before toggling debug capture.");
                return;
            }

            RecordingHelper helper = FindActiveHelper();
            if (helper == null)
            {
                Debug.LogWarning("[RecordingDebugControls] No RecordingHelper found in the active scene.");
                return;
            }

            helper.ToggleDebugRecording();
        }

        [MenuItem("RLMovie/Recording/Reset To Default View")]
        public static void ResetToDefaultView()
        {
            RecordingHelper helper = FindActiveHelper();
            if (helper == null)
            {
                Debug.LogWarning("[RecordingDebugControls] No RecordingHelper found in the active scene.");
                return;
            }

            helper.RestoreDefaultView();
        }

        [MenuItem("RLMovie/Recording/Next Camera")]
        public static void NextCamera()
        {
            RecordingHelper helper = FindActiveHelper();
            if (helper == null)
            {
                Debug.LogWarning("[RecordingDebugControls] No RecordingHelper found in the active scene.");
                return;
            }

            helper.NextCamera();
        }

        private static RecordingHelper FindActiveHelper()
        {
            return Object.FindFirstObjectByType<RecordingHelper>();
        }
    }
}
