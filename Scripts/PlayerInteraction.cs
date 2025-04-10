using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public ActionBarUI actionBarUI;

    [Header("Interaction Settings")]
    public float interactRange = 3f;
    public Transform carryPosition;
    public Transform toolCarryPosition;

    [Header("Inventory Settings")]
    public int actionBarSize = 4;
    private List<GameObject> actionBar = new List<GameObject>();
    private GameObject carriedItem = null;
    private int selectedSlot = 0;

    [Header("Action UI")]
    public ActionProgressUI actionProgressUI;
    
    private UseableObject currentUseableObject;
    private bool isPerformingAction;

    private float dropCooldown = 0.5f; // Adjust this value to change the cooldown time
    private float lastDropTime;

    [Header("Attachment System")]
    private AttachmentPoint currentAttachmentPoint;
    private GameObject activePreview;
    private bool isNearAttachmentPoint;

    void Start()
    {
        actionBarUI = FindObjectOfType<ActionBarUI>();
        if (actionBarUI == null)
        {
            Debug.LogError("ActionBarUI not found in scene!");
            return;
        }

        Debug.Log($"ActionBarUI found: {actionBarUI.name}");
        
        actionBar = new List<GameObject>(actionBarSize);
        for (int i = 0; i < actionBarSize; i++)
        {
            actionBar.Add(null);
        }
        
        Debug.Log($"Action bar initialized with {actionBar.Count} slots");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
        HandleActionBarInput();
        if (Input.GetMouseButton(0)) // Left mouse button held
        {
            TryUseToolOnObject();
        }
        else if (isPerformingAction)
        {
            CancelAction();
        }

        UpdateActionProgress();
        UpdateAttachmentPreview();
    }

    void TryUseToolOnObject()
    {
        if (carriedItem == null) return;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, interactRange))
        {
            UseableObject useableObject = hit.collider.GetComponent<UseableObject>();
            InteractibleItem targetItem = hit.collider.GetComponent<InteractibleItem>();
            InteractibleItem tool = carriedItem.GetComponent<InteractibleItem>();

            if (useableObject != null && targetItem != null && tool != null && tool.isTool)
            {
                Debug.Log($"Found item: {targetItem.name} in state: {targetItem.currentState}");
                
                // First check if the tool type matches
                if (!useableObject.CanBeUsedWith(tool.toolType))
                {
                    Debug.Log("Wrong tool type");
                    return;
                }

                // Then check the state
                switch (targetItem.currentState)
                {
                    case AttachmentState.Loose:
                        Debug.Log("Starting tightening action");
                        if (!isPerformingAction)
                        {
                            StartAction(useableObject);
                        }
                        break;

                    case AttachmentState.Fixed:
                        Debug.Log("Starting loosening action");
                        if (!isPerformingAction)
                        {
                            StartAction(useableObject);
                        }
                        break;

                    case AttachmentState.Detached:
                        Debug.Log("Cannot use tools on detached items");
                        break;
                }
            }
        }
        else if (isPerformingAction)
        {
            CancelAction();
        }
    }


    void StartAction(UseableObject useableObject)
    {
        InteractibleItem targetItem = useableObject.GetComponent<InteractibleItem>();
        if (targetItem != null)
        {
            // Only allow action to start if item is in correct state
            if (targetItem.currentState == AttachmentState.Detached)
            {
                Debug.Log("Cannot perform action on detached item");
                return;
            }
        }

        currentUseableObject = useableObject;
        isPerformingAction = true;
        useableObject.Use(carriedItem.GetComponent<InteractibleItem>().toolType);
        actionProgressUI.ShowProgress(0f);
    }

    void CancelAction()
    {
        isPerformingAction = false;
        currentUseableObject = null;
        actionProgressUI.HideProgress();
    }

    void UpdateActionProgress()
    {
        if (isPerformingAction && currentUseableObject != null)
        {
            currentUseableObject.UpdateProgress(Time.deltaTime);
            actionProgressUI.ShowProgress(currentUseableObject.GetProgress());
        }
    }

    void TryInteract()
{
    RaycastHit hit;
    // Check if we're using a tool on a loose/fixed item
    if (carriedItem != null)
    {
        if (carriedItem.GetComponent<InteractibleItem>()?.isTool == true)
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, interactRange))
            {
                InteractibleItem targetItem = hit.collider.GetComponent<InteractibleItem>();
                if (targetItem != null)
                {
                    InteractibleItem tool = carriedItem.GetComponent<InteractibleItem>();
                    
                    // Try to tighten if loose
                    if (targetItem.currentState == AttachmentState.Loose)
                    {
                        if (targetItem.TryTighten(tool.toolType))
                        {
                            Debug.Log($"Tightened {targetItem.gameObject.name}");
                            return;
                        }
                    }
                    // Try to loosen if fixed
                    else if (targetItem.currentState == AttachmentState.Fixed)
                    {
                        if (targetItem.TryLoosen(tool.toolType))
                        {
                            Debug.Log($"Loosened {targetItem.gameObject.name}");
                            return;
                        }
                    }
                }
            }
        }
    }
