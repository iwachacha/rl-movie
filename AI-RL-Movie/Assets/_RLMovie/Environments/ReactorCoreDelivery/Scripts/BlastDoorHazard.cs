using UnityEngine;

namespace RLMovie.Environments.ReactorCoreDelivery
{
    public sealed class BlastDoorHazard : MonoBehaviour
    {
        [SerializeField] private Transform leftPanel;
        [SerializeField] private Transform rightPanel;
        [SerializeField] private Renderer[] indicatorRenderers = new Renderer[0];
        [SerializeField] private float cycleDuration = 3.2f;
        [SerializeField] private float openDuration = 1.2f;
        [SerializeField] private float travelDistance = 1.35f;
        [SerializeField] private float passableThreshold = 0.7f;

        private float _phaseOffset01;
        private float _openScale = 1f;
        private Vector3 _leftClosedPosition;
        private Vector3 _rightClosedPosition;

        public Vector3 HazardCenter => transform.position;

        public float Phase01 => Mathf.Repeat((Time.time / Mathf.Max(0.1f, cycleDuration)) + _phaseOffset01, 1f);

        public float OpenAmount01
        {
            get
            {
                float holdOpenDuration = Mathf.Clamp(openDuration * _openScale, 0.15f, cycleDuration - 0.2f);
                float movingDuration = Mathf.Max(0.1f, (cycleDuration - holdOpenDuration) * 0.5f);
                float t = Phase01 * cycleDuration;

                if (t < movingDuration)
                {
                    return Mathf.SmoothStep(0f, 1f, t / movingDuration);
                }

                if (t < movingDuration + holdOpenDuration)
                {
                    return 1f;
                }

                return Mathf.SmoothStep(1f, 0f, (t - movingDuration - holdOpenDuration) / movingDuration);
            }
        }

        public bool IsPassable => OpenAmount01 >= passableThreshold;

        public void ResetCycle(float phaseOffset01, float openScale)
        {
            _phaseOffset01 = phaseOffset01;
            _openScale = Mathf.Max(0.7f, openScale);

            if (leftPanel != null)
            {
                _leftClosedPosition = leftPanel.localPosition;
            }

            if (rightPanel != null)
            {
                _rightClosedPosition = rightPanel.localPosition;
            }

            ApplyPose();
        }

        private void Update()
        {
            ApplyPose();
        }

        private void ApplyPose()
        {
            if (leftPanel != null)
            {
                leftPanel.localPosition = _leftClosedPosition + Vector3.left * (travelDistance * OpenAmount01);
            }

            if (rightPanel != null)
            {
                rightPanel.localPosition = _rightClosedPosition + Vector3.right * (travelDistance * OpenAmount01);
            }

            Color baseColor = IsPassable
                ? new Color(0.18f, 0.62f, 0.30f)
                : new Color(0.55f, 0.16f, 0.16f);

            Color emissionColor = IsPassable
                ? new Color(0.18f, 0.85f, 0.28f)
                : new Color(1.10f, 0.20f, 0.20f);

            if (indicatorRenderers == null)
            {
                return;
            }

            foreach (Renderer renderer in indicatorRenderers)
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
