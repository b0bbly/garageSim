using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionProgressUI : MonoBehaviour 
{
    public Image progressBar; // Reference to the progress bar image

    void Start()
    {
        gameObject.SetActive(false); // Hide by default
        progressBar.fillMethod = Image.FillMethod.Horizontal;
        progressBar.fillOrigin = (int)Image.OriginHorizontal.Right;
    }

    public void ShowProgress(float progress)
    {
        gameObject.SetActive(true);
        // Convert progress to go from 1 to 0 instead of 0 to 1
        progressBar.fillAmount = 1 - progress;
    }

    public void HideProgress()
    {
        gameObject.SetActive(false);
    }
}