if (Time.time - lastDropTime < dropCooldown) return;

    if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, interactRange))
    {
        InteractibleItem item = hit.collider.GetComponent<InteractibleItem>();
        if (item != null)
        {
            // First check if the item can be interacted with
            if (!item.canBePickedUp)
            {
                if (item.isPushable)
                {
                    Vector3 pushDirection = Camera.main.transform.forward;
                    item.Push(pushDirection);
                }
                return;
            }

            // If the item has no attachment type or is detached, treat it as a regular pickup
            if (string.IsNullOrEmpty(item.attachmentType) || item.currentState == AttachmentState.Detached)
            {
                HandleItemPickup(item);
                return;
            }

            // If the item is loose, handle it directly
            if (item.currentState == AttachmentState.Loose)
            {
                Debug.Log($"Found loose item {item.name}, detaching it");
                // Find and re-enable the attachment point's collider
                AttachmentPoint[] points = FindObjectsOfType<AttachmentPoint>();
                foreach (var point in points)
                {
                    if (Vector3.Distance(point.transform.position, item.transform.position) < 0.1f)
                    {
                        point.DetachItem();
                        point.EnableCollider();
                        break;
                    }
                }
                
                item.currentState = AttachmentState.Detached;
                HandleItemPickup(item);
                return;
            }

            // Only check for assembly if the item is fixed
            if (item.currentState == AttachmentState.Fixed && IsPartOfAssembledItem(item))
            {
                Transform root = item.transform.root;
                HandleAssemblyPickup(root.gameObject);
                return;
            }

            // Check state before allowing pickup
            if (item.currentState == AttachmentState.Fixed)
            {
                Debug.Log("Cannot pick up fixed item - must be loosened first");
                return;
            }
        }

        // Check for attachment points
        AttachmentPoint attachmentPoint = hit.collider.GetComponent<AttachmentPoint>();
        if (attachmentPoint != null && carriedItem != null)
        {
            InteractibleItem carriedInteractible = carriedItem.GetComponent<InteractibleItem>();
            if (carriedInteractible != null && carriedInteractible.attachmentType == attachmentPoint.acceptedType)
            {
                AttachItemToPoint(carriedInteractible, attachmentPoint);
                return;
            }
        }
    }

    // If we hit nothing and are carrying an item, drop it
    if (carriedItem != null)
    {
        DropCarriedItem();
    }
}

    private void AttachItemToPoint(InteractibleItem item, AttachmentPoint point)
    {
        // Check if point is already occupied
        if (point.IsOccupied())
        {
            Debug.Log("This attachment point is already occupied!");
            DropCarriedItem();
            return;
        }

        // Try to attach to the point
        if (!point.TryAttachItem(item))
        {
            Debug.Log("Failed to attach item to point!");
            return;
        }

        Debug.Log($"Attaching item {item.name} to point {point.name}");
        
        // Get the parent object that owns the attachment point
        Transform attachmentParent = point.transform.parent;
        
        // Store the parent's world position and rotation
        Vector3 parentWorldPos = attachmentParent.position;
        Quaternion parentWorldRot = attachmentParent.rotation;
        
        // Handle parent's Rigidbody if it exists
        Rigidbody parentRb = attachmentParent.GetComponent<Rigidbody>();
        if (parentRb != null)
        {
            parentRb.isKinematic = true;
            parentRb.useGravity = false;
        }
        
        // Handle item's Rigidbody - disable or destroy it since it's becoming part of the parent
        Rigidbody itemRb = item.GetComponent<Rigidbody>();
        if (itemRb != null)
        {
            Destroy(itemRb); // Remove the child's Rigidbody completely
        }
        
        // Set position and rotation first
        item.transform.position = point.transform.position + point.snapPosition;
        item.transform.rotation = point.transform.rotation * Quaternion.Euler(point.snapRotation);
        
        // Then parent
        item.transform.parent = attachmentParent;
        
        // Restore parent's position and rotation
        attachmentParent.position = parentWorldPos;
        attachmentParent.rotation = parentWorldRot;
        
        // Set state to Loose when attaching
        item.currentState = AttachmentState.Loose;
        
        // Handle attachment point collider
        Collider attachPointCollider = point.GetComponent<Collider>();
        if (attachPointCollider != null)
        {
            attachPointCollider.enabled = false;
        }
        
        // Re-enable the item's colliders
        Collider[] itemColliders = item.GetComponentsInChildren<Collider>();
        foreach (Collider col in itemColliders)
        {
            col.enabled = true;
        }

        // Clear carried item reference
        carriedItem = null;
        DestroyPreview();
        
        // If parent had a non-kinematic Rigidbody, restore its settings
        if (parentRb != null)
        {
            parentRb.isKinematic = false;
            parentRb.useGravity = true;
            parentRb.velocity = Vector3.zero;
            parentRb.angularVelocity = Vector3.zero;
        }
    }



    private void DestroyPreview()
    {
        if (activePreview != null)
        {
            Destroy(activePreview);
            activePreview = null;
        }
    }

    private void UpdateAttachmentPreview()
    {
        if (carriedItem == null)
        {
            DestroyPreview();
            return;
        }

        InteractibleItem item = carriedItem.GetComponent<InteractibleItem>();
        if (item == null || item.previewPrefab == null) return;

        // Cast a sphere to find nearby attachment points
        Collider[] hitColliders = Physics.OverlapSphere(carriedItem.transform.position, 2f);
        AttachmentPoint nearestPoint = null;
        float nearestDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            AttachmentPoint point = hitCollider.GetComponent<AttachmentPoint>();
            if (point != null && point.acceptedType == item.attachmentType)
            {
                float distance = Vector3.Distance(carriedItem.transform.position, point.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPoint = point;
                }
            }
        }

        if (nearestPoint != null && nearestDistance < 2f)
        {
            isNearAttachmentPoint = true;
            currentAttachmentPoint = nearestPoint;
            
            // Show or update preview
            if (activePreview == null)
            {
                activePreview = Instantiate(item.previewPrefab, 
                    nearestPoint.transform.position + nearestPoint.snapPosition,
                    nearestPoint.transform.rotation * Quaternion.Euler(nearestPoint.snapRotation));
            }
            else
            {
                activePreview.transform.position = nearestPoint.transform.position + nearestPoint.snapPosition;
                activePreview.transform.rotation = nearestPoint.transform.rotation * Quaternion.Euler(nearestPoint.snapRotation);
            }
        }
        else
        {
            isNearAttachmentPoint = false;
            currentAttachmentPoint = null;
            DestroyPreview();
        }
    }



    void HandleItemPickup(InteractibleItem item)
    {
        if (carriedItem == null) // Player is not holding anything
        {
            // Always pick up the item first
            CarryItem(item);

            // If it can be stored, also add it to the action bar
            if (item.canBeStored)
            {
                int selectedSlot = actionBarUI.GetSelectedSlot();
                if (selectedSlot >= 0 && selectedSlot < actionBarSize)
                {
                    StoreInActionBar(item, selectedSlot);
                }
            }
        }
        else
        {
            Debug.Log("Drop current item before picking up another.");
        }
    }

    void CarryItem(InteractibleItem item)
    {
        if (item == null) return;

        carriedItem = item.gameObject;
        
        if (item.isTool)
        {
            // Original tool carrying behavior
            carriedItem.transform.SetParent(toolCarryPosition);
            carriedItem.transform.localPosition = Vector3.zero;
            carriedItem.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // New behavior for regular items - maintain world position/rotation
            Transform targetCarryPoint = carryPosition;
            
            // Store the world position and rotation before parenting
            Vector3 worldPosition = carriedItem.transform.position;
            Quaternion worldRotation = carriedItem.transform.rotation;

            // Parent to carry point
            carriedItem.transform.SetParent(targetCarryPoint);
            
            // Restore world position and rotation
            carriedItem.transform.position = worldPosition;
            carriedItem.transform.rotation = worldRotation;
        }
        
        // Disable physics while carried
        Rigidbody rb = carriedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Disable colliders while carried
        Collider[] colliders = carriedItem.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }


