using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JetController : MonoBehaviour
{
    public float maxThrust = 1000f;
    [HideInInspector]
    public float currentThrust;
    public float rotationSpeed = 100f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing from the GameObject.");
            return;
        }
        rb.mass = 5.0f;
        rb.drag = 0.5f;
        rb.angularDrag = 2.0f;
        rb.useGravity = false;
        currentThrust = maxThrust;
    }
    public Vector3 CurrentVelocity
    {
        get { return rb.velocity; }
    }
    public void ResetVelocity() 
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    private void FixedUpdate()
    {
        currentThrust = maxThrust * 0.5f;
        rb.AddForce(transform.forward * currentThrust * Time.fixedDeltaTime);
    }

    public void Turn(float horizontalInput, float verticalInput)
    {
        Debug.Log("Turning: Horizontal = " + horizontalInput + ", Vertical = " + verticalInput);
        rb.AddTorque(transform.up * horizontalInput * rotationSpeed * Time.deltaTime);
        rb.AddTorque(transform.right * verticalInput * rotationSpeed * Time.deltaTime);
    }

    public void AdjustThrust(float thrustAdjustment)
    {
        Debug.Log("Adjusting Thrust: " + thrustAdjustment);
        currentThrust = Mathf.Clamp(currentThrust + (thrustAdjustment * maxThrust), 0, maxThrust);
    }
}
