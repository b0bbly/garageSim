using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ConsumableType { Health, Food, Drink }
public class ConsumableItem : InteractibleItem
{
    [Header("Consumable Properties")]
    public ConsumableType consumableType;
    public float restoreAmount = 25f;

    public override string GetTooltipText()
    {
        PlayerInteraction player = FindObjectOfType<PlayerInteraction>();
        bool isHeld = player != null && player.GetCarriedItem() == gameObject;
        if (isHeld)
        {
            return $"Press F to consume {displayName}";
        }
        else if (currentState != AttachmentState.Fixed)
        {
            return displayName;
        }
        return base.GetTooltipText();
    }

    public bool TryConsume()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogError("PlayerStats.Instance is null");
            return false;
        }
        switch (consumableType)
        {
            case ConsumableType.Health:
                PlayerStats.Instance.AddHealth(restoreAmount);
                break;
            case ConsumableType.Food:
                PlayerStats.Instance.AddFood(restoreAmount);
                break;
            case ConsumableType.Drink:
                PlayerStats.Instance.AddDrink(restoreAmount);
                break;
        }
        Destroy(gameObject);
        return true;
    }
}
