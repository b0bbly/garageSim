using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrosshairController : MonoBehaviour
{
    public Image crosshairImage;
    public float defaultAlpha = 0.25f;
    public float hoverAlpha = 0.5f;
    public float interactAlpha = 1.0f;
    public float interactRange = 3f; // Should match your PlayerInteraction range

    //Performance Optimisation
    private InteractibleItem lastHitItem;
    private int raycastLayerMask;


    //Tooltip
    public TextMeshProUGUI itemNameText;
    public float tooltipDisplayDelay = 0.2f;
    private float hoverTimer = 0f;
    private bool showingTooltip = false;

    private void Start()
    {
        if (crosshairImage == null)
        {
            Debug.LogError("Crosshair Image not assigned!");
            return;
        }

        // Set default alpha
        SetCrosshairAlpha(defaultAlpha);
        if (itemNameText != null)
        {
            itemNameText.gameObject.SetActive(false);
        }
        raycastLayerMask = Physics.DefaultRaycastLayers;
    }

    private void Update()
    {
        if (Camera.main == null) return;

        RaycastHit hit;
        InteractibleItem currentItem = null;
        bool foundItem = false;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 20f, raycastLayerMask))
        {
            currentItem = hit.collider.GetComponent<InteractibleItem>();
            //If we hit on an item directly, use it
            if (currentItem != null)
            {
                foundItem = true;
                lastHitItem = currentItem;
            }
        }
        if (!foundItem)
        {
            int attachmentPointLayer = LayerMask.NameToLayer("AttachmentPoint");
            if (attachmentPointLayer != -1)
            {
                int secondRaycastMask = raycastLayerMask & ~(1 << attachmentPointLayer);
                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 20f, secondRaycastMask))
                {
                    currentItem = hit.collider.GetComponent<InteractibleItem>();
                    if (currentItem != null)
                    {
                        foundItem = true;
                        lastHitItem = currentItem;
                    }
                }
            }
        }

        // Process the item we found (if any)
        if (foundItem && currentItem != null)
        {
            float distance = Vector3.Distance(Camera.main.transform.position, hit.point);

            if (distance <= interactRange)
            {
                // In range and hovering over item
                SetCrosshairAlpha(interactAlpha);
                hoverTimer += Time.deltaTime;
                if (hoverTimer >= tooltipDisplayDelay)
                {
                    UpdateTooltip(currentItem);
                }
            }
            else
            {
                // Hovering but out of range
                SetCrosshairAlpha(hoverAlpha);
                HideTooltip();
            }
        }
        else
        {
            // Not hitting anything interactible
            lastHitItem = null;
            SetCrosshairAlpha(defaultAlpha);
            HideTooltip();
        }
    }

    /*
                if (lastHitItem == null || hit.collider.gameObject != lastHitItem.gameObject)
                {
                    currentItem = hit.collider.GetComponent<InteractibleItem>();
                    lastHitItem = currentItem;
                }
                else
                {
                    currentItem = lastHitItem;
                }

                if (currentItem != null)
                {


                    // InteractibleItem item = hit.collider.GetComponent<InteractibleItem>();
                    // Item is interactible, check distance
                    float distance = Vector3.Distance(Camera.main.transform.position, hit.point);

                    if (distance <= interactRange)
                    {
                        // In range and hovering over item
                        SetCrosshairAlpha(interactAlpha);
                        hoverTimer += Time.deltaTime;
                        if (hoverTimer >= tooltipDisplayDelay)
                        {
                            UpdateTooltip(currentItem);
                        }
                    }
                    else
                    {
                        // Hovering but out of range
                        SetCrosshairAlpha(hoverAlpha);
                        HideTooltip();
                    }
                }
                else
                {
                    // Not hovering over an interactible item
                    SetCrosshairAlpha(defaultAlpha);
                    HideTooltip();
                }
            }
            else
            {
                // Not hitting anything
                lastHitItem = null;
                SetCrosshairAlpha(defaultAlpha);
                HideTooltip();
            }
        }
        */

    private void SetCrosshairAlpha(float alpha)
    {
        Color color = crosshairImage.color;
        color.a = alpha;
        crosshairImage.color = color;
    }

    private void UpdateTooltip(InteractibleItem item)
    {
        if (itemNameText == null) return;


        CarSeat seat = item as CarSeat;
        if (seat != null && item.currentState == AttachmentState.Fixed)
        {
            if (!seat.isOccupied)
            {
                itemNameText.text = "Press E to sit";
                itemNameText.gameObject.SetActive(true);
                showingTooltip = true;
            }
            else
            {
                //HideTooltip();
            }
            return;
        }


        string tooltipText = item.GetTooltipText();

        // Only show tooltip if the item has a display name AND is not in Fixed state
        if (!string.IsNullOrEmpty(tooltipText))
        {

            itemNameText.text = tooltipText;
            itemNameText.gameObject.SetActive(true);
            showingTooltip = true;

        }
        else
        {
            HideTooltip();
        }
    }

    public void HideTooltip()
    {
        if (itemNameText != null && showingTooltip)
        {
            itemNameText.gameObject.SetActive(false);
            showingTooltip = false;
        }
        hoverTimer = 0f;
    }
    
    public void ShowTooltip(string text)
    {
        if (itemNameText != null && !string.IsNullOrEmpty(text))
        {
            itemNameText.text = text;
            itemNameText.gameObject.SetActive(true);
            showingTooltip = true;
        }
    }
    
}
