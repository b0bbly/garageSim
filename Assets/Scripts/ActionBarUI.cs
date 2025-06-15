using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionBarUI : MonoBehaviour
{
    public List<Image> slotImages;
    public List<Image> slotBorders;
    public Sprite emptySlotSprite;
    
    private int selectedSlot = 0;
    
    private const float SELECTED_ALPHA = 1f;
    private const float UNSELECTED_ALPHA = 0.5f;

    void Start()
    {
        // Validate components
        if (slotImages == null || slotImages.Count == 0)
        {
            Debug.LogError("Slot images not assigned in ActionBarUI!");
            return;
        }

        if (emptySlotSprite == null)
        {
            Debug.LogError("Empty slot sprite not assigned in ActionBarUI!");
            return;
        }

        // Initialize all slots to empty
        for (int i = 0; i < slotImages.Count; i++)
        {
            if (slotImages[i] != null)
            {
                slotImages[i].sprite = emptySlotSprite;
                slotImages[i].color = new Color(1, 1, 1, 0.5f);
            }
        }

        HighlightSelectedSlot();
    }

    public void UpdateActionBarUI(List<GameObject> actionBar)
    {
        if (slotImages == null)
        {
            Debug.LogError("Slot images list is null!");
            return;
        }

        if (actionBar == null)
        {
            Debug.LogError("Action bar list is null!");
            return;
        }

        for (int i = 0; i < slotImages.Count; i++)
        {
            if (i < actionBar.Count && actionBar[i] != null)
            {
                InteractibleItem item = actionBar[i].GetComponent<InteractibleItem>();
                slotImages[i].sprite = item != null ? item.itemIcon : emptySlotSprite;
            }
            else
            {
                slotImages[i].sprite = emptySlotSprite;
            }
        }
/*
        // Update each slot
        for (int i = 0; i < slotImages.Count; i++)
        {
            if (slotImages[i] == null)
            {
                Debug.LogError($"Slot image {i} is null!");
                continue;
            }

            // Set default empty state
            slotImages[i].sprite = emptySlotSprite;
            slotImages[i].color = new Color(1, 1, 1, 0.5f);

            // If we have a valid item in this slot, update its appearance
            if (i < actionBar.Count && actionBar[i] != null)
            {
                InteractibleItem item = actionBar[i].GetComponent<InteractibleItem>();
                if (item != null && item.itemIcon != null)
                {
                    slotImages[i].sprite = item.itemIcon;
                    slotImages[i].color = Color.white;
                }
            }
        }
        */

        HighlightSelectedSlot();
    }

    public void ChangeSelectedSlot(int newSlot)
    {
        if (newSlot >= 0 && newSlot < slotImages.Count)
        {
            selectedSlot = newSlot;
            HighlightSelectedSlot();
        }
    }

    private void HighlightSelectedSlot()
    {
        if (slotBorders == null || slotBorders.Count == 0)
        {
            return;
        }

        for (int i = 0; i < slotBorders.Count; i++)
        {
            if (slotBorders[i] != null)
            {
                Color borderColor = slotBorders[i].color;
                borderColor.a = (i == selectedSlot) ? SELECTED_ALPHA : UNSELECTED_ALPHA;
                slotBorders[i].color = borderColor;
            }
        }
    }

    public int GetSelectedSlot()
    {
        return selectedSlot;
    }
}

