using UnityEngine;

namespace RLMovie.Environments.ReactorCoreDelivery
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SweepingLaserHazard : MonoBehaviour
    {
        [SerializeField] private Transform pivot;
        [SerializeField] private Renderer beamRenderer;
        [SerializeField] private float sweepAngle = 70f;
        [SerializeField] private float cycleDuration = 2.4f;

        private float _phaseOffset01;
        private float _speedScale = 1f;

        public Vector3 HazardCenter => pivot != null ? pivot.position : transform.position;

        public float Phase01 => Mathf.Repeat((Time.time * Mathf.Max(0.01f, _speedScale) / Mathf.Max(0.1f, cycleDuration)) + _phaseOffset01, 1f);

        public float SweepAngle01
        {
            get
            {
                float sweep01 = Mathf.PingPong(Phase01 * 2f, 1f);
                return Mathf.Clamp01(sweep01);
            }
        }

        public void ResetCycle(float phaseOffset01, float speedScale)
        {
            _phaseOffset01 = phaseOffset01;
            _speedScale = Mathf.Max(0.5f, speedScale);
            ApplyPose();
        }

        private void Update()
        {
            ApplyPose();
        }

        private void OnTriggerEnter(Collider other)
        {
            ReactorCoreDeliveryAgent agent = other.GetComponentInParent<ReactorCoreDeliveryAgent>();
            if (agent != null)
            {
                agent.NotifyLaserHit();
            }
        }

        private void ApplyPose()
        {
            if (pivot == null)
            {
                return;
            }

            float angle = Mathf.Lerp(-sweepAngle, sweepAngle, SweepAngle01);
            pivot.localRotation = Quaternion.Euler(0f, angle, 0f);

            if (beamRenderer != null)
            {
                Material material = beamRenderer.material;
                float pulse = 0.6f + Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.8f;
                Color emission = new Color(1.4f, 0.2f, 0.2f) * pulse;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emission);
                }
            }
        }
    }
}
