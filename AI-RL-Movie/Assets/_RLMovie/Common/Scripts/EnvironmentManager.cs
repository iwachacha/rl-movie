using System;
using System.Collections.Generic;
using UnityEngine;

namespace RLMovie.Common
{
    public enum EnvironmentAreaShape
    {
        Circle,
        Rectangle
    }

    /// <summary>
    /// RL環境の管理を行うマネージャー。
    /// 環境のランダム化、並列配置、リセットを管理。
    /// Circle/Rectangle の両形状に対応し、マルチスポーンとリセットイベントを提供。
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        [Header("=== Environment Settings ===")]
        [Tooltip("環境エリアの形状")]
        [SerializeField] private EnvironmentAreaShape areaShape = EnvironmentAreaShape.Circle;

        [Tooltip("環境エリアの半径（Circle時）")]
        [SerializeField] private float areaRadius = 5f;

        [Tooltip("矩形エリアのサイズ（Rectangle時、幅×奥行き）")]
        [SerializeField] private Vector2 rectangleSize = new Vector2(10f, 10f);

        [Tooltip("環境の境界（エージェントが落ちた判定用）")]
        [SerializeField] private float fallThreshold = -5f;

        [Header("=== Randomization ===")]
        [Tooltip("エピソード毎に位置をランダム化するか")]
        [SerializeField] private bool randomizePositions = true;

        [Tooltip("ランダム化の強度 (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float randomizationStrength = 0.5f;

        /// <summary>環境リセット時に発火するイベント。Agent の OnEpisodeReset 等から呼び出す。</summary>
        public event Action EnvironmentReset;

        /// <summary>環境リセットを通知する。</summary>
        public void NotifyReset()
        {
            EnvironmentReset?.Invoke();
        }

        /// <summary>環境エリア内のランダムな位置を返す</summary>
        public Vector3 GetRandomPosition(float yOffset = 0f)
        {
            if (!randomizePositions) return transform.position + Vector3.up * yOffset;

            return areaShape == EnvironmentAreaShape.Rectangle
                ? GetRandomRectPosition(yOffset)
                : GetRandomCirclePosition(yOffset);
        }

        /// <summary>環境エリアの端にランダムな位置を返す（ゴール配置用）</summary>
        public Vector3 GetRandomEdgePosition(float yOffset = 0f)
        {
            if (!randomizePositions)
            {
                float fallbackRadius = areaShape == EnvironmentAreaShape.Rectangle
                    ? Mathf.Min(rectangleSize.x, rectangleSize.y) * 0.5f * randomizationStrength
                    : areaRadius * randomizationStrength;
                return transform.position + new Vector3(fallbackRadius, yOffset, 0f);
            }

            return areaShape == EnvironmentAreaShape.Rectangle
                ? GetRandomRectEdgePosition(yOffset)
                : GetRandomCircleEdgePosition(yOffset);
        }

        /// <summary>重ならないランダム位置を複数取得する</summary>
        public List<Vector3> GetRandomPositions(int count, float yOffset = 0f, float minSeparation = 1f, int maxAttempts = 64)
        {
            var positions = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                Vector3 candidate = Vector3.zero;
                bool placed = false;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    candidate = GetRandomPosition(yOffset);
                    if (IsSeparated(candidate, positions, minSeparation))
                    {
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    candidate = GetRandomPosition(yOffset);
                }

                positions.Add(candidate);
            }

            return positions;
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
            if (areaShape == EnvironmentAreaShape.Rectangle)
            {
                Vector2 halfSize = rectangleSize * 0.5f;
                return Mathf.Abs(localPos.x) <= halfSize.x && Mathf.Abs(localPos.z) <= halfSize.y;
            }

            return localPos.magnitude <= areaRadius;
        }

        /// <summary>環境エリアの半径（Circle時）</summary>
        public float AreaRadius => areaRadius;

        /// <summary>環境エリアの矩形サイズ（Rectangle時）</summary>
        public Vector2 RectangleSize => rectangleSize;

        /// <summary>環境エリアの形状</summary>
        public EnvironmentAreaShape AreaShape => areaShape;

        /// <summary>ランダム化の強度</summary>
        public float RandomizationStrength
        {
            get => randomizationStrength;
            set => randomizationStrength = Mathf.Clamp01(value);
        }

        private Vector3 GetRandomCirclePosition(float yOffset)
        {
            float radius = areaRadius * randomizationStrength;
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
            return transform.position + new Vector3(randomCircle.x, yOffset, randomCircle.y);
        }

        private Vector3 GetRandomRectPosition(float yOffset)
        {
            Vector2 halfSize = rectangleSize * 0.5f * randomizationStrength;
            float x = UnityEngine.Random.Range(-halfSize.x, halfSize.x);
            float z = UnityEngine.Random.Range(-halfSize.y, halfSize.y);
            return transform.position + new Vector3(x, yOffset, z);
        }

        private Vector3 GetRandomCircleEdgePosition(float yOffset)
        {
            float radius = areaRadius * randomizationStrength;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, yOffset, Mathf.Sin(angle) * radius);
            return transform.position + pos;
        }

        private Vector3 GetRandomRectEdgePosition(float yOffset)
        {
            Vector2 halfSize = rectangleSize * 0.5f * randomizationStrength;
            int edge = UnityEngine.Random.Range(0, 4);
            float x, z;
            switch (edge)
            {
                case 0:
                    x = -halfSize.x;
                    z = UnityEngine.Random.Range(-halfSize.y, halfSize.y);
                    break;
                case 1:
                    x = halfSize.x;
                    z = UnityEngine.Random.Range(-halfSize.y, halfSize.y);
                    break;
                case 2:
                    x = UnityEngine.Random.Range(-halfSize.x, halfSize.x);
                    z = halfSize.y;
                    break;
                default:
                    x = UnityEngine.Random.Range(-halfSize.x, halfSize.x);
                    z = -halfSize.y;
                    break;
            }

            return transform.position + new Vector3(x, yOffset, z);
        }

        private static bool IsSeparated(Vector3 candidate, List<Vector3> existing, float minSeparation)
        {
            float sqrMin = minSeparation * minSeparation;
            for (int i = 0; i < existing.Count; i++)
            {
                if ((candidate - existing[i]).sqrMagnitude < sqrMin)
                    return false;
            }

            return true;
        }

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (areaShape == EnvironmentAreaShape.Rectangle)
            {
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
                Gizmos.DrawCube(transform.position, new Vector3(rectangleSize.x, 0.1f, rectangleSize.y));
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
                Gizmos.DrawWireCube(transform.position, new Vector3(rectangleSize.x, 0.1f, rectangleSize.y));
            }
            else
            {
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
                Gizmos.DrawSphere(transform.position, areaRadius);
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, areaRadius);
            }

            float gizmoWidth = areaShape == EnvironmentAreaShape.Rectangle ? rectangleSize.x : areaRadius * 2;
            float gizmoDepth = areaShape == EnvironmentAreaShape.Rectangle ? rectangleSize.y : areaRadius * 2;
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawCube(
                transform.position + Vector3.up * fallThreshold,
                new Vector3(gizmoWidth, 0.1f, gizmoDepth));
        }

        #endregion
    }
}
