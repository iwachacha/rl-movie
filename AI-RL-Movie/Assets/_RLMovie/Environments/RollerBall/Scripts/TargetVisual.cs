using UnityEngine;

namespace RLMovie.Environments.RollerBall
{
    /// <summary>
    /// ターゲットのビジュアル演出。
    /// パルスアニメーション、到達時のパーティクルエフェクトなど。
    /// </summary>
    public class TargetVisual : MonoBehaviour
    {
        [Header("=== Pulse Animation ===")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMin = 0.8f;
        [SerializeField] private float pulseMax = 1.2f;

        [Header("=== Rotation ===")]
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Header("=== Glow ===")]
        [SerializeField] private Color glowColor = new Color(0.2f, 1f, 0.4f, 1f);
        [SerializeField] private float glowIntensity = 2f;

        private Renderer _renderer;
        private Vector3 _originalScale;
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _originalScale = transform.localScale;
            _propBlock = new MaterialPropertyBlock();

            // エミッション色の設定
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_EmissionColor", glowColor * glowIntensity);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        private void Update()
        {
            // パルスアニメーション
            float pulse = Mathf.Lerp(pulseMin, pulseMax,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            transform.localScale = _originalScale * pulse;

            // 回転アニメーション
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);

            // グロー強度のアニメーション
            if (_renderer != null)
            {
                float glow = Mathf.Lerp(glowIntensity * 0.5f, glowIntensity,
                    (Mathf.Sin(Time.time * pulseSpeed * 1.5f) + 1f) * 0.5f);
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_EmissionColor", glowColor * glow);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        /// <summary>ターゲット到達時のエフェクト</summary>
        public void OnReached()
        {
            // パーティクルシステムが子にあれば再生
            var particles = GetComponentInChildren<ParticleSystem>();
            if (particles != null)
            {
                particles.Play();
            }
        }
    }
}
