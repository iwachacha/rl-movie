using RLMovie.Common;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RLMovie.Environments.ReactorCoreDelivery
{
    /// <summary>
    /// Learns to escort, push, and settle a loose reactor core into the final socket.
    /// </summary>
    public class ReactorCoreDeliveryAgent : BaseRLAgent
    {
        [Header("=== Reactor Core Delivery References ===")]
        [SerializeField] private ReactorCoreDeliveryCourse course;
        [SerializeField] private EnvironmentManager environmentManager;
        [SerializeField] private RecordingHelper recordingHelper;

        [Header("=== Movement ===")]
        [SerializeField] private float maxMoveSpeed = 4.5f;
        [SerializeField] private float acceleration = 22f;
        [SerializeField] private float insertDistance = 1.2f;
        [SerializeField] private float coreEngageDistance = 0.95f;

        [Header("=== Rewards ===")]
        [SerializeField] private float engageCoreReward = 0.20f;
        [SerializeField] private float approachProgressRewardScale = 0.012f;
        [SerializeField] private float approachRegressionPenaltyScale = 0.008f;
        [SerializeField] private float coreProgressRewardScale = 0.018f;
        [SerializeField] private float coreRegressionPenaltyScale = 0.01f;
        [SerializeField] private float coreSettleProgressRewardScale = 0.05f;
        [SerializeField] private float coreDockBonus = 0.25f;
        [SerializeField] private float zoneBonus = 0.06f;
        [SerializeField] private float stepPenalty = 0.00075f;
        [SerializeField] private float emptyGoalPenalty = 0.03f;
        [SerializeField] private float successReward = 1.5f;
        [SerializeField] private float speedBonusMax = 0.75f;
        [SerializeField] private float lostCorePenalty = 1.0f;
        [SerializeField] private float timeoutPenalty = 1.1f;

        [Header("=== Shock Response ===")]
        [SerializeField] private float shockPenalty = 0.10f;
        [SerializeField] private float shockKnockback = 6.4f;
        [SerializeField] private float shockCooldown = 0.55f;
        [SerializeField] private float meltdownLaunchForce = 13.5f;
        [SerializeField] private float meltdownLiftForce = 8.0f;
        [SerializeField] private float meltdownSpinTorque = 24f;
        [SerializeField] private float meltdownFailDelay = 0.42f;
        [SerializeField] private float meltdownCameraCutDelay = 0.14f;
        [SerializeField] private float meltdownCameraCutDuration = 0.18f;

        private Rigidbody _rb;
        private RigidbodyConstraints _defaultConstraints;
        private float _defaultMaxAngularVelocity;
        private ReactorCoreDeliveryVisualController _visualController;
        private float _previousApproachProgress;
        private float _previousCoreProgress;
        private float _previousDockReadiness;
        private int _lastCoreZoneIndex;
        private float _shockRecoverRemaining;
        private bool _episodeFinished;
        private bool _hasEngagedCore;
        private bool _emptyGoalPenaltyConsumed;
        private Coroutine _pendingFailRoutine;

        private void OnValidate()
        {
            bool changed = EnsureSceneReferencesResolved();

#if UNITY_EDITOR
            if (changed)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }

        protected override void OnAgentInitialize()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _defaultConstraints = _rb.constraints;
                _defaultMaxAngularVelocity = _rb.maxAngularVelocity;
            }

            _visualController = GetComponentInChildren<ReactorCoreDeliveryVisualController>();
            EnsureSceneReferencesResolved();
        }

        protected override void OnEpisodeReset()
        {
            if (_pendingFailRoutine != null)
            {
                StopCoroutine(_pendingFailRoutine);
                _pendingFailRoutine = null;
            }

            _episodeFinished = false;
            _hasEngagedCore = false;
            _emptyGoalPenaltyConsumed = false;
            _shockRecoverRemaining = 0f;
            EnsureSceneReferencesResolved();
            recordingHelper?.ClearTemporaryBlackout();

            if (_rb != null)
            {
                _rb.constraints = _defaultConstraints;
                _rb.maxAngularVelocity = _defaultMaxAngularVelocity;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            course?.ResetEpisode(this);
            course?.UpdateObjectiveState();
            course?.ResetSuccessVisuals();

            _previousApproachProgress = course != null ? course.GetAgentToCoreProgress01(transform.position) : 0f;
            _previousCoreProgress = course != null ? course.GetCoreGoalProgress01() : 0f;
            _previousDockReadiness = course != null ? course.CoreDockReadiness01 : 0f;
            _lastCoreZoneIndex = course != null ? course.GetCoreZoneIndex() : 0;

            _visualController = GetComponentInChildren<ReactorCoreDeliveryVisualController>();
            _visualController?.ResetState();
        }

        private bool EnsureSceneReferencesResolved()
        {
            bool changed = false;

            if (course == null)
            {
                course = FindFirstObjectByType<ReactorCoreDeliveryCourse>();
                changed |= course != null;
            }

            if (environmentManager == null)
            {
                environmentManager = FindFirstObjectByType<EnvironmentManager>();
                changed |= environmentManager != null;
            }

            if (recordingHelper == null)
            {
                recordingHelper = FindFirstObjectByType<RecordingHelper>();
                changed |= recordingHelper != null;
            }

            return changed;
        }

        protected override void CollectAgentObservations(VectorSensor sensor)
        {
            if (course == null)
            {
                sensor.AddObservation(new float[ReactorCoreDeliveryCourse.ObservationSize]);
                return;
            }

            float shockRecover01 = shockCooldown > 0f
                ? Mathf.Clamp01(_shockRecoverRemaining / shockCooldown)
                : 0f;

            course.AppendObservations(
                sensor,
                transform.position,
                _rb != null ? _rb.linearVelocity : Vector3.zero,
                shockRecover01);
        }

        protected override void ExecuteActions(ActionBuffers actions)
        {
            if (_episodeFinished)
            {
                return;
            }

            Vector2 moveInput = new Vector2(
                actions.ContinuousActions[0],
                actions.ContinuousActions[1]);

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            MoveAgent(moveInput);

            if (_shockRecoverRemaining > 0f)
            {
                _shockRecoverRemaining = Mathf.Max(0f, _shockRecoverRemaining - Time.fixedDeltaTime);
            }

            _visualController?.SetMotion(_rb != null ? _rb.linearVelocity : Vector3.zero);
            course?.UpdateObjectiveState();

            if (!_hasEngagedCore && course != null && course.IsAgentNearCore(transform.position, coreEngageDistance))
            {
                _hasEngagedCore = true;
                AddTrackedReward(engageCoreReward);
            }

            if (course != null && course.TryConsumeCoreDockBonus())
            {
                AddTrackedReward(coreDockBonus);
            }

            if (course != null && course.CanCompleteDelivery(transform.position, insertDistance))
            {
                _episodeFinished = true;
                course.NotifySuccess();
                _visualController?.TriggerSuccess();
                float speedBonus = course.TimeRemaining01 * speedBonusMax;
                Success(successReward + speedBonus);
                return;
            }

            if (course != null && course.TryConsumeTimeout())
            {
                TriggerMeltdownTimeout();
                return;
            }

            bool isNearGoal = course != null && course.IsAgentNearGoal(transform.position, insertDistance);
            if (course != null && !course.IsCoreDocked && isNearGoal)
            {
                if (!_emptyGoalPenaltyConsumed)
                {
                    AddTrackedReward(-emptyGoalPenalty);
                    _emptyGoalPenaltyConsumed = true;
                }
            }
            else
            {
                _emptyGoalPenaltyConsumed = false;
            }

            if (environmentManager != null && environmentManager.HasFallen(transform))
            {
                _episodeFinished = true;
                course?.NotifyFailure();
                Fail(-1.0f);
                return;
            }

            if (course != null)
            {
                Transform objectiveCoreTransform = course.ObjectiveCoreTransform;
                bool coreHasFallen = environmentManager != null
                    && objectiveCoreTransform != null
                    && environmentManager.HasFallen(objectiveCoreTransform);

                if (course.IsCoreOutOfBounds() || coreHasFallen)
                {
                    _episodeFinished = true;
                    course.NotifyFailure();
                    Fail(-lostCorePenalty);
                    return;
                }
            }

            if (MaxStep > 0 && StepCount >= MaxStep - 1)
            {
                _episodeFinished = true;
                course?.NotifyFailure();
                Fail(-1.0f);
                return;
            }

            if (course != null)
            {
                if (!course.HasCoreEnteredEscortPhase())
                {
                    float progress = course.GetAgentToCoreProgress01(transform.position);
                    float delta = progress - _previousApproachProgress;

                    if (delta > 0f)
                    {
                        AddTrackedReward(delta * approachProgressRewardScale);
                    }
                    else if (delta < 0f)
                    {
                        AddTrackedReward(delta * approachRegressionPenaltyScale);
                    }

                    _previousApproachProgress = progress;
                    _previousDockReadiness = course.CoreDockReadiness01;
                }
                else
                {
                    float progress = course.GetCoreGoalProgress01();
                    float delta = progress - _previousCoreProgress;

                    if (delta > 0f)
                    {
                        AddTrackedReward(delta * coreProgressRewardScale);
                    }
                    else if (delta < 0f)
                    {
                        AddTrackedReward(delta * coreRegressionPenaltyScale);
                    }

                    _previousCoreProgress = progress;

                    int zoneIndex = course.GetCoreZoneIndex();
                    if (zoneIndex > _lastCoreZoneIndex)
                    {
                        AddTrackedReward(zoneBonus);
                        _lastCoreZoneIndex = zoneIndex;
                    }

                    float dockReadiness = course.CoreDockReadiness01;
                    float dockReadinessDelta = dockReadiness - _previousDockReadiness;
                    if (dockReadinessDelta > 0f)
                    {
                        AddTrackedReward(dockReadinessDelta * coreSettleProgressRewardScale);
                    }

                    _previousDockReadiness = dockReadiness;
                }
            }

            AddTrackedReward(-stepPenalty);
        }

        protected override void ProvideHeuristicInput(in ActionBuffers actionsOut)
        {
            var continuousActions = actionsOut.ContinuousActions;
            float moveX = 0f;
            float moveZ = 0f;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                {
                    moveX = -1f;
                }
                else if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                {
                    moveX = 1f;
                }

                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
                {
                    moveZ = -1f;
                }
                else if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
                {
                    moveZ = 1f;
                }
            }

            continuousActions[0] = moveX;
            continuousActions[1] = moveZ;
        }

        public void NotifyLaserHit()
        {
            if (_episodeFinished)
            {
                return;
            }

            _episodeFinished = true;
            course?.NotifyFailure();
            _visualController?.TriggerShock();
            Fail(-1.0f);
        }

        public void NotifyShockPulse(Vector3 hazardOrigin)
        {
            if (_episodeFinished || _shockRecoverRemaining > 0f)
            {
                return;
            }

            _shockRecoverRemaining = shockCooldown;
            AddTrackedReward(-shockPenalty);

            if (_rb != null)
            {
                Vector3 knockbackDirection = transform.position - hazardOrigin;
                knockbackDirection.y = 0f;

                if (knockbackDirection.sqrMagnitude < 0.001f)
                {
                    knockbackDirection = Vector3.back;
                }

                _rb.AddForce(knockbackDirection.normalized * shockKnockback, ForceMode.VelocityChange);
            }

            course?.NotifyShockHit();
            _visualController?.TriggerShock();
        }

        private void TriggerMeltdownTimeout()
        {
            _episodeFinished = true;
            course?.TriggerMeltdown();
            _visualController?.TriggerShock();
            LaunchFromMeltdown();

            if (IsVisualRuntime() && recordingHelper != null)
            {
                recordingHelper.TriggerTemporaryBlackout(
                    meltdownCameraCutDelay,
                    meltdownCameraCutDuration,
                    holdUntilClear: true);
            }

            if (IsVisualRuntime() && meltdownFailDelay > 0f)
            {
                _pendingFailRoutine = StartCoroutine(FailAfterDelay(timeoutPenalty, meltdownFailDelay));
                return;
            }

            Fail(-timeoutPenalty);
        }

        private System.Collections.IEnumerator FailAfterDelay(float penalty, float delay)
        {
            yield return new WaitForSeconds(delay);
            _pendingFailRoutine = null;
            Fail(-penalty);
        }

        private void LaunchFromMeltdown()
        {
            if (_rb == null)
            {
                return;
            }

            _rb.constraints = RigidbodyConstraints.None;
            _rb.maxAngularVelocity = Mathf.Max(_defaultMaxAngularVelocity, 40f);

            Vector3 blastDirection = transform.position - (course != null ? course.MeltdownOrigin : transform.position + Vector3.forward);
            blastDirection.y = 0f;
            blastDirection += new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.10f, 0.30f));

            if (blastDirection.sqrMagnitude < 0.001f)
            {
                blastDirection = Vector3.back;
            }

            _rb.angularVelocity = Vector3.zero;
            _rb.AddForce(
                (blastDirection.normalized * meltdownLaunchForce) + (Vector3.up * meltdownLiftForce),
                ForceMode.VelocityChange);
            _rb.AddTorque(Random.onUnitSphere * meltdownSpinTorque, ForceMode.VelocityChange);
        }

        private static bool IsVisualRuntime()
        {
            return !Application.isBatchMode && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
        }

        private void MoveAgent(Vector2 input)
        {
            if (_rb == null)
            {
                return;
            }

            float controlScale = _shockRecoverRemaining > 0f ? 0.55f : 1f;
            Vector3 desiredVelocity = new Vector3(input.x, 0f, input.y) * (maxMoveSpeed * controlScale);
            Vector3 currentPlanarVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            Vector3 velocityDelta = desiredVelocity - currentPlanarVelocity;
            Vector3 velocityStep = Vector3.ClampMagnitude(velocityDelta, acceleration * Time.fixedDeltaTime);

            _rb.AddForce(velocityStep, ForceMode.VelocityChange);

            Vector3 clampedPlanarVelocity = Vector3.ClampMagnitude(
                new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z),
                maxMoveSpeed);

            _rb.linearVelocity = new Vector3(
                clampedPlanarVelocity.x,
                _rb.linearVelocity.y,
                clampedPlanarVelocity.z);
        }
    }
}
