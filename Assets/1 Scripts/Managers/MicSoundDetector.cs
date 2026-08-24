using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Captures physical microphone input in real time, computes RMS volume/dB,
/// provides noise floor calibration, smooth loudness output, and dispatches sound detection events.
/// Audio is processed purely in memory and never routed to audio listeners.
/// </summary>
public class MicSoundDetector : MonoBehaviour
{
    public static MicSoundDetector Instance { get; private set; }

    [Header("Device Settings")]
    [Tooltip("Microphone device name. Leave empty to use system default.")]
    [SerializeField] private string selectedDevice = "";
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int recordingLength = 10;
    [SerializeField] private int sampleWindow = 256;

    [Header("Loudness & Sensitivity")]
    [Range(1f, 50f)]
    [Tooltip("Multiplier for microphone signal gain.")]
    public float sensitivity = 12f;

    [Range(0f, 1f)]
    [Tooltip("Noise floor to filter out ambient room/PC background noise.")]
    public float noiseFloor = 0.015f;

    [Range(0.01f, 1f)]
    [Tooltip("Volume threshold above which sound/speaking is detected.")]
    public float noiseThreshold = 0.12f;

    [Range(0.01f, 1f)]
    [Tooltip("Volume threshold above which screaming or loud sudden noises are triggered.")]
    public float loudThreshold = 0.55f;

    [Header("Smoothing")]
    [Tooltip("How fast volume responds to sudden peaks (attack).")]
    public float attackSpeed = 30f;
    [Tooltip("How smoothly volume drops down during silence (decay).")]
    public float decaySpeed = 6f;

    [Header("Calibration")]
    [Tooltip("Automatically sample background room noise at start to set baseline noise floor.")]
    public bool autoCalibrateOnStart = true;
    [Tooltip("Duration in seconds for noise floor calibration.")]
    public float calibrationDuration = 1.5f;

    [Header("Debug & Fallback")]
    [Tooltip("Enable keyboard shortcuts to simulate mic input in Editor/Build for testing.")]
    public bool enableDebugSimulation = true;
    public KeyCode debugSpeakKey = KeyCode.T;
    [Range(0f, 1f)] public float debugSpeakVolume = 0.35f;
    public KeyCode debugScreamKey = KeyCode.Y;
    [Range(0f, 1f)] public float debugScreamVolume = 0.85f;

    [Header("Events")]
    public UnityEvent<float> onVolumeChangedUnity;
    public UnityEvent<float> onSoundDetectedUnity;
    public UnityEvent<float> onLoudNoiseSpikeUnity;
    public UnityEvent onSoundStoppedUnity;

    // C# Events for high performance script subscription
    public event Action<float> OnVolumeChanged;
    public event Action<float> OnSoundDetected;
    public event Action<float> OnLoudNoiseSpike;
    public event Action OnSoundStopped;
    public event Action<bool> OnMicrophoneStateChanged;

    // Runtime state
    public string ActiveDeviceName => selectedDevice;
    public bool IsRecording => isRecording;
    public bool IsCalibrating => isCalibrating;
    public float RawVolume => rawVolume;
    public float CurrentVolume => currentVolume;
    public float Decibels => currentDecibels;
    public bool IsSoundDetected => isSoundDetected;
    public bool IsLoudSoundDetected => isLoudSoundDetected;

