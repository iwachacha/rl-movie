using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// Thin registry of the reusable pieces that every scenario starter should expose.
    /// Theme-specific logic stays elsewhere; this only tracks the shared backbone.
    /// </summary>
    public sealed class ScenarioGoldenSpine : MonoBehaviour
    {
        [Header("=== Golden Spine References ===")]
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private BaseRLAgent primaryAgent;
        [SerializeField] private Transform primaryGoal;
        [SerializeField] private EnvironmentManager environmentManager;
        [SerializeField] private TrainingVisualizer trainingVisualizer;
        [SerializeField] private RecordingHelper recordingHelper;
        [SerializeField] private ScenarioBroadcastOverlay scenarioBroadcastOverlay;
        [SerializeField] private ScenarioHighlightTracker scenarioHighlightTracker;

        [Header("=== Camera Anchors ===")]
        [SerializeField] private Transform defaultCameraView;
        [SerializeField] private Transform[] recordingCameraViews = new Transform[0];

        private void Reset()
        {
            environmentRoot = transform;
        }

        public Transform EnvironmentRoot => environmentRoot;

        public BaseRLAgent PrimaryAgent => primaryAgent;

        public Transform PrimaryGoal => primaryGoal;

        public EnvironmentManager EnvironmentManager => environmentManager;

        public TrainingVisualizer TrainingVisualizer => trainingVisualizer;

        public RecordingHelper RecordingHelper => recordingHelper;

        public ScenarioBroadcastOverlay ScenarioBroadcastOverlay => scenarioBroadcastOverlay;

        public ScenarioHighlightTracker ScenarioHighlightTracker => scenarioHighlightTracker;

        public Transform DefaultCameraView => defaultCameraView;

        public Transform[] RecordingCameraViews => recordingCameraViews;
    }
}
