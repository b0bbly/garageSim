using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{

    public Light flashlight;

    // Start is called before the first frame update
    void Start()
    {
        flashlight.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            PlayerInteraction player = FindObjectOfType<PlayerInteraction>();
            if (player != null && player.GetCarriedItem() != null)
            {
                ConsumableItem consumableI = player.GetCarriedItem().GetComponent<ConsumableItem>();
                if (consumableI != null)
                {
                    return; //Do not toggle flashlight if holding consumable
                }
            }
            flashlight.enabled = !flashlight.enabled;
        }
    }
}
