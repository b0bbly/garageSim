using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GamePersistence;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Game state
    public GameState CurrentGameState { get; private set; } = new GameState();
    public string CurrentSceneName { get; private set; }

    // Scene transition data
    private Vector3 playerEntryPosition;
    private Quaternion playerEntryRotation;
    private GameObject playerVehicle;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize game state
        CurrentGameState.SceneStates = new Dictionary<string, GamePersistence.SceneState>();
    }

    // Call this when transitioning between scenes
    public void TransitionToScene(string sceneName, Vector3 entryPosition, Quaternion entryRotation)
    {
        // Save current scene state
        SaveCurrentSceneState();

        // Set entry data for next scene
        playerEntryPosition = entryPosition;
        playerEntryRotation = entryRotation;

        // Load the new scene
        CurrentSceneName = sceneName;
        SceneManager.LoadScene(sceneName);
    }

    // Call this when scene is loaded
    public void OnSceneLoaded()
    {
        // Load scene state if it exists
        if (CurrentGameState.SceneStates.ContainsKey(CurrentSceneName))
        {
            LoadSceneState(CurrentGameState.SceneStates[CurrentSceneName]);
        }

        // Position player/vehicle at entry point
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Check if player is in vehicle
            if (playerVehicle != null)
            {
                playerVehicle.transform.position = playerEntryPosition;
                playerVehicle.transform.rotation = playerEntryRotation;
            }
            else
            {
                player.transform.position = playerEntryPosition;
                player.transform.rotation = playerEntryRotation;
            }
        }
    }

    // Save current scene state
    private void SaveCurrentSceneState()
    {
        if (string.IsNullOrEmpty(CurrentSceneName)) return;

        GamePersistence.SceneState sceneState = new GamePersistence.SceneState();

        // Find all objects with PersistentObject component
        PersistentObject[] persistentObjects = FindObjectsOfType<PersistentObject>();
        foreach (var obj in persistentObjects)
        {
            sceneState.ObjectStates.Add(obj.GetObjectState());
        }

        // Store scene state
        CurrentGameState.SceneStates[CurrentSceneName] = sceneState;
    }

    // Load scene state
    private void LoadSceneState(GamePersistence.SceneState sceneState)
    {
        // Find all objects with PersistentObject component
        PersistentObject[] persistentObjects = FindObjectsOfType<PersistentObject>();

        // Create dictionary for faster lookup
        Dictionary<string, PersistentObject> objectsById = new Dictionary<string, PersistentObject>();
        foreach (var obj in persistentObjects)
        {
            objectsById[obj.ObjectId] = obj;
        }

        // Apply saved states
        foreach (var objectState in sceneState.ObjectStates)
        {
            if (objectsById.TryGetValue(objectState.ObjectId, out PersistentObject obj))
            {
                obj.ApplyObjectState(objectState);
            }
        }
    }

    // Save game to file
    public void SaveGame(string saveName)
    {
        // Save current scene before saving game
        SaveCurrentSceneState();

        // Update player state
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Get player stats
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                CurrentGameState.PlayerState.Health = playerStats.health;
                CurrentGameState.PlayerState.Hunger = playerStats.food;
                CurrentGameState.PlayerState.Thirst = playerStats.drink;
                // Add any other stats you need to save
            }

            // Check if player is in a vehicle
            PlayerInteraction playerInteraction = player.GetComponent<PlayerInteraction>();
            if (playerInteraction != null)
            {
                CarSeat currentSeat = playerInteraction.GetCurrentSeat();
                // Store vehicle ID
                PersistentObject vehiclePersistent = currentSeat.GetComponentInParent<PersistentObject>();
                if (vehiclePersistent != null)
                {
                    CurrentGameState.PlayerState.CurrentVehicleId = vehiclePersistent.ObjectId;
                }
            }
        }

        // Save to file
        string path = Application.persistentDataPath + "/" + saveName + ".sav";
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Create);

        formatter.Serialize(stream, CurrentGameState);
        stream.Close();

        Debug.Log("Game saved to: " + path);
    }

    // Load game from file
    public void LoadGame(string saveName)
    {
        string path = Application.persistentDataPath + "/" + saveName + ".sav";

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            CurrentGameState = (GameState)formatter.Deserialize(stream);
            stream.Close();

            // Load player state
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Apply player stats
                PlayerStats playerStats = player.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.health = CurrentGameState.PlayerState.Health;
                    playerStats.food = CurrentGameState.PlayerState.Hunger;
                    playerStats.drink = CurrentGameState.PlayerState.Thirst;
                    // Apply any other stats you saved
                }

                // If player was in a vehicle, find and enter it
                if (!string.IsNullOrEmpty(CurrentGameState.PlayerState.CurrentVehicleId))
                {
                    PersistentObject[] objects = FindObjectsOfType<PersistentObject>();
                    foreach (var obj in objects)
                    {
                        if (obj.ObjectId == CurrentGameState.PlayerState.CurrentVehicleId)
                        {
                            CarSeat seat = obj.GetComponentInChildren<CarSeat>();
                            if (seat != null)
                            {
                                seat.Sit(player);
                            }
                            break;
                        }
                    }
                }
            }

            // Load current scene state
            if (CurrentGameState.SceneStates.ContainsKey(CurrentSceneName))
            {
                LoadSceneState(CurrentGameState.SceneStates[CurrentSceneName]);
            }

            Debug.Log("Game loaded from: " + path);
        }
        else
        {
            Debug.LogError("Save file not found: " + path);
        }
    }
/*
    public void TransitionToSeededScene(Vector3 exitPosition, Quaternion exitRotation)
    {
        string nextScene = SceneGenerator.Instance.GetSceneNameForSeed(currentSeed, currentLocationIndex + 1);
        StartCoroutine(LoadSceneAsync(nextScene, exitPosition, exitRotation));
    }
    */
}
