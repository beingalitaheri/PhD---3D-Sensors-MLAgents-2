using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Grid;
using System.Collections.Generic;
using MBaske.Driver;

/// <summary>
/// Represents a Jet Agent that uses ML-Agents for training and interacts with targets managed by TargetManager.
/// Capable of moving in all three axes (X, Y, Z) using a Grid Sensor.
/// </summary>
public class JetAgent : Agent
{
    // Reference to the JetController component
    public JetController jetController;

    // Reference to the GridSensorComponent3D
    [SerializeField] private GridSensorComponent3D sensorComponent;

    // List of important tags to detect targets
    public List<string> importantTags = new List<string>();

    // Angle and distance settings for following targets
    public float targetFollowAngle = 45f;
    public float targetFollowDistance = 100f;

    // Maximum allowed distance from the environment center
    public float maxAllowedDistance = 65f;

    // Reference to the environment center
    public Transform environmentCenter;

    // Reference to the TargetManager
    private TargetManager targetManager;

    // List of all agents in the scene
    public List<JetAgent> allAgents = new List<JetAgent>();
    public List<Obstacle_Cube> obstacle_Cubes = new List<Obstacle_Cube>();
    public List<Target_Cube> target_Cubes = new List<Target_Cube>();
    private IList<GameObject> m_Targets;
    public List<Wall> walls = new List<Wall>();
    // Current assigned target for this agent
    private Transform assignedTarget;
    public string targetTag = "Target";
    // Maximum number of targets to consider in observations
    /// <summary>
    /// Initializes the agent by setting up references and initial positions.
    /// </summary>
    public override void Initialize()
    {
        // Ensure JetController is assigned
        jetController = GetComponent<JetController>();
        if (jetController == null)
        {
            Debug.LogError("JetController component is missing from the Agent.");
        }

        // Ensure GridSensorComponent3D is assigned
        if (sensorComponent == null)
        {
            sensorComponent = GetComponent<GridSensorComponent3D>();
            if (sensorComponent == null)
            {
                Debug.LogError("GridSensorComponent3D is not assigned.");
            }
        }

        // Find the TargetManager in the scene
        targetManager = FindObjectOfType<TargetManager>();
        if (targetManager == null)
        {
            Debug.LogError("TargetManager not found in the scene.");
        }
        m_Targets = new List<GameObject>(27);
        // Gather all JetAgent instances in the scene
        //allAgents = new List<JetAgent>(FindObjectsOfType<JetAgent>());
        //
        // Assign target via TargetManager
        if (targetManager != null) { targetManager.AssignTargetToAgent(this); }

        // Set initial position of the agent
        //UpdatePosition();
    }

    /// <summary>
    /// Called at the beginning of each episode to reset agent and targets.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Reset agent's position
        UpdatePosition();

        // Reset agent's velocity
        jetController.ResetVelocity();

        // Reset all targets via TargetManager
        //targetManager.ResetAllTargets();

