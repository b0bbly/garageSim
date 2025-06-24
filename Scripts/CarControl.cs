using System;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

public class CarControl : MonoBehaviour
{
    [Header("Car Properties")]
    public float motorTorque = 2000f;
    public float brakeTorque = 2000f;
    public float handbrakeTorque = 4000f;
    public float maxSpeed = 20f;
    public float steerAngle = 30f;
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSped = 10f;
    public float centreOfGravityOffset = -1f;

    public enum DriveType { RearWheelDrive, FrontWheelDrive, AllWheelDrive }
    [System.Serializable]
    public class WheelColliders
    {
        public WheelCollider FRWheel;
        public WheelCollider FLWheel;
        public WheelCollider RRWheel;
        public WheelCollider RLWheel;
    }
    [System.Serializable]
    public class WheelMeshes
    {
        public MeshRenderer FRWheel;
        public MeshRenderer FLWheel;
        public MeshRenderer RRWheel;
        public MeshRenderer RLWheel;
    }
    

    [Header("Drive Configuration")]
    public DriveType driveType = DriveType.RearWheelDrive;
    public TransmissionType transmissionType = TransmissionType.Automatic;

    public WheelColliders colliders;
    public WheelMeshes wheelMeshes;

    [Header("Engine and Transmission")]
    public float idleRPM = 800f;
    public float maxRPM = 7000f;
    public float redlineRPM = 6500f;
    public float optimalRPM = 5000f;
    public float engineBrakingFactor = 0.1f;
    public float clutchEngageSpeed = 10f;
    public float shiftDelay = 0.5f;
    public GearRatio[] gearRatios;

    [Header("UI Display")]
    public TextMeshProUGUI rpmDisplay;
    public TextMeshProUGUI gearDisplay;
    public TextMeshProUGUI speedDisplay;
    public bool useMetricUnits = true;
    public float wheelRadius = 0.33f;

    //public float[] gearRatios = { 3.5f, 2.5f, 1.8f, 1.4f, 1.2f, 1.0f };

    [Header("Braking")]
    public float frontBrakeRatio = 0.7f;

    [Header("Physics Tuning")]
    [Range(0.1f, 1.0f)] public float wheelGripFactor = 0.8f;
    [Range(0.1f, 2.0f)] public float burnoutThreshold = 0.7f;
    [Range(0.1f, 1.0f)] public float handbrakeGripLoss = 0.8f;
    [Range(0.1f, 5.0f)] public float angularDragWhenDrifting = 1.0f;

    [Header("Player Interaction")]
    public CarSeat driverSeat;

    private WheelControl[] wheels;
    private Rigidbody rigidBody;
    private float originalAngularDrag;
    private WheelFrictionCurve[] originalFrictionCurves;

    //Transmission variables
    private GearPosition currentGear = GearPosition.Park;
    private float currentRPM;
    private float targetRPM;
    private float clutchEngagement = 0f;
    private float shiftTime = 0f;
    private bool isShifting = false;
    private float vehicleSpeed = 0f;
    private float gasInput, brakeInput = 0f;

    void Awake()
    {
        enabled = false;
        Debug.Log("CarControl.cs is disabled - use CarController.cs instead");
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!enabled) return;
        rigidBody = GetComponent<Rigidbody>();
        originalAngularDrag = rigidBody.angularDrag;

        //Adjust center of mass to improve stability and prevent rolling
        Vector3 centerOfMass = rigidBody.centerOfMass;
        centerOfMass.y += centreOfGravityOffset;
        rigidBody.centerOfMass = centerOfMass;

        //get all wheel components attached to the car
        wheels = GetComponentsInChildren<WheelControl>();

