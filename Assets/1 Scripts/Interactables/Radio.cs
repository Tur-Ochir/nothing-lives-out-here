using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class Radio : Interactable
{
    [Header("Radio Settings")]
    public CinemachineCamera focusCamera;
    
    [Header("Input Actions")]
    public InputActionProperty tuneAction;
    public InputActionProperty exitAction;

    [Header("Visuals")]
    public MeshRenderer meshRenderer;
    public Transform knob;
    public Transform needle;
    public Vector3 knobMinRotation;
    public Vector3 knobMaxRotation;
    
    [Header("Needle Settings")]
    public Vector3 needleMinPos;
    public Vector3 needleMaxPos;
    
    [Header("Tuning")]
    public float frequency = 88.0f;
    public float minFrequency = 88.0f;
    public float maxFrequency = 108.0f;
    public float targetFrequency = 95.0f;
    public float frequencyThreshold = 0.5f;
    public float scrollSensitivity = 0.01f;

    [Header("Audio")]
    public AudioSource staticSource;
    public AudioSource signalSource;
    public float maxVolume = 1.0f;

    private bool isFocused = false;
    private void OnEnable()
    {
        // Optionally enable actions if they aren't part of a PlayerInput that is already enabled
        tuneAction.action?.Enable();
        exitAction.action?.Enable();
    }

    public override void Interact()
    {
        if (!canInteract) return;

        isFocused = !isFocused;

        if (isFocused)
        {
            EnterFocus();
        }
        else
        {
            ExitFocus();
        }
    }

    private void EnterFocus()
    {
        if (focusCamera != null) focusCamera.gameObject.SetActive(true);
        PlayerManager.Instance.canMove = false;
        PlayerManager.Instance.canCrouch = false;
        meshRenderer.material.EnableKeyword("_EMISSION");
        
        staticSource?.Play();
        signalSource?.Play();
        UpdateAudio();
        
        // Debug.Log("Radio: Focused");
    }

    private void ExitFocus()
    {
        if (focusCamera != null) focusCamera.gameObject.SetActive(false);
        PlayerManager.Instance.canMove = true;
        PlayerManager.Instance.canCrouch = true;
        meshRenderer.material.DisableKeyword("_EMISSION");
        
        staticSource?.Stop();
        signalSource?.Stop();
        
        // Debug.Log("Radio: Unfocused");
    }

    private void Update()
    {
        if (!isFocused) return;

        // Exit focus with Escape/Cancel action
        if (exitAction.action != null && exitAction.action.WasPressedThisFrame())
        {
            ExitFocus();
            isFocused = false;
            return;
        }

        // Handle Tuning via Scroll Action
        if (tuneAction.action != null)
        {
            Vector2 scrollDelta = tuneAction.action.ReadValue<Vector2>();
            if (scrollDelta.y != 0)
            {
                frequency += scrollDelta.y * scrollSensitivity;
                frequency = Mathf.Clamp(frequency, minFrequency, maxFrequency);
                UpdateVisuals();
                UpdateAudio();
            }
        }
    }

    private void UpdateAudio()
    {
        if (staticSource == null || signalSource == null) return;

        float dist = Mathf.Abs(frequency - targetFrequency);
        float signalStrength = 1.0f - Mathf.Clamp01(dist / frequencyThreshold);

        // We use a simple linear blend: signal volume up, static volume down
        signalSource.volume = signalStrength * maxVolume;
        staticSource.volume = (1.0f - signalStrength) * maxVolume;
    }

    private void UpdateVisuals()
    {
        float t = Mathf.InverseLerp(minFrequency, maxFrequency, frequency);

        // Update Needle Position
        if (needle != null)
        {
            needle.localPosition = Vector3.Lerp(needleMinPos, needleMaxPos, t);
        }

        // Update Knob Rotation
        if (knob != null)
        {
            knob.localRotation = Quaternion.Euler(Vector3.Lerp(knobMinRotation, knobMaxRotation, t));
        }
    }

    protected override void Start()
    {
        base.Start();
        // Don't play on start, wait for focus
    }
}
