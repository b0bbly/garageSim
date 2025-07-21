using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using Unity.VisualScripting;

public class CarSeat : InteractibleItem
{
    public Transform seatCameraPosition;
    public bool isOccupied = false;
    private bool canExit = false;
    private Vector3 exitPosition;
    public float exitCheckDistance = 2.0f;
    public LayerMask groundLayer;

    private GameObject playerObject;
    private CinemachineVirtualCamera playerVirtualCamera;
    private CinemachineVirtualCamera seatVirtualCamera;
    private CinemachineVirtualCamera thirdPersonCamera;
    private bool isThirdPersonView = false;

    [Header("Third Person Camera")]
    public float thirdPersonDistance = 5f;
    public float thirdPersonHeight = 2f;
    public float minZoom = 3f;
    public float maxZoom = 8f;
    public float zoomSpeed = 1f;
    public float rotationSpeed = 3f;
    private float currentDistance;
    private float currentRotation = 180f; //start behind the car
    private float currentHeight;


    //Car camera variables
    public float lookSensitivity = 2.0f;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float maxLookAngle = 60f; // Limit how far up/down the player can look
    public float maxHorizontalAngle = 150f; // How far left/right the player can look (120 degrees each way)
    private float initialYRotation; // Store the initial forward direction


    private void Start()
    {
        if (seatVirtualCamera == null && seatCameraPosition != null)
        {
            GameObject vcamObj = new GameObject($"SeatVCam_{gameObject.name}");
            seatVirtualCamera = vcamObj.AddComponent<CinemachineVirtualCamera>();
            seatVirtualCamera.Priority = 0; // Low priority by default

            // Position the virtual camera at the seat position
            vcamObj.transform.position = seatCameraPosition.position;
            vcamObj.transform.rotation = seatCameraPosition.rotation;

            vcamObj.transform.SetParent(transform.root);

            // Set some basic properties
            seatVirtualCamera.m_Lens.FieldOfView = 60;
            seatVirtualCamera.gameObject.SetActive(false);
        }
        //Set up third person camera
        if (thirdPersonCamera == null)
        {
            GameObject tpCamObj = new GameObject($"ThirdPersonVCam_{gameObject.name}");
            thirdPersonCamera = tpCamObj.AddComponent<CinemachineVirtualCamera>();
            thirdPersonCamera.Priority = 0; // Low priority by default

            // Add a transposer for better third-person following
            var transposer = thirdPersonCamera.GetCinemachineComponent<CinemachineTransposer>() 
                ?? thirdPersonCamera.AddCinemachineComponent<CinemachineTransposer>();
            transposer.m_FollowOffset = new Vector3(0, thirdPersonHeight, -thirdPersonDistance);

            thirdPersonCamera.m_Follow = transform.root;
            thirdPersonCamera.m_LookAt = transform.root;

            thirdPersonCamera.m_Lens.FieldOfView = 60;
            thirdPersonCamera.gameObject.SetActive(false);

            tpCamObj.transform.SetParent(null);
        }
        currentDistance = thirdPersonDistance;
        currentHeight = thirdPersonHeight;
    }

