using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class POISpawnPoint : MonoBehaviour
{
    public string poiID;
    public float spawnChance = 0.75f;
    public List<GameObject> allowedPrefabs;

    public GameObject GetRandomPrefab()
    {
        if (allowedPrefabs == null || allowedPrefabs.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, allowedPrefabs.Count);
        return allowedPrefabs[index];
    }
} 