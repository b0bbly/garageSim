using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image healthBar;
    public Image foodBar;
    public Image drinkBar;

    // Update is called once per frame
    void Update()
    {
        if (PlayerStats.Instance != null)
        {
            healthBar.fillAmount = PlayerStats.Instance.health / 100f;
            foodBar.fillAmount = PlayerStats.Instance.food / 100f;
            drinkBar.fillAmount = PlayerStats.Instance.drink / 100f;
        }
    }
}
