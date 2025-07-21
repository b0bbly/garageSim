using UnityEngine;
using GamePersistence;

public class PersistentObject : MonoBehaviour
{
    public string ObjectId;
    
    protected virtual void Awake()
    {
        // Generate unique ID if not set
        if (string.IsNullOrEmpty(ObjectId))
        {
            ObjectId = System.Guid.NewGuid().ToString();
        }
    }
    
    public virtual ObjectState GetObjectState()
    {
        ObjectState state = new ObjectState
        {
            ObjectId = ObjectId,
            Position = transform.position,
            Rotation = transform.rotation,
            IsActive = gameObject.activeSelf
        };
        
        return state;
    }
    
    public virtual void ApplyObjectState(ObjectState state)
    {
        transform.position = state.Position;
        transform.rotation = state.Rotation;
        gameObject.SetActive(state.IsActive);
    }
}
