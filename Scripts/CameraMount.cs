using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMount : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Offset from the head position")]
    public Vector3 cameraOffset = new Vector3(0, 0.65f, 0); // Adjust these values to match your desired camera position

    [Tooltip("How much of the forward/backward movement to keep (0-1)")]
    [Range(0f, 1f)]
    public float forwardMovement = 1f;

    private Transform headBone;
    private Vector3 initialLocalPosition;

    void Start()
    {
        // Find the head bone in the character's armature
        headBone = transform.parent.GetComponentInChildren<Animator>()
            .GetBoneTransform(HumanBodyBones.Head);

        if (headBone == null)
        {
            Debug.LogError("Head bone not found!");
            return;
        }

        initialLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (headBone == null) return;

        // Get the head's forward direction
        Vector3 headForward = headBone.forward;
        
        // Calculate the desired position
        Vector3 targetPosition = headBone.position + cameraOffset;
        
        // Only keep the forward/backward movement relative to the initial position
        Vector3 currentPos = transform.position;
        float forwardDelta = Vector3.Dot(headForward, targetPosition - currentPos);
        
        Vector3 newPosition = transform.position;
        if (forwardMovement > 0)
        {
            newPosition += headForward * (forwardDelta * forwardMovement);
        }

        // Update position while maintaining the original height
        transform.position = new Vector3(
            newPosition.x,
            headBone.position.y + cameraOffset.y,
            newPosition.z
        );

        // Make the camera look forward
        transform.rotation = Quaternion.Euler(0, headBone.rotation.eulerAngles.y, 0);
    }
}

