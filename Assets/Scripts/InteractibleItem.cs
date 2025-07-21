using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using GamePersistence;

public class InteractibleItem : PersistentObject
{

    public InteractibleItemType itemType = InteractibleItemType.Standard;
    public bool canBeStored;
    public Sprite itemIcon;

    public string displayName = "";

    public AttachmentState currentState = AttachmentState.Detached;
    public AttachmentType attachmentType = AttachmentType.None; // e.g. "CarDoor", "Tire", etc.
    public GameObject previewPrefab;

    [Header("Visual Feedback")]
    public Color looseColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Darker color for loose state
    private Material[] originalMaterials;
    private Renderer[] renderers;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float detachProgress = 0f;
    private bool isDetaching = false;
    private float detachDuration = 2f;
    private float wiggleAmount = 0.01f;

    [Header("Interaction Settings")]
    [SerializeField] public bool _canBePickedUp = true;

    public bool isPushable = false;
    public float pushForce = 5f;

    protected override void Awake()
    {
        base.Awake();
    }

    protected virtual void Start()
    {
        // Store original materials and renderers
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = new Material(renderers[i].material); // Create copy of original material
        }

        UpdateVisualState();
    }

    protected virtual void OnStateChanged(AttachmentState oldstate, AttachmentState newState)
    {
        //Base Implementation does nothing, but derived classes can override
    }

    public bool canBePickedUp
    {
        get
        {
            // Chassis items can never be picked up
            if (itemType == InteractibleItemType.Chassis)
                return false;

            // If item is Fixed and parent is chassis/non-pickable, can't pick up
            if (currentState == AttachmentState.Fixed)
            {
                Transform parent = transform.parent;
                if (parent != null)
                {
                    InteractibleItem parentItem = parent.GetComponent<InteractibleItem>();
                    if (parentItem != null && !parentItem.canBePickedUp)
                        return false;
                }
            }

            return _canBePickedUp;
        }
        set { _canBePickedUp = value; }
    }

    public virtual bool TryTighten(ToolType toolType)
    {
        if (currentState == AttachmentState.Loose)
        {
            AttachmentState oldState = currentState;
            currentState = AttachmentState.Fixed;
            UpdateVisualState();
            OnStateChanged(oldState, currentState);
            return true;
        }
        return false;
    }

    #region New detaching

    public void StartDetaching()
    {
        if (currentState == AttachmentState.Fixed)
        {
            isDetaching = true;
            detachProgress = 0f;
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
            //Start wiggle animation
            StartCoroutine(WiggleAnimation());
        }
    }

    public void StopDetaching()
    {
        if (isDetaching)
        {
            isDetaching = false;
            detachProgress = 0f;
            //Snap back to original position
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            StopAllCoroutines();
        }
    }

    public bool UpdateDetaching(float deltaTime)
    {
        if (!isDetaching) return false;

        detachProgress += deltaTime / detachDuration;
        if (detachProgress >= 1f)
        {
            //detachment complete
            isDetaching = false;
            detachProgress = 0f;
            AttachmentState oldState = currentState;
            currentState = AttachmentState.Detached;
            OnStateChanged(oldState, currentState);
            return true;
        }
        return false;
    }

    //Wiggle animation coroutine
    private System.Collections.IEnumerator WiggleAnimation()
    {
        while (isDetaching)
        {
            float wiggleX = Mathf.Sin(Time.time * 20) * wiggleAmount;
            float wiggleY = Mathf.Sin(Time.time * 15) * wiggleAmount;

            transform.localPosition = originalPosition + new Vector3(wiggleX, wiggleY, 0);
            yield return null;
        }
    }

    #endregion

    public virtual bool TryLoosen(ToolType toolType)
    {
        if (currentState == AttachmentState.Fixed)
        {
            AttachmentState oldState = currentState;
            currentState = AttachmentState.Loose;
            UpdateVisualState();
            OnStateChanged(oldState, currentState);
            return true;
        }
        return false;
    }


    //TODO: remove UpdateVisualState or replace with something that actually works for me?
    private void UpdateVisualState()
    {
        if (renderers == null || renderers.Length == 0) return;

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            foreach (Material mat in materials)
            {
                if (currentState == AttachmentState.Loose)
                {
                    //mat.color = looseColor;
                }
                else // Fixed or Detached
                {
                    //mat.color = Color.white; // Reset to original color
                }
            }
        }
    }

    public void Push(Vector3 direction)
    {
        if (!isPushable) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(direction * pushForce, ForceMode.Impulse);
        }
    }

    // Optional: Reset materials when destroyed
    private void OnDestroy()
    {
        if (renderers != null && originalMaterials != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && originalMaterials[i] != null)
                {
                    renderers[i].material = originalMaterials[i];
                }
            }
        }
    }

    public void DetachIfLoose()
    {
        Debug.Log("Detaching if loose. DO NOT REMOVE THIS DEBUG! I DON'T KNOW WHY THIS FIXED THE DETACH IF LOOSE BUT IT DOES.");
        if (currentState == AttachmentState.Loose)
        {
            AttachmentState oldState = currentState;
            currentState = AttachmentState.Detached;
            transform.SetParent(null);

            // Enable physics if needed
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = false;

            UpdateVisualState();
            OnStateChanged(oldState, currentState);
        }
    }

    public virtual string GetTooltipText()
    {
        // Default behavior - show displayName only when not fixed
        if (currentState != AttachmentState.Fixed || string.IsNullOrEmpty(displayName))
            return displayName;
        return ""; // Return empty string for fixed items by default
    }
    

     // Override GetObjectState to include InteractibleItem-specific data
    public override ObjectState GetObjectState()
    {
        ObjectState state = base.GetObjectState();
        
        // Add InteractibleItem-specific data
        state.CustomData["currentState"] = (int)currentState;
        state.CustomData["attachmentType"] = (int)attachmentType;
        
        // Check if attached to another object
        if (currentState != AttachmentState.Detached)
        {
            // Find attachment point
            AttachmentPoint[] points = FindObjectsOfType<AttachmentPoint>();
            foreach (var point in points)
            {
                if (Vector3.Distance(point.transform.position, transform.position) < 0.2f)
                {
                    PersistentObject parentObj = point.GetComponentInParent<PersistentObject>();
                    if (parentObj != null)
                    {
                        state.AttachedToId = parentObj.ObjectId;
                    }
                    break;
                }
            }
        }
        
        return state;
    }
    
    // Override ApplyObjectState to handle InteractibleItem-specific data
    public override void ApplyObjectState(ObjectState state)
    {
        base.ApplyObjectState(state);
        
        // Apply InteractibleItem-specific data
        if (state.CustomData.ContainsKey("currentState"))
        {
            currentState = (AttachmentState)state.CustomData["currentState"];
        }
        
        // Handle attachment
        if (!string.IsNullOrEmpty(state.AttachedToId))
        {
            // Find attachment point on parent
            PersistentObject[] objects = FindObjectsOfType<PersistentObject>();
            foreach (var obj in objects)
            {
                if (obj.ObjectId == state.AttachedToId)
                {
                    AttachmentPoint[] points = obj.GetComponentsInChildren<AttachmentPoint>();
                    foreach (var point in points)
                    {
                        if (point.acceptedType == attachmentType && !point.IsOccupied())
                        {
                            // Attach to point
                            point.TryAttachItem(this);
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }
}

public interface IUseable
{
    bool CanBeUsedWith(ToolType toolType);
    void Use(ToolType toolType);
    float GetActionDuration();
}

public enum ToolType
{
    None,
    Wrench,
    Screwdriver,
    FuelNozzle
    // Add other tools as needed
}

public enum AttachmentState
{
    Fixed,
    Loose,
    Detached
}

public enum InteractibleItemType
{
    Standard,  // Regular pickable items
    Chassis,   // Fixed items that can't be picked up
    Attachment // Items that can be attached to other items
}



