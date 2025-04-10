using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    public Image crosshairImage;
    public float defaultAlpha = 0.25f;
    public float hoverAlpha = 0.5f;
    public float interactAlpha = 1.0f;
    public float interactRange = 3f; // Should match your PlayerInteraction range

    private void Start()
    {
        if (crosshairImage == null)
        {
            Debug.LogError("Crosshair Image not assigned!");
            return;
        }
        
        // Set default alpha
        SetCrosshairAlpha(defaultAlpha);
    }

    private void Update()
    {
        if (Camera.main == null) return;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
        {
            InteractibleItem item = hit.collider.GetComponent<InteractibleItem>();
            
            if (item != null)
            {
                // Item is interactible, check distance
                float distance = Vector3.Distance(Camera.main.transform.position, hit.point);
                
                if (distance <= interactRange)
                {
                    // In range and hovering over item
                    SetCrosshairAlpha(interactAlpha);
                }
                else
                {
                    // Hovering but out of range
                    SetCrosshairAlpha(hoverAlpha);
                }
            }
            else
            {
                // Not hovering over an interactible item
                SetCrosshairAlpha(defaultAlpha);
            }
        }
        else
        {
            // Not hitting anything
            SetCrosshairAlpha(defaultAlpha);
        }
    }

    private void SetCrosshairAlpha(float alpha)
    {
        Color color = crosshairImage.color;
        color.a = alpha;
        crosshairImage.color = color;
    }
}