        // Reassign target after resetting targets
        if (targetManager != null) { targetManager.AssignTargetToAgent(this); }
    }

    /// <summary>
    /// Updates the agent's position to a random location within the spawn range.
    /// </summary>
    private void UpdatePosition()
    {
        transform.position = new Vector3(
            Random.Range(environmentCenter.position.x - 4.5f, environmentCenter.position.x + 4.5f),
            Random.Range(environmentCenter.position.y - 4.5f, environmentCenter.position.y + 4.5f),
            Random.Range(environmentCenter.position.z - 4.5f, environmentCenter.position.z + 4.5f)
        );
    }

    /// <summary>
    /// Collects observations from the environment and assigned target.
    /// </summary>
    /// <param name="sensor">The sensor to collect observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Add agent's position and rotation to observations
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.rotation);
        sensor.AddObservation(jetController.currentThrust);
        //
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;

        foreach (var target in sensorComponent.GetDetectedGameObjects(targetTag))
        {
            Vector3 delta = target.transform.position - pos;
            if (Vector3.Angle(fwd, delta) < targetFollowAngle && delta.sqrMagnitude < targetFollowDistance) 
            {
                m_Targets.Add(target);
            }
        }

        // Collect observations related to the assigned target
        //CollectTargetObservations(sensor);

        // Collect observations related to other agents
        //CollectAgentObservations(sensor);
    }

    /// <summary>
    /// Collects observations related to the assigned target.
    /// </summary>
    /// <param name="sensor">The sensor to collect observations.</param>
    private void CollectTargetObservations(VectorSensor sensor)
    {
        if (assignedTarget != null)
        {
            Vector3 delta = assignedTarget.position - transform.position;
            Vector3 fwd = transform.forward;

            if (IsValidTarget(delta, fwd))
            {
                sensor.AddObservation(delta);
                sensor.AddObservation(assignedTarget.rotation);
            }
        }
    }

    /// <summary>
    /// Determines if a target is valid based on angle and distance constraints.
    /// </summary>
    /// <param name="delta">Vector from agent to target.</param>
    /// <param name="fwd">Forward direction of the agent.</param>
    /// <returns>True if target is valid, otherwise false.</returns>
    private bool IsValidTarget(Vector3 delta, Vector3 fwd)
    {
        return Vector3.Angle(fwd, delta) < targetFollowAngle &&
               delta.sqrMagnitude < targetFollowDistance * targetFollowDistance;
    }

    /// <summary>
    /// Collects observations related to other agents in the environment.
    /// </summary>
    /// <param name="sensor">The sensor to collect observations.</param>
    private void CollectAgentObservations(VectorSensor sensor)
    {
        foreach (var agent in allAgents)
        {
            if (agent != this)
            {
                Vector3 delta = agent.transform.position - transform.position;
                sensor.AddObservation(delta);
                sensor.AddObservation(agent.transform.rotation);
            }
        }
        foreach (var target in target_Cubes)
        {
            if (target != this)
            {
                Vector3 delta = target.transform.position - transform.position;
                sensor.AddObservation(delta);
                sensor.AddObservation(target.transform.position);
                sensor.AddObservation(target.transform.rotation);
            }
        }
        foreach (var wall in walls)
        {
            if (wall != this)
            {
                Vector3 delta = wall.transform.position - transform.position;
                sensor.AddObservation(delta);
                sensor.AddObservation(wall.transform.position);
                sensor.AddObservation(wall.transform.rotation);
            }
        }
    }

    /// <summary>
    /// Receives and processes actions from the agent's policy.
    /// </summary>
    /// <param name="actionBuffers">The action buffers containing actions.</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Process discrete actions
        ProcessActions(actionBuffers.DiscreteActions);

        // Check target statuses and apply rewards
        CheckTargets();

        // Apply movement penalties to encourage efficient actions
        ApplyMovementPenalties(actionBuffers.DiscreteActions);

        // Penalize and end episode if agent is too far from the environment center
        if (Vector3.Distance(transform.position, environmentCenter.position) > maxAllowedDistance)
        {
            AddReward(-3.0f);
            EndEpisode();
        }
    }

    /// <summary>
    /// Processes discrete actions received from the policy.
    /// </summary>
    /// <param name="actions">The discrete actions.</param>
    private void ProcessActions(ActionSegment<int> actions)
    {
        // Convert discrete actions to control signals (-1, 0, 1)
        float horizontal = actions[0] - 1f; // 0,1,2 => -1,0,1
        float vertical = actions[1] - 1f;
        float thrustChange = actions[2] ;

        // Apply controls to the JetController
        jetController.Turn(horizontal, vertical);
        jetController.AdjustThrust(thrustChange);
    }

    /// <summary>
    /// Applies movement penalties based on the actions taken to encourage minimal necessary movements.
    /// </summary>
    /// <param name="actions">The discrete actions.</param>
    private void ApplyMovementPenalties(ActionSegment<int> actions)
    {
        // Penalize for horizontal and vertical movements
        AddReward(-0.1f * Mathf.Abs(actions[0] - 1));
        AddReward(-0.1f * Mathf.Abs(actions[1] - 1));
    }

    /// <summary>
    /// Checks the status of the assigned target and assigns rewards or penalties accordingly.
    /// </summary>
    private void CheckTargets()
    {
        if (assignedTarget != null)
        {
            Vector3 pos = transform.position;
            Vector3 velocity = jetController.CurrentVelocity;
            Vector3 delta = assignedTarget.position - pos;
            float distance = delta.magnitude;
            float speedTowardsTarget = Vector3.Dot(delta.normalized, velocity);

            if (speedTowardsTarget > 0)
            {
                float reward = Mathf.Clamp(speedTowardsTarget / distance, -1f, 1f);
                AddReward(reward * 0.01f);
            }

            if (distance < 1.0f)
            {
                AddReward(1.0f); // Reward for reaching the target
                Debug.LogWarning("Reward: Reached the target.");
                EndEpisode();
            }
        }
    }

    /// <summary>
    /// Handles collision events with other agents and targets.
    /// </summary>
    /// <param name="collision">The collision information.</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<JetAgent>() != null)
        {
            AddReward(-1.0f);
            Debug.LogWarning("Penalty: Collided with another agent.");
            EndEpisode();
        }

        if (collision.gameObject.GetComponent<Wall>() != null)
        {
            AddReward(-1.0f);
            Debug.LogWarning("Penalty: Collided with a wall.");
            EndEpisode();
        }

        if (collision.gameObject.GetComponent<Target_Cube>() != null)
        {
            AddReward(1.0f);
            Debug.LogWarning("Reward: Collided with a Target.");
            EndEpisode();
        }
    }

    /// <summary>
    /// Defines the heuristic (manual control) for testing the agent.
    /// </summary>
    /// <param name="actionsOut">The action buffers to output actions.</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Manual control using discrete actions
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Convert input axes to discrete actions (0, 1, 2)
        discreteActionsOut[0] = Mathf.RoundToInt(Input.GetAxis("Horizontal") + 1f);
        discreteActionsOut[1] = Mathf.RoundToInt(Input.GetAxis("Vertical") + 1f);
        discreteActionsOut[2] = Mathf.RoundToInt(Input.GetAxis("Thrust") + 1f);

        Debug.Log($"Heuristic Actions - Horizontal: {discreteActionsOut[0]}, Vertical: {discreteActionsOut[1]}, Thrust: {discreteActionsOut[2]}");
    }

    /// <summary>
    /// Sets the assigned target for this agent.
    /// </summary>
    /// <param name="target">The target to assign.</param>
    public void SetAssignedTarget(Transform target)
    {
        assignedTarget = target;
    }
}
