#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(POISpawnPoint))]
public class POISpawnPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        POISpawnPoint poiSpawn = (POISpawnPoint)target;

        if (GUILayout.Button("Spawn Random POI"))
        {
            GameObject prefab = poiSpawn.GetRandomPrefab();
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = poiSpawn.transform.position;
                instance.transform.rotation = poiSpawn.transform.rotation;
                Undo.RegisterCreatedObjectUndo(instance, "Spawn POI");
            }
        }
    }
}
#endif