    private AudioClip micClip;
    private float[] sampleBuffer;
    private bool isRecording;
    private bool isCalibrating;
    private float rawVolume;
    private float currentVolume;
    private float currentDecibels = -80f;
    private bool isSoundDetected;
    private bool isLoudSoundDetected;
    private float loudSpikeCooldownTimer = 0f;
    private const float LOUD_SPIKE_COOLDOWN = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        sampleBuffer = new float[sampleWindow];
    }

    private void Start()
    {
        InitializeMicrophone();

        if (autoCalibrateOnStart && isRecording)
        {
            StartCoroutine(CalibrateNoiseFloorRoutine(calibrationDuration));
        }
    }

    private void Update()
    {
        if (loudSpikeCooldownTimer > 0f)
        {
            loudSpikeCooldownTimer -= Time.deltaTime;
        }

        float measuredVolume = 0f;

        if (isRecording && micClip != null)
        {
            measuredVolume = SampleMicrophoneRMS();
        }

        // Handle debug simulation
        if (enableDebugSimulation)
        {
            if (Input.GetKey(debugScreamKey))
            {
                measuredVolume = Mathf.Max(measuredVolume, debugScreamVolume);
            }
            else if (Input.GetKey(debugSpeakKey))
            {
                measuredVolume = Mathf.Max(measuredVolume, debugSpeakVolume);
            }
        }

        rawVolume = measuredVolume;

        // Apply asymmetric smoothing (instant attack, smooth decay)
        if (rawVolume > currentVolume)
        {
            currentVolume = Mathf.Lerp(currentVolume, rawVolume, attackSpeed * Time.deltaTime);
        }
        else
        {
            currentVolume = Mathf.Lerp(currentVolume, rawVolume, decaySpeed * Time.deltaTime);
        }

        // Avoid subnormal float values
        if (currentVolume < 0.0001f) currentVolume = 0f;

        // Calculate Decibels
        float effectiveRMS = Mathf.Max(rawVolume / sensitivity, 0.00001f);
        currentDecibels = 20f * Mathf.Log10(effectiveRMS);

        // Notify volume change
        OnVolumeChanged?.Invoke(currentVolume);
        onVolumeChangedUnity?.Invoke(currentVolume);

        if (!isCalibrating)
        {
            EvaluateSoundThresholds();
        }
    }

    private void EvaluateSoundThresholds()
    {
        bool wasDetected = isSoundDetected;
        isSoundDetected = currentVolume >= noiseThreshold;

        // Sound detection state transitions
        if (isSoundDetected)
        {
            OnSoundDetected?.Invoke(currentVolume);
            onSoundDetectedUnity?.Invoke(currentVolume);
        }
        else if (wasDetected && !isSoundDetected)
        {
            OnSoundStopped?.Invoke();
            onSoundStoppedUnity?.Invoke();
        }

        // Loud noise spike detection
        isLoudSoundDetected = currentVolume >= loudThreshold;
        if (isLoudSoundDetected && loudSpikeCooldownTimer <= 0f)
        {
            loudSpikeCooldownTimer = LOUD_SPIKE_COOLDOWN;
            OnLoudNoiseSpike?.Invoke(currentVolume);
            onLoudNoiseSpikeUnity?.Invoke(currentVolume);
        }
    }

    /// <summary>
    /// Reads PCM samples from the microphone ring buffer and computes RMS loudness.
    /// </summary>
    private float SampleMicrophoneRMS()
    {
        if (string.IsNullOrEmpty(selectedDevice)) return 0f;

        int micPosition = Microphone.GetPosition(selectedDevice);
        if (micPosition < 0) return 0f;

        int startPosition = micPosition - sampleWindow;
        if (startPosition < 0)
        {
            // When wrapping near buffer beginning, return last volume
            return rawVolume;
        }

        try
        {
            micClip.GetData(sampleBuffer, startPosition);
        }
        catch
        {
            return 0f;
        }

        // Compute Root Mean Square (RMS)
        float sumSquares = 0f;
        for (int i = 0; i < sampleWindow; i++)
        {
            float sample = sampleBuffer[i];
            sumSquares += sample * sample;
        }

        float rms = Mathf.Sqrt(sumSquares / sampleWindow);

        // Subtract ambient noise floor and apply sensitivity gain
        float adjustedVolume = Mathf.Max(0f, rms - noiseFloor) * sensitivity;
        return Mathf.Clamp01(adjustedVolume);
    }

    public void InitializeMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[MicSoundDetector] No microphone devices found on this system.");
            isRecording = false;
            OnMicrophoneStateChanged?.Invoke(false);
            return;
        }

        // Resolve device name
        if (string.IsNullOrEmpty(selectedDevice) || !Array.Exists(Microphone.devices, d => d == selectedDevice))
        {
            selectedDevice = Microphone.devices[0];
            Debug.Log($"[MicSoundDetector] Defaulting to microphone: {selectedDevice}");
        }

        StartRecording();
    }

    public void StartRecording()
    {
        if (string.IsNullOrEmpty(selectedDevice)) return;

        if (Microphone.IsRecording(selectedDevice))
        {
            Microphone.End(selectedDevice);
        }

        // Start continuous loop recording without saving to disk
        micClip = Microphone.Start(selectedDevice, true, recordingLength, sampleRate);
        isRecording = micClip != null;

        if (isRecording)
        {
            Debug.Log($"[MicSoundDetector] Started listening to microphone '{selectedDevice}' at {sampleRate}Hz.");
        }
        else
        {
            Debug.LogError($"[MicSoundDetector] Failed to start microphone recording on '{selectedDevice}'.");
        }

        OnMicrophoneStateChanged?.Invoke(isRecording);
    }

    public void StopRecording()
    {
        if (isRecording && !string.IsNullOrEmpty(selectedDevice))
        {
            Microphone.End(selectedDevice);
            isRecording = false;
            Debug.Log("[MicSoundDetector] Microphone recording stopped.");
            OnMicrophoneStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Measures ambient room noise for a given duration and adjusts noise floor automatically.
    /// </summary>
    public void CalibrateNoiseFloor(float duration = 1.5f)
    {
        if (!isRecording) return;
        StartCoroutine(CalibrateNoiseFloorRoutine(duration));
    }

    private IEnumerator CalibrateNoiseFloorRoutine(float duration)
    {
        isCalibrating = true;
        Debug.Log("[MicSoundDetector] Calibrating background noise floor... Please remain quiet.");

        float timer = 0f;
        float peakAmbientRMS = 0f;
        float totalRMS = 0f;
        int sampleCount = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (isRecording && micClip != null)
            {
                int micPos = Microphone.GetPosition(selectedDevice);
                if (micPos >= sampleWindow)
                {
                    micClip.GetData(sampleBuffer, micPos - sampleWindow);
                    float sum = 0f;
                    for (int i = 0; i < sampleWindow; i++)
                    {
                        sum += sampleBuffer[i] * sampleBuffer[i];
                    }
                    float rms = Mathf.Sqrt(sum / sampleWindow);
                    totalRMS += rms;
                    if (rms > peakAmbientRMS) peakAmbientRMS = rms;
                    sampleCount++;
                }
            }

            yield return null;
        }

        if (sampleCount > 0)
        {
            float averageRMS = totalRMS / sampleCount;
            // Set noise floor slightly above average ambient noise (plus safety margin)
            noiseFloor = Mathf.Clamp(Mathf.Max(averageRMS * 1.25f, peakAmbientRMS * 0.9f), 0.005f, 0.2f);
            Debug.Log($"[MicSoundDetector] Calibration complete. Noise Floor set to: {noiseFloor:F4} (Peak RMS: {peakAmbientRMS:F4}, Avg RMS: {averageRMS:F4})");
        }

        isCalibrating = false;
    }

    public void SetDevice(string newDevice)
    {
        if (selectedDevice == newDevice && isRecording) return;

        StopRecording();
        selectedDevice = newDevice;
        StartRecording();
    }

    public string[] GetAvailableDevices()
    {
        return Microphone.devices;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !isRecording && !string.IsNullOrEmpty(selectedDevice))
        {
            StartRecording();
        }
    }

    private void OnDisable()
    {
        StopRecording();
    }

    private void OnDestroy()
    {
        StopRecording();
    }
}
