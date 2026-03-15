using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using RLMovie.Common;

namespace RLMovie.Environments.GluttonousMonitor
{
    /// <summary>
    /// 巨大な「暴食モニター」にプロップを捧げるエージェント。
    /// 物理ベースの「押し」でアイテムを運びます。
    /// </summary>
    public class GluttonousMonitorAgent : BaseRLAgent
    {
        [Header("Monitor Settings")]
        [SerializeField] private Transform monitorMouth;
        [SerializeField] private float eatDistance = 2.0f;
        [SerializeField] private LayerMask itemLayer;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float turnSpeed = 200f;

        private Rigidbody _rb;

        protected override void OnAgentInitialize()
        {
            _rb = GetComponent<Rigidbody>();
        }

        protected override void OnEpisodeReset()
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        protected override void CollectAgentObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.localPosition);
            sensor.AddObservation(transform.forward);
            
            if (monitorMouth != null)
            {
                Vector3 toMouth = monitorMouth.position - transform.position;
                sensor.AddObservation(toMouth.normalized);
                sensor.AddObservation(toMouth.magnitude);
            }
            else
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0f);
            }

            // 周囲のアイテムの検知（簡易的なRaycast等も検討可能だが、ここではなし）
        }

        protected override void ExecuteActions(ActionBuffers actions)
        {
            float move = actions.ContinuousActions[0]; // 前進
            float turn = actions.ContinuousActions[1]; // 回転

            // 物理ベースの移動
            Vector3 velocity = transform.forward * move * moveSpeed;
            velocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = velocity;

            transform.Rotate(Vector3.up * turn * turnSpeed * Time.deltaTime);

            // アイテム収集判定は、アイテム側のトリガーまたはここでチェック
            // 今回は簡略化のため、一定範囲内のアイテムを「食べた」ことにする
            CheckForItems();

            // 場外・タイムアウト
            if (transform.localPosition.y < -5f || StepCount > 2000)
            {
                Fail(-1.0f);
            }
        }

        private void CheckForItems()
        {
            if (monitorMouth == null) return;

            // プロップ（アイテム）がモニターの口に近いかチェック
            // 本来はアイテム側にスクリプトを持たせるのが良いが、ここではエージェントが「運んだ」ことを検知
            var colliders = Physics.OverlapSphere(monitorMouth.position, eatDistance, itemLayer);
            foreach (var col in colliders)
            {
                // アイテムを消して報酬
                // 注意: 実際の実装ではリセット時に復活させる必要がある
                if (col.CompareTag("Goal")) // 仮のタグ
                {
                    AddTrackedReward(1.0f);
                    Success(1.0f); // 1つ食べたら成功とするか、複数必要かは設計次第
                    // col.gameObject.SetActive(false); // 演出
                }
            }
        }

        protected override void ProvideHeuristicInput(in ActionBuffers actionsOut)
        {
            var continuousActions = actionsOut.ContinuousActions;
            continuousActions[0] = Input.GetAxis("Vertical");
            continuousActions[1] = Input.GetAxis("Horizontal");
        }
    }
}
