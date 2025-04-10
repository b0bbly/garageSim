using UnityEngine;

public class InteractibleItem : MonoBehaviour
{
    public bool canBeStored;
    public Sprite itemIcon;
    public bool isTool;
    public ToolType toolType = ToolType.None;

    public AttachmentState currentState = AttachmentState.Detached;
    public string attachmentType = ""; // e.g. "CarDoor", "Tire", etc.
    public GameObject previewPrefab;

    [Header("Visual Feedback")]
    public Color looseColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Darker color for loose state
    private Material[] originalMaterials;
    private Renderer[] renderers;

    [Header("Interaction Settings")]
    public bool canBePickedUp = true;
    public bool isPushable = false;
    public float pushForce = 5f;

    private void Start()
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

    public bool TryTighten(ToolType toolType)
    {
        if (currentState == AttachmentState.Loose && this.toolType == toolType)
        {
            currentState = AttachmentState.Fixed;
            UpdateVisualState();
            return true;
        }
        return false;
    }
    public bool TryLoosen(ToolType toolType)
    {
        if (currentState == AttachmentState.Fixed && this.toolType == toolType)
        {
            currentState = AttachmentState.Loose;
            UpdateVisualState();
            return true;
        }
        return false;
    }

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
                    mat.color = looseColor;
                }
                else // Fixed or Detached
                {
                    mat.color = Color.white; // Reset to original color
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



