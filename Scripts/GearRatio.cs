using UnityEngine;
using TMPro;

public enum TransmissionType { Automatic, Manual }
public enum GearPosition { Park, Reverse, Neutral, Drive, First, Second, Third, Fourth, Fifth }

[System.Serializable]
public class GearRatio
{
    public GearPosition gear;
    public float gearRatio;
    public float ratio;
    public float minRPM;
    public float maxRPM;
    public float optimalMinRPM;
    public float optimalMaxRPM;
}
