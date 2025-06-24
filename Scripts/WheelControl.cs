using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class WheelControl : MonoBehaviour
{
    public Transform wheelModel;
    [HideInInspector] public WheelCollider wheelCollider;
    public bool steerable;
    public bool motorized;
    
    // Add enum for wheel position to make setup easier
    public enum WheelPosition { FrontLeft, FrontRight, RearLeft, RearRight }
    public WheelPosition wheelPosition;
    
    private float rotationAmount = 0f;
    
    void Start()
    {
        if (wheelCollider == null)
        {
            wheelCollider = GetComponent<WheelCollider>();
        }
        // Set initial correct orientation based on wheel position
        bool isLeftWheel = wheelPosition == WheelPosition.FrontLeft || wheelPosition == WheelPosition.RearLeft;
        if (isLeftWheel)
        {
            wheelModel.localRotation = Quaternion.Euler(0, -90, 90);
        }
        else
        {
            wheelModel.localRotation = Quaternion.Euler(0, 90, 90);
        }
    }
    
    void Update()
    {
        Vector3 position;
        Quaternion rotation;
        
        // Get wheel collider position
        wheelCollider.GetWorldPose(out position, out rotation);
        
        // Update position
        wheelModel.position = position;
        
        // Update rotation amount based on wheel RPM
        rotationAmount -= wheelCollider.rpm * Time.deltaTime * 6;
        
        // Determine if this is a left wheel
        bool isLeftWheel = wheelPosition == WheelPosition.FrontLeft || wheelPosition == WheelPosition.RearLeft;
        
        // Apply steering for steerable wheels
        if (steerable)
        {
            // Set rotation with steering angle
            if (isLeftWheel)
            {
                // For left wheels, rotate around X axis
                wheelModel.localRotation = Quaternion.Euler(0, -90 + wheelCollider.steerAngle, 90 + rotationAmount);
            }
            else
            {
                // For right wheels, rotate around X axis
                wheelModel.localRotation = Quaternion.Euler(0, 90 + wheelCollider.steerAngle, 90 - rotationAmount);
            }
        }
        else
        {
            // Set rotation without steering
            if (isLeftWheel)
            {
                // For left wheels, rotate around X axis
                wheelModel.localRotation = Quaternion.Euler(0, -90, 90 + rotationAmount);
            }
            else
            {
                // For right wheels, rotate around X axis (negative to match direction)
                wheelModel.localRotation = Quaternion.Euler(0, 90, 90 - rotationAmount);
            }
        }
    }
}
