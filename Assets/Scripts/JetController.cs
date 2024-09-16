using UnityEngine;

/// <summary>
/// Controls the movement and rotation of the Jet Agent using Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class JetController : MonoBehaviour
{
    // Reference to the Rigidbody component
    private Rigidbody rb;

    // Current thrust value
    public float currentThrust = 0f;

    // Thrust adjustment speed
    public float thrustAdjustmentSpeed = 1f;

    // Maximum thrust
    public float maxThrust = 10f;

    // Rotation speed
    public float rotationSpeed = 100f;

    /// <summary>
    /// Gets the current velocity of the Rigidbody.
    /// </summary>
    public Vector3 CurrentVelocity
    {
        get { return rb.velocity; }
    }

    /// <summary>
    /// Initializes the Rigidbody component.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity if not needed
        rb.drag = 0f; // Adjust drag as necessary
        rb.angularDrag = 0f; // Adjust angular drag as necessary
    }

    /// <summary>
    /// Resets the velocity of the Rigidbody.
    /// </summary>
    public void ResetVelocity()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        currentThrust = 0f;
    }

    /// <summary>
    /// Adjusts the thrust based on input.
    /// </summary>
    /// <param name="thrustChange">Change in thrust (-1, 0, 1).</param>
    public void AdjustThrust(float thrustChange)
    {
        currentThrust += thrustChange * thrustAdjustmentSpeed * Time.fixedDeltaTime;
        currentThrust = Mathf.Clamp(currentThrust, 0f, maxThrust);
        Debug.Log($"AdjustThrust called. Current Thrust: {currentThrust}");
    }

    /// <summary>
    /// Turns the agent based on input.
    /// </summary>
    /// <param name="horizontal">Horizontal input for yaw.</param>
    /// <param name="vertical">Vertical input for pitch.</param>
    public void Turn(float horizontal, float vertical)
    {
        Vector3 rotation = new Vector3(-vertical, horizontal, 0f) * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(rotation));
        Debug.Log($"Turn called. Horizontal: {horizontal}, Vertical: {vertical}");
    }

    /// <summary>
    /// Applies thrust to move the agent forward.
    /// </summary>
    private void FixedUpdate()
    {
        Vector3 thrustForce = transform.forward * currentThrust;
        rb.AddForce(thrustForce, ForceMode.Force);
        Debug.Log($"FixedUpdate called. Applying Thrust: {thrustForce}");
    }
}