private void HandleAssemblyPickup(GameObject assembly)
{
    Debug.Log("Starting HandleAssemblyPickup");
    
    // Before picking up, check for and detach any loose parts
    InteractibleItem[] allParts = assembly.GetComponentsInChildren<InteractibleItem>();
    Debug.Log($"Found {allParts.Length} parts in assembly");
    
    List<GameObject> partsToDetach = new List<GameObject>();
    
    foreach (InteractibleItem part in allParts)
    {
        Debug.Log($"Checking part {part.name} with state {part.currentState}");
        if (part.currentState == AttachmentState.Loose)
        {
            Debug.Log($"Adding {part.name} to detach list");
            partsToDetach.Add(part.gameObject);
        }
    }

    Debug.Log($"Found {partsToDetach.Count} loose parts to detach");

    // Detach all loose parts before picking up the assembly
    foreach (GameObject partToDetach in partsToDetach)
    {
        InteractibleItem detachingPart = partToDetach.GetComponent<InteractibleItem>();
        if (detachingPart != null)
        {
            Debug.Log($"Processing detachment for {detachingPart.name}");
            
            // Find and re-enable the attachment point's collider
            AttachmentPoint[] points = FindObjectsOfType<AttachmentPoint>();
            foreach (var point in points)
            {
                if (Vector3.Distance(point.transform.position, partToDetach.transform.position) < 0.1f)
                {
                    Debug.Log($"Found matching attachment point for {detachingPart.name}");
                    point.DetachItem();
                    point.EnableCollider();
                    break;
                }
            }

            // Unparent before adding physics components
            partToDetach.transform.SetParent(null);
            
            // Add Rigidbody for physics if it doesn't exist
            if (!partToDetach.GetComponent<Rigidbody>())
            {
                Rigidbody rb = partToDetach.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            
            detachingPart.currentState = AttachmentState.Detached;
            Debug.Log($"Set {detachingPart.name} state to Detached");
            
            // Re-enable colliders
            Collider[] colliders = partToDetach.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }
        }
    }

    // Store the world position and rotation before parenting
    Vector3 worldPosition = assembly.transform.position;
    Quaternion worldRotation = assembly.transform.rotation;

    // Remove any existing Rigidbodies from the remaining assembly parts
    Rigidbody[] rigidbodies = assembly.GetComponentsInChildren<Rigidbody>();
    foreach (Rigidbody rb in rigidbodies)
    {
        Destroy(rb);
    }

    // Keep all colliders enabled for the assembled object
    carriedItem = assembly;
    carriedItem.transform.SetParent(carryPosition);
    
    // Restore world position and rotation
    carriedItem.transform.position = worldPosition;
    carriedItem.transform.rotation = worldRotation;
}
    void StoreInActionBar(InteractibleItem item, int targetSlot)
    {
        if (actionBarUI == null)
        {
            Debug.LogError("ActionBar UI reference is missing!");
            return;
        }

        if (targetSlot < 0 || targetSlot >= actionBarSize)
        {
            Debug.LogError($"Invalid slot index: {targetSlot}");
            return;
        }

        // Check if the target slot is already occupied
        if (actionBar[targetSlot] != null)
        {
            Debug.Log("Selected slot is already occupied!");
            return;
        }

        // Store the item in the selected slot
        actionBar[targetSlot] = item.gameObject;
        
        // Don't deactivate the object if it's being carried
        if (item.gameObject != carriedItem)
        {
            item.gameObject.SetActive(false);
        }
        
        Debug.Log($"Stored {item.gameObject.name} in action bar slot {targetSlot}");
        actionBarUI.UpdateActionBarUI(actionBar);
    }

    void HandleActionBarInput()
    {
        int previousSlot = actionBarUI.GetSelectedSlot();

        // Handle number keys 1-4
        if (Input.GetKeyDown(KeyCode.Alpha1)) actionBarUI.ChangeSelectedSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) actionBarUI.ChangeSelectedSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) actionBarUI.ChangeSelectedSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) actionBarUI.ChangeSelectedSlot(3);

        // Handle scroll wheel
        float scrollDelta = Input.mouseScrollDelta.y;
        if (scrollDelta > 0)
        {
            int newSlot = (actionBarUI.GetSelectedSlot() - 1 + actionBarSize) % actionBarSize;
            actionBarUI.ChangeSelectedSlot(newSlot);
        }
        else if (scrollDelta < 0)
        {
            int newSlot = (actionBarUI.GetSelectedSlot() + 1) % actionBarSize;
            actionBarUI.ChangeSelectedSlot(newSlot);
        }

        // If the slot changed, handle visibility of carried item
        if (previousSlot != actionBarUI.GetSelectedSlot())
        {
            // Check if currently holding a non-storable item
            if (carriedItem != null)
            {
                InteractibleItem item = carriedItem.GetComponent<InteractibleItem>();
                if (item != null && !item.canBeStored)
                {
                    DropCarriedItem();
                }
            }
            UpdateCarriedItemVisibility();
        }
    }

    void UpdateCarriedItemVisibility()
    {
        int currentSlot = actionBarUI.GetSelectedSlot();
        
        // Hide currently carried item
        if (carriedItem != null)
        {
            // Only deactivate if the item is in the action bar
            if (actionBar.Contains(carriedItem))
            {
                carriedItem.SetActive(false);
            }
            carriedItem = null;
        }

        // Show item from current slot if it exists
        if (currentSlot < actionBar.Count && actionBar[currentSlot] != null)
        {
            GameObject slotItem = actionBar[currentSlot];
            // Only activate and carry if the item still exists in the world
            if (slotItem != null)
            {
                slotItem.SetActive(true);
                CarryItem(slotItem.GetComponent<InteractibleItem>());
            }
            else
            {
                // If the item no longer exists, remove it from the action bar
                actionBar[currentSlot] = null;
                actionBarUI.UpdateActionBarUI(actionBar);
            }
        }
    }
