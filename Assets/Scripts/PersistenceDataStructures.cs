// Create a new file named PersistenceDataStructures.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePersistence
{
    [Serializable]
    public class GameState
    {
        public PlayerState PlayerState = new PlayerState();
        public Dictionary<string, SceneState> SceneStates = new Dictionary<string, SceneState>();
        public float TimeOfDay;
        public int CurrentSeed;
    }

    [Serializable]
    public class PlayerState
    {
        public float Health;
        public float Hunger;
        public float Thirst;
        public List<string> Inventory = new List<string>();
        public string CurrentVehicleId;
    }

    [Serializable]
    public class SceneState
    {
        public List<ObjectState> ObjectStates = new List<ObjectState>();
    }

    [Serializable]
    public class ObjectState
    {
        public string ObjectId;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsActive;
        public string AttachedToId;
        public Dictionary<string, object> CustomData = new Dictionary<string, object>();
    }
}
