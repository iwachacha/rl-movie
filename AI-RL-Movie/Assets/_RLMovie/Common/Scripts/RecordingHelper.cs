using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// 録画関連のヘルパー機能。
    /// Unity Recorder と組み合わせて使用。
    /// </summary>
    public class RecordingHelper : MonoBehaviour
    {
        [Header("=== Recording Settings ===")]
        [Tooltip("録画中にUIを非表示にするか")]
        [SerializeField] private bool hideUIWhenRecording = false;

        [Tooltip("録画用のカメラ切替を有効にするか")]
        [SerializeField] private bool enableCameraSwitching = true;

        [Header("=== Camera Presets ===")]
        [SerializeField] private Transform[] cameraPositions;

        [SerializeField] private float cameraSwitchInterval = 10f;

        private Camera _mainCamera;
        private int _currentCamIdx = 0;
        private float _switchTimer = 0f;
        private bool _isRecording = false;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!enableCameraSwitching || !_isRecording) return;
            if (cameraPositions == null || cameraPositions.Length <= 1) return;

            _switchTimer += Time.deltaTime;
            if (_switchTimer >= cameraSwitchInterval)
            {
                _switchTimer = 0f;
                NextCamera();
            }
        }

        /// <summary>録画開始時に呼ぶ</summary>
        public void OnRecordingStart()
        {
            _isRecording = true;
            _switchTimer = 0f;
            _currentCamIdx = 0;

            if (hideUIWhenRecording)
            {
                // TrainingVisualizer などのUIを無効にする
                var visualizers = FindObjectsByType<TrainingVisualizer>(FindObjectsSortMode.None);
                foreach (var v in visualizers)
                    v.enabled = false;
            }

            Debug.Log("[RecordingHelper] 🎬 Recording started");
        }

        /// <summary>録画終了時に呼ぶ</summary>
        public void OnRecordingStop()
        {
            _isRecording = false;

            if (hideUIWhenRecording)
            {
                var visualizers = FindObjectsByType<TrainingVisualizer>(FindObjectsSortMode.None);
                foreach (var v in visualizers)
                    v.enabled = true;
            }

            Debug.Log("[RecordingHelper] 🎬 Recording stopped");
        }

        /// <summary>次のカメラ位置に切替</summary>
        public void NextCamera()
        {
            if (cameraPositions == null || cameraPositions.Length == 0) return;
            if (_mainCamera == null) return;

            _currentCamIdx = (_currentCamIdx + 1) % cameraPositions.Length;
            var target = cameraPositions[_currentCamIdx];

            _mainCamera.transform.position = target.position;
            _mainCamera.transform.rotation = target.rotation;
        }

        /// <summary>特定のカメラ位置にジャンプ</summary>
        public void SetCamera(int index)
        {
            if (cameraPositions == null || index < 0 || index >= cameraPositions.Length) return;
            if (_mainCamera == null) return;

            _currentCamIdx = index;
            var target = cameraPositions[_currentCamIdx];

            _mainCamera.transform.position = target.position;
            _mainCamera.transform.rotation = target.rotation;
        }
    }
}
