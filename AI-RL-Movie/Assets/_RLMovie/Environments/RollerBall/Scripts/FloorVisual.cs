using UnityEngine;

namespace RLMovie.Environments.RollerBall
{
    /// <summary>
    /// 床のビジュアル演出。グリッドパターンやエージェントの位置に応じた色変化。
    /// </summary>
    public class FloorVisual : MonoBehaviour
    {
        [Header("=== Floor Visual Settings ===")]
        [SerializeField] private Color defaultColor = new Color(0.15f, 0.15f, 0.2f);
        [SerializeField] private Color successColor = new Color(0.1f, 0.6f, 0.3f);
        [SerializeField] private Color failColor = new Color(0.6f, 0.1f, 0.1f);
        [SerializeField] private float flashDuration = 0.5f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Color _targetColor;
        private float _flashTimer;
        private bool _isFlashing;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            _targetColor = defaultColor;
            ApplyColor(defaultColor);
        }

        private void Update()
        {
            if (_isFlashing)
            {
                _flashTimer -= Time.deltaTime;
                float t = _flashTimer / flashDuration;

                Color currentColor = Color.Lerp(defaultColor, _targetColor, t);
                ApplyColor(currentColor);

                if (_flashTimer <= 0f)
                {
                    _isFlashing = false;
                    ApplyColor(defaultColor);
                }
            }
        }

        /// <summary>成功時のフラッシュ</summary>
        public void FlashSuccess()
        {
            _targetColor = successColor;
            _flashTimer = flashDuration;
            _isFlashing = true;
        }

        /// <summary>失敗時のフラッシュ</summary>
        public void FlashFail()
        {
            _targetColor = failColor;
            _flashTimer = flashDuration;
            _isFlashing = true;
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", color);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
