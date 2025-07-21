using System.Collections.Generic;
using System.Drawing;
using System.Net.Mail;
using StarterAssets;
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
    private CarSeat currentSeat;

    private InteractibleItem itemBeingDetached = null;
    private float detachTimer = 0f;
    private float detachDuration = 2f;

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
        if (Input.GetKeyDown(KeyCode.F) && carriedItem != null)
        {
            ConsumableItem consumable = carriedItem.GetComponent<ConsumableItem>();
            if (consumable != null && consumable.TryConsume())
            {
                //remove from actionbar if stored
                int currentSlot = actionBarUI.GetSelectedSlot();
                if (currentSlot < actionBar.Count && actionBar[currentSlot] == carriedItem)
                {
                    actionBar[currentSlot] = null;
                    actionBarUI.UpdateActionBarUI(actionBar);
                }
                Destroy(carriedItem);
                carriedItem = null;
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentSeat == null)
            {
                TryInteract();
            }
        }
        if (Input.GetMouseButtonDown(1)) // Right mouse button released
        {
            TryStartDetaching();
        }
        else if (Input.GetMouseButton(1)) // Right mouse button released
        {
            UpdateDetaching();
        }
        else if (Input.GetMouseButtonUp(1)) // Right mouse button released
        {
            CancelDetaching();
        }
        else if (isPerformingAction)
        {
            CancelAction();
        }
        HandleActionBarInput();

        //UpdateActionProgress();
        UpdateAttachmentPreview();
    }

    #region detachment

    void TryStartDetaching()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, interactRange))
        {
            InteractibleItem item = hit.collider.GetComponent<InteractibleItem>();
            if (item != null && item.currentState == AttachmentState.Fixed)
            {
                itemBeingDetached = item;
                detachTimer = 0f;
                item.StartDetaching();

                //Show progress UI
                actionProgressUI.ShowProgress(0f);
            }
        }
    }

    void UpdateDetaching()
    {
        if (itemBeingDetached != null)
        {
            detachTimer += Time.deltaTime;
            float progress = detachTimer / detachDuration;
            actionProgressUI.ShowProgress(progress);

            //Update item detach animation
            bool detachComplete = itemBeingDetached.UpdateDetaching(Time.deltaTime);

            if (detachComplete || progress >= 1.0f)
            {
                DetachAndPickupItem(itemBeingDetached);
                itemBeingDetached = null;
                actionProgressUI.HideProgress();
            }
        }
    }

    void CancelDetaching()
    {
        if (itemBeingDetached != null)
        {
            itemBeingDetached.StopDetaching();
            itemBeingDetached = null;
            actionProgressUI.HideProgress();
        }
    }

    void DetachAndPickupItem(InteractibleItem item)
    {
        //Find and re-enable the attachment points's collider
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
        HandleItemPickup(item);
    }

    #endregion

    /*
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
        */

    /*
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
        */

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

        if (currentSeat != null)
        {
            ExitSeat();
            return;
        }

        //New Block
        //if carrying an item, check for attachment points first using sphere cast
        if (carriedItem != null)
        {
            //find all attachmentpoints within range
            InteractibleItem carriedInteractibleItem = carriedItem.GetComponent<InteractibleItem>();
            if (carriedInteractibleItem != null)
            {
                //find all attachment points within range
                Collider[] hitColliders = Physics.OverlapSphere(carriedItem.transform.position, 2f);
                AttachmentPoint nearestPoint = null;
                float nearestDistance = float.MaxValue;
                foreach (var hitCollider in hitColliders)
                {
                    AttachmentPoint point = hitCollider.GetComponent<AttachmentPoint>();
                    if (point != null && point.acceptedType == carriedInteractibleItem.attachmentType && !point.IsOccupied())
                    {
                        float distance = Vector3.Distance(carriedItem.transform.position, point.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestPoint = point;
                        }
                    }
                }

                //if we found a valid attachment point wihtin range, attach to it
                if (nearestPoint != null && nearestDistance <= 2f)
                {
                    AttachItemToPoint(carriedInteractibleItem, nearestPoint);
                    return;
                }
            }
        }

        //End of new Block
        RaycastHit hit;
        if (Time.time - lastDropTime < dropCooldown) return;

        // First, try to raycast and hit any object (including attachment points with items)
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, interactRange))
        {
            // Check if we hit an interactible item directly
            InteractibleItem item = hit.collider.GetComponent<InteractibleItem>();
            if (item != null)
            {
                // Handle item interaction as before
                HandleItemInteraction(item);
                return;
            }

            // Check for attachment points when carrying an item
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

        // If we didn't hit anything useful with the first raycast, try a second raycast
        // that ignores empty attachment points to see if there's something behind them
        int attachmentPointLayer = LayerMask.NameToLayer("AttachmentPoint");
        if (attachmentPointLayer != -1)
        {
            LayerMask raycastMask = Physics.DefaultRaycastLayers & ~(1 << attachmentPointLayer);
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, interactRange, raycastMask))
            {
                // Check if we hit an interactible item through an empty attachment point
                InteractibleItem item = hit.collider.GetComponent<InteractibleItem>();
                if (item != null)
                {
                    // Handle item interaction as before
                    HandleItemInteraction(item);
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

    // Helper method to handle item interaction logic
    private void HandleItemInteraction(InteractibleItem item)
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
        if (item.currentState == AttachmentState.Detached)
        {
            HandleItemPickup(item);
            return;
        }

        // If the item is loose, handle it directly
        if (item.currentState == AttachmentState.Loose)
        {
            Debug.Log($"Found loose item {item.name}, detaching it");
            // Call the new method to handle detachment
            item.DetachIfLoose();

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
            StartCoroutine(PickupAfterDetach(item));
            return;
        }

        // Only check for assembly if the item is fixed
        if (item.currentState == AttachmentState.Fixed)
        {
            // Check if it's a hinged item
            HingedItem hingedItem = item as HingedItem;
            if (hingedItem != null)
            {
                hingedItem.OnInteract();
                return;
            }

            //Car Seat code
            CarSeat seat = item as CarSeat;
            if (seat != null && !seat.isOccupied)
            {
                //Sit on the seat
                seat.Sit(gameObject);
                currentSeat = seat;
                //Disable player movement controls
                GetComponent<FirstPersonController>().enabled = false;
                return;
            }

            if (IsPartOfAssembledItem(item))
            {
                GameObject root = FindTopmostAssemblyRoot(item.gameObject);
                DetachLooseParts(root);
                HandleAssemblyPickup(root);
                return;
            }
            else
            {
                HandleItemPickup(item);
                return;
            }
        }

        // Check state before allowing pickup
        if (item.currentState == AttachmentState.Fixed)
        {
            Debug.Log("Cannot pick up fixed item - must be loosened first");
        }
    }

    public GameObject GetCarriedItem()
    {
        return carriedItem;
    }

    private void ExitSeat()
    {
        if (currentSeat != null)
        {
            currentSeat.Exit();
            currentSeat = null;
            GetComponent<FirstPersonController>().enabled = true;
            Debug.Log("Exiting seat");
        }
    }

    private void AttachItemToPoint(InteractibleItem item, AttachmentPoint point)
    {
        if (point == null || item == null)
        {
            Debug.LogError("Null point or item passed to AttachItemToPoint!");
            return;
        }
        if (point.IsOccupied())
        {
            Debug.Log("This attachment point is already occupied!");
            DropCarriedItem();
            return;
        }

        if (!point.TryAttachItem(item))
        {
            Debug.Log("Failed to attach item to point!");
            return;
        }

        Debug.Log($"Attaching item {item.name} to point {point.name}");

        Transform attachmentParent = point.transform.parent;
        if (attachmentParent == null)
        {
            string originalName = point.name.Replace("(Clone)", "").Trim();
            Debug.Log($"Looking for original attachment point: {originalName}");
            // Find all attachment points in the scene
            AttachmentPoint[] allPoints = FindObjectsOfType<AttachmentPoint>();
            foreach (var p in allPoints)
            {
                // Skip the current point and clones
                if (p == point || p.name.Contains("(Clone)")) continue;

                // If we find a matching original point, use its parent
                if (p.name == originalName && p.transform.parent != null)
                {
                    attachmentParent = p.transform.parent;
                    Debug.Log($"Found original point parent: {attachmentParent.name}");
                    break;
                }
            }
            // If we still don't have a parent, try to find a car body or main model
            if (attachmentParent == null)
            {
                GameObject carBody = GameObject.Find("CarBody");
                if (carBody != null)
                {
                    attachmentParent = carBody.transform;
                    Debug.Log("Using CarBody as parent");
                }
            }
        }
        Debug.Log($"Using attachment parent: {attachmentParent.name}");
        // First, disable physics
        Rigidbody itemRb = item.GetComponent<Rigidbody>();
        if (itemRb != null)
        {
            Destroy(itemRb);
        }
        try
        {
            // Important: First parent the object, then set local position/rotation
            item.transform.SetParent(attachmentParent);
            Debug.Log($"Item parent set to {attachmentParent.name}");
            // Convert point's position/rotation to local space relative to the new parent
            Vector3 localSnapPosition = attachmentParent.InverseTransformPoint(point.transform.position + point.snapPosition);
            Quaternion localSnapRotation = Quaternion.Inverse(attachmentParent.rotation) *
                                         (point.transform.rotation * Quaternion.Euler(point.snapRotation));

            // Apply local position and rotation
            item.transform.localPosition = localSnapPosition;
            item.transform.localRotation = localSnapRotation;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in attaching item to point: {e.Message}");
            item.transform.SetParent(null);
            point.DetachItem();
            return;
        }
        // Set state and handle colliders
        item.currentState = AttachmentState.Fixed;
        /*
            // Handle attachment point collider
            Collider pointCollider = point.GetComponent<Collider>();
            if (pointCollider != null)
            {
                pointCollider.enabled = false;
            }
        */
        // Re-enable item colliders
        foreach (Collider col in item.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }
        MeshCollider col2 = point.GetComponent<MeshCollider>();
        if (col2 != null)
        {
            col2.enabled = false;
        }

        carriedItem = null;
        DestroyPreview();
    }

    private System.Collections.IEnumerator PickupAfterDetach(InteractibleItem item)
    {
        yield return null; // Wait one frame

        // Now the item should be fully detached
        if (item != null)
        {
            HandleItemPickup(item);
        }
    }

    private System.Collections.IEnumerator DelayedParent(Transform child, Transform parent)
    {
        yield return new WaitForFixedUpdate();
        child.SetParent(parent);
    }

    void DestroyPreview()
    {
        if (activePreview != null)
        {
            Destroy(activePreview);
            activePreview = null;
        }
    }

    private void UpdateAttachmentPreview()
    {
        // Hide all attachment point indicators by default
        AttachmentPoint[] allPoints = FindObjectsOfType<AttachmentPoint>();
        foreach (var point in allPoints)
        {
            point.ShowIndicator(false);
        }

        if (carriedItem == null)
        {
            DestroyPreview();
            return;
        }

        InteractibleItem item = carriedItem.GetComponent<InteractibleItem>();
        if (item == null) return;

        // Cast a sphere to find nearby attachment points
        Collider[] hitColliders = Physics.OverlapSphere(carriedItem.transform.position, 2f);
        AttachmentPoint nearestPoint = null;
        float nearestDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            AttachmentPoint point = hitCollider.GetComponent<AttachmentPoint>();
            if (point != null && point.acceptedType == item.attachmentType && !point.IsOccupied())
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

            // Show the indicator for the nearest point
            nearestPoint.ShowIndicator(true);

            // Show or update preview if using the old preview system
            if (item.previewPrefab != null)
            {
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
        // Detach any loose children from this item’s root before doing anything
        GameObject root = FindTopmostAssemblyRoot(item.gameObject);
        DetachLooseParts(root);
        // If child is Loose but parent is Fixed, detach it first
        if (item.currentState == AttachmentState.Loose && IsPartOfAssembledItem(item))
        {
            item.transform.SetParent(null);
            item.currentState = AttachmentState.Detached;

            if (!item.TryGetComponent<Rigidbody>(out var rb))
            {
                rb = item.gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = false;
            rb.useGravity = true;

            Collider[] colliders = item.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
            }

            foreach (var point in FindObjectsOfType<AttachmentPoint>())
            {
                if (Vector3.Distance(point.transform.position, item.transform.position) < 0.2f)
                {
                    point.DetachItem();
                    point.EnableCollider();
                    break;
                }
            }
        }

        item = FindTopmostAssemblyRoot(item.gameObject).GetComponent<InteractibleItem>();

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

        //if (item.isTool)
        //{
        // Original tool carrying behavior
        //    carriedItem.transform.SetParent(toolCarryPosition);
        //    carriedItem.transform.localPosition = Vector3.zero;
        //    carriedItem.transform.localRotation = Quaternion.identity;
        //}
        //else
        //{
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
        //}

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

        //Update all attachmentPoints to handle raycast blocking
        foreach (var point in FindObjectsOfType<AttachmentPoint>())
        {
            point.UpdateRaycastInteraction();
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
            if (part.itemType == InteractibleItemType.Chassis)
            {
                Debug.Log("Cannot pick up assembly containing chassis items");
                return;
            }
        }

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

        // Check if the root is a chassis item
        InteractibleItem rootItem = root.GetComponent<InteractibleItem>();
        if (rootItem != null && rootItem.itemType == InteractibleItemType.Chassis)
        {
            Debug.Log("Root is a chassis item - cannot pick up assembly");
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
        carriedItem = FindTopmostAssemblyRoot(carriedItem);
        if (carriedItem == null) return;

        // Get all InteractibleItems in the assembly
        InteractibleItem[] items = carriedItem.GetComponentsInChildren<InteractibleItem>();
        bool isAssembly = items.Length > 1;

        bool hasLooseParts = false;
        foreach (var i in items)
        {
            if (i.currentState == AttachmentState.Loose)
            {
                hasLooseParts = true;
                break;
            }
        }

        if (isAssembly)
        {
            Debug.Log("Dropping assembly — detaching loose parts if any");
            DetachLooseParts(carriedItem);
        }

        // Drop logic for fixed-only or single items
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

        // Re-enable colliders
        Collider[] colliders = carriedItem.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }

        carriedItem.transform.SetParent(null);

        // Remove from action bar
        int currentSlot = actionBarUI.GetSelectedSlot();
        if (currentSlot < actionBar.Count && actionBar[currentSlot] == carriedItem)
        {
            actionBar[currentSlot] = null;
            actionBarUI.UpdateActionBarUI(actionBar);
        }

        lastDropTime = Time.time;
        carriedItem = null;

        //Update all attachmentPoints to handle raycast blocking
        foreach (var point in FindObjectsOfType<AttachmentPoint>())
        {
            point.UpdateRaycastInteraction();
        }
    }

    private GameObject FindTopmostAssemblyRoot(GameObject obj)
    {
        while (obj.transform.parent != null && obj.transform.parent.GetComponent<InteractibleItem>() != null)
        {
            obj = obj.transform.parent.gameObject;
        }
        return obj;
    }

    public void OnExitSeat()
    {
        currentSeat = null;
        Debug.Log("PlayerInteraction notified of seat exit");
    }

    private void DetachLooseParts(GameObject assembly)
    {
        InteractibleItem[] allParts = assembly.GetComponentsInChildren<InteractibleItem>();

        foreach (var part in allParts)
        {
            if (part.currentState == AttachmentState.Loose)
            {
                Debug.Log($"Detaching loose part: {part.name}");

                // Detach from parent
                part.transform.SetParent(null);
                part.currentState = AttachmentState.Detached;

                // Re-enable collider and physics
                Collider[] colliders = part.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = true;
                }

                if (!part.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb = part.gameObject.AddComponent<Rigidbody>();
                }
                rb.isKinematic = false;
                rb.useGravity = true;

                // Re-enable the attachment point collider
                foreach (var point in FindObjectsOfType<AttachmentPoint>())
                {
                    if (Vector3.Distance(point.transform.position, part.transform.position) < 0.2f)
                    {
                        point.DetachItem();
                        point.EnableCollider();
                        break;
                    }
                }
            }
        }
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
    public CarSeat GetCurrentSeat()
{
    return currentSeat;
}


}
