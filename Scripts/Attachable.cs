using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attachable : MonoBehaviour
{
    public AttachmentState currentState = AttachmentState.Fixed;
    public string requiredToolType = "Wrench"; // Tool type needed to loosen/tighten
    public string attachmentType = "CarDoor"; // What type of part this is
    
    [SerializeField] private GameObject previewPrefab; // Semi-transparent version of the object
    private GameObject activePreview;
    private AttachmentPoint nearestPoint;

    public bool TryInteract(string toolType, bool isEmptyHand)
    {
        switch (currentState)
        {
            case AttachmentState.Fixed:
                if (toolType == requiredToolType)
                {
                    currentState = AttachmentState.Loose;
                    return true;
                }
                return false;

            case AttachmentState.Loose:
                if (isEmptyHand)
                {
                    currentState = AttachmentState.Detached;
                    return true; // Allow pickup
                }
                else if (toolType == requiredToolType)
                {
                    currentState = AttachmentState.Fixed;
                    return true;
                }
                return false;

            case AttachmentState.Detached:
                return true; // Always allow interaction when detached
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState != AttachmentState.Detached) return;

        var attachPoint = other.GetComponent<AttachmentPoint>();
        if (attachPoint && attachPoint.acceptedType == attachmentType)
        {
            nearestPoint = attachPoint;
            ShowPreview(attachPoint);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var attachPoint = other.GetComponent<AttachmentPoint>();
        if (attachPoint == nearestPoint)
        {
            HidePreview();
            nearestPoint = null;
        }
    }

    private void ShowPreview(AttachmentPoint point)
    {
        if (activePreview == null)
        {
            activePreview = Instantiate(previewPrefab, point.transform.position, point.transform.rotation);
            // Set semi-transparent material
            var renderers = activePreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.materials;
                foreach (Material mat in materials)
                {
                    Color color = mat.color;
                    color.a = 0.5f;
                    mat.color = color;
                }
            }
        }
    }

    private void HidePreview()
    {
        if (activePreview != null)
        {
            Destroy(activePreview);
            activePreview = null;
        }
    }

    public bool TrySnap()
    {
        if (nearestPoint != null)
        {
            transform.position = nearestPoint.transform.position;
            transform.rotation = nearestPoint.transform.rotation;
            currentState = AttachmentState.Loose;
            return true;
        }
        return false;
    }
}

