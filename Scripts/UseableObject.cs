using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UseableObject : MonoBehaviour, IUseable
{
    [Header("Interaction Settings")]
    public ToolType requiredToolType;
    public float actionDuration = 2f;
    
    [Header("Optional")]
    public UnityEvent onActionComplete;

    private bool isBeingUsed = false;
    private float currentProgress = 0f;
    private InteractibleItem attachedItem;

    private void Start()
    {
        attachedItem = GetComponent<InteractibleItem>();
        if (attachedItem == null)
        {
            Debug.LogError("UseableObject requires an InteractibleItem component");
        }
    }

    public bool CanBeUsedWith(ToolType toolType)
    {
        return toolType == requiredToolType;
    }

    public float GetActionDuration()
    {
        return actionDuration;
    }

    public void Use(ToolType toolType)
    {
        if (attachedItem == null) return;

        // Only allow use if the item is in the correct state AND the tool matches
        if (toolType == requiredToolType && !isBeingUsed)
        {
            // Explicitly check states
            if (attachedItem.currentState == AttachmentState.Loose ||
                attachedItem.currentState == AttachmentState.Fixed)
            {
                isBeingUsed = true;
                currentProgress = 0f;
                Debug.Log($"Starting use action on {gameObject.name} with current state: {attachedItem.currentState}");
            }
            else
            {
                Debug.Log($"Cannot use tool on item in state: {attachedItem.currentState}");
            }
        }
    }

    public void UpdateProgress(float deltaTime)
    {
        if (!isBeingUsed) return;

        currentProgress += deltaTime;
        
        if (currentProgress >= actionDuration)
        {
            CompleteAction();
        }
    }

    public float GetProgress()
    {
        return currentProgress / actionDuration;
    }

    public void CompleteAction()
    {
        if (attachedItem == null) return;

        isBeingUsed = false;
        currentProgress = 0f;

        // Change state based on current state
        if (attachedItem.currentState == AttachmentState.Loose)
        {
            attachedItem.TryTighten(requiredToolType);
        }
        else if (attachedItem.currentState == AttachmentState.Fixed)
        {
            attachedItem.TryLoosen(requiredToolType);
        }

        onActionComplete?.Invoke();
    }
}