    private void Update()
    {
        if (!isOccupied)
        {
            return;
        }

        //Toggle between first and third person views
        if (Input.GetKeyDown(KeyCode.C))
        {
            isThirdPersonView = !isThirdPersonView;
            UpdateCameraState();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractWhileSeated();
        }

        if (isThirdPersonView)
        {
            UpdateThirdPersonCamera();
        }
        else
        {
            UpdateFirstPersonCamera();
        }

        canExit = CheckCanExit();
            
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (canExit)
            {
                Exit();
                if(playerObject != null)
                {
                    playerObject.transform.position = exitPosition;
                }
            }
        }
        
    }
    

    private void UpdateFirstPersonCamera()
    {
            
        // Handle mouse look when seated
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        // Calculate rotation
        rotationY += mouseX;
        rotationX -= mouseY; // Inverted for natural feel
        rotationX = Mathf.Clamp(rotationX, -maxLookAngle, maxLookAngle); // Limit up/down look
        float deltaY = Mathf.DeltaAngle(initialYRotation, rotationY);

        if (Mathf.Abs(deltaY) > maxHorizontalAngle)
        {
            rotationY = initialYRotation + Mathf.Sign(deltaY) * maxHorizontalAngle;
        }

        // Apply rotation to the seat camera
        if (seatVirtualCamera != null && seatVirtualCamera.gameObject.activeInHierarchy)
        {
            seatVirtualCamera.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
    }

    private void UpdateThirdPersonCamera()
    {
        //rotate around the car
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        currentRotation += mouseX;

        //zoom in and out
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minZoom, maxZoom);

        Transform carTransform = transform.root;
        //Calculate the camera position based on rotation around the car
        float angleRad = currentRotation * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * currentDistance;
        Vector3 targetPosition = carTransform.position + offset + Vector3.up * currentHeight;
    
    // Check for ground collision
    RaycastHit hit;
    if (Physics.Raycast(carTransform.position + Vector3.up * currentHeight, offset.normalized, out hit, currentDistance, groundLayer))
    {
        // Adjust position to avoid clipping through ground
        targetPosition = hit.point + hit.normal * 0.5f;
    }
    
    // Update the Cinemachine Transposer directly
    var transposer = thirdPersonCamera.GetCinemachineComponent<CinemachineTransposer>();
    if (transposer != null)
    {
        // Convert the world-space offset to local space relative to the car
        Vector3 localOffset = Quaternion.Inverse(carTransform.rotation) * offset;
        localOffset.y = currentHeight; // Add height
        
        // Set the follow offset
        transposer.m_FollowOffset = localOffset;
    }
    else
    {
        // Fallback if transposer is not available
        thirdPersonCamera.transform.position = targetPosition;
        thirdPersonCamera.transform.LookAt(carTransform.position + Vector3.up * (currentHeight * 0.5f));
    }
    }

    private void UpdateThirdPersonCameraPosition()
    {
        if (thirdPersonCamera == null) return;

        //calculate the desired position
        Transform carTransform = transform.root;
        Vector3 carPosition = transform.root.position;
        float angleRad = currentRotation * Mathf.Deg2Rad;
        Vector3 forward = carTransform.forward;
        Vector3 right = carTransform.right;

        Vector3 offset = (-forward * Mathf.Cos(angleRad) + right * Mathf.Sin(angleRad)) * currentDistance;
        Vector3 targetPosition = carPosition + offset + Vector3.up * currentHeight;

        //check for ground collision
        RaycastHit hit;
        if (Physics.Raycast(carPosition + Vector3.up * currentHeight, offset.normalized, out hit, currentDistance, groundLayer))
        {
            //Adjust position to avoid clipping through ground
            targetPosition = hit.point + hit.normal * 0.5f;
        }
        //update camera position
        thirdPersonCamera.transform.position = targetPosition;
        thirdPersonCamera.transform.LookAt(carPosition + Vector3.up * (currentHeight * 0.5f));
    }

        private void UpdateCameraState()
    {
        if (isThirdPersonView)
        {
            // Switch to third person
            seatVirtualCamera.gameObject.SetActive(false);
            seatVirtualCamera.Priority = 0;
            thirdPersonCamera.gameObject.SetActive(true);
            thirdPersonCamera.Priority = 20;

            // Reset third person camera position to behind the car
            currentRotation = 180f;
            UpdateThirdPersonCameraPosition();
            Debug.Log("Switched to third person camera");
        }
        else
        {
            thirdPersonCamera.gameObject.SetActive(false);
            // Switch to first person
            thirdPersonCamera.Priority = 0;
            seatVirtualCamera.gameObject.SetActive(true);
            seatVirtualCamera.Priority = 20;
            Debug.Log("Switched to first person camera");
        }
    }

