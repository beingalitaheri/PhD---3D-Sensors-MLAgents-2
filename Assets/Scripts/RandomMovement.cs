using UnityEngine;
using System.Collections;

public class RandomMovement : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform centerTransform; // The central transform from which points are selected
    public float minRadius = 5f;      // Minimum radius
    public float maxRadius = 15f;     // Maximum radius

    [Header("Movement Settings")]
    public float speed = 3f;              // Speed of the object's movement
    public float reachThreshold = 0.1f;   // Distance threshold to consider the target as reached

    private Vector3 targetPosition;

    void Start()
    {
        if (centerTransform == null)
        {
            centerTransform = this.transform; // If the central transform is not set, use the object's own transform
        }

        SetNewTarget();
    }

    void Update()
    {
        MoveTowardsTarget();
    }

    void SetNewTarget()
    {
        float randomRadius = Random.Range(minRadius, maxRadius);
        // Select a random angle
        float randomAngle = Random.Range(0f, Mathf.PI * 2);

        // Calculate the target position using the random radius and angle
        float x = centerTransform.position.x + randomRadius * Mathf.Cos(randomAngle);
        float z = centerTransform.position.z + randomRadius * Mathf.Sin(randomAngle);
        float y = centerTransform.position.y + Random.Range(-10f, 10f); 

        targetPosition = new Vector3(x, y, z);
    }

    void MoveTowardsTarget()
    {
        // Calculate the direction of movement
        Vector3 direction = targetPosition - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (direction.magnitude <= reachThreshold)
        {
            // Reached the target
            SetNewTarget();
        }
        else
        {
            // Move towards the target
            Vector3 move = direction.normalized * distanceThisFrame;
            // Ensure the object does not overshoot the target
            if (move.magnitude > direction.magnitude)
            {
                move = direction;
            }
            transform.Translate(move, Space.World);
        }
    }

    // (Optional) Display the target in the editor for easier visualization
    void OnDrawGizmosSelected()
    {
        if (centerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(centerTransform.position, minRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centerTransform.position, maxRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(targetPosition, 0.3f);
        }
    }
}
