using UnityEngine;

[ExecuteInEditMode]
public class POISpawnGizmo : MonoBehaviour
{
    public Color gizmoColor = Color.cyan;
    public float radius = 1.5f;
    public string poiLabel;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);
        if (!string.IsNullOrEmpty(poiLabel))
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, poiLabel);
#endif
        }
    }
}