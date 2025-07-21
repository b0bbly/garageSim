using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum CarComponentType
{
    Ignition,
    Indicator,
    Headlights,
    Radio,
    Horn,
    Wipers,
    Generic
}

public class InteractibleCarComponent : InteractibleItem
{
    [Header("Car Component Settings")]
    public CarComponentType componentType = CarComponentType.Generic;
    public bool isToggleable = true;
    public bool isActive = false;
    public bool canBeUsedFromOutside = true;

    [Header("Interaction")]
    [SerializeField]
    public CarComponentAction[] actions = new CarComponentAction[1];
    public int currentActionIndex = 0;


    [Header("Mouse Input")]
    public bool useMouseInput = false;
    public bool holdToActivate = false;
    
    [Header("Visual Feedback")]
    public GameObject activeStateVisual;
    public Material activeMaterial;
    public Material inactiveMaterial;
    public Renderer targetRenderer;

    [Header("Target Components")]
    public string messageToSend = "";
    public GameObject[] targetObjects;
    public Light[] controlledLights;
    public AudioSource soundEffect;
    
    private bool playerIsSeated = false;
    private bool isMouseDown = false;

    private void Update()
    {
        // Check for hotkey presses when player is seated
        if (playerIsSeated && actions.Length > 0)
        {
            foreach (var action in actions)
            {
                if (action != null && action.hotkey != KeyCode.None && Input.GetKeyDown(action.hotkey))
                {
                    ActivateCurrentAction();
                    break;
                }
            }
        }
        if (useMouseInput)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isMouseDown = true;
                if (holdToActivate)
                {
                    SetActiveState(true);
                }
                else
                {
                    ActivateCurrentAction();
                }
                if (holdToActivate && Input.GetMouseButtonUp(0) && isMouseDown)
                {
                    isMouseDown = false;
                    SetActiveState(false);
                }
            }
        }
    }
    
    public void SetPlayerSeated(bool isSeated)
    {
        playerIsSeated = isSeated;
    }
    
    public override string GetTooltipText()
    {
        if (actions.Length > 0 && currentActionIndex < actions.Length && actions[currentActionIndex] != null)
        {
            string hotkeyText = actions[currentActionIndex].hotkey != KeyCode.None ?
                $" or press {actions[currentActionIndex].hotkey}" : "";
            return $"Press E{hotkeyText} to {actions[currentActionIndex].actionName}"; 
        }
        return base.GetTooltipText();
    }
    
    // Called when player interacts with this component
    public void OnInteract()
    {
        // Check if player can use this component
        if (!canBeUsedFromOutside && !playerIsSeated)
        {
            Debug.Log("Must be seated to use this component");
            return;
        }
        
        if (actions.Length > 0 && currentActionIndex < actions.Length)
        {
            // If component requires player to be seated and player isn't seated, don't allow interaction
            if (actions[currentActionIndex].requiresSeated && !playerIsSeated)
            {
                Debug.Log($"Must be seated to {actions[currentActionIndex].actionName}");
                return;
            }
            
            ActivateCurrentAction();
        }
    }
    
    private void ActivateCurrentAction()
    {
        if (actions.Length == 0 || currentActionIndex >= actions.Length || actions[currentActionIndex] == null) return;

        if (isToggleable)
        {
            SetActiveState(!isActive);
        }
        else
        {
            // For non-toggleable components, just invoke the activate event
            actions[currentActionIndex].onActivate?.Invoke();
            
            // Send messages to target objects
            SendComponentMessage(true);
            
            // Play sound if available
            if (soundEffect != null)
            {
                soundEffect.Play();
            }
        }
    }
    
    private void SetActiveState(bool active)
    {
        isActive = active;
        if (actions.Length > 0 && currentActionIndex < actions.Length && actions[currentActionIndex] != null)
        {
            if (isActive)
            {
                actions[currentActionIndex].onActivate?.Invoke();
            }
            else
            {
                actions[currentActionIndex].onDeactivate?.Invoke();
            }
        }
        
        // Send messages to target objects
        SendComponentMessage(isActive);
        
        // Control lights if assigned
        if (controlledLights != null)
        {
            foreach (Light light in controlledLights)
            {
                if (light != null)
                {
                    light.enabled = isActive;
                }
            }
        }
        
        // Play or stop sound if available
        if (soundEffect != null)
        {
            if (isActive && !soundEffect.isPlaying)
            {
                soundEffect.Play();
            }
            else if (!isActive && soundEffect.isPlaying)
            {
                soundEffect.Stop();
            }
        }
        
        UpdateVisuals();
    }

    private void SendComponentMessage(bool isActive)
    {
        if (string.IsNullOrEmpty(messageToSend) || targetObjects == null) return;
        
        foreach (GameObject target in targetObjects)
        {
            if (target != null)
            {
                target.SendMessage(messageToSend, isActive, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
    
    private void UpdateVisuals()
    {
        if (activeStateVisual != null)
        {
            activeStateVisual.SetActive(isActive);
        }

        if (targetRenderer != null)
        {
            targetRenderer.material = isActive ? activeMaterial : inactiveMaterial;
        }
    }
    
    // Method to cycle through available actions (for multi-function components)
    public void CycleAction()
    {
        if (actions.Length > 0)
        {
            currentActionIndex = (currentActionIndex + 1) % actions.Length;
        }
    }
}
