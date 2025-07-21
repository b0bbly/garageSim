using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawnPoint : MonoBehaviour
{
    public string itemID;
    public string category;
    public float spawnChance = 0.75f;

    private GameObject spawnedItem;

    public ItemSpawnState CaptureState()
    {
        return new ItemSpawnState
        {
            itemID = itemID,
            itemPrefab = spawnedItem ? spawnedItem.name : null,
            wasPickedUp = spawnedItem == null,
            position = transform.position
        };
    }

    public void RestoreState(ItemSpawnState state)
    {
        if (!state.wasPickedUp && !string.IsNullOrEmpty(state.itemPrefab))
        {
            GameObject prefab = Resources.Load<GameObject>("Items/" + state.itemPrefab);
            spawnedItem = Instantiate(prefab, transform.position, Quaternion.identity);
        }
    }

    void Start()
    {
        if (UnityEngine.Random.value <= spawnChance)
        {
            GameObject itemPrefab = LootTableManager.GetRandomItem(category);
            if (itemPrefab)
            {
                spawnedItem = Instantiate(itemPrefab, transform.position, Quaternion.identity);
            }
        }
    }
} 
