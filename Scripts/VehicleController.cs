using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VehicleController : MonoBehaviour
{
    public enum DriveType { FrontWheel, RearWheel, AllWheel }
    
    [SerializeField] private DriveType driveType;
    [SerializeField] private List<AttachmentPoint> chassisPoints;
    [SerializeField] private float maxMotorTorque = 1500f;
    [SerializeField] private float maxBrakeTorque = 2000f;
    
    private Engine attachedEngine;
    private Dictionary<WheelPosition, Wheel> attachedWheels = new Dictionary<WheelPosition, Wheel>();
    private bool isRunning = false;

    private void Update()
    {
        if (isRunning)
        {
            UpdateVehiclePhysics();
        }

        // Update wheel visuals
        foreach (var wheel in attachedWheels.Values)
        {
            wheel.UpdateWheelVisuals();
        }
    }

    public bool HasWorkingWheels(WheelPosition position)
    {
        return attachedWheels.ContainsKey(position) && attachedWheels[position].IsFunction;
    }

    public bool CanStart()
    {
        if (attachedEngine == null || !attachedEngine.IsFunction)
            return false;

        bool hasFrontWheels = HasWorkingWheels(WheelPosition.FrontLeft) && 
                             HasWorkingWheels(WheelPosition.FrontRight);
        bool hasRearWheels = HasWorkingWheels(WheelPosition.RearLeft) && 
                            HasWorkingWheels(WheelPosition.RearRight);

        switch (driveType)
        {
            case DriveType.FrontWheel:
                return hasFrontWheels;
            case DriveType.RearWheel:
                return hasRearWheels;
            case DriveType.AllWheel:
                return hasFrontWheels && hasRearWheels;
            default:
                return false;
        }
    }

    public void StartEngine()
    {
        if (CanStart() && attachedEngine != null)
        {
            attachedEngine.StartEngine();
            isRunning = true;
        }
    }

    private void UpdateVehiclePhysics()
    {
        if (!isRunning) return;

        float stabilityMultiplier = CalculateStabilityMultiplier();
        float motorTorque = maxMotorTorque * Input.GetAxis("Vertical") * stabilityMultiplier;
        float brakeTorque = Input.GetKey(KeyCode.Space) ? maxBrakeTorque : 0f;

        // Apply torque based on drive type
        foreach (var wheel in attachedWheels)
        {
            bool isDriveWheel = false;
            switch (driveType)
            {
                case DriveType.FrontWheel:
                    isDriveWheel = wheel.Key == WheelPosition.FrontLeft || 
                                 wheel.Key == WheelPosition.FrontRight;
                    break;
                case DriveType.RearWheel:
                    isDriveWheel = wheel.Key == WheelPosition.RearLeft || 
                                 wheel.Key == WheelPosition.RearRight;
                    break;
                case DriveType.AllWheel:
                    isDriveWheel = true;
                    break;
            }

            WheelCollider wheelCollider = wheel.Value.GetComponent<WheelCollider>();
            if (wheelCollider != null)
            {
                wheelCollider.motorTorque = isDriveWheel ? motorTorque : 0f;
                wheelCollider.brakeTorque = brakeTorque;
            }
        }
    }

    private float CalculateStabilityMultiplier()
    {
        float stability = 1f;
        foreach (var wheel in attachedWheels.Values)
        {
            if (wheel.Durability < 50f)
                stability *= 0.75f;
        }
        return stability;
    }

    public void AttachPart(IVehiclePart part, AttachmentPoint point)
    {
        if (part is Engine engine)
        {
            attachedEngine = engine;
        }
        else if (part is Wheel wheel)
        {
            WheelPosition position = point.GetWheelPosition();
            attachedWheels[position] = wheel;
            wheel.Initialize(position);
        }
    }

    public void DetachPart(AttachmentPoint point)
    {
        // Implementation for detaching parts
    }
}
