using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all targets in the environment centrally to prevent conflicts between multiple agents.
/// Ensures targets are placed at varying heights.
/// </summary>
public class TargetManager : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform targetPrefab; // Prefab of the target
    public int numberOfTargets = 10; // Number of targets to spawn
    public float spawnRange = 10f; // Range around the environment center to spawn targets
    public float minHeight = 1f; // Minimum height for targets
    public float maxHeight = 5f; // Maximum height for targets
    public float minDistanceBetweenTargets = 2f; // Minimum distance between two targets

    [Header("Environment Settings")]
    public Transform environmentCenter; // Center point of the environment

    private List<Transform> targets = new List<Transform>(); // List to keep track of all targets
    private Dictionary<JetAgent, Transform> agentTargetAssignments = new Dictionary<JetAgent, Transform>(); // Mapping agents to their assigned targets

    /// <summary>
    /// Initializes targets when the game starts.
    /// </summary>
    private void Start()
    {
        InitializeTargets();
        ResetAllTargets();
    }

    /// <summary>
    /// Instantiates target prefabs and adds them to the targets list.
    /// </summary>
    private void InitializeTargets()
    {
        for (int i = 0; i < numberOfTargets; i++)
        {
            Transform newTarget = Instantiate(targetPrefab, Vector3.zero, Quaternion.identity, this.transform);
            targets.Add(newTarget);
        }
    }

    /// <summary>
    /// Resets all targets to new random positions with varying heights.
    /// </summary>
    public void ResetAllTargets()
    {
        foreach (var target in targets)
        {
            SpawnTarget(target);
        }
    }

    /// <summary>
    /// Spawns a single target at a random position ensuring minimum distance from other targets.
    /// </summary>
    /// <param name="target">The target to spawn.</param>
    private void SpawnTarget(Transform target)
    {
        bool validPosition = false;
        Vector3 newPosition = Vector3.zero;
        int attempts = 0;
        int maxAttempts = 100; // Prevents infinite loops in case of crowded spawn areas

        while (!validPosition && attempts < maxAttempts)
        {
            newPosition = new Vector3(
                Random.Range(environmentCenter.position.x - spawnRange, environmentCenter.position.x + spawnRange),
                Random.Range(minHeight, maxHeight), // Varying Y positions
                Random.Range(environmentCenter.position.z - spawnRange, environmentCenter.position.z + spawnRange)
            );

            validPosition = true;
            foreach (var otherTarget in targets)
            {
                if (otherTarget != target && Vector3.Distance(newPosition, otherTarget.position) < minDistanceBetweenTargets)
                {
                    validPosition = false;
                    break;
                }
            }

            attempts++;
        }

        if (validPosition)
        {
            target.position = newPosition;
            target.rotation = Quaternion.identity; // Reset rotation for consistency
        }
        else
        {
            Debug.LogWarning("Unable to find a valid position for target after maximum attempts.");
        }
    }

    /// <summary>
    /// Retrieves the nearest target to a given agent position.
    /// </summary>
    /// <param name="agentPosition">Position of the agent.</param>
    /// <returns>The nearest target Transform.</returns>
    public Transform GetNearestTarget(Vector3 agentPosition)
    {
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var target in targets)
        {
            float distance = Vector3.Distance(agentPosition, target.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = target;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Assigns the nearest available target to the given agent.
    /// </summary>
    /// <param name="agent">The agent to assign a target to.</param>
    public void AssignTargetToAgent(JetAgent agent)
    {
        if (agentTargetAssignments.ContainsKey(agent))
        {
            // Agent already has an assigned target
            return;
        }

        Transform nearestTarget = GetNearestTarget(agent.transform.position);
        if (nearestTarget != null)
        {
            agentTargetAssignments.Add(agent, nearestTarget);
            agent.SetAssignedTarget(nearestTarget); // Assign the target to the agent
        }
    }

    /// <summary>
    /// Unassigns the target from the given agent.
    /// </summary>
    /// <param name="agent">The agent to unassign a target from.</param>
    public void UnassignTargetFromAgent(JetAgent agent)
    {
        if (agentTargetAssignments.ContainsKey(agent))
        {
            agentTargetAssignments.Remove(agent);
            agent.SetAssignedTarget(null); // Remove the target from the agent
        }
    }

    /// <summary>
    /// Clears all target assignments.
    /// </summary>
    public void ClearAllAssignments()
    {
        foreach (var agent in agentTargetAssignments.Keys)
        {
            agent.SetAssignedTarget(null);
        }
        agentTargetAssignments.Clear();
    }
}
