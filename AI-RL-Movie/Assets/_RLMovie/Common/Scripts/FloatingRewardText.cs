using UnityEngine;
using TMPro;

namespace RLMovie.Common
{
    /// <summary>
    /// 報酬やペナルティを頭上に表示するクラス。
    /// 一定時間で上昇しながらフェードアウトし、自動で削除されます。
    /// </summary>
    public class FloatingRewardText : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private float duration = 1.0f;
        [SerializeField] private float moveSpeed = 1.0f;
        [SerializeField] private Color positiveColor = Color.green;
        [SerializeField] private Color negativeColor = Color.red;

        private float _timer;
        private Color _initialColor;

        public void Setup(float amount)
        {
            if (textMesh == null) textMesh = GetComponentInChildren<TextMeshPro>();
            
            string sign = amount > 0 ? "+" : "";
            textMesh.text = $"{sign}{amount:F2}";
            textMesh.color = amount >= 0 ? positiveColor : negativeColor;
            _initialColor = textMesh.color;
            _timer = duration;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                Destroy(gameObject);
                return;
            }

            // 上昇
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            // フェードアウト
            float alpha = _timer / duration;
            Color c = textMesh.color;
            c.a = alpha;
            textMesh.color = c;
        }
    }
}
