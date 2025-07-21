// README:
// This prototype implements a procedural POI and loot spawning system for Unity.
// It uses POIInstance, POISpawnPoint, and SceneStateManager to persist POI/loot states.
//
// HOW TO IMPLEMENT:
// 1. Create prefab folders: Resources/POIs/ and Resources/Items/.
// 2. Create prefabs for POIs (e.g. house, gas station) and for items (e.g. food, parts).
// 3. Add a POISpawnPoint script to empty GameObjects in the scene to act as POI anchor points.
// 4. Assign allowedPrefabs list in POISpawnPoint to the prefabs you want that point to choose from.
// 5. Attach a POIInstance to your POI prefabs and link itemSpawns (ItemSpawnPoint) to where loot appears.
// 6. Assign unique itemIDs to all ItemSpawnPoints for proper state saving.
// 7. Place SceneStateManager on a manager GameObject in your scene and fill in the list of POISpawnPoints.
// 8. Optionally: implement SaveManager to persist data to disk (currently uses in-memory dictionary).
// 9. Optionally: extend LootTableManager to categorize loot more finely.
//
// OPTIONAL TOOLS:
// A. Inspector Tool (POISpawnPointEditor.cs)
//    - Adds buttons in Inspector to spawn/preview POI prefabs.
// B. Scene Gizmo Visualizer (POISpawnGizmo.cs)
//    - Shows spawn radius/IDs in scene view for organization.

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneState
{
    public string sceneName;
    public List<POIState> poiStates = new();
}

[Serializable]
public class POIState
{
    public string poiID;
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
    public List<ItemSpawnState> itemStates = new();
}

[Serializable]
public class ItemSpawnState
{
    public string itemID;
    public string itemPrefab;
    public bool wasPickedUp;
    public Vector3 position;
}

public class SceneStateManager : MonoBehaviour
{
    public string sceneName;
    public List<POISpawnPoint> poiSpawnPoints = new();

    void Start()
    {
        LoadScenePOIs(sceneName);
    }

    public void LoadScenePOIs(string sceneName)
    {
        SceneState state = SaveManager.GetSceneState(sceneName);

        if (state == null)
        {
            state = new SceneState { sceneName = sceneName };

            foreach (var poiSpawn in poiSpawnPoints)
            {
                if (UnityEngine.Random.value <= poiSpawn.spawnChance)
                {
                    GameObject prefab = poiSpawn.GetRandomPrefab();
                    GameObject instance = Instantiate(prefab, poiSpawn.transform.position, poiSpawn.transform.rotation);

                    POIState poiState = new POIState
                    {
                        poiID = poiSpawn.poiID,
                        prefabName = prefab.name,
                        position = poiSpawn.transform.position,
                        rotation = poiSpawn.transform.rotation
                    };

                    var poiInstance = instance.GetComponent<POIInstance>();
                    poiState.itemStates = poiInstance.CaptureLootState();

                    state.poiStates.Add(poiState);
                }
            }

            SaveManager.SaveSceneState(sceneName, state);
        }
        else
        {
            foreach (var poiState in state.poiStates)
            {
                GameObject prefab = Resources.Load<GameObject>("POIs/" + poiState.prefabName);
                GameObject instance = Instantiate(prefab, poiState.position, poiState.rotation);

                var poiInstance = instance.GetComponent<POIInstance>();
                poiInstance.RestoreLootState(poiState.itemStates);
            }
        }
    }
} 




// Dummy SaveManager to complete the example
public static class SaveManager
{
    private static Dictionary<string, SceneState> sceneStates = new();

    public static SceneState GetSceneState(string sceneName)
    {
        return sceneStates.ContainsKey(sceneName) ? sceneStates[sceneName] : null;
    }

    public static void SaveSceneState(string sceneName, SceneState state)
    {
        sceneStates[sceneName] = state;
    }
} 

// Dummy LootTableManager
public static class LootTableManager
{
    public static GameObject GetRandomItem(string category)
    {
        // Replace with actual loot table lookup
        return Resources.Load<GameObject>("Items/ExampleItem");
    }
}
