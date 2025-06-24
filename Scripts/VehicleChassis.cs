using System.Collections.Generic;
using UnityEngine;

public class VehicleChassis : InteractibleItem
{
    [Header("Chassis Settings")]
    public float chassisHeight = 0.2f; // Height of chassis from ground when no wheels
    public float wheeledHeight = 0.5f; // Height when wheels are attached
    public Transform[] wheelAttachmentPoints;
    
    private List<WheelAttachment> attachedWheels = new List<WheelAttachment>();
    private bool hadWheelsBefore = false;
    
    private void Start()
    {
        base.Start();
        // Set initial position based on whether we have wheels at start
        UpdateVehicleHeight();
    }
    
    public void WheelAttached(WheelAttachment wheel)
    {
        if (!attachedWheels.Contains(wheel))
        {
            attachedWheels.Add(wheel);
            UpdateVehicleHeight();
        }
    }
    
    public void WheelDetached(WheelAttachment wheel)
    {
        if (attachedWheels.Contains(wheel))
        {
            attachedWheels.Remove(wheel);
            UpdateVehicleHeight();
        }
    }
    
    private void UpdateVehicleHeight()
    {
        bool hasWheels = attachedWheels.Count > 0;
        
        // Only adjust height if wheel state changed
        if (hasWheels != hadWheelsBefore)
        {
            Vector3 position = transform.position;
            
            if (hasWheels)
            {
                // Raise the vehicle to account for wheels
                position.y += wheeledHeight - chassisHeight;
            }
            else
            {
                // Lower the vehicle to rest on chassis
                position.y -= wheeledHeight - chassisHeight;
            }
            
            transform.position = position;
            hadWheelsBefore = hasWheels;
        }
    }
}
