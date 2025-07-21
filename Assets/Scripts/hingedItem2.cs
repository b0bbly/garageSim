using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HingedItem2 : InteractibleItem
{
    [Header("Hinge Settings")]
    public float maxOpenAngle = 80f;
    public float minClosedAngle = 0f;
    public float openCloseThreshold = 10f;
    public float dragSpeed = 5f;
    public Vector3 hingeAxis = Vector3.right; // Axis for the hinge joint

    [Header("Physics")]
    public float springForce = 10f;
    public float damperForce = 1f;
    
    private HingeJoint hingeJoint;
    private Rigidbody itemRigidbody;
    private bool isDragging = false;
    private float targetAngle = 0f;
    private Vector3 lastMousePosition;
    private JointSpring jointSpring;
    
    private new void Start()
    {
        base.Start();
        
        // Setup rigidbody
        itemRigidbody = GetComponent<Rigidbody>();
        if (itemRigidbody == null)
        {
            itemRigidbody = gameObject.AddComponent<Rigidbody>();
            itemRigidbody.useGravity = false;
            itemRigidbody.isKinematic = true; // Start kinematic until fixed
        }
        

        
        // Disable joint until fixed
        //hingeJoint.enabled = false;
    }
    
    private void Update()
    {
        // Only allow interaction when fixed
        if (currentState != AttachmentState.Fixed)
            return;
            
        // Handle door dragging
        if (isDragging)
        {
            Debug.Log("HingedItem2: isDragging = " + isDragging);
            if (Input.GetKey(KeyCode.E))
            {
                HandleDragging();
            }
            else
            {
                // Released E key
                isDragging = false;
                lastMousePosition = Vector3.zero;

                // Check if near fully open or closed position for snapping
                float currentAngle = hingeJoint.angle;
                if (Mathf.Abs(currentAngle - maxOpenAngle) < openCloseThreshold)
                {
                    // Snap to fully open
                    jointSpring.targetPosition = maxOpenAngle;
                    hingeJoint.spring = jointSpring;
                }
                else if (Mathf.Abs(currentAngle - minClosedAngle) < openCloseThreshold)
                {
                    // Snap to fully closed
                    jointSpring.targetPosition = minClosedAngle;
                    hingeJoint.spring = jointSpring;
                }
                else
                {
                    // Let gravity affect it
                    jointSpring.targetPosition = minClosedAngle;
                    hingeJoint.spring = jointSpring;
                }
            }
        }
    }
private void HandleDragging()
{
    Vector3 mousePos = Input.mousePosition;

    if (lastMousePosition != Vector3.zero && hingeJoint != null)
    {
        float mouseDelta;
        
        // Choose which mouse axis to use based on hinge axis
        if (hingeAxis == Vector3.right || hingeAxis == Vector3.left)
        {
            // For X-axis hinge, use vertical mouse movement
            mouseDelta = (mousePos.y - lastMousePosition.y) * dragSpeed * 0.5f;
        }
        else
        {
            // For other axes, use horizontal movement
            mouseDelta = (mousePos.x - lastMousePosition.x) * dragSpeed * 0.5f;
        }
        
        // Get the current limits
        JointLimits limits = hingeJoint.limits;
        
        // Update target position for the spring
        float newTarget = Mathf.Clamp(
            targetAngle + mouseDelta,
            limits.min,
            limits.max
        );
        
        // Apply to joint spring
        jointSpring.targetPosition = newTarget;
        hingeJoint.spring = jointSpring;
        
        // Update target angle
        targetAngle = newTarget;
    }
    
    lastMousePosition = mousePos;
}

    
    public void StartDragging()
    {
        Debug.Log($"StartDragging called. Current state: {currentState}, Fixed state: {AttachmentState.Fixed}");
        if (currentState == AttachmentState.Fixed)
        {
            isDragging = true;
            lastMousePosition = Vector3.zero;
            Debug.Log("Started dragging Hinged Item - hingedItem2.cs l131");
        }
    }
    
    public override string GetTooltipText()
    {
        if (currentState == AttachmentState.Fixed)
        {
            return "Hold E to open/close";
        }
        return base.GetTooltipText();
    }
    
public override bool TryTighten(ToolType toolType)
{
    bool result = base.TryTighten(toolType);

    if (result)
    {
        // Store the current rotation as the "closed" position
        float currentRotationAngle = transform.localRotation.eulerAngles[(hingeAxis == Vector3.right || hingeAxis == Vector3.left) ? 0 : 1];
        float adjustedMinAngle = currentRotationAngle;
        float adjustedMaxAngle = currentRotationAngle + (maxOpenAngle - minClosedAngle);
        
        // Enable physics for the hinged item
        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = true; // Keep kinematic to prevent sliding
        }

        // Create and configure the hinge joint
        if (hingeJoint == null)
        {
            hingeJoint = gameObject.AddComponent<HingeJoint>();

            // Configure hinge joint
            hingeJoint.axis = hingeAxis;
            hingeJoint.useSpring = true;
            hingeJoint.enablePreprocessing = false;
            
            // Set the anchor to the hinge point (back edge of hood)
            hingeJoint.anchor = new Vector3(0, 0, -0.5f); // Adjust based on your object
            
            // Configure limits based on current rotation
            JointLimits limits = hingeJoint.limits;
            limits.min = adjustedMinAngle;
            limits.max = adjustedMaxAngle;
            limits.bounciness = 0.1f;
            hingeJoint.limits = limits;
            hingeJoint.useLimits = true;

            // Configure spring
            jointSpring = hingeJoint.spring;
            jointSpring.spring = springForce;
            jointSpring.damper = damperForce;
            jointSpring.targetPosition = adjustedMinAngle;
            hingeJoint.spring = jointSpring;
            
            // Initialize target angle
            targetAngle = adjustedMinAngle;
            
            // Find parent rigidbody
            Rigidbody parentRb = null;
            Transform parent = transform.parent;
            while (parent != null)
            {
                parentRb = parent.GetComponent<Rigidbody>();
                if (parentRb != null) break;
                parent = parent.parent;
            }
            
            // If no parent rigidbody, add one to the parent
            if (parentRb == null && transform.parent != null)
            {
                parentRb = transform.parent.gameObject.AddComponent<Rigidbody>();
                parentRb.isKinematic = true; // Make parent kinematic
            }
            
            // Connect to parent
            if (parentRb != null)
            {
                hingeJoint.connectedBody = parentRb;
            }
        }
    }
    return result;
}
    
    // Override TryLoosen to disable physics when loosened// Override TryLoosen to disable physics when loosened
    public override bool TryLoosen(ToolType toolType)
    {
        bool result = base.TryLoosen(toolType);
        
        if (result)
        {
            // Disable physics when loosened
            if (hingeJoint != null)
            {
                Destroy(hingeJoint);
                hingeJoint = null;
            }
            
            if (itemRigidbody != null)
            {
                itemRigidbody.isKinematic = true;
            }
        }
        
        return result;
    }
}
