using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POIInstance : MonoBehaviour
{
    public List<ItemSpawnPoint> itemSpawns = new();

    public List<ItemSpawnState> CaptureLootState()
    {
        List<ItemSpawnState> result = new();

        foreach (var spawn in itemSpawns)
        {
            result.Add(spawn.CaptureState());
        }

        return result;
    }

    public void RestoreLootState(List<ItemSpawnState> states)
    {
        foreach (var state in states)
        {
            ItemSpawnPoint spawn = itemSpawns.Find(s => s.itemID == state.itemID);
            if (spawn != null)
            {
                spawn.RestoreState(state);
            }
        }
    }
} 
