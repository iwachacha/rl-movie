using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// RL環境の管理を行うマネージャー。
    /// 環境のランダム化、並列配置、リセットを管理。
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        [Header("=== Environment Settings ===")]
        [Tooltip("環境エリアの半径（ランダム配置範囲）")]
        [SerializeField] private float areaRadius = 5f;

        [Tooltip("地面の高さ")]
        [SerializeField] private float groundLevel = 0f;

        [Tooltip("環境の境界（エージェントが落ちた判定用）")]
        [SerializeField] private float fallThreshold = -5f;

        [Header("=== Randomization ===")]
        [Tooltip("エピソード毎に位置をランダム化するか")]
        [SerializeField] private bool randomizePositions = true;

        [Tooltip("ランダム化の強度 (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float randomizationStrength = 0.5f;

        /// <summary>環境エリア内のランダムな位置を返す</summary>
        public Vector3 GetRandomPosition(float yOffset = 0f)
        {
            if (!randomizePositions) return transform.position + Vector3.up * yOffset;

            float radius = areaRadius * randomizationStrength;
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            return transform.position + new Vector3(randomCircle.x, yOffset, randomCircle.y);
        }

        /// <summary>環境エリアの端にランダムな位置を返す（ゴール配置用）</summary>
        public Vector3 GetRandomEdgePosition(float yOffset = 0f)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = areaRadius * randomizationStrength;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, yOffset, Mathf.Sin(angle) * radius);
            return transform.position + pos;
        }

        /// <summary>オブジェクトが境界外に落ちたか判定</summary>
        public bool HasFallen(Transform obj)
        {
            return obj.position.y < fallThreshold;
        }

        /// <summary>オブジェクトが環境エリア内にいるか判定</summary>
        public bool IsInsideArea(Vector3 position)
        {
            Vector3 localPos = position - transform.position;
            return localPos.magnitude <= areaRadius;
        }

        /// <summary>環境エリアの半径</summary>
        public float AreaRadius => areaRadius;

        /// <summary>ランダム化の強度</summary>
        public float RandomizationStrength
        {
            get => randomizationStrength;
            set => randomizationStrength = Mathf.Clamp01(value);
        }

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // 環境エリアの可視化
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
            Gizmos.DrawSphere(transform.position, areaRadius);

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, areaRadius);

            // 落下閾値の可視化
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawCube(
                transform.position + Vector3.up * fallThreshold,
                new Vector3(areaRadius * 2, 0.1f, areaRadius * 2)
            );
        }

        #endregion
    }
}
