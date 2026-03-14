using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// Runtime helper for short debug capture sessions.
    /// Works with Unity Recorder, but can also be toggled manually during Play mode.
    /// </summary>
    public class RecordingHelper : MonoBehaviour
    {
        [Header("=== Recording Settings ===")]
        [Tooltip("Hide training UI while a debug recording session is active.")]
        [SerializeField] private bool hideUIWhenRecording = false;

        [Tooltip("Cycle through the recording camera anchors while recording is active.")]
        [SerializeField] private bool enableCameraSwitching = true;

        [Header("=== Camera Presets ===")]
        [SerializeField] private Transform[] cameraPositions;

        [SerializeField] private float cameraSwitchInterval = 10f;

        [Header("=== Debug Controls ===")]
        [Tooltip("Enable simple Play mode hotkeys for short debug recordings.")]
        [SerializeField] private bool enableDebugHotkeys = true;

        [SerializeField] private KeyCode toggleRecordingKey = KeyCode.F8;

        [SerializeField] private KeyCode resetToDefaultViewKey = KeyCode.F7;

        [SerializeField] private KeyCode nextCameraKey = KeyCode.F9;

        private Camera _mainCamera;
        private ScenarioGoldenSpine _goldenSpine;
        private int _currentCamIdx;
        private float _switchTimer;
        private bool _isRecording;
        private bool _loggedDebugHotkeys;

        private void Awake()
        {
            CacheSceneReferences();
        }

        private void Update()
        {
            HandleDebugHotkeys();

            if (!enableCameraSwitching || !_isRecording) return;
            if (cameraPositions == null || cameraPositions.Length <= 1) return;

            _switchTimer += Time.deltaTime;
            if (_switchTimer >= cameraSwitchInterval)
            {
                _switchTimer = 0f;
                NextCamera();
            }
        }

        public void OnRecordingStart()
        {
            CacheSceneReferences();

            _isRecording = true;
            _switchTimer = 0f;
            _currentCamIdx = 0;

            if (cameraPositions != null && cameraPositions.Length > 0)
            {
                ApplyCamera(cameraPositions[0]);
            }

            if (hideUIWhenRecording)
            {
                var visualizers = FindObjectsByType<TrainingVisualizer>(FindObjectsSortMode.None);
                foreach (var visualizer in visualizers)
                {
                    visualizer.enabled = false;
                }
            }

            Debug.Log("[RecordingHelper] Recording started");
        }

        public void OnRecordingStop()
        {
            _isRecording = false;

            if (hideUIWhenRecording)
            {
                var visualizers = FindObjectsByType<TrainingVisualizer>(FindObjectsSortMode.None);
                foreach (var visualizer in visualizers)
                {
                    visualizer.enabled = true;
                }
            }

            RestoreDefaultView();
            Debug.Log("[RecordingHelper] Recording stopped");
        }

        public void NextCamera()
        {
            if (cameraPositions == null || cameraPositions.Length == 0) return;

            CacheSceneReferences();
            if (_mainCamera == null) return;

            _currentCamIdx = (_currentCamIdx + 1) % cameraPositions.Length;
            ApplyCamera(cameraPositions[_currentCamIdx]);
        }

        public void SetCamera(int index)
        {
            if (cameraPositions == null || index < 0 || index >= cameraPositions.Length) return;

            CacheSceneReferences();
            if (_mainCamera == null) return;

            _currentCamIdx = index;
            ApplyCamera(cameraPositions[_currentCamIdx]);
        }

        public void ToggleDebugRecording()
        {
            if (_isRecording)
            {
                OnRecordingStop();
            }
            else
            {
                OnRecordingStart();
            }
        }

        private void HandleDebugHotkeys()
        {
            if (!enableDebugHotkeys) return;

            if (!_loggedDebugHotkeys)
            {
                _loggedDebugHotkeys = true;
                Debug.Log($"[RecordingHelper] Debug hotkeys: {toggleRecordingKey} toggle, {resetToDefaultViewKey} default view, {nextCameraKey} next camera");
            }

            if (Input.GetKeyDown(toggleRecordingKey))
            {
                ToggleDebugRecording();
            }

            if (Input.GetKeyDown(resetToDefaultViewKey))
            {
                RestoreDefaultView();
            }

            if (Input.GetKeyDown(nextCameraKey))
            {
                NextCamera();
            }
        }

        private void CacheSceneReferences()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_goldenSpine == null)
            {
                _goldenSpine = GetComponentInParent<ScenarioGoldenSpine>();
            }
        }

        public void RestoreDefaultView()
        {
            CacheSceneReferences();

            Transform defaultView = _goldenSpine != null ? _goldenSpine.DefaultCameraView : null;
            if (defaultView == null) return;

            ApplyCamera(defaultView);
        }

        private void ApplyCamera(Transform target)
        {
            if (_mainCamera == null || target == null) return;

            _mainCamera.transform.position = target.position;
            _mainCamera.transform.rotation = target.rotation;
        }
    }
}
