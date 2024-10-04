using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Grid;
using System.Collections.Generic;

public class JetAgent_Child: Agent
{
    // Reference to the GridSensorComponent3D
    [SerializeField] private GridSensorComponent3D sensorComponent;

    // Movement settings
    public float moveSpeed = 10f;       // Speed of movement
    public float rotateSpeed = 100f;    // Speed of rotation

    // Angle and distance settings for following targets
    public float targetFollowAngle = 45f;
    public float targetFollowDistance = 100f;

    // Maximum allowed distance from the environment center
    public float maxAllowedDistance = 65f;
    //
    public float generateDistanceEnviro = 6;
    // Reference to the environment center
    private Rigidbody rb;
    //
    public GameObject agentMother_Transform;
    /// <summary>
    /// Initializes the agent by setting up references and initial positions.
    /// </summary>
    public override void Initialize()
    {

        // Get the Rigidbody component attached to the Agent
        rb = GetComponent<Rigidbody>();

        // Ensure GridSensorComponent3D is assigned
        if (sensorComponent == null)
        {
            sensorComponent = GetComponent<GridSensorComponent3D>();
            if (sensorComponent == null)
            {
                Debug.LogError("GridSensorComponent3D is not assigned.");
            }
        }

    }

    /// <summary>
    /// Called at the beginning of each episode to reset agent and targets.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Reset agent's position
        UpdatePosition();

        // Reset the Agent's velocity and position
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.identity; // Starting rotation

    }

    /// <summary>
    /// Updates the agent's position to a random location within the spawn range.
    /// </summary>
    private void UpdatePosition()
    {
        int xNum = Random.Range(1, 3);
        int yNum = Random.Range(1, 3);
        int zNum = Random.Range(1, 3);
        float xPos, yPos, zPos;
        if (xNum == 1)
        {
            xPos = Random.Range(agentMother_Transform.transform.position.x + generateDistanceEnviro, agentMother_Transform.transform.position.x + (2 * generateDistanceEnviro));
        }
        else
        {
            xPos = Random.Range(agentMother_Transform.transform.position.x - generateDistanceEnviro, agentMother_Transform.transform.position.x - (2 * generateDistanceEnviro));
        }
        //
        if (yNum == 1)
        {
            yPos = Random.Range(agentMother_Transform.transform.position.y + generateDistanceEnviro, agentMother_Transform.transform.position.y + (2 * generateDistanceEnviro));
        }
        else
        {
            yPos = Random.Range(agentMother_Transform.transform.position.y - generateDistanceEnviro, agentMother_Transform.transform.position.y - (2 * generateDistanceEnviro));
        }
        //
        if (zNum == 1)
        {
            zPos = Random.Range(agentMother_Transform.transform.position.z + generateDistanceEnviro, agentMother_Transform.transform.position.z + (2 * generateDistanceEnviro));
        }
        else
        {
            zPos = Random.Range(agentMother_Transform.transform.position.z - generateDistanceEnviro, agentMother_Transform.transform.position.z - (2 * generateDistanceEnviro));
        }
        //
        transform.position = new Vector3(xPos, yPos, zPos);
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
        sensor.AddObservation(transform.rotation.eulerAngles);
        sensor.AddObservation(agentMother_Transform.transform.position);
        sensor.AddObservation(agentMother_Transform.transform.rotation);
        //
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        CollectAgentObservations(sensor);
    }

    /// <summary>
    /// Collects observations related to other agents in the environment.
    /// </summary>
    /// <param name="sensor">The sensor to collect observations.</param>
    private void CollectAgentObservations(VectorSensor sensor)
    {
        Vector3 delta = agentMother_Transform.transform.position - transform.position;
        sensor.AddObservation(delta);
        sensor.AddObservation(agentMother_Transform.transform.rotation);
    }

    /// <summary>
    /// Receives and processes actions from the agent's policy.
    /// </summary>
    /// <param name="actionBuffers">The action buffers containing actions.</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Process discrete actions
        ProcessActions(actionBuffers.DiscreteActions);
        
        CheckTargets();

        // Penalize and end episode if agent is too far from the environment center
        if (Vector3.Distance(transform.position, agentMother_Transform.transform.position) > maxAllowedDistance)
        {
            AddReward(-3.0f);
            Debug.LogWarning("Penalty: Out of area.");
            EndEpisode();
        }
    }

    /// <summary>
    /// Processes discrete actions received from the policy.
    /// </summary>
    /// <param name="actions">The discrete actions.</param>
    private void ProcessActions(ActionSegment<int> actions)
    {
        float moveX = actions[0] - 1;
        float moveY = actions[1] - 1;
        float moveZ = actions[2] - 1;
        float rotateY = actions[3] - 1;
        float rotateZ = actions[4] - 1;
        // Apply movement force
        Vector3 movement = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;
        rb.AddForce(movement, ForceMode.VelocityChange);

        // Apply rotation torque
        Vector3 rotation = new Vector3(0, rotateY, rotateZ) * rotateSpeed * Time.deltaTime;
        rb.AddTorque(rotation, ForceMode.VelocityChange);

        // Clamp the Agent's velocity to prevent excessive speed
        float maxSpeed = 20f;
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        // Provide a small positive reward for staying active
        AddReward(0.01f);

        // Apply controls to the JetController
    }

    /// <summary>
    /// Checks the status of the assigned target and assigns rewards or penalties accordingly.
    /// </summary>
    private void CheckTargets()
    {
        if (agentMother_Transform != null)
        {
            Vector3 pos = transform.position;
            Vector3 velocity = rb.velocity;
            Vector3 delta = agentMother_Transform.transform.position - pos;
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

        if (collision.gameObject.GetComponent<Wall>() != null || collision.gameObject.tag == "wall")
        {
            AddReward(-1.0f); // Penalty for colliding with a wall
            Debug.LogWarning("Penalty: Collided with a wall.");
            EndEpisode();
        }

        if (collision.gameObject == agentMother_Transform.gameObject)
        {
            AddReward(1.0f); // Reward for colliding with a target
            Debug.LogWarning("Reward: Collided with a Target.");
            //EndEpisode();
        }
    }

    /// <summary>
    /// Defines the heuristic (manual control) for testing the agent.
    /// </summary>
    /// <param name="actionsOut">The action buffers to output actions.</param>
    // Heuristic method for manual control (e.g., using keyboard)
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Jump");
        continuousActionsOut[2] = Input.GetAxis("Vertical");
        continuousActionsOut[3] = Input.GetAxis("RotateY");
        continuousActionsOut[4] = Input.GetAxis("RotateZ");
    }
}
