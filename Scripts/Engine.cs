using UnityEngine;

public interface IVehiclePart
{
    float Durability { get; set; }
    bool IsFunction { get; }
}

public class Engine : MonoBehaviour, IVehiclePart
{
    public float Durability { get; set; } = 100f;
    public float Temperature { get; private set; }
    public float OilLevel { get; private set; } = 100f;
    public float CoolantLevel { get; private set; } = 100f;
    public float FuelLevel { get; private set; }
    public string RequiredFuelType { get; private set; } = "Regular";
    public bool HasBatteryPower { get; private set; }
    
    private const float MAX_TEMP = 120f;
    private const float DAMAGE_RATE = 5f;
    private const float FUEL_CONSUMPTION_RATE = 0.1f;
    private bool isRunning = false;
    
    public bool IsFunction => Durability > 0 && 
                            OilLevel > 0 && 
                            CoolantLevel > 0 && 
                            HasBatteryPower &&
                            FuelLevel > 0;

    public bool IsRunning => isRunning && IsFunction;

    private void Update()
    {
        if (isRunning)
        {
            UpdateTemperature();
            ConsumeFuel();
            CheckEngineConditions();
        }
    }

    public void StartEngine()
    {
        if (IsFunction)
        {
            isRunning = true;
        }
    }

    public void StopEngine()
    {
        isRunning = false;
    }

    private void UpdateTemperature()
    {
        float baseTemp = 80f; // Normal operating temperature
        
        if (CoolantLevel <= 0)
            Temperature += Time.deltaTime * 10f;
        else if (OilLevel <= 0)
            Temperature += Time.deltaTime * 15f;
        else
            Temperature = Mathf.Lerp(Temperature, baseTemp, Time.deltaTime);

        if (Temperature > MAX_TEMP)
        {
            Durability -= DAMAGE_RATE * Time.deltaTime;
            if (Durability <= 0)
            {
                EngineSeize();
            }
        }
    }

    private void ConsumeFuel()
    {
        FuelLevel -= FUEL_CONSUMPTION_RATE * Time.deltaTime;
        if (FuelLevel <= 0)
        {
            StopEngine();
        }
    }

    private void CheckEngineConditions()
    {
        if (!HasBatteryPower || OilLevel <= 0 || CoolantLevel <= 0 || FuelLevel <= 0)
        {
            StopEngine();
        }
    }

    private void EngineSeize()
    {
        Durability = 0;
        StopEngine();
        // Trigger any visual/audio effects for engine failure
    }

    public void AddFuel(float amount, string fuelType)
    {
        if (fuelType == RequiredFuelType)
        {
            FuelLevel = Mathf.Min(FuelLevel + amount, 100f);
        }
    }

    public void ConnectBattery(bool connected)
    {
        HasBatteryPower = connected;
    }
}
