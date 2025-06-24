using UnityEngine;

public class InteractivePart : MonoBehaviour
{
    [Header("Interactive Properties")]
    public PartType partType = PartType.Default;
    public float interactionSpeed = 2f;
    public float maxAngle = 90f;        // For doors
    public Vector3 rotationAxis = Vector3.up;
    
    private bool isInteracting = false;
    private float currentAngle = 0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private InteractibleItem item;

    private void Start()
    {
        item = GetComponent<InteractibleItem>();
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (isInteracting && item.currentState == AttachmentState.Fixed)
        {
            HandleInteraction();
        }
    }

    public void StartInteraction()
    {
        if (item.currentState != AttachmentState.Fixed) return;
        
        isInteracting = true;
    }

    public void StopInteraction()
    {
        isInteracting = false;
    }

    private void HandleInteraction()
    {
        switch (partType)
        {
            case PartType.Door:
                HandleDoorInteraction();
                break;
            case PartType.Lever:
                HandleLeverInteraction();
                break;
            case PartType.Button:
                HandleButtonInteraction();
                break;
        }
    }

    private void HandleDoorInteraction()
    {
        // Get mouse movement direction
        float mouseX = Input.GetAxis("Mouse X");
        
        // Update door angle based on mouse movement
        currentAngle = Mathf.Clamp(currentAngle + mouseX * interactionSpeed, 0f, maxAngle);
        
        // Apply rotation
        transform.localRotation = originalRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }

    private void HandleLeverInteraction()
    {
        float mouseY = Input.GetAxis("Mouse Y");
        currentAngle = Mathf.Clamp(currentAngle + mouseY * interactionSpeed, -maxAngle, maxAngle);
        transform.localRotation = originalRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }

    private void HandleButtonInteraction()
    {
        // Simple push/pull interaction
        float mouseY = Input.GetAxis("Mouse Y");
        Vector3 movement = transform.forward * mouseY * interactionSpeed * Time.deltaTime;
        transform.localPosition = Vector3.Lerp(originalPosition, originalPosition + movement, 0.5f);
    }

    public void ResetToOriginal()
    {
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        currentAngle = 0f;
    }
}

public enum PartType
{
    Default,
    Door,
    Lever,
    Button
}