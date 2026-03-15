using UnityEngine;

namespace RLMovie.Environments.ReactorCoreDelivery
{
    /// <summary>
    /// Keeps the humanoid visual and reaction animation aligned with the physics agent.
    /// </summary>
    public sealed class ReactorCoreDeliveryVisualController : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int ShockId = Animator.StringToHash("Shock");

        [SerializeField] private Animator animator;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private float runSpeedForFullBlend = 4.8f;
        [SerializeField] private float facingDeadZone = 0.05f;
        [SerializeField] private float yawOffset = 0f;

        private Vector3 _planarVelocity;

        private bool HasAnimatorController => animator != null && animator.runtimeAnimatorController != null;

        private void Update()
        {
            UpdateFacing();
        }

        public void ResetState()
        {
            _planarVelocity = Vector3.zero;

            if (HasAnimatorController)
            {
                animator.ResetTrigger(ShockId);
                animator.SetFloat(SpeedId, 0f);
            }
        }

        public void SetMotion(Vector3 worldVelocity)
        {
            _planarVelocity = new Vector3(worldVelocity.x, 0f, worldVelocity.z);

            if (HasAnimatorController)
            {
                float normalizedSpeed = Mathf.Clamp01(_planarVelocity.magnitude / Mathf.Max(0.1f, runSpeedForFullBlend));
                animator.SetFloat(SpeedId, normalizedSpeed);
            }
        }

        public void TriggerShock()
        {
            if (HasAnimatorController)
            {
                animator.SetTrigger(ShockId);
            }
        }

        public void TriggerSuccess()
        {
            if (HasAnimatorController)
            {
                animator.SetFloat(SpeedId, 0f);
            }
        }

        private void UpdateFacing()
        {
            if (modelRoot == null || _planarVelocity.sqrMagnitude < facingDeadZone * facingDeadZone)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(_planarVelocity.normalized, Vector3.up)
                * Quaternion.Euler(0f, yawOffset, 0f);

            modelRoot.rotation = Quaternion.Slerp(
                modelRoot.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }
    }
}
