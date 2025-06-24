using UnityEngine;

public class WheelAttachment : InteractibleItem
{
    [Header("Wheel Settings")]
    public float wheelRadius = 0.3f;
    public Transform wheelHub;
    
    protected override void OnStateChanged(AttachmentState oldState, AttachmentState newState)
    {
        base.OnStateChanged(oldState, newState);
        
        // Find the car chassis
        VehicleChassis chassis = GetComponentInParent<VehicleChassis>();
        
        if (chassis != null)
        {
            // If wheel was attached
            if ((oldState == AttachmentState.Detached || oldState == AttachmentState.Loose) && 
                newState == AttachmentState.Fixed)
            {
                chassis.WheelAttached(this);
            }
            // If wheel was detached
            else if (oldState == AttachmentState.Fixed && 
                    (newState == AttachmentState.Detached || newState == AttachmentState.Loose))
            {
                chassis.WheelDetached(this);
            }
        }
    }
}
