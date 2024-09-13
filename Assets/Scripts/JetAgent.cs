using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Grid;
using System.Collections.Generic;

public class JetAgent : Agent
{
    public JetController jetController;
    [SerializeField] private GridSensorComponent3D sensorComponent;
    public List<string> importantTags = new List<string>(); // List of important tags set in the Inspector
    public float targetFollowAngle = 45f;
    public float targetFollowDistance = 100f;
    public float maxAllowedDistance = 65;
    public Transform environmentCenter;
    private List<GameObject> m_Targets = new List<GameObject>();

    public override void Initialize()
    {
        jetController = GetComponent<JetController>();
        UpdatePosition();
    }

    private List<JetAgent> FindAllAgents()
    {
        return new List<JetAgent>(FindObjectsOfType<JetAgent>());
    }

    public override void OnEpisodeBegin()
    {
        UpdatePosition();
        jetController.ResetVelocity();
    }

    private void UpdatePosition()
    {
        transform.position = new Vector3(
            Random.Range(environmentCenter.position.x - 10, environmentCenter.position.x + 10),
            Random.Range(transform.position.y - 10, transform.position.y + 10),
            Random.Range(transform.position.z - 10, transform.position.z + 10)
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.rotation);
        sensor.AddObservation(jetController.currentThrust);

        CollectTargetObservations(sensor);
        CollectAgentObservations(sensor);
    }

    private void CollectTargetObservations(VectorSensor sensor)
    {
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        m_Targets.Clear();

        foreach (var target in sensorComponent.GetDetectedGameObjects(tag))
        {
            Vector3 delta = target.transform.position - pos;
            if (IsValidTarget(delta, fwd))
            {
                m_Targets.Add(target);
                sensor.AddObservation(delta);
                sensor.AddObservation(target.transform.rotation);
            }
        }
    }

    private bool IsValidTarget(Vector3 delta, Vector3 fwd)
    {
        return Vector3.Angle(fwd, delta) < targetFollowAngle &&
               delta.sqrMagnitude < targetFollowDistance * targetFollowDistance;
    }

    private void CollectAgentObservations(VectorSensor sensor)
    {
        var agents = FindAllAgents();
        foreach (var agent in agents)
        {
            if (agent != this)
            {
                Vector3 delta = agent.transform.position - transform.position;
                sensor.AddObservation(delta);
                sensor.AddObservation(agent.transform.rotation);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);
        ProcessActions(actionBuffers.DiscreteActions);
        CheckTargets();
        ApplyMovementPenalties(actionBuffers.DiscreteActions);
        if (Vector3.Distance(transform.position, environmentCenter.position) > maxAllowedDistance)
        {
            // Penalize and respawn or end episode
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    private void ProcessActions(ActionSegment<int> actions)
    {
        float horizontal = actions[0] - 1;
        float vertical = actions[1] - 1;
        float thrustChange = actions[2] - 1;

        jetController.Turn(horizontal, vertical);
        jetController.AdjustThrust(thrustChange);
    }

    private void ApplyMovementPenalties(ActionSegment<int> actions)
    {
        AddReward(-0.1f * Mathf.Abs(actions[0] - 1));
        AddReward(-0.1f * Mathf.Abs(actions[1] - 1));
    }

    private void CheckTargets()
    {
        Vector3 pos = transform.position;
        Vector3 vlc = jetController.CurrentVelocity;

        foreach (var target in m_Targets)
        {
            Vector3 delta = target.transform.position - pos;
            float distance = delta.magnitude;
            float speedTowardsTarget = Vector3.Dot(delta.normalized, vlc);
            if (speedTowardsTarget > 0)
            {
                float reward = speedTowardsTarget / distance;
                AddReward(reward * 0.01f);
            }
            if (distance < 1.0f)
            {
                AddReward(-1.0f);
                Debug.LogWarning("ITSF");
                EndEpisode();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<JetAgent>() != null)
        {
            AddReward(-1.0f);
            Debug.Log("Penalty for colliding with another agent.");
        }
        if (collision.gameObject.tag == "Target")
        {
            Debug.Log("Penalty for colliding with another Target.");
            AddReward(1.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetAxis("Thrust");
        Debug.Log("Heuristic - Horizontal: " + continuousActionsOut[0] + ", Vertical: " + continuousActionsOut[1] + ", Thrust: " + continuousActionsOut[2]);
    }
}