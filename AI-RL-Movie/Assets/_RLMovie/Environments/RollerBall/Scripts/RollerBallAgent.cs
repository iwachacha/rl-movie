using RLMovie.Common;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RLMovie.Environments.RollerBall
{
    /// <summary>
    /// 🎱 ローラーボール
    /// ボール（エージェント）が床上を転がってターゲットに到達するシンプルなRL環境。
    /// 強化学習の基本を視覚的に楽しめる入門シナリオ。
    /// </summary>
    public class RollerBallAgent : BaseRLAgent
    {
        [Header("=== Roller Ball Settings ===")]
        [Tooltip("ターゲット（ゴール）のTransform")]
        [SerializeField] private Transform target;

        [Tooltip("環境マネージャー")]
        [SerializeField] private EnvironmentManager envManager;

        [Tooltip("移動力の大きさ")]
        [SerializeField] private float moveForce = 1.0f;

        [Tooltip("ゴール到達とみなす距離")]
        [SerializeField] private float goalDistance = 1.42f;

        private Rigidbody _rb;

        protected override void OnAgentInitialize()
        {
            _rb = GetComponent<Rigidbody>();
        }

        protected override void OnEpisodeReset()
        {
            // エージェントの速度をリセット
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            // ランダムな位置に配置
            if (envManager != null)
            {
                transform.localPosition = envManager.GetRandomPosition(0.5f);
                target.localPosition = envManager.GetRandomEdgePosition(0.5f);
            }
            else
            {
                // envManager がない場合のフォールバック
                transform.localPosition = new Vector3(
                    Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
                target.localPosition = new Vector3(
                    Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
            }
        }

        protected override void CollectAgentObservations(VectorSensor sensor)
        {
            // エージェントの位置 (3)
            sensor.AddObservation(transform.localPosition);

            // ターゲットの位置 (3)
            sensor.AddObservation(target.localPosition);

            // エージェントの速度 (3)
            sensor.AddObservation(_rb.linearVelocity);

            // ターゲットへの方向 (3) - 正規化
            Vector3 dirToTarget = (target.localPosition - transform.localPosition).normalized;
            sensor.AddObservation(dirToTarget);

            // ターゲットまでの距離 (1)
            float distToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
            sensor.AddObservation(distToTarget);

            // 合計: 13 observations
        }

        protected override void ExecuteActions(ActionBuffers actions)
        {
            // 連続アクション: X方向とZ方向の力
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];

            Vector3 force = new Vector3(moveX, 0f, moveZ) * moveForce;
            _rb.AddForce(force, ForceMode.VelocityChange);

            // --- 報酬設計 ---

            // ターゲットまでの距離
            float distToTarget = Vector3.Distance(transform.localPosition, target.localPosition);

            // ゴール到達判定
            if (distToTarget < goalDistance)
            {
                Success(1.0f);
                return;
            }

            // 落下判定
            if (envManager != null && envManager.HasFallen(transform))
            {
                Fail(-1.0f);
                return;
            }
            else if (transform.localPosition.y < -1f)
            {
                Fail(-1.0f);
                return;
            }

            // 距離ベースの報酬（近づくほど正の報酬、遠ざかるほど負の報酬）
            float distanceReward = -distToTarget * 0.001f;
            AddTrackedReward(distanceReward);

            // ステップペナルティ（早く到達するインセンティブ）
            AddTrackedReward(-0.0005f);
        }

        protected override void ProvideHeuristicInput(in ActionBuffers actionsOut)
        {
            var continuousActions = actionsOut.ContinuousActions;
            continuousActions[0] = Input.GetAxis("Horizontal");
            continuousActions[1] = Input.GetAxis("Vertical");
        }
    }
}
