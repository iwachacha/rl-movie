using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering;

namespace RLMovie.Common
{
    /// <summary>
    /// 全RLエージェントの基底クラス。
    /// 新しいシナリオを作る際はこのクラスを継承してください。
    /// </summary>
    public abstract class BaseRLAgent : Agent
    {
        [Header("=== Base RL Agent Settings ===")]
        [Tooltip("デバッグ用に報酬情報を表示するか")]
        [SerializeField] protected bool showDebugInfo = true;

        [Tooltip("エピソード開始時にエージェントを自動配置するか")]
        [SerializeField] protected bool autoResetPosition = true;

        [Tooltip("エージェントの初期位置")]
        [SerializeField] protected Vector3 startPosition = Vector3.zero;

        [Tooltip("エージェントの初期回転")]
        [SerializeField] protected Quaternion startRotation = Quaternion.identity;

        // --- エピソード統計 ---
        protected int totalEpisodes = 0;
        protected int successCount = 0;
        protected int failCount = 0;
        protected float episodeReward = 0f;
        protected float lastCompletedEpisodeReward = 0f;

        // --- ビジュアルフィードバック ---
        private Renderer _agentRenderer;
        private Color _originalColor;
        private float _flashTimer = 0f;
        private Color _flashColor;
        private bool _isFlashing = false;
        private bool _visualDebugEnabled = true;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _visualDebugEnabled = !IsHeadlessRuntime();

            if (!_visualDebugEnabled)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[{name}] Visual debug is disabled for headless training runtime.");
                }

                showDebugInfo = false;
                return;
            }

            _agentRenderer = GetComponentInChildren<Renderer>();
            if (_agentRenderer != null)
            {
                _originalColor = _agentRenderer.material.color;
            }
        }

        protected virtual void Update()
        {
            if (!_visualDebugEnabled)
            {
                return;
            }

            if (_isFlashing)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f)
                {
                    _isFlashing = false;
                    if (_agentRenderer != null)
                        _agentRenderer.material.color = _originalColor;
                }
            }
        }

        #endregion

        #region ML-Agents Overrides

        public override void Initialize()
        {
            base.Initialize();
            OnAgentInitialize();
        }

        public override void OnEpisodeBegin()
        {
            totalEpisodes++;
            episodeReward = 0f;

            if (autoResetPosition)
            {
                transform.localPosition = startPosition;
                transform.localRotation = startRotation;

                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            OnEpisodeReset();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            CollectAgentObservations(sensor);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            ExecuteActions(actions);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ProvideHeuristicInput(actionsOut);
        }

        #endregion

        #region Abstract Methods (implement in child classes)

        /// <summary>エージェント初期化時の処理</summary>
        protected abstract void OnAgentInitialize();

        /// <summary>エピソードリセット時の処理</summary>
        protected abstract void OnEpisodeReset();

        /// <summary>観測データの収集</summary>
        protected abstract void CollectAgentObservations(VectorSensor sensor);

        /// <summary>アクション実行</summary>
        protected abstract void ExecuteActions(ActionBuffers actions);

        /// <summary>Heuristic（手動操作）モードの入力</summary>
        protected abstract void ProvideHeuristicInput(in ActionBuffers actionsOut);

        #endregion

        #region Helper Methods

        /// <summary>成功報酬を与えてエピソード終了</summary>
        protected void Success(float reward = 1.0f)
        {
            successCount++;
            AddReward(reward);
            episodeReward += reward;
            lastCompletedEpisodeReward = episodeReward;

            FlashColor(Color.green, 0.3f);

            if (showDebugInfo)
                Debug.Log($"[{name}] ✅ SUCCESS! Episode {totalEpisodes} | Reward: {episodeReward:F2} | Rate: {SuccessRate:P1}");

            EndEpisode();
        }

        /// <summary>失敗ペナルティを与えてエピソード終了</summary>
        protected void Fail(float penalty = -1.0f)
        {
            failCount++;
            AddReward(penalty);
            episodeReward += penalty;
            lastCompletedEpisodeReward = episodeReward;

            FlashColor(Color.red, 0.3f);

            if (showDebugInfo)
                Debug.Log($"[{name}] ❌ FAIL! Episode {totalEpisodes} | Reward: {episodeReward:F2} | Rate: {SuccessRate:P1}");

            EndEpisode();
        }

        /// <summary>報酬を追加（追跡付き）</summary>
        protected void AddTrackedReward(float reward)
        {
            AddReward(reward);
            episodeReward += reward;
        }

        /// <summary>エージェントの色を一瞬変える（ビジュアルフィードバック）</summary>
        protected void FlashColor(Color color, float duration = 0.3f)
        {
            if (!_visualDebugEnabled || _agentRenderer == null) return;
            _agentRenderer.material.color = color;
            _flashColor = color;
            _flashTimer = duration;
            _isFlashing = true;
        }

        /// <summary>成功率</summary>
        public float SuccessRate => CompletedEpisodes > 0 ? (float)successCount / CompletedEpisodes : 0f;

        /// <summary>現在のエピソード報酬</summary>
        public float CurrentEpisodeReward => episodeReward;

        /// <summary>直近で完了したエピソード報酬</summary>
        public float LastCompletedEpisodeReward => lastCompletedEpisodeReward;

        /// <summary>完了済みエピソード数</summary>
        public new int CompletedEpisodes => successCount + failCount;

        /// <summary>総エピソード数（完了済み）</summary>
        public int TotalEpisodes => CompletedEpisodes;

        /// <summary>現在進行中のエピソード番号</summary>
        public int CurrentEpisodeNumber => totalEpisodes;

        #endregion

        #region Debug GUI

#if !UNITY_SERVER
        private void OnGUI()
        {
            if (!showDebugInfo || !_visualDebugEnabled) return;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f)
                : Vector3.zero;

            if (screenPos.z > 0)
            {
                float x = screenPos.x - 80;
                float y = Screen.height - screenPos.y - 40;

                GUI.Label(new Rect(x, y, 200, 20),
                    $"Ep: {CurrentEpisodeNumber} | R: {episodeReward:F2}", style);
                GUI.Label(new Rect(x, y + 20, 200, 20),
                    $"Success: {SuccessRate:P1}", style);
            }
        }
#endif

        private static bool IsHeadlessRuntime()
        {
            return Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }

        #endregion
    }
}
