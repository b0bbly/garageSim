using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HingedItem : InteractibleItem
{
    [Header("Hinge Settings")]
    public Transform hingePoint;
    public float maxOpenAngle = 80f;
    public float minClosedAngle = 0f;
    public Vector3 rotationAxis = Vector3.up;
    public float openCloseThreshold = 10f;
    public float animationSpeed = 3f;

    //state tracking
    private bool isDragging = false;
    public bool isOpen = false;
    private float currentAngle = 0f;
    private float targetAngle = 0f;
    private Vector3 lastMousePosition;

    //Original rotation for reset
    private Vector3 originalPosition;
    private Quaternion originalHingeRotation;
    private Quaternion originalRotation;
    private Vector3 attachmentPosition;
    private Quaternion attachmentRotation;
    private Quaternion initialLocalRotation;

    // Start is called before the first frame update
    private new void Start()
    {
        base.Start();
        // Store original rotation
        originalRotation = transform.localRotation;

        // Create hinge point if not assigned
        if (hingePoint == null)
        {
            GameObject hinge = new GameObject("HingePoint");
            hinge.transform.SetParent(transform.parent);
            hinge.transform.position = transform.position;
            hingePoint = hinge.transform;
            Debug.LogWarning($"No hinge point assigned to {gameObject.name}. Created one at object position.");
        }
    }

    private void Update()
    {
        // Only allow interaction when fixed
        if (currentState != AttachmentState.Fixed)
            return;

        //Smooth movement to target angle
        if (Mathf.Abs(currentAngle - targetAngle) > 0.1f)
        {
            float newAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * animationSpeed);
            float deltaAngle = newAngle - currentAngle;
            if (hingePoint != null)
            {
                transform.RotateAround(
                    hingePoint.position,
                    hingePoint.TransformDirection(rotationAxis),
                    deltaAngle
                );
            }
            currentAngle = newAngle;
        }
    }

    private void OnEnable()
    {
        //If this object does not have a UseableObject component, add one
        UseableObject useableObject = GetComponent<UseableObject>();
        if (useableObject == null)
        {
            useableObject = gameObject.AddComponent<UseableObject>();
            useableObject.actionDuration = 2f;
        }

    }

    public void OnInteract()
    {
        if (currentState == AttachmentState.Fixed)
        {
            isOpen = !isOpen;
            targetAngle = isOpen ? maxOpenAngle : minClosedAngle;
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

    // Override TryTighten to ensure it works with HingedItem
    public override bool TryTighten(ToolType toolType)
    {
        return base.TryTighten(toolType);
    }

    // Override TryLoosen to ensure it works with HingedItem
    public override bool TryLoosen(ToolType toolType)
    {
        // Call the base class method to handle the state change
        return base.TryLoosen(toolType);
    }

    public void ResetPosition()
    {
        if (currentState == AttachmentState.Fixed)
        {
            // Reset the door to its original position and rotation
            transform.position = originalPosition;
            if (hingePoint != null)
            {
                hingePoint.rotation = originalHingeRotation;
            }
        }
    }
    
    public bool IsOpen()
    {
        return isOpen;
    }
}
