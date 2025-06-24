using UnityEngine;
using System.Collections.Generic;

public class AssemblyRoot : MonoBehaviour
{
    [Header("Assembly Properties")]
    public bool isRootItem = true;
    public float mass = 1000f;  // Mass in kg for physics calculations
    public bool canBePushed = true;
    public float pushForce = 5f;

    private Rigidbody rb;
    private List<AttachmentPoint> attachmentPoints;
    private bool isBeingPushed = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null && isRootItem)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.drag = 1f;
            rb.angularDrag = 0.5f;
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        // Collect all attachment points
        attachmentPoints = new List<AttachmentPoint>(GetComponentsInChildren<AttachmentPoint>());
    }

    public void StartPush(Vector3 direction)
    {
        if (!canBePushed || rb == null) return;
        
        isBeingPushed = true;
        rb.AddForce(direction * pushForce, ForceMode.Impulse);
    }

    public void StopPush()
    {
        isBeingPushed = false;
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void HandleWheelAttachment(Transform wheelTransform)
    {
        // When a wheel is attached, lift the chassis at that corner
        if (!isRootItem) return;

        // Raycast down from the wheel position to find ground
        RaycastHit hit;
        if (Physics.Raycast(wheelTransform.position, Vector3.down, out hit))
        {
            float groundClearance = 0.1f; // Desired clearance above ground
            float liftAmount = hit.distance + groundClearance;
            
            // Apply upward force at the wheel position
            if (rb != null)
            {
                Vector3 liftForce = Vector3.up * (liftAmount * rb.mass * Physics.gravity.magnitude);
                rb.AddForceAtPosition(liftForce, wheelTransform.position, ForceMode.Force);
            }
        }
    }

    public bool IsPartFixed(InteractibleItem part)
    {
        if (part == null) return false;
        return part.currentState == AttachmentState.Fixed;
    }

    public void OnPartAttached(InteractibleItem part)
    {
        if (part.CompareTag("Wheel"))
        {
            HandleWheelAttachment(part.transform);
        }
    }

    public bool CanInteractWithPart(InteractibleItem part)
    {
        // Check if the part is directly attached to this assembly
        return part.transform.parent == transform || 
               part.GetComponentInParent<AssemblyRoot>() == this;
    }
}