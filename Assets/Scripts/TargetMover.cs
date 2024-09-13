using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMover : MonoBehaviour
{
    public GameObject target; // Separate variable for the target
    public float moveInterval = 5f; // Time interval between movements
    public float radius = 50f; // Radius of the spherical area for movement

    private float timer;

    void Start()
    {
        MoveTarget(); // Move the target for the first time
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= moveInterval)
        {
            MoveTarget();
            timer = 0;
        }
    }

    private void MoveTarget()
    {
        // Calculate a new position for the target randomly within the specified radius
        Vector3 newPosition = Random.insideUnitSphere * radius;
        newPosition.y = 0; // Set height, if you want the target to move only in the horizontal plane
        target.transform.position = newPosition + transform.parent.position; // Add the position of the parent if the target moves relative to a parent object
    }
}
