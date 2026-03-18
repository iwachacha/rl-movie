using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

        [Tooltip("Start the camera preview automatically when Play mode begins, even without an active recording.")]
        [SerializeField] private bool autoPreviewCamerasInPlayMode = false;

        [Tooltip("Keep the main camera aligned to the active recording view while a capture is running.")]
        [SerializeField] private bool keepCameraAlignedWhileRecording = true;

        [Header("=== Camera Presets ===")]
        [SerializeField] private ScenarioGoldenSpine scenarioSpine;
        [SerializeField] private string defaultCameraRole = "default_camera";
        [SerializeField] private string[] cameraCycleRoles = Array.Empty<string>();
        [SerializeField] private string[] knownCameraRoleNames = Array.Empty<string>();
        [SerializeField] private Transform[] legacyCameraPositions;

        [SerializeField] private float cameraSwitchInterval = 10f;

        [Header("=== Dynamic Follow Cut ===")]
        [Tooltip("Recording cut role that should behave as a dynamic follow shot. Leave empty to disable role-based follow.")]
        [SerializeField] private string followCameraRole = "follow_optional";

        [Tooltip("Agent role to follow when role-based spine bindings are available.")]
        [SerializeField] private string followTargetRole = "hero";

        [Tooltip("Fallback team to follow when no specific followTargetRole resolves.")]
        [SerializeField] private string followTargetTeam = string.Empty;

        [SerializeField] private Transform followTarget;

        [SerializeField] private Vector3 followPositionOffset = new Vector3(-1.8f, 2.0f, -4.8f);

        [SerializeField] private Vector3 followLookAtOffset = new Vector3(0f, 1.15f, 2.2f);

        [SerializeField] private float followPositionSharpness = 6f;

        [SerializeField] private float followRotationSharpness = 8f;

        [Tooltip("Optional per-camera follow overrides so multiple cuts can behave as dynamic tracking shots.")]
        [SerializeField] private FollowCameraProfile[] followCameraProfiles = Array.Empty<FollowCameraProfile>();

        [Header("=== Occluder Ghosting ===")]
        [Tooltip("Fade blocking renderers between the active camera and follow target so the character remains readable indoors.")]
        [SerializeField] private bool enableOccluderGhosting = true;

        [SerializeField] [Range(0.05f, 1f)] private float occluderGhostAlpha = 0.18f;

        [SerializeField] [Range(0.01f, 1.5f)] private float occluderProbeRadius = 0.28f;

        [SerializeField] private LayerMask occluderLayers = ~0;

        [Header("=== Transitional Cuts ===")]
        [Tooltip("Allow gameplay events to briefly cut the feed to black before the next episode begins.")]
        [SerializeField] private bool enableTemporaryBlackoutCuts = true;

        [Header("=== Debug Controls ===")]
        [Tooltip("Enable simple Play mode hotkeys for short debug recordings.")]
        [SerializeField] private bool enableDebugHotkeys = true;

        [SerializeField] private KeyCode toggleRecordingKey = KeyCode.F8;

        [SerializeField] private KeyCode resetToDefaultViewKey = KeyCode.F7;

        [SerializeField] private KeyCode nextCameraKey = KeyCode.F9;

        private Camera _mainCamera;
        private ScenarioGoldenSpine _goldenSpine;
        private int _currentCamIdx;
        private float _nextCameraSwitchAtRealtime;
        private bool _isRecording;
        private bool _loggedDebugHotkeys;
        private bool _capturedRunInBackground;
        private bool _previousRunInBackground;
        private readonly Dictionary<TrainingVisualizer, bool> _visualizerEnabledStates = new Dictionary<TrainingVisualizer, bool>();
        private readonly Dictionary<Renderer, GhostedRendererState> _ghostedRenderers = new Dictionary<Renderer, GhostedRendererState>();
        private float _temporaryBlackoutStartRealtime = -1f;
        private float _temporaryBlackoutEndRealtime = -1f;
        private bool _holdBlackoutUntilCleared;
        private bool _blackoutStateCaptured;
        private CameraClearFlags _cachedBlackoutClearFlags;
        private Color _cachedBlackoutBackground;
        private int _cachedBlackoutCullingMask;

        private void Awake()
        {
            if (IsHeadlessRuntime())
            {
                enabled = false;
                return;
            }

            CacheSceneReferences();
            CaptureRunInBackgroundSetting();
        }

        private void Start()
        {
            if (!autoPreviewCamerasInPlayMode || IsHeadlessRuntime())
            {
                return;
            }

            StartCameraPreview();
        }

        private void OnDestroy()
        {
            ClearTemporaryBlackout();
            RestoreVisualizerVisibilityAfterRecording();
            RestoreAllGhostedRenderers();
            RestoreRunInBackgroundSetting();
        }

        private void Update()
        {
            HandleDebugHotkeys();
            UpdateTemporaryBlackout();

            bool shouldMaintainActiveView = !_blackoutStateCaptured && (_isRecording || IsFollowPreviewActive());
            if (shouldMaintainActiveView)
            {
                UpdateActiveCameraPose();
                UpdateOccluderGhosting();
            }
            else
            {
                RestoreAllGhostedRenderers();
            }

            if (!enableCameraSwitching || !_isRecording) return;
            Transform[] activeSequence = GetActiveCameraSequence();
            if (activeSequence.Length <= 1) return;

            if (Time.realtimeSinceStartup >= _nextCameraSwitchAtRealtime)
            {
                _nextCameraSwitchAtRealtime = Time.realtimeSinceStartup + cameraSwitchInterval;
                NextCamera();
            }
        }

        public void OnRecordingStart()
        {
            if (_isRecording)
            {
                return;
            }

            CacheSceneReferences();
            EnableRunInBackground();

            _isRecording = true;
            _currentCamIdx = 0;
            _nextCameraSwitchAtRealtime = Time.realtimeSinceStartup + cameraSwitchInterval;
            ApplyCurrentViewPose(immediate: true);

            if (hideUIWhenRecording)
            {
                HideVisualizersForRecording();
            }

            Debug.Log("[RecordingHelper] Recording started");
        }

        public void OnRecordingStop()
        {
            if (!_isRecording)
            {
                RestoreDefaultView();
                return;
            }

            _isRecording = false;
            ClearTemporaryBlackout();
            RestoreAllGhostedRenderers();

            if (hideUIWhenRecording)
            {
                RestoreVisualizerVisibilityAfterRecording();
            }

            RestoreRunInBackgroundSetting();
            RestoreDefaultView();
            Debug.Log("[RecordingHelper] Recording stopped");
        }

        public void NextCamera()
        {
            CacheSceneReferences();
            if (_mainCamera == null) return;

            Transform[] activeSequence = GetActiveCameraSequence();
            if (activeSequence.Length == 0) return;

            _currentCamIdx = (_currentCamIdx + 1) % activeSequence.Length;
            ApplyCurrentViewPose(immediate: true);
        }

        public void SetCamera(int index)
        {
            CacheSceneReferences();
            if (_mainCamera == null) return;

            Transform[] activeSequence = GetActiveCameraSequence();
            if (index < 0 || index >= activeSequence.Length) return;

            _currentCamIdx = index;
            ApplyCurrentViewPose(immediate: true);
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

        public void TriggerTemporaryBlackout(float startDelay, float duration, bool holdUntilClear = false)
        {
            if (!enableTemporaryBlackoutCuts || IsHeadlessRuntime())
            {
                return;
            }

            float clampedDelay = Mathf.Max(0f, startDelay);
            float clampedDuration = Mathf.Max(0.01f, duration);
            _temporaryBlackoutStartRealtime = Time.realtimeSinceStartup + clampedDelay;
            _temporaryBlackoutEndRealtime = _temporaryBlackoutStartRealtime + clampedDuration;
            _holdBlackoutUntilCleared = holdUntilClear;
        }

        public void ClearTemporaryBlackout()
        {
            _temporaryBlackoutStartRealtime = -1f;
            _temporaryBlackoutEndRealtime = -1f;
            _holdBlackoutUntilCleared = false;
            RestoreTemporaryBlackoutState();
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

        private void StartCameraPreview()
        {
            CacheSceneReferences();
            _currentCamIdx = 0;
            ApplyCurrentViewPose(immediate: true);
        }

        private void HideVisualizersForRecording()
        {
            _visualizerEnabledStates.Clear();

            var visualizers = FindObjectsByType<TrainingVisualizer>(FindObjectsSortMode.None);
            foreach (var visualizer in visualizers)
            {
                if (visualizer == null)
                {
                    continue;
                }

                _visualizerEnabledStates[visualizer] = visualizer.enabled;
                if (visualizer.enabled)
                {
                    visualizer.enabled = false;
                }
            }
        }

        private void RestoreVisualizerVisibilityAfterRecording()
        {
            if (_visualizerEnabledStates.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<TrainingVisualizer, bool> entry in _visualizerEnabledStates)
            {
                if (entry.Key != null)
                {
                    entry.Key.enabled = entry.Value;
                }
            }

            _visualizerEnabledStates.Clear();
        }

        private void UpdateTemporaryBlackout()
        {
            if (!enableTemporaryBlackoutCuts || IsHeadlessRuntime())
            {
                return;
            }

            CacheSceneReferences();
            if (_mainCamera == null)
            {
                return;
            }

            float realtime = Time.realtimeSinceStartup;
            bool blackoutStarted =
                _temporaryBlackoutStartRealtime >= 0f &&
                realtime >= _temporaryBlackoutStartRealtime;

            bool blackoutRequested =
                blackoutStarted &&
                (realtime < _temporaryBlackoutEndRealtime || _holdBlackoutUntilCleared);

            if (blackoutRequested)
            {
                ApplyTemporaryBlackoutState();
                return;
            }

            if (!_holdBlackoutUntilCleared &&
                _temporaryBlackoutEndRealtime >= 0f &&
                realtime >= _temporaryBlackoutEndRealtime)
            {
                _temporaryBlackoutStartRealtime = -1f;
                _temporaryBlackoutEndRealtime = -1f;
            }

            RestoreTemporaryBlackoutState();
        }

        private void CacheSceneReferences()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_goldenSpine == null && scenarioSpine != null)
            {
                _goldenSpine = scenarioSpine;
            }

            if (_goldenSpine == null)
            {
                _goldenSpine = GetComponentInParent<ScenarioGoldenSpine>();
            }

            if (followTarget == null && _goldenSpine != null && !string.IsNullOrWhiteSpace(followTargetRole))
            {
                if (_goldenSpine.TryGetAgentRole(followTargetRole, out BaseRLAgent agent) && agent != null)
                {
                    followTarget = agent.transform;
                }
                else if (_goldenSpine.TryGetSceneRole(followTargetRole, out Transform roleTransform) && roleTransform != null)
                {
                    followTarget = roleTransform;
                }
            }

            if (followTarget == null && _goldenSpine != null && !string.IsNullOrWhiteSpace(followTargetTeam))
            {
                if (_goldenSpine.TryGetPrimaryAgentForTeam(followTargetTeam, out BaseRLAgent teamAgent) && teamAgent != null)
                {
                    followTarget = teamAgent.transform;
                }
            }
        }

        public void RestoreDefaultView()
        {
            CacheSceneReferences();
            ClearTemporaryBlackout();
            RestoreAllGhostedRenderers();

            Transform defaultView = GetDefaultCameraView();
            if (defaultView == null) return;

            ApplyCamera(defaultView);
        }

        private void ApplyCamera(Transform target)
        {
            if (_mainCamera == null || target == null) return;

            _mainCamera.transform.position = target.position;
            _mainCamera.transform.rotation = target.rotation;
        }

        private void ApplyCurrentViewPose(bool immediate)
        {
            CacheSceneReferences();
            if (_mainCamera == null) return;

            if (TryApplyFollowCamera(immediate))
            {
                return;
            }

            Transform activeCamera = GetActiveCamera();
            if (activeCamera != null)
            {
                ApplyCamera(activeCamera);
            }
        }

        private void UpdateActiveCameraPose()
        {
            if (TryApplyFollowCamera(immediate: false))
            {
                return;
            }

            if (!keepCameraAlignedWhileRecording || !_isRecording)
            {
                return;
            }

            Transform activeCamera = GetActiveCamera();
            if (activeCamera != null)
            {
                ApplyCamera(activeCamera);
            }
        }

        private Transform GetActiveCamera()
        {
            Transform[] activeSequence = GetActiveCameraSequence();
            if (_currentCamIdx < 0 || _currentCamIdx >= activeSequence.Length)
            {
                return null;
            }

            return activeSequence[_currentCamIdx];
        }

        private bool IsFollowPreviewActive()
        {
            return !_isRecording &&
                   HasFollowCameraForIndex(_currentCamIdx) &&
                   followTarget != null;
        }

        private bool TryApplyFollowCamera(bool immediate)
        {
            if (_mainCamera == null ||
                followTarget == null ||
                !TryGetFollowSettings(_currentCamIdx, out FollowCameraProfile followProfile))
            {
                return false;
            }

            Quaternion followYawRotation = GetFollowYawRotation();
            Vector3 desiredPosition = followTarget.position + followYawRotation * followProfile.PositionOffset;
            Vector3 focusPoint = followTarget.position + followYawRotation * followProfile.LookAtOffset;
            Vector3 lookDirection = focusPoint - desiredPosition;

            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                lookDirection = followTarget.forward;
            }

            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                lookDirection = Vector3.forward;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

            if (immediate)
            {
                _mainCamera.transform.SetPositionAndRotation(desiredPosition, desiredRotation);
                return true;
            }

            float positionT = 1f - Mathf.Exp(-Mathf.Max(0.01f, followProfile.PositionSharpness) * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-Mathf.Max(0.01f, followProfile.RotationSharpness) * Time.deltaTime);
            _mainCamera.transform.position = Vector3.Lerp(_mainCamera.transform.position, desiredPosition, positionT);
            _mainCamera.transform.rotation = Quaternion.Slerp(_mainCamera.transform.rotation, desiredRotation, rotationT);
            return true;
        }

        private bool HasFollowCameraForIndex(int cameraIndex)
        {
            if (cameraIndex < 0)
            {
                return false;
            }

            if (TryGetConfiguredFollowProfile(cameraIndex, out _))
            {
                return true;
            }

            string currentRole = GetCameraRoleForIndex(cameraIndex);
            return !string.IsNullOrWhiteSpace(followCameraRole)
                && string.Equals(currentRole, followCameraRole, StringComparison.Ordinal);
        }

        private bool TryGetFollowSettings(int cameraIndex, out FollowCameraProfile followProfile)
        {
            if (TryGetConfiguredFollowProfile(cameraIndex, out followProfile))
            {
                return true;
            }

            string currentRole = GetCameraRoleForIndex(cameraIndex);
            if (!string.IsNullOrWhiteSpace(followCameraRole) &&
                string.Equals(currentRole, followCameraRole, StringComparison.Ordinal))
            {
                followProfile = FollowCameraProfile.Create(
                    cameraIndex,
                    followPositionOffset,
                    followLookAtOffset,
                    followPositionSharpness,
                    followRotationSharpness);
                return true;
            }

            followProfile = default;
            return false;
        }

        private Transform GetDefaultCameraView()
        {
            CacheSceneReferences();
            if (_goldenSpine != null && !string.IsNullOrWhiteSpace(defaultCameraRole))
            {
                if (_goldenSpine.TryGetCameraRole(defaultCameraRole, out Transform anchor) && anchor != null)
                {
                    return anchor;
                }
            }

            Transform[] fallbackSequence = GetFallbackLegacyCameraPositions();
            return fallbackSequence.Length > 0 ? fallbackSequence[0] : null;
        }

        private Transform[] GetActiveCameraSequence()
        {
            CacheSceneReferences();

            if (_goldenSpine != null && cameraCycleRoles != null && cameraCycleRoles.Length > 0)
            {
                var resolved = new List<Transform>();
                for (int i = 0; i < cameraCycleRoles.Length; i++)
                {
                    string role = cameraCycleRoles[i];
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        continue;
                    }

                    if (_goldenSpine.TryGetCameraRole(role, out Transform anchor) && anchor != null)
                    {
                        resolved.Add(anchor);
                    }
                }

                if (resolved.Count > 0)
                {
                    return resolved.ToArray();
                }
            }

            return GetFallbackLegacyCameraPositions();
        }

        private Transform[] GetFallbackLegacyCameraPositions()
        {
            return legacyCameraPositions ?? Array.Empty<Transform>();
        }

        private string GetCameraRoleForIndex(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraCycleRoles == null || cameraIndex >= cameraCycleRoles.Length)
            {
                return string.Empty;
            }

            return cameraCycleRoles[cameraIndex] ?? string.Empty;
        }

        private bool TryGetConfiguredFollowProfile(int cameraIndex, out FollowCameraProfile followProfile)
        {
            if (followCameraProfiles != null)
            {
                for (int i = 0; i < followCameraProfiles.Length; i++)
                {
                    if (followCameraProfiles[i].CameraIndex != cameraIndex)
                    {
                        continue;
                    }

                    followProfile = followCameraProfiles[i];
                    return true;
                }
            }

            followProfile = default;
            return false;
        }

        private Quaternion GetFollowYawRotation()
        {
            Vector3 flatForward = followTarget != null ? followTarget.forward : Vector3.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }

            return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        }

        private void UpdateOccluderGhosting()
        {
            if (!enableOccluderGhosting ||
                _mainCamera == null ||
                followTarget == null ||
                !TryGetFollowSettings(_currentCamIdx, out FollowCameraProfile followProfile))
            {
                RestoreAllGhostedRenderers();
                return;
            }

            Quaternion followYawRotation = GetFollowYawRotation();
            Vector3 focusPoint = followTarget.position + followYawRotation * followProfile.LookAtOffset;
            Vector3 cameraPosition = _mainCamera.transform.position;
            Vector3 toFocus = focusPoint - cameraPosition;
            float distance = toFocus.magnitude;

            if (distance <= 0.05f)
            {
                RestoreAllGhostedRenderers();
                return;
            }

            Vector3 direction = toFocus / distance;
            RaycastHit[] hits = Physics.SphereCastAll(
                cameraPosition,
                Mathf.Max(0.01f, occluderProbeRadius),
                direction,
                distance,
                occluderLayers,
                QueryTriggerInteraction.Ignore);

            var occludersThisFrame = new HashSet<Renderer>();
            for (int i = 0; i < hits.Length; i++)
            {
                Renderer renderer = ResolveOccluderRenderer(hits[i].collider);
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (renderer.transform.IsChildOf(followTarget) || followTarget.IsChildOf(renderer.transform))
                {
                    continue;
                }

                occludersThisFrame.Add(renderer);

                if (!_ghostedRenderers.TryGetValue(renderer, out GhostedRendererState ghostState))
                {
                    ghostState = new GhostedRendererState(renderer);
                    _ghostedRenderers.Add(renderer, ghostState);
                }

                ghostState.Apply(occluderGhostAlpha);
            }

            if (_ghostedRenderers.Count == occludersThisFrame.Count)
            {
                return;
            }

            var staleRenderers = new List<Renderer>();
            foreach (KeyValuePair<Renderer, GhostedRendererState> entry in _ghostedRenderers)
            {
                if (!occludersThisFrame.Contains(entry.Key))
                {
                    staleRenderers.Add(entry.Key);
                }
            }

            for (int i = 0; i < staleRenderers.Count; i++)
            {
                Renderer renderer = staleRenderers[i];
                if (!_ghostedRenderers.TryGetValue(renderer, out GhostedRendererState ghostState))
                {
                    continue;
                }

                ghostState.Restore();
                _ghostedRenderers.Remove(renderer);
            }
        }

        private void RestoreAllGhostedRenderers()
        {
            if (_ghostedRenderers.Count == 0)
            {
                return;
            }

            foreach (GhostedRendererState ghostState in _ghostedRenderers.Values)
            {
                ghostState.Restore();
            }

            _ghostedRenderers.Clear();
        }

        private static Renderer ResolveOccluderRenderer(Collider collider)
        {
            if (collider == null)
            {
                return null;
            }

            Transform current = collider.transform;
            while (current != null)
            {
                Renderer renderer = current.GetComponent<Renderer>();
                if (renderer != null)
                {
                    return renderer;
                }

                current = current.parent;
            }

            return collider.GetComponentInChildren<Renderer>();
        }

        private static bool IsHeadlessRuntime()
        {
            return Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
        }

        private void ApplyTemporaryBlackoutState()
        {
            if (_mainCamera == null)
            {
                return;
            }

            if (!_blackoutStateCaptured)
            {
                _cachedBlackoutClearFlags = _mainCamera.clearFlags;
                _cachedBlackoutBackground = _mainCamera.backgroundColor;
                _cachedBlackoutCullingMask = _mainCamera.cullingMask;
                _blackoutStateCaptured = true;
            }

            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = Color.black;
            _mainCamera.cullingMask = 0;
        }

        private void RestoreTemporaryBlackoutState()
        {
            if (!_blackoutStateCaptured || _mainCamera == null)
            {
                return;
            }

            _mainCamera.clearFlags = _cachedBlackoutClearFlags;
            _mainCamera.backgroundColor = _cachedBlackoutBackground;
            _mainCamera.cullingMask = _cachedBlackoutCullingMask;
            _blackoutStateCaptured = false;
        }

        private void CaptureRunInBackgroundSetting()
        {
            if (_capturedRunInBackground)
            {
                return;
            }

            _previousRunInBackground = Application.runInBackground;
            _capturedRunInBackground = true;
        }

        private void EnableRunInBackground()
        {
            CaptureRunInBackgroundSetting();
            Application.runInBackground = true;
        }

        private void RestoreRunInBackgroundSetting()
        {
            if (!_capturedRunInBackground)
            {
                return;
            }

            Application.runInBackground = _previousRunInBackground;
        }

        [Serializable]
        private struct FollowCameraProfile
        {
            [SerializeField] private int cameraIndex;
            [SerializeField] private Vector3 positionOffset;
            [SerializeField] private Vector3 lookAtOffset;
            [SerializeField] private float positionSharpness;
            [SerializeField] private float rotationSharpness;

            public int CameraIndex => cameraIndex;
            public Vector3 PositionOffset => positionOffset;
            public Vector3 LookAtOffset => lookAtOffset;
            public float PositionSharpness => positionSharpness;
            public float RotationSharpness => rotationSharpness;

            public static FollowCameraProfile Create(
                int index,
                Vector3 position,
                Vector3 lookAt,
                float positionDamping,
                float rotationDamping)
            {
                return new FollowCameraProfile
                {
                    cameraIndex = index,
                    positionOffset = position,
                    lookAtOffset = lookAt,
                    positionSharpness = positionDamping,
                    rotationSharpness = rotationDamping
                };
            }
        }

        private sealed class GhostedRendererState
        {
            private readonly GhostedMaterialState[] _materials;

            public GhostedRendererState(Renderer renderer)
            {
                Material[] runtimeMaterials = renderer != null ? renderer.materials : Array.Empty<Material>();
                _materials = new GhostedMaterialState[runtimeMaterials.Length];
                for (int i = 0; i < runtimeMaterials.Length; i++)
                {
                    _materials[i] = new GhostedMaterialState(runtimeMaterials[i]);
                }
            }

            public void Apply(float alpha)
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    _materials[i].Apply(alpha);
                }
            }

            public void Restore()
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    _materials[i].Restore();
                }
            }
        }

        private sealed class GhostedMaterialState
        {
            private readonly Material _material;
            private readonly bool _hasBaseColor;
            private readonly Color _baseColor;
            private readonly bool _hasColor;
            private readonly Color _color;
            private readonly bool _hasSurface;
            private readonly float _surface;
            private readonly bool _hasBlend;
            private readonly float _blend;
            private readonly bool _hasSrcBlend;
            private readonly float _srcBlend;
            private readonly bool _hasDstBlend;
            private readonly float _dstBlend;
            private readonly bool _hasZWrite;
            private readonly float _zWrite;
            private readonly int _renderQueue;
            private readonly string _renderTypeTag;
            private readonly bool _transparentKeywordEnabled;
            private readonly bool _opaqueKeywordEnabled;
            private readonly bool _alphaBlendKeywordEnabled;

            public GhostedMaterialState(Material material)
            {
                _material = material;
                _hasBaseColor = material != null && material.HasProperty("_BaseColor");
                _baseColor = _hasBaseColor ? material.GetColor("_BaseColor") : Color.white;
                _hasColor = material != null && material.HasProperty("_Color");
                _color = _hasColor ? material.GetColor("_Color") : Color.white;
                _hasSurface = material != null && material.HasProperty("_Surface");
                _surface = _hasSurface ? material.GetFloat("_Surface") : 0f;
                _hasBlend = material != null && material.HasProperty("_Blend");
                _blend = _hasBlend ? material.GetFloat("_Blend") : 0f;
                _hasSrcBlend = material != null && material.HasProperty("_SrcBlend");
                _srcBlend = _hasSrcBlend ? material.GetFloat("_SrcBlend") : 0f;
                _hasDstBlend = material != null && material.HasProperty("_DstBlend");
                _dstBlend = _hasDstBlend ? material.GetFloat("_DstBlend") : 0f;
                _hasZWrite = material != null && material.HasProperty("_ZWrite");
                _zWrite = _hasZWrite ? material.GetFloat("_ZWrite") : 0f;
                _renderQueue = material != null ? material.renderQueue : -1;
                _renderTypeTag = material != null ? material.GetTag("RenderType", false, string.Empty) : string.Empty;
                _transparentKeywordEnabled = material != null && material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
                _opaqueKeywordEnabled = material != null && material.IsKeywordEnabled("_SURFACE_TYPE_OPAQUE");
                _alphaBlendKeywordEnabled = material != null && material.IsKeywordEnabled("_ALPHABLEND_ON");
            }

            public void Apply(float alpha)
            {
                if (_material == null)
                {
                    return;
                }

                if (_hasBaseColor)
                {
                    _material.SetColor("_BaseColor", WithAlpha(_baseColor, alpha));
                }

                if (_hasColor)
                {
                    _material.SetColor("_Color", WithAlpha(_color, alpha));
                }

                if (_hasSurface)
                {
                    _material.SetFloat("_Surface", 1f);
                }

                if (_hasBlend)
                {
                    _material.SetFloat("_Blend", 0f);
                }

                if (_hasSrcBlend)
                {
                    _material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                }

                if (_hasDstBlend)
                {
                    _material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                }

                if (_hasZWrite)
                {
                    _material.SetFloat("_ZWrite", 0f);
                }

                _material.SetOverrideTag("RenderType", "Transparent");
                _material.renderQueue = (int)RenderQueue.Transparent;
                _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                _material.EnableKeyword("_ALPHABLEND_ON");
            }

            public void Restore()
            {
                if (_material == null)
                {
                    return;
                }

                if (_hasBaseColor)
                {
                    _material.SetColor("_BaseColor", _baseColor);
                }

                if (_hasColor)
                {
                    _material.SetColor("_Color", _color);
                }

                if (_hasSurface)
                {
                    _material.SetFloat("_Surface", _surface);
                }

                if (_hasBlend)
                {
                    _material.SetFloat("_Blend", _blend);
                }

                if (_hasSrcBlend)
                {
                    _material.SetFloat("_SrcBlend", _srcBlend);
                }

                if (_hasDstBlend)
                {
                    _material.SetFloat("_DstBlend", _dstBlend);
                }

                if (_hasZWrite)
                {
                    _material.SetFloat("_ZWrite", _zWrite);
                }

                _material.SetOverrideTag("RenderType", _renderTypeTag);
                _material.renderQueue = _renderQueue;
                SetKeyword(_material, "_SURFACE_TYPE_TRANSPARENT", _transparentKeywordEnabled);
                SetKeyword(_material, "_SURFACE_TYPE_OPAQUE", _opaqueKeywordEnabled);
                SetKeyword(_material, "_ALPHABLEND_ON", _alphaBlendKeywordEnabled);
            }

            private static Color WithAlpha(Color color, float alpha)
            {
                color.a = Mathf.Clamp01(Mathf.Min(color.a, alpha));
                return color;
            }

            private static void SetKeyword(Material material, string keyword, bool enabled)
            {
                if (enabled)
                {
                    material.EnableKeyword(keyword);
                    return;
                }

                material.DisableKeyword(keyword);
            }
        }
    }
}
