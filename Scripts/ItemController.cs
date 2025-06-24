using UnityEngine;

public class ItemController : MonoBehaviour
{
    [Header("Item States")]
    public bool isEquippable = false;
    public bool isUseable = false;
    public bool isEquipped = false;
    public bool isStored = false;

    [Header("Rotation Settings")]
    public float rotationSpeed = 100f;
    private bool isRotating = false;
    private Vector3 lastMousePosition;
    private Vector3 originalRotation;

    [Header("Positions")]
    public Transform carriedPosition;
    public Transform equippedPosition;
    public Transform storedPosition;

    private Camera mainCamera;
    private Rigidbody rb;
    private Collider[] itemColliders;

    private void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        itemColliders = GetComponentsInChildren<Collider>();
        originalRotation = transform.eulerAngles;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && isEquippable)
        {
            ToggleEquipped();
        }

    }


    public void ToggleEquipped()
    {
        isEquipped = !isEquipped;
        Transform targetPosition = isEquipped ? equippedPosition : carriedPosition;
        
        // Smoothly move to the new position
        StartCoroutine(SmoothTransition(targetPosition));
    }

    private System.Collections.IEnumerator SmoothTransition(Transform target)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, target.position, t);
            transform.rotation = Quaternion.Lerp(startRot, target.rotation, t);

            yield return null;
        }

        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    public void EnableCollision(bool enable)
    {
        foreach (Collider col in itemColliders)
        {
            col.enabled = enable;
        }
    }

    public void SetKinematic(bool isKinematic)
    {
        if (rb != null)
        {
            rb.isKinematic = isKinematic;
            rb.useGravity = !isKinematic;
        }
    }

    public void ResetRotation()
    {
        transform.eulerAngles = originalRotation;
    }
}