using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    public float food = 100f;
    public float drink = 100f;

    [Header("Decay Rates")]
    public float foodDecayRate = 1f;
    public float drinkDecayRate = 1f;

    public static PlayerStats Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        food = Mathf.Max(0, food - foodDecayRate * Time.deltaTime);
        drink = Mathf.Max(0, drink - drinkDecayRate * Time.deltaTime);

        if (food <= 0 || drink <= 0)
        {
            health = Mathf.Max(0, health - 1 * Time.deltaTime);
        }
    }

    public void AddHealth(float amount) => health = Mathf.Min(100, health + amount);
    public void AddFood(float amount) => food = Mathf.Min(100, food + amount);
    public void AddDrink(float amount) => drink = Mathf.Min(100, drink + amount);
}
