using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RLMovie.Environments.ReactorCoreDelivery
{
    /// <summary>
    /// Owns the mandatory delivery spine, the loose reactor core, and the carry/grab state.
    /// </summary>
    public sealed class ReactorCoreDeliveryCourse : MonoBehaviour
    {
        public const int ObservationSize = 39;

        [Header("=== Navigation Anchors ===")]
        [SerializeField] private Transform startAnchor;
        [SerializeField] private Transform coreSpawnAnchor;
        [SerializeField] private Transform laserCheckpoint;
        [SerializeField] private Transform shockCheckpoint;
        [SerializeField] private Transform doorCheckpoint;
        [SerializeField] private Transform goalSocket;

        [Header("=== Objective Core ===")]
        [SerializeField] private Rigidbody objectiveCore;
        [SerializeField] private Renderer coreRenderer;
        [SerializeField] private float coreDockDistance = 0.72f;
        [SerializeField] private float coreDockSettleTime = 0.35f;
        [SerializeField] private float coreDockMaxPlanarSpeed = 0.36f;
        [SerializeField] private float coreEscortSwitchDistance = 0.55f;
        [SerializeField] private float grabCooldown = 0.32f;
        [SerializeField] private float heldLinearDamping = 5.2f;
        [SerializeField] private float heldAngularDamping = 8.4f;
        [SerializeField] private float laserCoreKnockback = 4.2f;
        [SerializeField] private float shockCoreKnockback = 3.3f;
        [SerializeField] private float shockCoreLift = 1.4f;
        [SerializeField] private float coreShockRecoverDuration = 0.32f;

        [Header("=== Hazards ===")]
        [SerializeField] private SweepingLaserHazard laserHazard;
        [SerializeField] private ShockFloorHazard shockFloorHazard;
        [SerializeField] private BlastDoorHazard blastDoorHazard;

        [Header("=== Visual Feedback ===")]
        [SerializeField] private Renderer reactorGlowRenderer;
        [SerializeField] private ParticleSystem ignitionFx;
        [SerializeField] private ParticleSystem shockFx;
        [SerializeField] private Renderer[] alertBeaconRenderers = new Renderer[0];
        [SerializeField] private Renderer countdownFillRenderer;
        [SerializeField] private Transform countdownFill;
        [SerializeField] private Renderer[] countdownDigitRenderers = new Renderer[0];
        [SerializeField] private ParticleSystem meltdownFx;
        [SerializeField] private ParticleSystem meltdownDebrisFx;
        [SerializeField] private Transform meltdownOrigin;

        [Header("=== Meltdown Pressure ===")]
        [SerializeField] private float baseDeliveryDeadline = 20.5f;
        [SerializeField] private float minDeliveryDeadline = 12f;
        [SerializeField] private float meltdownCoreBlastForce = 8.8f;
        [SerializeField] private float meltdownCoreLiftForce = 6.4f;

        [Header("=== Course Bounds ===")]
        [SerializeField] private float courseHalfWidth = 2.35f;
        [SerializeField] private float progressStartZ = -10f;
        [SerializeField] private float progressEndZ = 18f;
        [SerializeField] private float spawnJitterX = 0.35f;
        [SerializeField] private float spawnJitterZ = 0.28f;
        [SerializeField] private float coreOutOfBoundsPadding = 0.75f;

        [Header("=== Gate Thresholds ===")]
        [SerializeField] private float laserGateZ = 0.2f;
        [SerializeField] private float shockGateZ = 7.0f;
        [SerializeField] private float doorGateZ = 13.0f;

        private readonly Color _defaultGlowBase = new Color(0.10f, 0.22f, 0.28f);
        private readonly Color _defaultGlowEmission = new Color(0.00f, 0.10f, 0.15f);
        private readonly Color _successGlowBase = new Color(0.22f, 0.95f, 1.20f);
        private readonly Color _successGlowEmission = new Color(0.35f, 1.40f, 1.80f);
        private readonly Color _defaultCoreBase = new Color(0.20f, 0.80f, 1.00f);
        private readonly Color _defaultCoreEmission = new Color(0.18f, 0.95f, 1.35f);
        private readonly Color _escortCoreBase = new Color(0.24f, 0.92f, 1.08f);
        private readonly Color _escortCoreEmission = new Color(0.24f, 1.18f, 1.58f);
        private readonly Color _heldCoreBase = new Color(0.95f, 0.88f, 0.32f);
        private readonly Color _heldCoreEmission = new Color(1.45f, 1.10f, 0.18f);
        private readonly Color _dockedCoreBase = new Color(0.30f, 1.04f, 1.18f);
        private readonly Color _dockedCoreEmission = new Color(0.30f, 1.42f, 1.84f);
        private readonly Color _successCoreBase = new Color(0.38f, 1.20f, 1.32f);
        private readonly Color _successCoreEmission = new Color(0.42f, 1.72f, 2.18f);
        private readonly Color _failureGlowBase = new Color(0.36f, 0.10f, 0.08f);
        private readonly Color _failureGlowEmission = new Color(1.40f, 0.42f, 0.18f);
        private readonly Color _alertOffBase = new Color(0.10f, 0.10f, 0.12f);
        private readonly Color _alertOffEmission = new Color(0.02f, 0.02f, 0.03f);

        private bool _escortPhaseStarted;
        private bool _coreDocked;
        private bool _coreDockBonusPending;
        private bool _timeoutPending;
        private bool _meltdownTriggered;
        private bool _coreLaserPenaltyPending;
        private bool _coreShockPenaltyPending;
        private float _coreDockSettleTimer;
        private float _agentToCoreReferenceDistance = 1f;
        private float _coreGoalReferenceDistance = 1f;
        private float _deliveryDeadline = 1f;
        private float _deliveryTimerRemaining = 1f;
        private float _grabCooldownRemaining;
        private float _coreShockRecoverRemaining;
        private float _baseCoreLinearDamping = -1f;
        private float _baseCoreAngularDamping = -1f;
        private Vector3 _countdownFillRestScale = Vector3.one;
        private FixedJoint _holdJoint;
        private Rigidbody _carrierBody;

        public bool IsCoreDocked => _coreDocked;
        public bool IsHoldingCore => _holdJoint != null;
        public float GrabCooldown01 => grabCooldown <= 0f ? 0f : Mathf.Clamp01(_grabCooldownRemaining / grabCooldown);
        public float CoreDockReadiness01 => _coreDocked
            ? 1f
            : coreDockSettleTime <= 0f
                ? 0f
                : Mathf.Clamp01(_coreDockSettleTimer / coreDockSettleTime);
        public float TimeRemaining01 => _deliveryDeadline <= 0f
            ? 1f
            : Mathf.Clamp01(_deliveryTimerRemaining / _deliveryDeadline);
        public float TimeRemainingSeconds => Mathf.Max(0f, _deliveryTimerRemaining);
        public bool IsAlertActive => !_coreDocked && !_meltdownTriggered && _deliveryTimerRemaining > 0f;
        public bool IsMeltdownTriggered => _meltdownTriggered;
        public Transform ObjectiveCoreTransform => objectiveCore != null ? objectiveCore.transform : null;
        public Vector3 MeltdownOrigin => meltdownOrigin != null
            ? meltdownOrigin.position
            : goalSocket != null
                ? goalSocket.position
                : transform.position;

        private void Awake()
        {
            CacheCorePhysicsDefaults();
        }

        public void ResetEpisode(ReactorCoreDeliveryAgent agent)
        {
            if (agent == null || startAnchor == null)
            {
                return;
            }

            CacheCorePhysicsDefaults();

            float randomizationStrength = GetEnvironmentParameter("randomization_strength", 0.28f);
            float phaseJitter = GetEnvironmentParameter("phase_jitter", 0.24f);
            float laserSpeedScale = GetEnvironmentParameter("laser_speed_scale", 1.0f);
            float shockActiveScale = GetEnvironmentParameter("shock_active_scale", 1.0f);
            float doorOpenScale = GetEnvironmentParameter("door_open_scale", 1.0f);
            float deadlineScale = GetEnvironmentParameter("delivery_deadline_scale", 1.0f);

            Vector3 startPosition = startAnchor.position;
            startPosition.x += Random.Range(-spawnJitterX, spawnJitterX) * randomizationStrength;
            startPosition.z += Random.Range(-spawnJitterZ, spawnJitterZ) * randomizationStrength;

            agent.transform.position = startPosition;
            agent.transform.rotation = startAnchor.rotation;

            DestroyHoldJointImmediate();
            ResetObjectiveCore();

            laserHazard?.ResetCycle(Random.Range(0f, phaseJitter), laserSpeedScale);
            shockFloorHazard?.ResetCycle(Random.Range(0f, phaseJitter), shockActiveScale);
            blastDoorHazard?.ResetCycle(Random.Range(0f, phaseJitter), doorOpenScale);

            _escortPhaseStarted = false;
            _coreDocked = false;
            _coreDockBonusPending = false;
            _timeoutPending = false;
            _meltdownTriggered = false;
            _coreLaserPenaltyPending = false;
            _coreShockPenaltyPending = false;
            _coreDockSettleTimer = 0f;
            _grabCooldownRemaining = 0f;
            _coreShockRecoverRemaining = 0f;
            _agentToCoreReferenceDistance = Mathf.Max(0.75f, PlanarDistance(startPosition, GetCorePosition()));
            _coreGoalReferenceDistance = Mathf.Max(4f, PlanarDistance(GetCorePosition(), goalSocket != null ? goalSocket.position : GetCorePosition()));
            _deliveryDeadline = Mathf.Max(minDeliveryDeadline, baseDeliveryDeadline * Mathf.Max(0.55f, deadlineScale));
            _deliveryTimerRemaining = _deliveryDeadline;

            if (countdownFill != null)
            {
                _countdownFillRestScale = countdownFill.localScale;
            }

            ApplyGlow(_defaultGlowBase, _defaultGlowEmission);
            UpdateObjectiveVisuals(delivered: false);
            ResetFailureVisuals();
            UpdateAlertVisuals();
        }

        public void UpdateObjectiveState()
        {
            bool wasDocked = _coreDocked;

            if (_grabCooldownRemaining > 0f)
            {
                _grabCooldownRemaining = Mathf.Max(0f, _grabCooldownRemaining - Time.fixedDeltaTime);
            }

            if (_coreShockRecoverRemaining > 0f)
            {
                _coreShockRecoverRemaining = Mathf.Max(0f, _coreShockRecoverRemaining - Time.fixedDeltaTime);
            }

            if (!_escortPhaseStarted && coreSpawnAnchor != null)
            {
                _escortPhaseStarted = IsHoldingCore || PlanarDistance(GetCorePosition(), coreSpawnAnchor.position) >= coreEscortSwitchDistance;
            }

            bool isCoreInsideDockZone = ComputeCoreInsideDockZone();
            bool coreIsSettled = !IsHoldingCore && isCoreInsideDockZone && GetCorePlanarSpeed() <= coreDockMaxPlanarSpeed;

            if (coreIsSettled)
            {
                _coreDockSettleTimer = coreDockSettleTime <= 0f
                    ? 0f
                    : Mathf.Min(coreDockSettleTime, _coreDockSettleTimer + Time.fixedDeltaTime);
            }
            else
            {
                _coreDockSettleTimer = 0f;
            }

            _coreDocked = isCoreInsideDockZone
                && (coreDockSettleTime <= 0f || _coreDockSettleTimer >= coreDockSettleTime);

            if (!wasDocked && _coreDocked)
            {
                _coreDockBonusPending = true;
            }

            if (!_coreDocked && !_meltdownTriggered && _deliveryTimerRemaining > 0f)
            {
                _deliveryTimerRemaining = Mathf.Max(0f, _deliveryTimerRemaining - Time.fixedDeltaTime);
                if (_deliveryTimerRemaining <= 0f)
                {
                    _timeoutPending = true;
                }
            }

            UpdateObjectiveVisuals(delivered: false);
            UpdateAlertVisuals();
        }

        public void AppendObservations(
            VectorSensor sensor,
            Vector3 worldPosition,
            Vector3 velocity,
            float shockRecover01)
        {
            Vector3 corePosition = GetCorePosition();
            Vector3 coreVelocity = GetCoreVelocity();
            Vector3 coreDelta = corePosition - worldPosition;
            Vector3 goalDelta = goalSocket != null ? goalSocket.position - worldPosition : Vector3.zero;
            Vector3 coreGoalDelta = goalSocket != null ? goalSocket.position - corePosition : Vector3.zero;
            Transform checkpoint = GetCurrentCheckpoint(corePosition);
            Vector3 checkpointDelta = checkpoint != null ? checkpoint.position - worldPosition : Vector3.zero;

            sensor.AddObservation(Mathf.Clamp((worldPosition.x - transform.position.x) / courseHalfWidth, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(Mathf.InverseLerp(progressStartZ, progressEndZ, worldPosition.z) * 2f - 1f, -1f, 1f));

            sensor.AddObservation(Mathf.Clamp(velocity.x / 6f, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(velocity.z / 6f, -1f, 1f));

            sensor.AddObservation(Mathf.Clamp(coreDelta.x / courseHalfWidth, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(coreDelta.z / 10f, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp01(coreDelta.magnitude / Mathf.Max(1f, progressEndZ - progressStartZ)));

            sensor.AddObservation(Mathf.Clamp(goalDelta.x / courseHalfWidth, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(goalDelta.z / 10f, -1f, 1f));

            sensor.AddObservation(Mathf.Clamp(coreGoalDelta.x / courseHalfWidth, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(coreGoalDelta.z / 10f, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp01(coreGoalDelta.magnitude / Mathf.Max(1f, _coreGoalReferenceDistance)));

            sensor.AddObservation(Mathf.Clamp(coreVelocity.x / 6f, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(coreVelocity.z / 6f, -1f, 1f));

            sensor.AddObservation(Mathf.Clamp(checkpointDelta.x / courseHalfWidth, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(checkpointDelta.z / 10f, -1f, 1f));

            AppendLaserObservations(sensor, worldPosition);
            AppendShockObservations(sensor, worldPosition);
            AppendDoorObservations(sensor, worldPosition);

            sensor.AddObservation(CoreDockReadiness01);
            sensor.AddObservation(_coreDocked ? 1f : 0f);
            sensor.AddObservation(TimeRemaining01);
            sensor.AddObservation(shockRecover01);
            sensor.AddObservation(IsHoldingCore ? 1f : 0f);
            sensor.AddObservation(GrabCooldown01);
            sensor.AddObservation(GetCoreGateIndex() / 3f);
        }

        public float GetAgentToCoreProgress01(Vector3 worldPosition)
        {
            if (objectiveCore == null)
            {
                return 0f;
            }

            return 1f - Mathf.Clamp01(PlanarDistance(worldPosition, GetCorePosition()) / Mathf.Max(0.01f, _agentToCoreReferenceDistance));
        }

        public float GetCoreGoalProgress01()
        {
            if (objectiveCore == null || goalSocket == null)
            {
                return 0f;
            }

            return 1f - Mathf.Clamp01(PlanarDistance(GetCorePosition(), goalSocket.position) / Mathf.Max(0.01f, _coreGoalReferenceDistance));
        }

        public bool HasCoreEnteredEscortPhase()
        {
            return _escortPhaseStarted;
        }

        public int GetCoreGateIndex()
        {
            Vector3 corePosition = GetCorePosition();
            if (corePosition.z < laserGateZ)
            {
                return 0;
            }

            if (corePosition.z < shockGateZ)
            {
                return 1;
            }

            if (corePosition.z < doorGateZ)
            {
                return 2;
            }

            return 3;
        }

        public bool IsAgentNearCore(Vector3 worldPosition, float distance)
        {
            if (objectiveCore == null)
            {
                return false;
            }

            return PlanarDistance(worldPosition, GetCorePosition()) <= distance;
        }

        public bool IsCoreOutOfBounds()
        {
            if (objectiveCore == null)
            {
                return false;
            }

            Vector3 corePosition = GetCorePosition();
            float lateralOffset = Mathf.Abs(corePosition.x - transform.position.x);

            return lateralOffset > courseHalfWidth + coreOutOfBoundsPadding
                || corePosition.z < progressStartZ - (coreOutOfBoundsPadding * 2f)
                || corePosition.z > progressEndZ + (coreOutOfBoundsPadding * 2f);
        }

        public bool TryGrabCore(Rigidbody carrierBody, Transform carryAnchor, float maxDistance)
        {
            if (carrierBody == null || carryAnchor == null || objectiveCore == null || _coreDocked || _meltdownTriggered || IsHoldingCore)
            {
                return false;
            }

            if (_grabCooldownRemaining > 0f)
            {
                return false;
            }

            if (Vector3.Distance(carryAnchor.position, objectiveCore.position) > maxDistance)
            {
                return false;
            }

            DestroyHoldJointImmediate();

            _holdJoint = objectiveCore.gameObject.AddComponent<FixedJoint>();
            _holdJoint.autoConfigureConnectedAnchor = false;
            _holdJoint.connectedBody = carrierBody;
            _holdJoint.connectedAnchor = carrierBody.transform.InverseTransformPoint(carryAnchor.position);
            _holdJoint.enableCollision = false;
            _holdJoint.breakForce = float.PositiveInfinity;
            _holdJoint.breakTorque = float.PositiveInfinity;

            objectiveCore.linearVelocity *= 0.2f;
            objectiveCore.angularVelocity *= 0.1f;
            objectiveCore.linearDamping = heldLinearDamping;
            objectiveCore.angularDamping = heldAngularDamping;

            _carrierBody = carrierBody;
            _escortPhaseStarted = true;
            UpdateObjectiveVisuals(delivered: false);
            return true;
        }

        public bool ReleaseHeldCore(bool startCooldown)
        {
            return ReleaseHeldCore(startCooldown, Vector3.zero);
        }

        public bool ForceReleaseHeldCore(Vector3 releaseImpulse)
        {
            return ReleaseHeldCore(true, releaseImpulse);
        }

        public bool ReleaseHeldCore(bool startCooldown, Vector3 releaseImpulse)
        {
            if (!IsHoldingCore || objectiveCore == null)
            {
                return false;
            }

            DestroyHoldJointImmediate();
            RestoreCorePhysicsDefaults();

            if (releaseImpulse.sqrMagnitude > 0.0001f)
            {
                objectiveCore.AddForce(releaseImpulse, ForceMode.VelocityChange);
            }

            _carrierBody = null;
            if (startCooldown)
            {
                _grabCooldownRemaining = grabCooldown;
            }

            UpdateObjectiveVisuals(delivered: false);
            return true;
        }

        public bool TryConsumeCoreDockBonus()
        {
            if (!_coreDockBonusPending)
            {
                return false;
            }

            _coreDockBonusPending = false;
            return true;
        }

        public bool TryConsumeTimeout()
        {
            if (!_timeoutPending)
            {
                return false;
            }

            _timeoutPending = false;
            return true;
        }

        public bool TryConsumeCoreLaserPenalty()
        {
            if (!_coreLaserPenaltyPending)
            {
                return false;
            }

            _coreLaserPenaltyPending = false;
            return true;
        }

        public bool TryConsumeCoreShockPenalty()
        {
            if (!_coreShockPenaltyPending)
            {
                return false;
            }

            _coreShockPenaltyPending = false;
            return true;
        }

        public bool IsObjectiveCore(Rigidbody body)
        {
            return body != null && body == objectiveCore;
        }

        public void NotifyCoreLaserHit(Vector3 hazardOrigin)
        {
            if (objectiveCore == null || _meltdownTriggered || _coreDocked)
            {
                return;
            }

            Vector3 blastDirection = GetCorePosition() - hazardOrigin;
            blastDirection.y = 0f;

            if (blastDirection.sqrMagnitude < 0.001f)
            {
                blastDirection = Vector3.back;
            }

            Vector3 impulse = (blastDirection.normalized * laserCoreKnockback) + (Vector3.back * 0.8f);
            if (IsHoldingCore)
            {
                ReleaseHeldCore(true, impulse);
            }
            else
            {
                objectiveCore.AddForce(impulse, ForceMode.VelocityChange);
            }

            _coreLaserPenaltyPending = true;
            NotifyShockHit();
        }

        public void NotifyCoreShockPulse(Vector3 hazardOrigin)
        {
            if (objectiveCore == null || _meltdownTriggered || _coreDocked || _coreShockRecoverRemaining > 0f)
            {
                return;
            }

            _coreShockRecoverRemaining = coreShockRecoverDuration;

            Vector3 blastDirection = GetCorePosition() - hazardOrigin;
            blastDirection.y = 0f;

            if (blastDirection.sqrMagnitude < 0.001f)
            {
                blastDirection = Vector3.right;
            }

            Vector3 impulse = (blastDirection.normalized * shockCoreKnockback) + (Vector3.up * shockCoreLift);
            if (IsHoldingCore)
            {
                ReleaseHeldCore(true, impulse);
            }
            else
            {
                objectiveCore.AddForce(impulse, ForceMode.VelocityChange);
            }

            _coreShockPenaltyPending = true;
            NotifyShockHit();
        }

        public void TriggerMeltdown()
        {
            if (_meltdownTriggered)
            {
                return;
            }

            _meltdownTriggered = true;
            _timeoutPending = false;

            if (IsHoldingCore)
            {
                ReleaseHeldCore(false);
            }

            if (ignitionFx != null && ignitionFx.isPlaying)
            {
                ignitionFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            PlayParticleSystem(meltdownFx);
            PlayParticleSystem(meltdownDebrisFx);
            ApplyGlow(_failureGlowBase, _failureGlowEmission);
            UpdateAlertVisuals();

            if (objectiveCore != null)
            {
                Vector3 blastDirection = objectiveCore.position - MeltdownOrigin;
                blastDirection.y = 0f;

                if (blastDirection.sqrMagnitude < 0.001f)
                {
                    blastDirection = Vector3.back;
                }

                objectiveCore.AddForce(
                    (blastDirection.normalized * meltdownCoreBlastForce) + (Vector3.up * meltdownCoreLiftForce),
                    ForceMode.VelocityChange);
            }
        }

        public void NotifySuccess()
        {
            if (IsHoldingCore)
            {
                ReleaseHeldCore(false);
            }

            if (objectiveCore != null)
            {
                objectiveCore.linearVelocity = Vector3.zero;
                objectiveCore.angularVelocity = Vector3.zero;
            }

            UpdateObjectiveVisuals(delivered: true);
            ApplyGlow(_successGlowBase, _successGlowEmission);
            ResetFailureVisuals();
            UpdateAlertVisuals();

            if (ignitionFx != null)
            {
                ignitionFx.Play(true);
            }
        }

        public void NotifyFailure()
        {
            if (ignitionFx != null && ignitionFx.isPlaying)
            {
                ignitionFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (!_meltdownTriggered)
            {
                UpdateAlertVisuals();
            }
        }

        public void NotifyShockHit()
        {
            if (shockFx != null)
            {
                shockFx.Play(true);
            }
        }

        public void ResetSuccessVisuals()
        {
            UpdateObjectiveVisuals(delivered: false);
            ApplyGlow(_defaultGlowBase, _defaultGlowEmission);
            ResetFailureVisuals();
            UpdateAlertVisuals();

            if (ignitionFx != null)
            {
                ignitionFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (shockFx != null)
            {
                shockFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void AppendLaserObservations(VectorSensor sensor, Vector3 worldPosition)
        {
            if (laserHazard == null)
            {
                sensor.AddObservation(new float[5]);
                return;
            }

            Vector3 delta = laserHazard.HazardCenter - worldPosition;
            float phaseRadians = laserHazard.Phase01 * Mathf.PI * 2f;

            sensor.AddObservation(Mathf.Clamp(delta.x / courseHalfWidth, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(delta.z / 10f, -1f, 1f));
            sensor.AddObservation(Mathf.Sin(phaseRadians));
            sensor.AddObservation(Mathf.Cos(phaseRadians));
            sensor.AddObservation(laserHazard.SweepAngle01);
        }

        private void AppendShockObservations(VectorSensor sensor, Vector3 worldPosition)
        {
            if (shockFloorHazard == null)
            {
                sensor.AddObservation(new float[5]);
                return;
            }

            Vector3 delta = shockFloorHazard.HazardCenter - worldPosition;
            float phaseRadians = shockFloorHazard.Phase01 * Mathf.PI * 2f;

            sensor.AddObservation(Mathf.Clamp(delta.x / courseHalfWidth, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(delta.z / 10f, -1f, 1f));
            sensor.AddObservation(Mathf.Sin(phaseRadians));
            sensor.AddObservation(Mathf.Cos(phaseRadians));
            sensor.AddObservation(shockFloorHazard.IsActive ? 1f : 0f);
        }

        private void AppendDoorObservations(VectorSensor sensor, Vector3 worldPosition)
        {
            if (blastDoorHazard == null)
            {
                sensor.AddObservation(new float[6]);
                return;
            }

            Vector3 delta = blastDoorHazard.HazardCenter - worldPosition;
            float phaseRadians = blastDoorHazard.Phase01 * Mathf.PI * 2f;

            sensor.AddObservation(Mathf.Clamp(delta.x / courseHalfWidth, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(delta.z / 10f, -1f, 1f));
            sensor.AddObservation(Mathf.Sin(phaseRadians));
            sensor.AddObservation(Mathf.Cos(phaseRadians));
            sensor.AddObservation(blastDoorHazard.OpenAmount01);
            sensor.AddObservation(blastDoorHazard.IsPassable ? 1f : 0f);
        }

        private Transform GetCurrentCheckpoint(Vector3 corePosition)
        {
            if (!_escortPhaseStarted)
            {
                return objectiveCore != null ? objectiveCore.transform : coreSpawnAnchor;
            }

            int gateIndex = GetCoreGateIndex();
            return gateIndex switch
            {
                0 => laserCheckpoint != null ? laserCheckpoint : shockCheckpoint,
                1 => shockCheckpoint != null ? shockCheckpoint : doorCheckpoint,
                2 => doorCheckpoint != null ? doorCheckpoint : goalSocket,
                _ => goalSocket
            };
        }

        private float GetEnvironmentParameter(string name, float fallbackValue)
        {
            Academy academy = Academy.Instance;
            return academy == null
                ? fallbackValue
                : academy.EnvironmentParameters.GetWithDefault(name, fallbackValue);
        }

        private void CacheCorePhysicsDefaults()
        {
            if (objectiveCore == null || _baseCoreLinearDamping >= 0f)
            {
                return;
            }

            _baseCoreLinearDamping = objectiveCore.linearDamping;
            _baseCoreAngularDamping = objectiveCore.angularDamping;
        }

        private void RestoreCorePhysicsDefaults()
        {
            if (objectiveCore == null)
            {
                return;
            }

            objectiveCore.linearDamping = _baseCoreLinearDamping >= 0f ? _baseCoreLinearDamping : objectiveCore.linearDamping;
            objectiveCore.angularDamping = _baseCoreAngularDamping >= 0f ? _baseCoreAngularDamping : objectiveCore.angularDamping;
        }

        private void ApplyGlow(Color baseColor, Color emissionColor)
        {
            if (reactorGlowRenderer == null)
            {
                return;
            }

            Material material = reactorGlowRenderer.material;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }
        }

        private void ResetObjectiveCore()
        {
            if (objectiveCore == null || coreSpawnAnchor == null)
            {
                return;
            }

            RestoreCorePhysicsDefaults();
            objectiveCore.linearVelocity = Vector3.zero;
            objectiveCore.angularVelocity = Vector3.zero;
            objectiveCore.position = coreSpawnAnchor.position;
            objectiveCore.rotation = coreSpawnAnchor.rotation;
            objectiveCore.Sleep();
        }

        private bool ComputeCoreInsideDockZone()
        {
            if (objectiveCore == null || goalSocket == null)
            {
                return false;
            }

            return PlanarDistance(GetCorePosition(), goalSocket.position) <= coreDockDistance;
        }

        private Vector3 GetCorePosition()
        {
            if (objectiveCore != null)
            {
                return objectiveCore.position;
            }

            return coreSpawnAnchor != null ? coreSpawnAnchor.position : transform.position;
        }

        private Vector3 GetCoreVelocity()
        {
            return objectiveCore != null ? objectiveCore.linearVelocity : Vector3.zero;
        }

        private float GetCorePlanarSpeed()
        {
            Vector3 coreVelocity = GetCoreVelocity();
            coreVelocity.y = 0f;
            return coreVelocity.magnitude;
        }

        private void UpdateObjectiveVisuals(bool delivered)
        {
            Color baseColor = _defaultCoreBase;
            Color emissionColor = _defaultCoreEmission;

            if (delivered)
            {
                baseColor = _successCoreBase;
                emissionColor = _successCoreEmission;
            }
            else if (_coreDocked)
            {
                baseColor = _dockedCoreBase;
                emissionColor = _dockedCoreEmission;
            }
            else if (IsHoldingCore)
            {
                baseColor = _heldCoreBase;
                emissionColor = _heldCoreEmission;
            }
            else if (_escortPhaseStarted)
            {
                baseColor = _escortCoreBase;
                emissionColor = _escortCoreEmission;
            }

            ApplyColor(coreRenderer, baseColor, emissionColor);
        }

        private void ResetFailureVisuals()
        {
            StopParticleSystem(meltdownFx);
            StopParticleSystem(meltdownDebrisFx);
        }

        private void UpdateAlertVisuals()
        {
            bool alertActive = IsAlertActive;
            bool showCountdown = alertActive || _meltdownTriggered;
            float urgency01 = _meltdownTriggered ? 1f : 1f - TimeRemaining01;
            float pulse01 = 0.45f + (Mathf.Abs(Mathf.Sin(Time.time * Mathf.Lerp(4.0f, 11.5f, urgency01))) * 0.55f);

            Color baseColor;
            Color emissionColor;

            if (_meltdownTriggered)
            {
                baseColor = new Color(0.88f, 0.14f, 0.12f);
                emissionColor = new Color(3.10f, 0.28f, 0.12f) * pulse01;
            }
            else if (alertActive)
            {
                baseColor = Color.Lerp(new Color(0.34f, 0.05f, 0.05f), new Color(0.90f, 0.08f, 0.08f), urgency01);
                emissionColor = Color.Lerp(new Color(0.95f, 0.10f, 0.10f), new Color(2.35f, 0.18f, 0.12f), urgency01) * pulse01;
            }
            else
            {
                baseColor = _alertOffBase;
                emissionColor = _alertOffEmission;
            }

            bool alertVisible = alertActive || _meltdownTriggered;
            ApplyColors(alertBeaconRenderers, baseColor, emissionColor, alertVisible);

            if (countdownFillRenderer != null)
            {
                countdownFillRenderer.enabled = alertVisible;
                if (alertVisible)
                {
                    ApplyColor(countdownFillRenderer, baseColor, emissionColor);
                }
            }

            if (countdownFill != null)
            {
                Vector3 fillScale = _countdownFillRestScale;
                fillScale.x *= _meltdownTriggered
                    ? 0.02f
                    : alertActive
                        ? Mathf.Max(0.02f, TimeRemaining01)
                        : 0.02f;
                countdownFill.localScale = fillScale;
            }

            if (countdownDigitRenderers == null || countdownDigitRenderers.Length < 14)
            {
                return;
            }

            int secondsRemaining = _meltdownTriggered
                ? 0
                : Mathf.Clamp(Mathf.CeilToInt(TimeRemainingSeconds), 0, 99);

            SetCountdownDigit(countdownDigitRenderers, 0, secondsRemaining / 10, showCountdown, baseColor, emissionColor);
            SetCountdownDigit(countdownDigitRenderers, 7, secondsRemaining % 10, showCountdown, baseColor, emissionColor);
        }

        private void SetCountdownDigit(Renderer[] renderers, int startIndex, int digit, bool visible, Color baseColor, Color emissionColor)
        {
            bool[] pattern = GetDigitPattern(digit);
            for (int i = 0; i < 7; i++)
            {
                Renderer renderer = renderers[startIndex + i];
                if (renderer == null)
                {
                    continue;
                }

                bool enabled = visible && pattern[i];
                renderer.enabled = enabled;
                if (enabled)
                {
                    ApplyColor(renderer, baseColor, emissionColor);
                }
            }
        }

        private void DestroyHoldJointImmediate()
        {
            if (_holdJoint == null)
            {
                return;
            }

            _holdJoint.connectedBody = null;

            if (Application.isPlaying)
            {
                Destroy(_holdJoint);
            }
            else
            {
                DestroyImmediate(_holdJoint);
            }

            _holdJoint = null;
        }

        private static bool[] GetDigitPattern(int digit)
        {
            return digit switch
            {
                0 => new[] { true, true, true, false, true, true, true },
                1 => new[] { false, false, true, false, false, true, false },
                2 => new[] { true, false, true, true, true, false, true },
                3 => new[] { true, false, true, true, false, true, true },
                4 => new[] { false, true, true, true, false, true, false },
                5 => new[] { true, true, false, true, false, true, true },
                6 => new[] { true, true, false, true, true, true, true },
                7 => new[] { true, false, true, false, false, true, false },
                8 => new[] { true, true, true, true, true, true, true },
                9 => new[] { true, true, true, true, false, true, true },
                _ => new[] { false, false, false, false, false, false, false }
            };
        }

        private static void ApplyColor(Renderer renderer, Color baseColor, Color emissionColor)
        {
            if (renderer == null)
            {
                return;
            }

            Material material = renderer.material;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }
        }

        private static void ApplyColors(Renderer[] renderers, Color baseColor, Color emissionColor, bool visible)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = visible;
                if (visible)
                {
                    ApplyColor(renderer, baseColor, emissionColor);
                }
            }
        }

        private static void PlayParticleSystem(ParticleSystem particleSystem)
        {
            if (particleSystem != null)
            {
                particleSystem.Play(true);
            }
        }

        private static void StopParticleSystem(ParticleSystem particleSystem)
        {
            if (particleSystem != null)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
