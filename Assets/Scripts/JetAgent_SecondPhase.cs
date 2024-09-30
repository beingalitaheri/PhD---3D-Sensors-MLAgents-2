using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Grid;
using System.Collections.Generic;

public class JetAgent_SecondPhase : Agent
{
    // Reference to the GridSensorComponent3D
    [SerializeField] private GridSensorComponent3D sensorComponent;

    // List of important tags to detect targets
    public List<string> importantTags = new List<string>();

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
    public Transform environmentCenter;
    private Rigidbody rb;

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

        m_Targets = new List<GameObject>(27);

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
            xPos = Random.Range(environmentCenter.position.x + generateDistanceEnviro, environmentCenter.position.x + (2 * generateDistanceEnviro));
        }
        else 
        {
            xPos = Random.Range(environmentCenter.position.x - generateDistanceEnviro, environmentCenter.position.x - (2 * generateDistanceEnviro));
        }
        //
        if (yNum ==1 ) 
        {
            yPos = Random.Range(environmentCenter.position.y + generateDistanceEnviro, environmentCenter.position.y + (2 * generateDistanceEnviro));
        }
        else
        {
            yPos = Random.Range(environmentCenter.position.y - generateDistanceEnviro, environmentCenter.position.y - (2 * generateDistanceEnviro));
        }
        //
        if (zNum == 1)
        {
            zPos = Random.Range(environmentCenter.position.z + generateDistanceEnviro, environmentCenter.position.z + (2 * generateDistanceEnviro));
        }
        else
        {
            zPos = Random.Range(environmentCenter.position.z - generateDistanceEnviro, environmentCenter.position.z - (2 * generateDistanceEnviro));
        }
        //
        transform.position = new Vector3(xPos , yPos, zPos);
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
        sensor.AddObservation(environmentCenter.position);
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
        // Add the positions of all targets
        foreach (var target in GameObject.FindGameObjectsWithTag("Target"))
        {
            Vector3 targetPosition = target.transform.position;
            sensor.AddObservation(targetPosition);
        }
        foreach (var wall in GameObject.FindGameObjectsWithTag("wall"))
        {
            Vector3 targetPosition = wall.transform.position;
            sensor.AddObservation(targetPosition);
        }
        // Add the positions of other Agents (if any)
        foreach (var otherAgent in GameObject.FindGameObjectsWithTag("Agent"))
        {
            if (otherAgent != this.gameObject)
            {
                Vector3 otherPosition = otherAgent.transform.position;
                sensor.AddObservation(otherPosition);
            }
        }
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

        // Penalize and end episode if agent is too far from the environment center
        if (Vector3.Distance(transform.position, environmentCenter.position) > maxAllowedDistance)
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
        if (assignedTarget != null)
        {
            Vector3 pos = transform.position;
            Vector3 velocity = rb.velocity;
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
        if (collision.gameObject.GetComponent<JetAgent_SecondPhase>() != null)
        {
            //AddReward(-1.0f); // Penalty for colliding with another agent
            Debug.LogWarning("Penalty: Collided with another agent.");
            //EndEpisode();
        }

        if (collision.gameObject.GetComponent<Wall>() != null || collision.gameObject.tag == "wall")
        {
            AddReward(-1.0f); // Penalty for colliding with a wall
            Debug.LogWarning("Penalty: Collided with a wall.");
            EndEpisode();
        }

        if (collision.gameObject.GetComponent<Target_Cube>())
        {
            AddReward(1.0f); // Reward for colliding with a target
            Debug.LogWarning("Reward: Collided with a Target.");
            EndEpisode();
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

    /// <summary>
    /// Sets the assigned target for this agent.
    /// </summary>
    /// <param name="target">The target to assign.</param>
    public void SetAssignedTarget(Transform target)
    {
        assignedTarget = target;
    }
}
