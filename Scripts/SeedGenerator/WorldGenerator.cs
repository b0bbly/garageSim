using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{

    public int seed = 12345;
    private System.Random rng;

    void Awake()
    {
        rng = new System.Random(seed);
    } 

}
