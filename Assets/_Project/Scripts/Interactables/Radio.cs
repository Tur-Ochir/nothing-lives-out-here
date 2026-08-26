using System;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class Radio : MonoBehaviour, IInteractable, IHighlightable
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

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;
    [HideInInspector] public Outline outline;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;

    private bool isFocused = false;

    private void Awake()
    {
        outline = GetComponent<Outline>();
    }

    private void OnEnable()
    {
        tuneAction.action?.Enable();
        exitAction.action?.Enable();
    }

    public void Interact()
    {
        if (!CanInteract) return;

        isFocused = !isFocused;

        if (isFocused)
        {
            EnterFocus();
        }
        else
        {
            ExitFocus();
        }

        OnInteracted?.Invoke();
    }

    private void EnterFocus()
    {
        if (focusCamera != null) focusCamera.gameObject.SetActive(true);

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.movement.canMove = false;
            PlayerManager.Instance.movement.canCrouch = false;
        }

        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.EnableKeyword("_EMISSION");
        }
        
        staticSource?.Play();
        signalSource?.Play();
        UpdateAudio();
    }

    private void ExitFocus()
    {
        if (focusCamera != null) focusCamera.gameObject.SetActive(false);

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.movement.canMove = true;
            PlayerManager.Instance.movement.canCrouch = true;
        }

        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.DisableKeyword("_EMISSION");
        }
        
        staticSource?.Stop();
        signalSource?.Stop();
    }

    private void Update()
    {
        if (!isFocused) return;

        if (exitAction.action != null && exitAction.action.WasPressedThisFrame())
        {
            ExitFocus();
            isFocused = false;
            return;
        }

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

        signalSource.volume = signalStrength * maxVolume;
        staticSource.volume = (1.0f - signalStrength) * maxVolume;
    }

    private void UpdateVisuals()
    {
        float t = Mathf.InverseLerp(minFrequency, maxFrequency, frequency);

        if (needle != null)
        {
            needle.localPosition = Vector3.Lerp(needleMinPos, needleMaxPos, t);
        }

        if (knob != null)
        {
            knob.localRotation = Quaternion.Euler(Vector3.Lerp(knobMinRotation, knobMaxRotation, t));
        }
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}
