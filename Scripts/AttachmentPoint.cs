using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachmentPoint : MonoBehaviour
{
    public string acceptedType; // What type of part can attach here
    public Vector3 snapPosition; // Local position offset for snapping
    public Vector3 snapRotation; // Local rotation for snapping
    private bool isOccupied = false;
    private InteractibleItem attachedItem;

    public void EnableCollider()
    {
        if (!isOccupied)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }

    public bool TryAttachItem(InteractibleItem item)
    {
        if (!isOccupied && item.attachmentType == acceptedType)
        {
            isOccupied = true;
            attachedItem = item;
            return true;
        }
        return false;
    }

    public void DetachItem()
    {
        isOccupied = false;
        attachedItem = null;
        EnableCollider();
    }

    public bool IsOccupied()
    {
        return isOccupied;
    }
}
