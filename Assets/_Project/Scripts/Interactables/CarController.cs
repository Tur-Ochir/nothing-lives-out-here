using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WheelCollider-based car controller that dynamically adds Rigidbody and enables WheelColliders 
/// when the driver enters, and destroys Rigidbody / disables WheelColliders when the driver exits.
/// Supports direction inversion for reversed 3D models.
/// </summary>
public class CarController : MonoBehaviour
{
    public enum DriveType
    {
        AllWheelDrive,
        FrontWheelDrive,
        RearWheelDrive
    }

    [Header("Driver & Seat References")]
    [Tooltip("The driving seat associated with this car.")]
    public CarSeat driverSeat;

    [Header("Orientation & Direction")]
    [Tooltip("Check this if pressing forward (W) drives the car backward due to model orientation.")]
    public bool invertDriveDirection = true;

    [Tooltip("Check this if steering left/right is inverted.")]
    public bool invertSteering = false;

    [Header("Wheel Colliders")]
    [Tooltip("Front Left Wheel Collider.")]
    public WheelCollider frontLeftCollider;
    [Tooltip("Front Right Wheel Collider.")]
    public WheelCollider frontRightCollider;
    [Tooltip("Rear Left Wheel Collider.")]
    public WheelCollider rearLeftCollider;
    [Tooltip("Rear Right Wheel Collider.")]
    public WheelCollider rearRightCollider;

    [Header("Visual Wheel Meshes")]
    [Tooltip("Visual Transform for Front Left Wheel.")]
    public Transform frontLeftMesh;
    [Tooltip("Visual Transform for Front Right Wheel.")]
    public Transform frontRightMesh;
    [Tooltip("Visual Transform for Rear Left Wheel.")]
    public Transform rearLeftMesh;
    [Tooltip("Visual Transform for Rear Right Wheel.")]
    public Transform rearRightMesh;
    [Tooltip("Optional rotation offset applied to visual wheel meshes (Euler angles).")]
    public Vector3 wheelMeshRotationOffset = Vector3.zero;

    [Header("Motor & Transmission")]
    [Tooltip("Drivetrain type (FWD, RWD, AWD).")]
    public DriveType driveType = DriveType.FrontWheelDrive;

    [Tooltip("Motor torque applied during forward acceleration.")]
    public float motorForce = 1500f;

    [Tooltip("Motor torque applied during reverse acceleration.")]
    public float reverseMotorForce = 1000f;

    [Tooltip("Braking torque applied when braking.")]
    public float brakeForce = 3000f;

    [Tooltip("Handbrake torque applied to rear wheels when handbraking.")]
    public float handbrakeForce = 5000f;

    [Tooltip("Light braking torque applied when coasting without throttle.")]
    public float coastBrakeForce = 60f;

    [Tooltip("Maximum forward speed in km/h.")]
    public float maxSpeedKmh = 100f;

    [Tooltip("Maximum reverse speed in km/h.")]
    public float maxReverseSpeedKmh = 30f;

    [Header("Steering")]
    [Tooltip("Maximum steering angle for front wheels in degrees.")]
    public float maxSteerAngle = 35f;

    [Tooltip("How fast steering turns towards input direction.")]
    public float steerResponseSpeed = 8f;

    [Tooltip("Whether steering angle reduces at high speeds for stability.")]
    public bool speedSensitiveSteering = true;

    [Tooltip("Steering multiplier at max speed (0.1 - 1.0).")]
    [Range(0.1f, 1f)]
    public float highSpeedSteerReduction = 0.5f;

    [Header("Stability & Physics")]
    [Tooltip("Car mass in kg when Rigidbody is added.")]
    public float vehicleMass = 1200f;

    [Tooltip("Offset applied to Rigidbody Center of Mass (lower center of mass prevents flipping).")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.4f, 0f);

    [Tooltip("Downforce applied to increase grip at higher speeds.")]
    public float downforce = 120f;

    [Tooltip("Anti-roll bar force to prevent body roll on sharp turns.")]
    public float antiRollForce = 5000f;

