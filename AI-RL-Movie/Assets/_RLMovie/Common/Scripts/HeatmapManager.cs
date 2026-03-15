using UnityEngine;
using System.Collections.Generic;

namespace RLMovie.Common
{
    /// <summary>
    /// エージェントの移動経路をヒートマップとして可視化するマネージャ。
    /// 現在は簡易的なパーティクルまたはトレイルによる可視化を想定。
    /// </summary>
    public class HeatmapManager : MonoBehaviour
    {
        public static HeatmapManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private Color heatColor = new Color(1, 0.5f, 0, 0.1f);
        [SerializeField] private float pointLifetime = 10f;
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private float pointSize = 0.5f;

        private ParticleSystem _particleSystem;
        private float _timer;
        private List<BaseRLAgent> _agents = new List<BaseRLAgent>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null)
            {
                // 動的にパーティクルシステムを作成
                _particleSystem = gameObject.AddComponent<ParticleSystem>();
                var main = _particleSystem.main;
                main.startColor = heatColor;
                main.startSize = pointSize;
                main.startLifetime = pointLifetime;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 50000;
                
                var emission = _particleSystem.emission;
                emission.enabled = false;

                var shape = _particleSystem.shape;
                shape.enabled = false;

                var renderer = GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                // 注意: 適切なマテリアルをアサインしないと紫になりますが、EditorScript等で補完を想定
            }
        }

        public void RegisterAgent(BaseRLAgent agent)
        {
            if (!_agents.Contains(agent)) _agents.Add(agent);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= updateInterval)
            {
                _timer = 0f;
                RecordPositions();
            }
        }

        private void RecordPositions()
        {
            if (_particleSystem == null) return;

            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                // TODO: BaseRLAgent 側に enableHeatmap フラグがあるが、ここでは全登録エージェントを記録
                var emitParams = new ParticleSystem.EmitParams();
                emitParams.position = agent.transform.position;
                _particleSystem.Emit(emitParams, 1);
            }
        }
    }
}
