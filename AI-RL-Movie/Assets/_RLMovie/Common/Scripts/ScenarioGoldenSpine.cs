using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RLMovie.Common
{
    /// <summary>
    /// Shared role registry for V2 scenario starters.
    /// Theme-specific gameplay stays outside this component; it only exposes common backbone references.
    /// </summary>
    public sealed class ScenarioGoldenSpine : MonoBehaviour
    {
        [Serializable]
        public struct SceneRoleBinding
        {
            public string role;
            public Transform target;
        }

        [Serializable]
        public struct AgentRoleBinding
        {
            public string role;
            public string team;
            public bool primary;
            public BaseRLAgent agent;
        }

        [Serializable]
        public struct TeamRoleBinding
        {
            public string team;
            public BaseRLAgent primaryAgent;
            public BaseRLAgent[] agents;
        }

        [Serializable]
        public struct CameraRoleBinding
        {
            public string role;
            public Transform anchor;
        }

        [Header("=== Shared Backbone References ===")]
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private EnvironmentManager environmentManager;
        [SerializeField] private TrainingVisualizer trainingVisualizer;
        [SerializeField] private RecordingHelper recordingHelper;
        [SerializeField] private ScenarioBroadcastOverlay scenarioBroadcastOverlay;
        [SerializeField] private ScenarioHighlightTracker scenarioHighlightTracker;

        [Header("=== Role Bindings ===")]
        [SerializeField] private SceneRoleBinding[] sceneRoles = Array.Empty<SceneRoleBinding>();
        [SerializeField] private AgentRoleBinding[] agentRoles = Array.Empty<AgentRoleBinding>();
        [SerializeField] private TeamRoleBinding[] teamRoles = Array.Empty<TeamRoleBinding>();
        [SerializeField] private CameraRoleBinding[] cameraRoles = Array.Empty<CameraRoleBinding>();

        private static readonly string[] DefaultRecordingCameraRoles =
        {
            "explain",
            "wide_a",
            "wide_b",
            "follow_optional",
            "comparison_optional"
        };

        private void Reset()
        {
            environmentRoot = transform;
        }

        public Transform EnvironmentRoot => environmentRoot;

        public EnvironmentManager EnvironmentManager => environmentManager;

        public TrainingVisualizer TrainingVisualizer => trainingVisualizer;

        public RecordingHelper RecordingHelper => recordingHelper;

        public ScenarioBroadcastOverlay ScenarioBroadcastOverlay => scenarioBroadcastOverlay;

        public ScenarioHighlightTracker ScenarioHighlightTracker => scenarioHighlightTracker;

        public SceneRoleBinding[] SceneRoles => sceneRoles;

        public AgentRoleBinding[] AgentRoles => agentRoles;

        public TeamRoleBinding[] TeamRoles => teamRoles;

        public CameraRoleBinding[] CameraRoles => cameraRoles;

        public BaseRLAgent PrimaryAgent => GetPrimaryAgent();

        public Transform PrimaryGoal => GetSceneRole("primary_target");

        public Transform DefaultCameraView => GetCameraRole("default_camera");

        public Transform[] RecordingCameraViews
        {
            get
            {
                var result = new System.Collections.Generic.List<Transform>();
                for (int i = 0; i < DefaultRecordingCameraRoles.Length; i++)
                {
                    Transform anchor = GetCameraRole(DefaultRecordingCameraRoles[i]);
                    if (anchor != null)
                    {
                        result.Add(anchor);
                    }
                }

                return result.ToArray();
            }
        }

        public void Configure(
            Transform root,
            EnvironmentManager manager,
            TrainingVisualizer visualizer,
            RecordingHelper helper,
            ScenarioBroadcastOverlay overlay,
            ScenarioHighlightTracker tracker,
            SceneRoleBinding[] sceneRoleBindings,
            AgentRoleBinding[] agentRoleBindings,
            TeamRoleBinding[] teamRoleBindings,
            CameraRoleBinding[] cameraRoleBindings)
        {
            environmentRoot = root;
            environmentManager = manager;
            trainingVisualizer = visualizer;
            recordingHelper = helper;
            scenarioBroadcastOverlay = overlay;
            scenarioHighlightTracker = tracker;
            sceneRoles = sceneRoleBindings ?? Array.Empty<SceneRoleBinding>();
            agentRoles = agentRoleBindings ?? Array.Empty<AgentRoleBinding>();
            teamRoles = teamRoleBindings ?? Array.Empty<TeamRoleBinding>();
            cameraRoles = cameraRoleBindings ?? Array.Empty<CameraRoleBinding>();
        }

        public bool TryGetSceneRole(string role, out Transform target)
        {
            for (int i = 0; i < sceneRoles.Length; i++)
            {
                if (!string.Equals(sceneRoles[i].role, role, StringComparison.Ordinal))
                {
                    continue;
                }

                target = sceneRoles[i].target;
                return target != null;
            }

            target = null;
            return false;
        }

        public bool TryGetAgentRole(string role, out BaseRLAgent agent)
        {
            for (int i = 0; i < agentRoles.Length; i++)
            {
                if (!string.Equals(agentRoles[i].role, role, StringComparison.Ordinal))
                {
                    continue;
                }

                agent = agentRoles[i].agent;
                return agent != null;
            }

            agent = null;
            return false;
        }

        public IReadOnlyList<BaseRLAgent> GetAgentsByTeam(string team)
        {
            if (string.IsNullOrWhiteSpace(team))
            {
                return Array.Empty<BaseRLAgent>();
            }

            for (int i = 0; i < teamRoles.Length; i++)
            {
                if (!string.Equals(teamRoles[i].team, team, StringComparison.Ordinal))
                {
                    continue;
                }

                return teamRoles[i].agents?.Where(agent => agent != null).ToArray() ?? Array.Empty<BaseRLAgent>();
            }

            return agentRoles
                .Where(binding => string.Equals(binding.team, team, StringComparison.Ordinal) && binding.agent != null)
                .Select(binding => binding.agent)
                .ToArray();
        }

        public bool TryGetPrimaryAgentForTeam(string team, out BaseRLAgent agent)
        {
            if (!string.IsNullOrWhiteSpace(team))
            {
                for (int i = 0; i < teamRoles.Length; i++)
                {
                    if (!string.Equals(teamRoles[i].team, team, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    agent = teamRoles[i].primaryAgent != null
                        ? teamRoles[i].primaryAgent
                        : teamRoles[i].agents?.FirstOrDefault(candidate => candidate != null);
                    return agent != null;
                }

                AgentRoleBinding primaryBinding = agentRoles.FirstOrDefault(binding =>
                    string.Equals(binding.team, team, StringComparison.Ordinal)
                    && binding.primary
                    && binding.agent != null);
                if (primaryBinding.agent != null)
                {
                    agent = primaryBinding.agent;
                    return true;
                }

                agent = agentRoles.FirstOrDefault(binding =>
                    string.Equals(binding.team, team, StringComparison.Ordinal)
                    && binding.agent != null).agent;
                return agent != null;
            }

            agent = null;
            return false;
        }

        public bool TryGetCameraRole(string role, out Transform anchor)
        {
            for (int i = 0; i < cameraRoles.Length; i++)
            {
                if (!string.Equals(cameraRoles[i].role, role, StringComparison.Ordinal))
                {
                    continue;
                }

                anchor = cameraRoles[i].anchor;
                return anchor != null;
            }

            anchor = null;
            return false;
        }

        private BaseRLAgent GetAgentRole(string role)
        {
            return TryGetAgentRole(role, out BaseRLAgent agent) ? agent : null;
        }

        private BaseRLAgent GetPrimaryAgent()
        {
            AgentRoleBinding primaryBinding = agentRoles.FirstOrDefault(binding => binding.primary && binding.agent != null);
            if (primaryBinding.agent != null)
            {
                return primaryBinding.agent;
            }

            return GetAgentRole("hero");
        }

        private Transform GetSceneRole(string role)
        {
            return TryGetSceneRole(role, out Transform target) ? target : null;
        }

        private Transform GetCameraRole(string role)
        {
            return TryGetCameraRole(role, out Transform anchor) ? anchor : null;
        }
    }
}