    [Header("Lights")]
    [Tooltip("Front headlight lights.")]
    public List<Light> headlights = new List<Light>();

    [Tooltip("Back taillights / brake lights.")]
    public List<Light> taillights = new List<Light>();

    [Tooltip("Whether headlights are currently turned on.")]
    public bool headlightsOn = true;

    [Header("Audio")]
    [Tooltip("Audio source for engine loop.")]
    public AudioSource engineAudio;

    [Tooltip("Audio clip for engine start.")]
    public AudioClip engineStartSFX;

    [Tooltip("Audio clip for engine stop.")]
    public AudioClip engineStopSFX;

    [Tooltip("Audio clip for horn.")]
    public AudioClip hornSFX;

    [Tooltip("Pitch of engine at idle.")]
    public float minEnginePitch = 0.7f;

    [Tooltip("Pitch of engine at max speed.")]
    public float maxEnginePitch = 1.8f;

    // Runtime properties
    public PlayerDriver CurrentDriver { get; private set; }
    public bool HasDriver => CurrentDriver != null;

    /// <summary>
    /// Current vehicle speed in km/h.
    /// </summary>
    public float CurrentSpeedKmh { get; private set; }

    /// <summary>
    /// Current vehicle forward speed in m/s (positive = forward, negative = reverse).
    /// </summary>
    public float CurrentForwardSpeed { get; private set; }

    /// <summary>
    /// Backward-compatible speed in m/s.
    /// </summary>
    public float CurrentSpeed => Mathf.Abs(CurrentForwardSpeed);

    public bool IsGrounded => (frontLeftCollider != null && frontLeftCollider.isGrounded) ||
                              (frontRightCollider != null && frontRightCollider.isGrounded) ||
                              (rearLeftCollider != null && rearLeftCollider.isGrounded) ||
                              (rearRightCollider != null && rearRightCollider.isGrounded);

    private Rigidbody rb;
    private float currentSteerAngle = 0f;
    private Vector2 currentInput = Vector2.zero;
    private bool isHandbraking = false;

    public event Action<PlayerDriver> OnDriverEntered;
    public event Action<PlayerDriver> OnDriverExited;
    public event Action<bool> OnHeadlightsChanged;

    private void Awake()
    {
        if (driverSeat == null)
        {
            driverSeat = GetComponentInChildren<CarSeat>();
        }

        if (engineAudio == null)
        {
            engineAudio = GetComponent<AudioSource>();
            if (engineAudio == null && (engineStartSFX != null || engineStopSFX != null))
            {
                engineAudio = gameObject.AddComponent<AudioSource>();
            }
        }

        AutoFindLightsAndMeshes();
    }

    private void Start()
    {
        // Remove Rigidbody and disable WheelColliders on startup until driver enters
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
            rb = null;
        }
        SetWheelCollidersActive(false);