        //Store original friction curves
        originalFrictionCurves = new WheelFrictionCurve[wheels.Length];
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i].wheelCollider == null)
            {
                wheels[i].wheelCollider = wheels[i].GetComponent<WheelCollider>();
                if (wheels[i].wheelCollider == null)
                {
                    Debug.LogError($"WheelCollider not found on {wheels[i].name}. Please add a WheelCollider component.");
                    continue;
                }
            }
            originalFrictionCurves[i] = wheels[i].wheelCollider.sidewaysFriction;
        }

        if (driverSeat == null)
        {
            driverSeat = GetComponentInChildren<CarSeat>();
        }

        //Initialise transmission
        if (gearRatios == null || gearRatios.Length == 0)
        {
            Debug.LogError("Gear ratios not set. Please set the gear ratios in the inspector.");
            IntializeDefaultGearRatios();
        }

        currentRPM = idleRPM;

        //Initialize in Park for auto, neutral for manual
        currentGear = transmissionType == TransmissionType.Automatic ? GearPosition.Park : GearPosition.Neutral;
    }

    void Update()
    {
        //Handle gear shifting input
        if (driverSeat != null && driverSeat.isOccupied)
        {
            if (transmissionType == TransmissionType.Manual)
            {
                //manual transmission controls
                if (Input.GetKeyDown(KeyCode.R) && !isShifting)
                {
                    ShiftUp();
                }
                else if (Input.GetKeyDown(KeyCode.F) && !isShifting)
                {
                    ShiftDown();
                }
            }
            else //Automatic transmission
            {
                //automatic transmission controls - cycle through P-R-N-D
                if (Input.GetKeyDown(KeyCode.R) && !isShifting)
                {
                    ShiftUpAutomatic();
                }
                else if (Input.GetKeyDown(KeyCode.F) && !isShifting)
                {
                    ShiftDownAutomatic();
                }
            }
            float vInput = Input.GetAxis("Vertical");
            float hInput = Input.GetAxis("Horizontal");
            if (vInput >= 0.1f)
            {
                gasInput = vInput;
                brakeInput = 0f;
            }
            if (vInput <= -0.1f)
            {
                gasInput = 0f;
                brakeInput = vInput;
            }
        }
        UpdateDisplays();
    }

    void FixedUpdate()
    {
        // Only process input if driver is seated
        if (driverSeat == null || !driverSeat.isOccupied)
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.motorTorque = 0;
                wheel.wheelCollider.brakeTorque = brakeTorque;
            }
            return;
        }

        //Calculate the speed of the vehicle
        vehicleSpeed = Vector3.Dot(transform.forward, rigidBody.velocity);
        //Update RPM based on wheel speed and gear ratio
        UpdateRPM();
        //Handle Shifting Logic
        HandleShifting();
        //Apply Engine braking when appropriate
        ApplyEngineBraking();

        //Process vehicle movement even when driver is not seated (rolling)
        bool hasDriver = (driverSeat != null && driverSeat.isOccupied);

        // Get input for acceleration and steering
        float vInput = Input.GetAxis("Vertical");
        float hInput = Input.GetAxis("Horizontal");
        bool handbrakeApplied = Input.GetKey(KeyCode.Space);

        // Calculate current speed along the car's forward axis
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(vehicleSpeed));

        // Determine if we're braking or want to go in reverse
        bool wantsToReverse = vInput < -0.1f;
        bool regularBrakeApplied = wantsToReverse && vehicleSpeed > 0.1f;
        //bool wantsToAccelerate = vInput > 0.1f || (wantsToReverse && vehicleSpeed <= 0.1f);

        bool canApplyPower = (currentGear == GearPosition.Drive || currentGear >= GearPosition.First || (currentGear == GearPosition.Reverse && wantsToReverse));
        bool wantsToAccelerate = vInput > 0.1f && canApplyPower;

        // Reduce motor torque and steering at high speeds for better handling
        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSped, speedFactor);

        // Handle handbrake physics at speed
        if (handbrakeApplied && Mathf.Abs(vehicleSpeed) > 5.0f)
        {
            rigidBody.angularDrag = angularDragWhenDrifting;
        }
        else
        {
            rigidBody.angularDrag = originalAngularDrag;
        }

        // Process each wheel
        foreach (var wheel in wheels)
        {
            bool isFrontWheel = (wheel.wheelPosition == WheelControl.WheelPosition.FrontLeft ||
                                wheel.wheelPosition == WheelControl.WheelPosition.FrontRight);
            bool isRearWheel = !isFrontWheel;
            bool isDriveWheel = false;

            // Determine if this is a drive wheel based on drive type
            switch (driveType)
            {
                case DriveType.FrontWheelDrive:
                    isDriveWheel = isFrontWheel;
                    break;
                case DriveType.RearWheelDrive:
                    isDriveWheel = isRearWheel;
                    break;
                case DriveType.AllWheelDrive:
                    isDriveWheel = true;
                    break;
            }

            // Apply steering
            if (wheel.steerable)
            {
                wheel.wheelCollider.steerAngle = hInput * currentSteerRange;
            }

            // Handle wheel friction
            WheelFrictionCurve sidewaysFriction = wheel.wheelCollider.sidewaysFriction;
            WheelFrictionCurve forwardFriction = wheel.wheelCollider.forwardFriction;

            // Reset friction to normal
            sidewaysFriction.stiffness = wheelGripFactor;
            forwardFriction.stiffness = wheelGripFactor;

            // Apply handbrake or burnout friction changes
            if (isDriveWheel && wantsToAccelerate && ((handbrakeApplied && isRearWheel) ||
                    (regularBrakeApplied && isFrontWheel && driveType == DriveType.FrontWheelDrive)))
            {
                forwardFriction.stiffness = wheelGripFactor * (1 - burnoutThreshold);
            }
            else if (handbrakeApplied && isRearWheel)
            {
                sidewaysFriction.stiffness = wheelGripFactor * (1 - handbrakeGripLoss);
                forwardFriction.stiffness = wheelGripFactor * (1 - handbrakeGripLoss);
            }

            // Apply modified friction curves
            wheel.wheelCollider.sidewaysFriction = sidewaysFriction;
            wheel.wheelCollider.forwardFriction = forwardFriction;

            //Apply motor torqque and brakes based on gear and input
            float torqueToApply = 0f;
            float brakeToApply = 0f;

            //Handle Park
            if (currentGear == GearPosition.Park)
            {
                brakeTorque = brakeTorque * 2f;
            }
            //Handle movement based on gear
            else if (currentGear == GearPosition.Neutral)
            {
                torqueToApply = 0f;

                //Apply brakes if requested
                if (regularBrakeApplied || handbrakeApplied)
                {
                    brakeToApply = handbrakeApplied && isRearWheel ? handbrakeTorque : brakeTorque * (isFrontWheel ? frontBrakeRatio : (1 - frontBrakeRatio));
                }
            }
            else if (currentGear == GearPosition.Reverse)
            {
                //In reverse gear, apply negative torque when pressing down
                if (vInput > 0.1f && isDriveWheel && clutchEngagement > 0.1f)
                {
                    float gearRatio = GetCurrentGearRatio();
                    torqueToApply = vInput * currentMotorTorque * gearRatio * clutchEngagement;
                }
                else if (vInput < -0.1f)
                {
                    brakeToApply = brakeTorque * (isFrontWheel ? frontBrakeRatio : (1 - frontBrakeRatio));
                }
            }
            else if (wantsToAccelerate && isDriveWheel && clutchEngagement > 0.1f)
            {
                //apply power through transmission
                float gearRatio = GetCurrentGearRatio();
                torqueToApply = currentMotorTorque * gearRatio * clutchEngagement;
                if (regularBrakeApplied && !isDriveWheel)
                {
                    brakeToApply = brakeTorque * frontBrakeRatio;
                }
            }
            else if (handbrakeApplied)
            {
                torqueToApply = 0f;
                brakeToApply = isRearWheel ? handbrakeTorque : 0f;
            }
            else if (regularBrakeApplied)
            {
                torqueToApply = 0f;
                float brakeRatio = isFrontWheel ? frontBrakeRatio : (1 - frontBrakeRatio);
                brakeToApply = vInput * brakeTorque * brakeRatio;

                //brakeToApply = ApplyBrake(wheel, isFrontWheel, vInput);
            }

            //apply calculated torque and braking
            wheel.wheelCollider.motorTorque = torqueToApply;
            wheel.wheelCollider.brakeTorque = brakeToApply;
        }
    }

    float ApplyBrake(WheelControl wheel, bool isFrontWheel, float vInput)
    {
        if (isFrontWheel)
            brakeTorque = vInput * brakeTorque * frontBrakeRatio;
        else
            brakeTorque = vInput * brakeTorque * (1 - frontBrakeRatio);
        return brakeTorque;
    }

    private void IntializeDefaultGearRatios()
    {
        //gearRatios = new float[] { 0, 0.25f, 0.5f, 0.75f, 1, 1.25f, 1.5f, 1.75f, 2 };
        //Create default Gear ratios
        if (transmissionType == TransmissionType.Automatic)
        {
            gearRatios = new GearRatio[]
            {
                new GearRatio { gear = GearPosition.Park, ratio = 0f, minRPM = 0f, maxRPM = 0f},
                new GearRatio { gear = GearPosition.Reverse, ratio = -3.5f, minRPM = idleRPM, maxRPM = redlineRPM, optimalMinRPM = idleRPM + 500, optimalMaxRPM = redlineRPM - 500 },
                new GearRatio { gear = GearPosition.Neutral, ratio = 0f, minRPM = 0f, maxRPM = 0f },
                new GearRatio { gear = GearPosition.Drive, ratio = 3.5f, minRPM = idleRPM, maxRPM = redlineRPM, optimalMinRPM = idleRPM + 500, optimalMaxRPM = redlineRPM - 500 }
            };
        }
        else //manual
        {
            gearRatios = new GearRatio[]
            {
                new GearRatio { gear = GearPosition.Reverse, ratio = -3.5f, minRPM = idleRPM, maxRPM = redlineRPM, optimalMinRPM = idleRPM + 500, optimalMaxRPM = redlineRPM - 500 },
                new GearRatio { gear = GearPosition.Neutral, ratio = 0f, minRPM = 0f, maxRPM = 0f },
                new GearRatio { gear = GearPosition.First, ratio = 3.5f, minRPM = idleRPM, maxRPM = redlineRPM, optimalMinRPM = idleRPM + 500, optimalMaxRPM = redlineRPM - 500 },
                new GearRatio { gear = GearPosition.Second, ratio = 2.5f, minRPM = idleRPM, maxRPM = redlineRPM, optimalMinRPM = idleRPM + 500, optimalMaxRPM = redlineRPM - 500 },
                new GearRatio { gear = GearPosition.Third, ratio = 1.8f, minRPM = idleRPM, maxRPM = redlineRPM, optimalMinRPM = idleRPM + 500, optimalMaxRPM = redlineRPM - 500 },
                new GearRatio { gear = GearPosition.Fourth, ratio = 1.3f, minRPM = idleRPM, maxRPM = redlineRPM, optimalMinRPM = idleRPM + 500, optimalMaxRPM = redlineRPM - 500 },
                new GearRatio { gear = GearPosition.Fifth, ratio = 1.0f, minRPM = idleRPM, maxRPM = redlineRPM, optimalMinRPM = idleRPM + 500, optimalMaxRPM = redlineRPM - 500 }
            };
        }
    }

    private float GetCurrentGearRatio()
    {
        foreach (var gearRatio in gearRatios)
        {
            if (gearRatio.gear == currentGear)
            {
                return gearRatio.ratio;
            }
        }
        return 0f;
    }

