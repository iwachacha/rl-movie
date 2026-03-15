using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RLMovie.Environments.ReactorCoreDelivery
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ShockFloorHazard : MonoBehaviour
    {
        [SerializeField] private ReactorCoreDeliveryCourse course;
        [SerializeField] private Renderer[] panelRenderers = new Renderer[0];
        [SerializeField] private Renderer[] indicatorRenderers = new Renderer[0];
        [SerializeField] private Transform[] ramHeads = new Transform[0];
        [SerializeField] private float cycleDuration = 2.2f;
        [SerializeField] private float activeDuration = 0.85f;
        [SerializeField] private float ramTravelDistance = 0.82f;
        [SerializeField] private float ramDirectionSign = 1f;

        private float _phaseOffset01;
        private float _activeScale = 1f;
        private Vector3[] _ramRestLocalPositions = new Vector3[0];

        public Vector3 HazardCenter => transform.position;

        public float Phase01 => Mathf.Repeat((Time.time / Mathf.Max(0.1f, cycleDuration)) + _phaseOffset01, 1f);

        public bool IsActive => (Phase01 * cycleDuration) < Mathf.Clamp(activeDuration * _activeScale, 0.2f, cycleDuration - 0.1f);

        public float SafeWindow01
        {
            get
            {
                if (IsActive)
                {
                    return 0f;
                }

                float activeEnd = Mathf.Clamp(activeDuration * _activeScale, 0.2f, cycleDuration - 0.1f);
                float currentTime = Phase01 * cycleDuration;
                return Mathf.Clamp01(1f - Mathf.InverseLerp(activeEnd, cycleDuration, currentTime));
            }
        }

        private void OnValidate()
        {
            ResolveCourseReference();

#if UNITY_EDITOR
            if (course != null)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }

        private void Awake()
        {
            ResolveCourseReference();
            CacheRamRestPose();
        }

        public void ResetCycle(float phaseOffset01, float activeScale)
        {
            _phaseOffset01 = phaseOffset01;
            _activeScale = Mathf.Max(0.35f, activeScale);
            UpdateVisualState();
        }

        private void Update()
        {
            UpdateVisualState();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsActive)
            {
                return;
            }

            ReactorCoreDeliveryAgent agent = other.GetComponentInParent<ReactorCoreDeliveryAgent>();
            if (agent != null)
            {
                agent.NotifyShockPulse(transform.position);
                return;
            }

            Rigidbody otherBody = other.attachedRigidbody ?? other.GetComponentInParent<Rigidbody>();
            if (otherBody == null)
            {
                return;
            }

            ResolveCourseReference();
            if (course != null && !course.IsHoldingCore && course.IsObjectiveCore(otherBody))
            {
                course.NotifyCoreShockPulse(transform.position);
            }
        }

        private void UpdateVisualState()
        {
            UpdateRamPose();

            Color baseColor = IsActive
                ? new Color(0.35f, 0.9f, 1.0f)
                : new Color(0.10f, 0.18f, 0.24f);

            Color emissionColor = IsActive
                ? new Color(0.20f, 1.35f, 1.80f)
                : new Color(0.00f, 0.05f, 0.10f);

            ApplyColors(panelRenderers, baseColor, emissionColor);
            ApplyColors(indicatorRenderers, baseColor, emissionColor);
        }

        private void CacheRamRestPose()
        {
            if (ramHeads == null)
            {
                _ramRestLocalPositions = new Vector3[0];
                return;
            }

            _ramRestLocalPositions = new Vector3[ramHeads.Length];
            for (int i = 0; i < ramHeads.Length; i++)
            {
                if (ramHeads[i] != null)
                {
                    _ramRestLocalPositions[i] = ramHeads[i].localPosition;
                }
            }
        }

        private void UpdateRamPose()
        {
            if (ramHeads == null || _ramRestLocalPositions.Length != ramHeads.Length)
            {
                return;
            }

            float extension01 = ComputeRamExtension01();
            for (int i = 0; i < ramHeads.Length; i++)
            {
                if (ramHeads[i] == null)
                {
                    continue;
                }

                ramHeads[i].localPosition = _ramRestLocalPositions[i] + (Vector3.right * (ramTravelDistance * ramDirectionSign * extension01));
            }
        }

        private float ComputeRamExtension01()
        {
            if (!IsActive)
            {
                return 0f;
            }

            float activeWindow = Mathf.Clamp(activeDuration * _activeScale, 0.12f, cycleDuration - 0.05f);
            float currentTime = Phase01 * cycleDuration;
            float extendWindow = Mathf.Min(0.18f, activeWindow * 0.45f);
            float retractStart = Mathf.Max(extendWindow, activeWindow * 0.72f);

            if (currentTime < extendWindow)
            {
                return Mathf.SmoothStep(0f, 1f, currentTime / Mathf.Max(0.01f, extendWindow));
            }

            if (currentTime < retractStart)
            {
                return 1f;
            }

            return Mathf.SmoothStep(1f, 0f, (currentTime - retractStart) / Mathf.Max(0.01f, activeWindow - retractStart));
        }

        private void ResolveCourseReference()
        {
            if (course == null)
            {
                course = FindFirstObjectByType<ReactorCoreDeliveryCourse>();
            }
        }

        private static void ApplyColors(Renderer[] renderers, Color baseColor, Color emissionColor)
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
        }
    }
}
