using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Grid;
using System.Collections.Generic;
using MBaske.Dogfight;


namespace MBaske
{
    /// <summary>
    /// Agent that pilots a <see cref="Spaceship>"/> and has to 
    /// follow other agents while avoiding <see cref="Asteroid"/>s.
    /// </summary>
    public class SmartAgent : Agent
    {
        private Spaceship m_Ship;
        private StatsRecorder m_Stats;

        [SerializeField]
        private GridSensorComponent3D m_SensorComponent;
        [SerializeField]
        private float m_TargetFollowAngle = 30;
        [SerializeField]
        private float m_TargetFollowDistance = 50;
        private float m_TargetFollowDistanceSqr;

        [SerializeField]
        private float idealDistance = 30f;
        [SerializeField]
        private float distanceTolerance = 10f;
        private float tooClosePenalty = -0.2f;
        private float tooFarPenalty = -0.1f;
        private float collisionPenalty = -1f;

        public Transform centralPoint; // The Transform to use as the center for respawn
        public float respawnRadius = 10f; // The radius around the central point to respawn the Agent

        private IList<GameObject> m_Targets;
        private static IDictionary<GameObject, PilotAgent> s_TargetCache;
        private static string m_TargetTag; // same for all.

        public override void Initialize()
        {
            m_Stats = Academy.Instance.StatsRecorder;
            m_Ship = GetComponentInChildren<Spaceship>();
            m_Ship.CollisionEvent += OnCollision;

            m_Targets = new List<GameObject>(10);
            m_TargetFollowDistanceSqr = m_TargetFollowDistance * m_TargetFollowDistance;

            AddDecisionRequester();
        }

        void Update()
        {
            CheckDistanceAndRespawn();
        }

        private void CheckDistanceAndRespawn()
        {
            // Check the distance to a specific target or condition if needed
            if (SomeCondition())
            {
                RespawnAgent();
            }
        }
        private bool SomeCondition()
        {
            // Define your condition that triggers the respawn, e.g., too far from central point
            return Vector3.Distance(m_Ship.transform.position, centralPoint.position) > respawnRadius;
        }
        private void RespawnAgent()
        {
            // Generate a random position within a specified radius from the central point
            Vector3 randomDirection = Random.insideUnitSphere * respawnRadius;
            Vector3 respawnPosition = centralPoint.position + randomDirection;

            m_Ship.transform.position = respawnPosition;
            Debug.Log("Agent respawned to a new position within the specified radius.");
        }
        private void AddDecisionRequester()
        {
            var req = gameObject.AddComponent<DecisionRequester>();
            req.DecisionPeriod = 4;
            req.TakeActionsBetweenDecisions = true;
        }

        public override void OnEpisodeBegin()
        {
            // Reset spaceship position randomly within the environment
            RespawnAgent();
            m_Targets.Clear();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(m_Ship.Throttle);
            sensor.AddObservation(m_Ship.Pitch);
            sensor.AddObservation(m_Ship.Roll);
            sensor.AddObservation(m_Ship.NormPosition);
            sensor.AddObservation(m_Ship.NormOrientation);

            Vector3 pos = m_Ship.transform.position;
            Vector3 fwd = m_Ship.transform.forward;

            foreach (var target in m_SensorComponent.GetDetectedGameObjects(m_TargetTag))
            {
                Vector3 delta = target.transform.position - pos;
                if (Vector3.Angle(fwd, delta) < m_TargetFollowAngle && delta.sqrMagnitude < m_TargetFollowDistanceSqr)
                {
                    m_Targets.Add(target);
                }
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            float speed = m_Ship.ManagedUpdate(
                actionBuffers.DiscreteActions[0] - 1,
                actionBuffers.DiscreteActions[1] - 1,
                actionBuffers.DiscreteActions[2] - 1);

            CheckDistances();
        }

        private void CheckDistances()
        {
            Vector3 pos = m_Ship.transform.position;
            Vector3 vlc = m_Ship.WorldVelocity;

            foreach (var target in m_Targets)
            {
                Vector3 delta = target.transform.position - pos;
                // Speed towards target.
                float speed = Vector3.Dot(delta.normalized, vlc);
                AddReward(speed * 0.01f); // Reward based on speed towards the target.

                // Penalize opponent for being followed if the speed towards the target is positive.
                if (speed > 0)
                {
                    s_TargetCache[target].AddReward(speed * -0.005f);
                }

                // Check if the agent is at an ideal distance from the target.
                float distance = Vector3.Distance(pos, target.transform.position);
                if (distance < idealDistance - distanceTolerance)
                {
                    AddReward(tooClosePenalty); // Penalize for being too close.
                }
                else if (distance > idealDistance + distanceTolerance)
                {
                    AddReward(tooFarPenalty); // Penalize for being too far.
                }
                else
                {
                    AddReward(0.1f); // Reward for maintaining the ideal distance.
                }
            }
        }

        private void OnCollision()
        {
            AddReward(collisionPenalty);
            EndEpisode();

        }
    }
}