private bool CheckCanExit()
    {
        // Cast a ray from the camera position in the forward direction
        RaycastHit hit;
        if (seatVirtualCamera == null) return false;

        // Get the camera's position and forward direction
        Vector3 rayOrigin = seatVirtualCamera.transform.position;
        Vector3 rayDirection = seatVirtualCamera.transform.forward;

        // Create a layermask that ignores empty attachment points
        int attachmentPointLayer = LayerMask.NameToLayer("AttachmentPoint");
        LayerMask raycastMask = Physics.DefaultRaycastLayers;

        if (attachmentPointLayer != -1)
        {
            // Only ignore attachment points that are empty
            // We need to check each attachment point in the ray path
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, exitCheckDistance))
            {
                AttachmentPoint attachmentPoint = hit.collider.GetComponent<AttachmentPoint>();
                if (attachmentPoint != null && !attachmentPoint.IsOccupied())
                {
                    // This is an empty attachment point, we can exit through it
                    exitPosition = hit.point + rayDirection * 1.0f; // Position slightly in front of hit point
                    return true;
                }

                // If we hit something that's not an empty attachment point, check if it's a door
                HingedItem door = hit.collider.GetComponent<HingedItem>();
                if (door != null && door.IsOpen())
                {
                    // Door is open, we can exit
                    exitPosition = hit.point + rayDirection * 1.0f;
                    return true;
                }

                // Hit something else blocking the exit
                return false;
            }
            else
            {
                // Nothing in the way, we can exit
                exitPosition = rayOrigin + rayDirection * exitCheckDistance;
                return true;
            }
        }

        // Default case - check if there's anything in the way
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, exitCheckDistance))
        {
            // Something is in the way
            return false;
        }

        // Nothing in the way, we can exit
        exitPosition = rayOrigin + rayDirection * exitCheckDistance;
        return true;
    }


    public void Sit(GameObject player)
    {
        if (currentState != AttachmentState.Fixed) return;

        playerObject = player;
        playerVirtualCamera = FindPlayerVirtualCamera();
        if (playerVirtualCamera == null)
        {
            Debug.LogError("Could not find the player virtual camera!");
            return;
        }

        // Initialize rotation values based on seat camera's initial rotation
        rotationX = seatCameraPosition.rotation.eulerAngles.x;
        rotationY = seatCameraPosition.rotation.eulerAngles.y;
        initialYRotation = rotationY; // Store the initial forward direction

        playerObject.SetActive(false);

        // Activate seat camera with high priority
        if (seatVirtualCamera != null)
        {
            seatVirtualCamera.transform.position = seatCameraPosition.position;
            seatVirtualCamera.transform.rotation = seatCameraPosition.rotation;

            seatVirtualCamera.gameObject.SetActive(true);
            seatVirtualCamera.Priority = 20; // Higher than player camera
        }
        if (thirdPersonCamera != null)
        {
            Transform carTransform = transform.root;
            Vector3 position = carTransform.position + carTransform.forward * thirdPersonDistance + Vector3.up * thirdPersonHeight;
            thirdPersonCamera.transform.position = position;
            thirdPersonCamera.transform.LookAt(carTransform);
            // Set the car as the follow target
            thirdPersonCamera.m_Follow = transform.root;
            thirdPersonCamera.m_LookAt = transform.root;
            
            thirdPersonCamera.gameObject.SetActive(false);
            thirdPersonCamera.Priority = 0;
        }
        isThirdPersonView = false;

        isOccupied = true;
        NotifyCarOfPlayerPresence(true);

        CrosshairController crosshairController = FindObjectOfType<CrosshairController>();
        if (crosshairController != null && crosshairController.itemNameText != null)
        {
            crosshairController.HideTooltip(); // Call the hide method directly
        }
    }

    public void Exit()

    {
        if (!isOccupied) return;

        if (playerObject != null)
        {
            // Use the exit position determined by CheckCanExit
            if (exitPosition != Vector3.zero)
            {
                playerObject.transform.position = exitPosition;
            }
            else
            {
                // Fallback position if exitPosition wasn't set
                playerObject.transform.position = seatCameraPosition.position + seatCameraPosition.forward * 1.5f;
            }
            
            playerObject.SetActive(true);

            var playerMovement = playerObject.GetComponent<FirstPersonController>();
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }

            // Notify PlayerInteraction that the player has exited
            PlayerInteraction playerInteraction = playerObject.GetComponent<PlayerInteraction>();
            if(playerInteraction != null)
            {
                playerInteraction.OnExitSeat(); 
            }
        }

        // Deactivate seat camera
        if (seatVirtualCamera != null)
        {
            seatVirtualCamera.Priority = 0;
            seatVirtualCamera.gameObject.SetActive(false);
        }

        isOccupied = false;
        NotifyCarOfPlayerPresence(false);
        Debug.Log("Player Exited");
    }

        
    public override string GetTooltipText()
    {
        if (currentState == AttachmentState.Fixed)
        {
            return isOccupied ? "Press F to exit" : "Press E to sit";
        }
        return base.GetTooltipText();
    }

    private void NotifyCarOfPlayerPresence(bool isPresent)
    {
        CarController carController = GetComponentInParent<CarController>();
        if (carController != null)
        {
            InteractibleCarComponent[] carComponents = carController.GetComponentsInChildren<InteractibleCarComponent>();
            foreach(var component in carComponents)
            {
                component.SetPlayerSeated(isPresent);
            }
        }
    }

// Add this new method to handle interactions while seated
    private void TryInteractWhileSeated()
    {
        if (seatVirtualCamera == null) return;
        
        // Get the camera's position and forward direction
        Vector3 rayOrigin = seatVirtualCamera.transform.position;
        Vector3 rayDirection = seatVirtualCamera.transform.forward;
        float interactRange = 3f; // Same as PlayerInteraction.interactRange
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactRange))
        {
            // Check for hinged items
            HingedItem hingedItem = hit.collider.GetComponent<HingedItem>();
            if (hingedItem != null)
            {
                hingedItem.OnInteract();
                return;
            }
            
            // Check for other interactible items
            InteractibleItem item = hit.collider.GetComponent<InteractibleItem>();
            if (item != null && item.currentState == AttachmentState.Fixed)
            {
                // Handle other types of fixed items if needed
                // For example, buttons, switches, etc.
            }
        }
    }


    private CinemachineVirtualCamera FindPlayerVirtualCamera()
    {
        // Find all virtual cameras in the scene
        CinemachineVirtualCamera[] vcams = FindObjectsOfType<CinemachineVirtualCamera>();

        // Return the first active one (assuming it's the player camera)
        foreach (var vcam in vcams)
        {
            if (vcam.gameObject.activeInHierarchy && vcam.Priority > 0)
            {
                return vcam;
            }
        }

        return null;
    }
}
