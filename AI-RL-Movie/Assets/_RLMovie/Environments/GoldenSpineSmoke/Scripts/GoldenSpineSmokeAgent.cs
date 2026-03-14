using RLMovie.Common;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RLMovie.Environments.GoldenSpineSmoke
{
    /// <summary>
    /// Golden starter agent for GoldenSpineSmoke.
    /// Replace observations, rewards, and action semantics for the actual theme.
    /// </summary>
    public class GoldenSpineSmokeAgent : BaseRLAgent
    {
        [Header("=== Starter Goal Navigation ===")]
        [SerializeField] private Transform goal;
        [SerializeField] private EnvironmentManager envManager;
        [SerializeField] private float moveForce = 1.0f;
        [SerializeField] private float goalDistance = 1.25f;

        private Rigidbody _rb;

        protected override void OnAgentInitialize()
        {
            _rb = GetComponent<Rigidbody>();
        }

        protected override void OnEpisodeReset()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            if (envManager != null)
            {
                transform.localPosition = envManager.GetRandomPosition(0.5f);
                goal.localPosition = envManager.GetRandomEdgePosition(0.5f);
            }
            else
            {
                transform.localPosition = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
                goal.localPosition = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
            }
        }

        protected override void CollectAgentObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.localPosition);
            sensor.AddObservation(goal.localPosition);
            sensor.AddObservation(_rb.linearVelocity);

            Vector3 directionToGoal = (goal.localPosition - transform.localPosition).normalized;
            sensor.AddObservation(directionToGoal);

            float distanceToGoal = Vector3.Distance(transform.localPosition, goal.localPosition);
            sensor.AddObservation(distanceToGoal);
        }

        protected override void ExecuteActions(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];

            _rb.AddForce(new Vector3(moveX, 0f, moveZ) * moveForce, ForceMode.VelocityChange);

            float distanceToGoal = Vector3.Distance(transform.localPosition, goal.localPosition);
            if (distanceToGoal < goalDistance)
            {
                Success(1.0f);
                return;
            }

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

            AddTrackedReward(-distanceToGoal * 0.001f);
            AddTrackedReward(-0.0005f);
        }

        protected override void ProvideHeuristicInput(in ActionBuffers actionsOut)
        {
            var continuousActions = actionsOut.ContinuousActions;
            float moveX = 0f;
            float moveZ = 0f;

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) moveZ = 1f;
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) moveZ = -1f;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) moveX = 1f;
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) moveX = -1f;
            }

            continuousActions[0] = moveX;
            continuousActions[1] = moveZ;
        }
    }
}
