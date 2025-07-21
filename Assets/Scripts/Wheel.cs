using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour, IVehiclePart
{
    public WheelPosition Position { get; private set; }
    public float Durability { get; set; } = 100f;
    public bool IsFunction => Durability > 20f;

    [SerializeField] private WheelCollider wheelCollider;
    [SerializeField] private Transform wheelMesh;
    
    private float gripMultiplier => Mathf.Lerp(0.2f, 1f, Durability / 100f);

    public void Initialize(WheelPosition position)
    {
        Position = position;
        UpdateWheelFriction();
    }

    private void UpdateWheelFriction()
    {
        WheelFrictionCurve fwdFriction = wheelCollider.forwardFriction;
        WheelFrictionCurve sideFriction = wheelCollider.sidewaysFriction;
        
        fwdFriction.stiffness *= gripMultiplier;
        sideFriction.stiffness *= gripMultiplier;
        
        wheelCollider.forwardFriction = fwdFriction;
        wheelCollider.sidewaysFriction = sideFriction;
    }

    public void UpdateWheelVisuals()
    {
        if (wheelCollider && wheelMesh)
        {
            Vector3 position;
            Quaternion rotation;
            wheelCollider.GetWorldPose(out position, out rotation);
            wheelMesh.position = position;
            wheelMesh.rotation = rotation;
        }
    }
}