private void UpdateRPM()
{
    float gearRatio = GetCurrentGearRatio();
    float throttleInput = driverSeat != null && driverSeat.isOccupied ? Input.GetAxis("Vertical") : 0f;
    bool isBraking = throttleInput < -0.1f;
    bool isAccelerating = throttleInput > 0.1f;
    
    if (gearRatio == 0f) // Neutral or Park
    {
        // In Neutral, RPM is controlled by throttle input
        targetRPM = Mathf.Lerp(idleRPM, redlineRPM * 0.6f, Mathf.Max(0, throttleInput));
    }
    else
    {
        // For Drive gears, separate the RPM calculation from wheel speed
        if (isAccelerating)
        {
            // When accelerating, RPM is primarily controlled by throttle input
            // This allows RPM to reach redline regardless of wheel speed
            targetRPM = Mathf.Lerp(currentRPM, redlineRPM, throttleInput * 0.1f);
            
            // Apply a maximum speed limit based on current gear
            float maxSpeed = 0f;
            switch (currentGear)
            {
                case GearPosition.First: maxSpeed = 40f; break;
                case GearPosition.Second: maxSpeed = 70f; break;
                case GearPosition.Third: maxSpeed = 100f; break;
                case GearPosition.Fourth: maxSpeed = 140f; break;
                case GearPosition.Fifth: maxSpeed = 200f; break;
                case GearPosition.Reverse: maxSpeed = 30f; break;
                default: maxSpeed = float.MaxValue; break;
            }
            
            // Calculate current speed in km/h
            float currentSpeed = CalculateSpeed();
            
            // If we're exceeding the max speed for this gear, limit RPM
            if (currentSpeed >= maxSpeed)
            {
                targetRPM = currentRPM;
            }
        }
        else
        {
            // When not accelerating, RPM is based on wheel speed
            float wheelAvgRPM = 0f;
            int wheelCount = 0;
            
            foreach (var wheel in wheels)
            {
                if (wheel.wheelCollider != null)
                {
                    wheelAvgRPM += Mathf.Abs(wheel.wheelCollider.rpm);
                    wheelCount++;
                }
            }
            
            if (wheelCount > 0)
            {
                wheelAvgRPM /= wheelCount;
                // Simple conversion factor that works well with the gear ratios
                float conversionFactor = 2.5f;
                targetRPM = wheelAvgRPM * Mathf.Abs(gearRatio) * conversionFactor + idleRPM;
            }
        }
    }
    
    // Use different interpolation speeds based on whether accelerating or decelerating
    float rpmChangeRate;
    if (isBraking)
    {
        // Rapid RPM drop when braking
        rpmChangeRate = 15f;
    }
    else if (!isAccelerating && currentRPM > targetRPM)
    {
        // Faster RPM drop when releasing accelerator
        rpmChangeRate = 10f;
    }
    else
    {
        // Normal RPM change rate
        rpmChangeRate = 5f;
    }
    
    // Smoothly interpolate current RPM to target with appropriate rate
    currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * rpmChangeRate);
    
    // Clamp RPM to valid range
    currentRPM = Mathf.Clamp(currentRPM, 0f, maxRPM);
}




    private float CalculateEngineTorque(float throttleInput)
    {
        //simple torque curve based on RPM
        float normalizedRPM = (currentRPM - idleRPM) / (redlineRPM - idleRPM);
        float torqueFactor = 0f;

        if (normalizedRPM < 0.3f)
        {
            torqueFactor = Mathf.Lerp(0.4f, 0.8f, normalizedRPM / 0.3f);
        }
        else if (normalizedRPM < 0.7f)
        {
            torqueFactor = Mathf.Lerp(0.8f, 1.0f, (normalizedRPM - 0.3f) / 0.4f);
        }
        else
        {
            torqueFactor = Mathf.Lerp(1.0f, 0.7f, (normalizedRPM - 0.7f) / 0.3f);
        }
        return motorTorque * torqueFactor * throttleInput;
    }

    private void HandleShifting()
    {
        //Handle automatic shifting
        if (transmissionType == TransmissionType.Automatic && !isShifting)
        {
            AutomaticShiftLogic();
        }
        //Handle shift timing and clutch engagement
        if (isShifting)
        {
            shiftTime += Time.deltaTime;
            clutchEngagement = Mathf.Clamp01((shiftDelay - shiftTime) / shiftDelay);

            if (shiftTime >= shiftDelay)
            {
                isShifting = false;
                clutchEngagement = 1f;
            }
        }
        else
        {
            //gradually engage clutch based on speed to prevent stalling
            {
                float speedFactor = Mathf.Clamp01(Mathf.Abs(vehicleSpeed) / clutchEngageSpeed);
                clutchEngagement = Mathf.Lerp(0.2f, 1f, speedFactor);
            }
        }
    }

    private void AutomaticShiftLogic()
    {
        /*
        //simple automatic transmission logic
        if (currentGear == GearPosition.Drive)
        {
            //already in Drive, nothing to do
            return;
        }

        float vInput = Input.GetAxis("Vertical");
        //Shift to appropriate gear based on input
        if ((vInput > 0.1f && currentGear != GearPosition.Drive))
        {
            ShiftToGear(GearPosition.Drive);
        }
        else if (vInput < -0.1f && vehicleSpeed < 1f && currentGear != GearPosition.Reverse)
        {
            ShiftToGear(GearPosition.Reverse);
        }
        else if (Mathf.Abs(vInput) < 0.1f && Mathf.Abs(vehicleSpeed) < 0.5f)
        {
            //When stopped and no input, shift to Park
            ShiftToGear(GearPosition.Park);
            //TODO: shoud this not be neutral with manual input for park?
        }
        */
    }

    private void ShiftUp()
    {
        if (transmissionType != TransmissionType.Manual)
        {
            return;
        }
        GearPosition nextGear = currentGear;
        switch (currentGear)
        {
            case GearPosition.Reverse:
                nextGear = GearPosition.Neutral;
                break;
            case GearPosition.Neutral:
                nextGear = GearPosition.First;
                break;
            case GearPosition.First:
                nextGear = GearPosition.Second;
                break;
            case GearPosition.Second:
                nextGear = GearPosition.Third;
                break;
            case GearPosition.Third:
                nextGear = GearPosition.Fourth;
                break;
            case GearPosition.Fourth:
                nextGear = GearPosition.Fifth;
                break;
        }
        if (nextGear != currentGear)
        {
            ShiftToGear(nextGear);
        }
    }

    private void ShiftDown()
    {
        if (transmissionType != TransmissionType.Manual)
        {
            return;
        }
        GearPosition nextGear = currentGear;
        switch (currentGear)
        {
            case GearPosition.First:
                nextGear = GearPosition.Neutral;
                break;
            case GearPosition.Second:
                nextGear = GearPosition.First;
                break;
            case GearPosition.Third:
                nextGear = GearPosition.Second;
                break;
            case GearPosition.Fourth:
                nextGear = GearPosition.Third;
                break;
            case GearPosition.Fifth:
                nextGear = GearPosition.Fourth;
                break;
            case GearPosition.Neutral:
                //Only allow reverse if nearly stopped
                if (Mathf.Abs(vehicleSpeed) < 2f)
                    nextGear = GearPosition.Reverse;
                break;
        }
        if (nextGear != currentGear)
        {
            ShiftToGear(nextGear);
        }
    }

    private void ShiftToGear(GearPosition gear)
    {
        //Don't allow shifting to reverse while moving forward quickly
        if (gear == GearPosition.Reverse && vehicleSpeed > 2f)
        {
            //TODO: add gearbox grinding noise here
            return;
        }
        //Don't allow shifting to a forward gear while moving backward quickly
        if ((gear == GearPosition.Drive || gear >= GearPosition.First) && vehicleSpeed < -2f)
        {
            return;
            //TODO: add the gearbox grinding noise here too
        }

        currentGear = gear;
        isShifting = true;
        shiftTime = 0f;

        //Debug Log
        Debug.Log($"Shifted to {gear}");
    }

    private void ApplyEngineBraking()
    {
        if (currentGear == GearPosition.Neutral || currentGear == GearPosition.Park)
            return;

        float throttleInput = driverSeat != null && driverSeat.isOccupied ? Input.GetAxis("Vertical") : 0f;

        // Apply engine braking when not accelerating
        if (Mathf.Abs(throttleInput) < 0.1f && Mathf.Abs(vehicleSpeed) > 1f)
        {
            float brakingForce = engineBrakingFactor * Mathf.Abs(GetCurrentGearRatio()) * brakeTorque;

            foreach (var wheel in wheels)
            {
                if (wheel.wheelCollider != null)
                {
                    wheel.wheelCollider.brakeTorque += brakingForce;
                }
            }
        }
    }

    private void ShiftUpAutomatic()
    {
        //Move between the PRDN pattern
        switch (currentGear)
        {
            case GearPosition.Park:
                currentGear = GearPosition.Reverse;
                break;
            case GearPosition.Reverse:
                currentGear = GearPosition.Neutral;
                break;
            case GearPosition.Neutral:
                currentGear = GearPosition.Drive;
                break;
        }
    }

    private void ShiftDownAutomatic()
    {
        //Move between the PRDN pattern
        switch (currentGear)
        {
            case GearPosition.Drive:
                currentGear = GearPosition.Neutral;
                break;
            case GearPosition.Neutral:
                currentGear = GearPosition.Reverse;
                break;
            case GearPosition.Reverse:
                currentGear = GearPosition.Park;
                break;
        }
    }

    private float CalculateSpeed()
    {
        //Find rear wheels
        WheelCollider rearLeftWheel = null;
        WheelCollider rearRightWheel = null;

        foreach (var wheel in wheels)
        {
            if (wheel.wheelPosition == WheelControl.WheelPosition.RearLeft)
                rearLeftWheel = wheel.wheelCollider;

            else if (wheel.wheelPosition == WheelControl.WheelPosition.RearRight)
                rearRightWheel = wheel.wheelCollider;
        }
        float rearWheelRPM = 0;
        int wheelCount = 0;

        if (rearLeftWheel != null)
        {
            rearWheelRPM += Mathf.Abs(rearLeftWheel.rpm);
            wheelRadius += rearLeftWheel.radius;
            wheelCount++;
        }
        if (rearRightWheel != null)
        {
            rearWheelRPM += Mathf.Abs(rearRightWheel.rpm);
            wheelRadius += rearRightWheel.radius;
            wheelCount++;
        }
        if (wheelCount > 0)
        {
            rearWheelRPM /= wheelCount;
            wheelRadius /= wheelCount;
            //calculate speed in m/s
            float speedMS = rearWheelRPM * 2f * Mathf.PI * wheelRadius / 60f;
            return useMetricUnits ? speedMS * 3.6f : speedMS * 2.23694f;
        }
        //fallback to rigidbody velocity if no wheels found
        return useMetricUnits ? Mathf.Abs(vehicleSpeed) * 3.6f : Mathf.Abs(vehicleSpeed) * 2.23694f;
    }


    private void UpdateDisplays()
    {
        if (rpmDisplay != null)
        {
            rpmDisplay.text = $"{Mathf.Round(currentRPM)} RPM";

            //Change color based on RPM range
            if (currentRPM > redlineRPM)
            {
                rpmDisplay.color = Color.red;
            }
            else if (currentRPM > optimalRPM)
            {
                rpmDisplay.color = Color.yellow;
            }
            else
            {
                rpmDisplay.color = Color.white;
            }
        }
        if (gearDisplay != null)
        {
            string gearText = "";
            if (transmissionType == TransmissionType.Automatic)
            {
                switch (currentGear)
                {
                    case GearPosition.Park:
                        gearText = "P";
                        break;
                    case GearPosition.Reverse:
                        gearText = "R";
                        break;
                    case GearPosition.Neutral:
                        gearText = "N";
                        break;
                    case GearPosition.Drive:
                        gearText = "D";
                        break;
                }
            }
            else
            {
                switch (currentGear)
                {
                    case GearPosition.Reverse: gearText = "R"; break;
                    case GearPosition.Neutral: gearText = "N"; break;
                    case GearPosition.First: gearText = "1"; break;
                    case GearPosition.Second: gearText = "2"; break;
                    case GearPosition.Third: gearText = "3"; break;
                    case GearPosition.Fourth: gearText = "4"; break;
                    case GearPosition.Fifth: gearText = "5"; break;
                }
            }
            gearDisplay.text = gearText;
        }
        if (speedDisplay != null)
        {
            speedDisplay.text = (rigidBody.velocity.magnitude * 3.6f).ToString("F1") + " km/h";;
            //speedDisplay.text = $"{Mathf.Round(CalculateSpeed())} {(useMetricUnits ? "km/h" : "mph")}";
        }
    }

    public void ToggleSpeedUnits()
    {
        useMetricUnits = !useMetricUnits;
        UpdateDisplays();
    }
}