private bool IsPartOfAssembledItem(InteractibleItem item)
{
    // Get the root parent
    Transform root = item.transform.root;
    Debug.Log($"Checking if {item.name} is part of assembly. Root: {root.name}");
    
    // Get all InteractibleItems that are part of this assembly
    InteractibleItem[] allParts = root.GetComponentsInChildren<InteractibleItem>();
    Debug.Log($"Found {allParts.Length} parts in potential assembly");
    
    // Check if there are multiple parts
    if (allParts.Length <= 1)
    {
        Debug.Log("Not an assembly - only one or zero parts found");
        return false;
    }

    // Log the state of each part
    foreach (InteractibleItem part in allParts)
    {
        Debug.Log($"Part {part.name} is in state: {part.currentState}");
    }
    
    return true; // If we got here, it's part of an assembly
}


    void SelectNextSlot()
    {
        selectedSlot = (selectedSlot + 1) % actionBar.Count;
        actionBarUI.ChangeSelectedSlot(selectedSlot);
    }

    void SelectPreviousSlot()
    {
        selectedSlot = (selectedSlot - 1 + actionBar.Count) % actionBar.Count;
        actionBarUI.ChangeSelectedSlot(selectedSlot);
    }

    void SelectSlot(int slot)
    {
        if (slot < actionBar.Count)
        {
            selectedSlot = slot;
            actionBarUI.ChangeSelectedSlot(selectedSlot);
        }
    }

    void UseSelectedItem()
    {
        if (selectedSlot < actionBar.Count)
        {
            GameObject item = actionBar[selectedSlot];
            actionBar.RemoveAt(selectedSlot);
            item.SetActive(true);
            CarryItem(item.GetComponent<InteractibleItem>());
            actionBarUI.UpdateActionBarUI(actionBar);
        }
    }

