using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AttachmentPoint : MonoBehaviour
{
    [Header("Attachment Settings")]
    public AttachmentType acceptedType; // What type of part can attach here
    public WheelPosition wheelPosition;
    public Vector3 snapPosition; // Local position offset for snapping
    public Vector3 snapRotation; // Local rotation for snapping
    private bool isOccupied = false;
    private InteractibleItem attachedItem;

    private Collider pointCollider;

    [Header("Visual Indicator")]
    public GameObject indicatorObject; //assign a child cube in the inspector
    public Color indicatorColor = new Color(0, 1, 0, 0.3f); //Transparent green

    private void Start()
    {
        pointCollider = GetComponent<Collider>();
        UpdateRaycastInteraction();
        //Set up indicator if assigned
        if (indicatorObject != null)
        {
            SetupIndicator();
        }
    }

    public void UpdateRaycastInteraction()
    {
        if (pointCollider == null) return;
        PlayerInteraction player = FindObjectOfType<PlayerInteraction>();
        if (player != null && player.GetCarriedItem() != null)
        {
            InteractibleItem carriedItem = player.GetCarriedItem().GetComponent<InteractibleItem>();
            //Only block raycasts if the player is NOT holding a matching Item
            bool shouldBlockRaycast = isOccupied || (carriedItem == null || carriedItem.attachmentType != acceptedType);
            pointCollider.enabled = true;
            pointCollider.isTrigger = !shouldBlockRaycast;
        }
        else
        {
            //If the player isn't holding anything, always block raycast if occupied
            pointCollider.isTrigger = !isOccupied;
        }
    }

    public void ShowIndicator(bool show)
    {
        
        if (indicatorObject != null)
        {
            indicatorObject.SetActive(show && !isOccupied);
        }
        

    }

    public void EnableCollider()
    {

        if (pointCollider != null)
        {
            pointCollider.enabled = true;
            UpdateRaycastInteraction();
        }
    }

    private void SetupIndicator()
    {
        Collider indicatorCollider = indicatorObject.GetComponent<Collider>();
        if (indicatorCollider != null)
        {
            Destroy(indicatorCollider);
        }
        //Set up the material for transparency
        Renderer renderer = indicatorObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = indicatorColor;
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
        }
        indicatorObject.SetActive(false);
    }

    public bool TryAttachItem(InteractibleItem item)
    {
        if (!isOccupied && item.attachmentType == acceptedType)
        {
            isOccupied = true;
            attachedItem = item;
            //Hide the indicator when occupied
            ShowIndicator(false);
            if(pointCollider != null)
            {
                pointCollider.enabled = false;
            }
            //Update raycast interaction
            UpdateRaycastInteraction();
            return true;
        }
        return false;
    }

    public void DetachItem()
    {
        isOccupied = false;
        attachedItem = null;
        EnableCollider();

        UpdateRaycastInteraction();
    }

    public bool IsOccupied()
    {
        return isOccupied;
    }

    public WheelPosition GetWheelPosition()
    {
        if(acceptedType != AttachmentType.Wheel)
        {
            Debug.LogError("Attempting to get wheel position from non-wheel object");
            return WheelPosition.FrontLeft; 
        }
        return wheelPosition;
    }



}

public enum AttachmentType
{
    None,
    Engine,
    Wheel,
    Battery,
    FuelTank,
    Radiator,
    CarDoorFront,
    CarDoorRear,
    Hood,
    Trunk,
    FrontFenderLeft,
    FrontFenderRight
    }

public enum WheelPosition
    {
        FrontLeft,
        FrontRight,
        RearLeft,
        RearRight
    }