        SetHeadlights(headlightsOn);
    }

    private void SetWheelCollidersActive(bool active)
    {
        if (frontLeftCollider != null) { frontLeftCollider.enabled = active; frontLeftCollider.gameObject.SetActive(active); }
        if (frontRightCollider != null) { frontRightCollider.enabled = active; frontRightCollider.gameObject.SetActive(active); }
        if (rearLeftCollider != null) { rearLeftCollider.enabled = active; rearLeftCollider.gameObject.SetActive(active); }
        if (rearRightCollider != null) { rearRightCollider.enabled = active; rearRightCollider.gameObject.SetActive(active); }
    }

    private void AutoFindLightsAndMeshes()
    {
        // Auto-find lights if list is empty
        if (headlights.Count == 0 || taillights.Count == 0)
        {
            var lights = GetComponentsInChildren<Light>(true);
            foreach (var l in lights)
            {
                string lowerName = l.gameObject.name.ToLower();
                if (lowerName.Contains("front") || lowerName.Contains("spot"))
                {
                    if (!headlights.Contains(l)) headlights.Add(l);
                }
                else if (lowerName.Contains("back") || lowerName.Contains("rear") || lowerName.Contains("tail"))
                {
                    if (!taillights.Contains(l)) taillights.Add(l);
                }
            }
        }

        // Auto-find wheel colliders if null
        if (frontLeftCollider == null || frontRightCollider == null || rearLeftCollider == null || rearRightCollider == null)
        {
            var colliders = GetComponentsInChildren<WheelCollider>();
            foreach (var col in colliders)
            {
                string lower = col.gameObject.name.ToLower();
                if (frontLeftCollider == null && (lower.Contains("fl") || (lower.Contains("front") && lower.Contains("left")) || lower.Contains("wheel_fl")))
                    frontLeftCollider = col;
                else if (frontRightCollider == null && (lower.Contains("fr") || (lower.Contains("front") && lower.Contains("right")) || lower.Contains("wheel_fr")))
                    frontRightCollider = col;
                else if (rearLeftCollider == null && (lower.Contains("rl") || (lower.Contains("rear") && lower.Contains("left")) || lower.Contains("wheel_rl")))
                    rearLeftCollider = col;
                else if (rearRightCollider == null && (lower.Contains("rr") || (lower.Contains("rear") && lower.Contains("right")) || lower.Contains("wheel_rr")))
                    rearRightCollider = col;
            }
        }

        // Auto-find visual wheel meshes if not set
        if (frontLeftMesh == null || frontRightMesh == null || rearLeftMesh == null || rearRightMesh == null)
        {
            var wheels = transform.Find("Wheels");
            var wheels1 = transform.Find("Wheels.001");
            var wheels2 = transform.Find("Wheels.002");
            var wheels3 = transform.Find("Wheels.003");

            if (frontRightMesh == null && wheels != null) frontRightMesh = wheels;
            if (frontLeftMesh == null && wheels1 != null) frontLeftMesh = wheels1;
            if (rearLeftMesh == null && wheels2 != null) rearLeftMesh = wheels2;
            if (rearRightMesh == null && wheels3 != null) rearRightMesh = wheels3;
        }
    }

    /// <summary>
    /// Set input values from the driver.
    /// </summary>
    public void Drive(Vector2 input, bool handbrake = false)
    {
        currentInput = input;
        isHandbraking = handbrake;
    }

    private void Update()
    {
        if (HasDriver)
        {
            UpdateWheelMeshes();
        }
        UpdateEngineAudio();
    }

    private void FixedUpdate()
    {
        if (!HasDriver || rb == null) return;

        CalculateSpeeds();
        ApplyMotorAndBrakes();
        ApplySteering();
        ApplyDownforce();
        ApplyAntiRollBars();
    }

    private void CalculateSpeeds()
    {
        if (rb == null) return;

        Vector3 velocity = rb.linearVelocity;
        CurrentSpeedKmh = velocity.magnitude * 3.6f;
        Vector3 forwardVec = invertDriveDirection ? -transform.forward : transform.forward;
        CurrentForwardSpeed = Vector3.Dot(velocity, forwardVec);
    }

    private void ApplyMotorAndBrakes()
    {
        float throttle = HasDriver ? currentInput.y : 0f;

        float forwardTorque = 0f;
        float currentBrakeTorque = 0f;

        bool hasWheelColliders = frontLeftCollider != null || rearLeftCollider != null;
        if (!hasWheelColliders) return;

        // Handbrake
        if (isHandbraking)
        {
            currentBrakeTorque = handbrakeForce;
            ApplyBrakeTorque(currentBrakeTorque);
            ApplyMotorTorque(0f);
            return;
        }

        // Forward driving (W)
        if (throttle > 0.05f)
        {
            if (CurrentForwardSpeed < -0.5f)
            {
                // Moving backward, apply brake to stop first
                currentBrakeTorque = brakeForce * throttle;
                forwardTorque = 0f;
            }
            else if (CurrentSpeedKmh < maxSpeedKmh)
            {
                forwardTorque = throttle * motorForce;
                currentBrakeTorque = 0f;
            }
            else
            {
                forwardTorque = 0f;
                currentBrakeTorque = 0f;
            }
        }
        // Reverse driving (S)
        else if (throttle < -0.05f)
        {
            float absThrottle = Mathf.Abs(throttle);
            if (CurrentForwardSpeed > 0.5f)
            {
                // Moving forward, apply brake to slow down
                currentBrakeTorque = brakeForce * absThrottle;
                forwardTorque = 0f;
            }
            else if (CurrentSpeedKmh < maxReverseSpeedKmh)
            {
                forwardTorque = -absThrottle * reverseMotorForce;
                currentBrakeTorque = 0f;
            }
            else
            {
                forwardTorque = 0f;
                currentBrakeTorque = 0f;
            }
        }
        // Coasting / No throttle
        else
        {
            forwardTorque = 0f;
            currentBrakeTorque = coastBrakeForce;
        }

        ApplyMotorTorque(forwardTorque);
        ApplyBrakeTorque(currentBrakeTorque);
    }

    private void ApplyMotorTorque(float torque)
    {
        float directedTorque = invertDriveDirection ? -torque : torque;

        switch (driveType)
        {
            case DriveType.FrontWheelDrive:
                if (frontLeftCollider != null) frontLeftCollider.motorTorque = directedTorque;
                if (frontRightCollider != null) frontRightCollider.motorTorque = directedTorque;
                if (rearLeftCollider != null) rearLeftCollider.motorTorque = 0f;
                if (rearRightCollider != null) rearRightCollider.motorTorque = 0f;
                break;

            case DriveType.RearWheelDrive:
                if (frontLeftCollider != null) frontLeftCollider.motorTorque = 0f;
                if (frontRightCollider != null) frontRightCollider.motorTorque = 0f;
                if (rearLeftCollider != null) rearLeftCollider.motorTorque = directedTorque;
                if (rearRightCollider != null) rearRightCollider.motorTorque = directedTorque;
                break;

            case DriveType.AllWheelDrive:
                float halfTorque = directedTorque * 0.5f;
                if (frontLeftCollider != null) frontLeftCollider.motorTorque = halfTorque;
                if (frontRightCollider != null) frontRightCollider.motorTorque = halfTorque;
                if (rearLeftCollider != null) rearLeftCollider.motorTorque = halfTorque;
                if (rearRightCollider != null) rearRightCollider.motorTorque = halfTorque;
                break;
        }
    }

    private void ApplyBrakeTorque(float torque)
    {
        if (frontLeftCollider != null) frontLeftCollider.brakeTorque = torque;
        if (frontRightCollider != null) frontRightCollider.brakeTorque = torque;
        if (rearLeftCollider != null) rearLeftCollider.brakeTorque = torque;
        if (rearRightCollider != null) rearRightCollider.brakeTorque = torque;
    }

    private void ApplySteering()
    {
        float steerInput = HasDriver ? currentInput.x : 0f;
        if (invertSteering) steerInput = -steerInput;

        float targetSteerAngle = steerInput * maxSteerAngle;

        // Reduce steer angle at higher speeds for smoother handling
        if (speedSensitiveSteering && maxSpeedKmh > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(CurrentSpeedKmh / maxSpeedKmh);
            float multiplier = Mathf.Lerp(1f, highSpeedSteerReduction, speedFactor);
            targetSteerAngle *= multiplier;
        }

        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * steerResponseSpeed);

        if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle;
        if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void ApplyDownforce()
    {
        if (rb != null && IsGrounded)
        {
            rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);
        }
    }

    private void ApplyAntiRollBars()
    {
        ApplyAntiRoll(frontLeftCollider, frontRightCollider);
        ApplyAntiRoll(rearLeftCollider, rearRightCollider);
    }

    private void ApplyAntiRoll(WheelCollider wheelL, WheelCollider wheelR)
    {
        if (wheelL == null || wheelR == null || rb == null) return;

        WheelHit hit;
        float travelL = 1.0f;
        float travelR = 1.0f;

        bool groundedL = wheelL.GetGroundHit(out hit);
        if (groundedL)
        {
            travelL = (-wheelL.transform.InverseTransformPoint(hit.point).y - wheelL.radius) / wheelL.suspensionDistance;
        }

        bool groundedR = wheelR.GetGroundHit(out hit);
        if (groundedR)
        {
            travelR = (-wheelR.transform.InverseTransformPoint(hit.point).y - wheelR.radius) / wheelR.suspensionDistance;
        }

        float antiRollDelta = (travelL - travelR) * antiRollForce;

        if (groundedL)
            rb.AddForceAtPosition(wheelL.transform.up * -antiRollDelta, wheelL.transform.position);
        if (groundedR)
            rb.AddForceAtPosition(wheelR.transform.up * antiRollDelta, wheelR.transform.position);
    }

    private void UpdateWheelMeshes()
    {
        UpdateSingleWheelPose(frontLeftCollider, frontLeftMesh);
        UpdateSingleWheelPose(frontRightCollider, frontRightMesh);
        UpdateSingleWheelPose(rearLeftCollider, rearLeftMesh);
        UpdateSingleWheelPose(rearRightCollider, rearRightMesh);
    }

    private void UpdateSingleWheelPose(WheelCollider col, Transform mesh)
    {
        if (!HasDriver) return;
        if (col == null || mesh == null) return;

        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot * Quaternion.Euler(wheelMeshRotationOffset);
    }

    private void UpdateEngineAudio()
    {
        if (engineAudio == null) return;

        if (HasDriver)
        {
            if (!engineAudio.isPlaying)
            {
                engineAudio.loop = true;
                engineAudio.Play();
            }

            float speedFraction = Mathf.Clamp01(CurrentSpeedKmh / maxSpeedKmh);
            engineAudio.pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, speedFraction);
        }
        else
        {
            if (engineAudio.isPlaying && CurrentSpeedKmh < 1f)
            {
                engineAudio.Stop();
            }
            else if (engineAudio.isPlaying)
            {
                engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, minEnginePitch, Time.deltaTime * 3f);
            }
        }
    }

    /// <summary>
    /// Attaches a driver to this car. Adds Rigidbody component and activates WheelColliders.
    /// </summary>
    public void SetDriver(PlayerDriver driver)
    {
        if (driver == null) return;

        CurrentDriver = driver;

        // 1. Add Rigidbody when driver enters
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
        }

        rb.mass = vehicleMass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.centerOfMass = centerOfMassOffset;

        // 2. Activate WheelColliders
        SetWheelCollidersActive(true);

        if (engineStartSFX != null && engineAudio != null)
        {
            engineAudio.PlayOneShot(engineStartSFX);
        }

        OnDriverEntered?.Invoke(driver);
    }

    /// <summary>
    /// Removes the current driver from this car. Deactivates WheelColliders and removes Rigidbody.
    /// </summary>
    public void RemoveDriver()
    {
        if (CurrentDriver == null) return;

        var prevDriver = CurrentDriver;
        CurrentDriver = null;
        currentInput = Vector2.zero;
        isHandbraking = false;

        // 1. Zero out wheel torques and deactivate wheel colliders
        ApplyMotorTorque(0f);
        ApplyBrakeTorque(brakeForce);
        SetWheelCollidersActive(false);

        // 2. Destroy Rigidbody when driver exits
        if (rb != null)
        {
            Destroy(rb);
            rb = null;
        }

        if (engineStopSFX != null && engineAudio != null)
        {
            engineAudio.PlayOneShot(engineStopSFX);
        }

        OnDriverExited?.Invoke(prevDriver);
    }

    /// <summary>
    /// Toggles the car headlights on/off.
    /// </summary>
    public void ToggleHeadlights()
    {
        SetHeadlights(!headlightsOn);
    }

    /// <summary>
    /// Sets headlights state.
    /// </summary>
    public void SetHeadlights(bool state)
    {
        headlightsOn = state;
        foreach (var light in headlights)
        {
            if (light != null)
            {
                light.enabled = headlightsOn;
            }
        }
        OnHeadlightsChanged?.Invoke(headlightsOn);
    }

    /// <summary>
    /// Plays the vehicle horn SFX.
    /// </summary>
    public void PlayHorn()
    {
        if (hornSFX != null && engineAudio != null)
        {
            engineAudio.PlayOneShot(hornSFX);
        }
    }
}