public void DropCarriedItem()
{
    if (carriedItem == null) return;

    // Get all InteractibleItems in the assembly
    InteractibleItem[] items = carriedItem.GetComponentsInChildren<InteractibleItem>();
    bool isAssembly = items.Length > 1;

    if (isAssembly)
    {
        // Make sure we have a Rigidbody on the root object
        Rigidbody rootRb = carriedItem.GetComponent<Rigidbody>();
        if (rootRb == null)
        {
            rootRb = carriedItem.AddComponent<Rigidbody>();
        }
        rootRb.isKinematic = false;
        rootRb.useGravity = true;
        rootRb.mass = 10f; // Adjust mass as needed
        rootRb.drag = 1f;
        rootRb.angularDrag = 0.5f;
        
        if (Camera.main != null)
        {
            rootRb.velocity = Camera.main.transform.forward * 2f;
        }
    }
    else
    {
        // Single item handling
        Rigidbody rb = carriedItem.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = carriedItem.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;
        
        if (Camera.main != null)
        {
            rb.velocity = Camera.main.transform.forward * 2f;
        }
    }

    // Re-enable colliders
    Collider[] colliders = carriedItem.GetComponentsInChildren<Collider>();
    foreach (Collider col in colliders)
    {
        col.enabled = true;
    }

    carriedItem.transform.SetParent(null);

    // Remove from action bar if it's there
    int currentSlot = actionBarUI.GetSelectedSlot();
    if (currentSlot < actionBar.Count && actionBar[currentSlot] == carriedItem)
    {
        actionBar[currentSlot] = null;
        actionBarUI.UpdateActionBarUI(actionBar);
    }

    lastDropTime = Time.time;
    carriedItem = null;
}

    void UseActionBarItem(int index)
    {
        if (index < actionBar.Count)
        {
            GameObject item = actionBar[index];
            actionBar.RemoveAt(index);
            item.SetActive(true);
            CarryItem(item.GetComponent<InteractibleItem>());
            actionBarUI.UpdateActionBarUI(actionBar); // Update UI after removing item
        }
    }

}
