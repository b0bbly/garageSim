using UnityEngine;

public class Bolt : UseableObject
{
    public bool isLoose = false;

    private void Start()
    {
        onActionComplete.AddListener(OnBoltLoosened);
    }

    private void OnBoltLoosened()
    {
        isLoose = true;
        // Add visual feedback
        GetComponent<Renderer>().material.color = Color.red;
        // Maybe play a sound
        // Maybe make the bolt slightly rotated
    }
}
