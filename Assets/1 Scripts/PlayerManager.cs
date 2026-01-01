using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    [Header("Movement")]
    public float speed;
    public float crouchSpeedMultiplier = 0.5f;
    public Vector3 standingOffset;
    public Vector3 crouchingOffset;
    private bool crouching;
    
    [Header("Camera")]
    public Transform camTarget;
    public CinemachineBasicMultiChannelPerlin camNoise;
    public float bobTransSpeed = 5f;
    public float walkingBobAmplitude = 2f;
    public float walkingBobFrequency = 0.02f;

    [Header("Interact")]
    public Transform handPoint;
    public Transform twoHandPoint;
    public float maxDistance = 5;
    public bool IsHoldingItem => heldItem != null;
    public Interactable heldItem;
    public Container currentContainer;
    
    private PlayerInput input;
    private CharacterController controller;
    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction dropAction;
    private InputAction crouchAction;
    private Transform camTransform;
    private bool isInteracting;
    private Interactable currentInteractable;
    private Vector3 moveDirection;
    
    private void Awake()
    {
        Instance = this;
        
        
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        camTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        input.actions.Enable();
        
        moveAction = input.actions["Move"];
        interactAction = input.actions["Interact"];
        dropAction = input.actions["Drop"];
        crouchAction = input.actions["Crouch"];
    }

    private void OnDisable()
    {
        input.actions.Disable();
    }

    private void Update()
    {
        var moveInput = moveAction.ReadValue<Vector2>();
        Move(moveInput);
        HandleHeadBob();

        if (interactAction.WasPressedThisFrame())
        {
            if (HandleInteraction()) return;
        }

        if (dropAction.WasPressedThisFrame())
        {
            if (currentContainer != null)
            {
                currentContainer.Release();
            }
            if (IsHoldingItem)
            {
                heldItem.Drop();   
            }
        }

        if (crouchAction.WasPressedThisFrame())
        {
            Crouch();
        }

        HandleLookAtInteractable();

    }

    private bool HandleInteraction()
    {
        if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit hit, maxDistance))
        {
            if (hit.transform.TryGetComponent(out Container container))
            {
                if (currentContainer != null)
                {
                    if (container.TryGet(currentContainer))
                    {
                        return true;
                    }
                }
                if (IsHoldingItem)
                {
                    if (container.TryContain(heldItem))
                    {
                        return true;
                    } 
                }
                else
                {
                    container.Hold();
                    // currentContainer = container;
                }
            }

            if (hit.transform.TryGetComponent(out Interactable interactable))
            {
                if (IsHoldingItem && interactable.dropCurrentItem)
                {
                    heldItem.Drop();
                }
                interactable.Interact();
            }
        }

        return false;
    }

    private void Move(Vector2 moveInput)
    {
        moveDirection = camTransform.right * moveInput.x + camTransform.forward * moveInput.y;
        controller.SimpleMove(moveDirection * speed);
    }
    
    void HandleLookAtInteractable()
    {
        if (Physics.Raycast(camTransform.position, camTransform.forward,
                out RaycastHit hit, maxDistance))
        {
            if (hit.transform.TryGetComponent(out Interactable interactable))
            {
                // New interactable
                if (currentInteractable != interactable)
                {
                    ClearCurrentInteractable();
                    currentInteractable = interactable;
                    currentInteractable.SetOutline(true);
                }
                return;
            }
        }

        // Nothing hit or not interactable
        ClearCurrentInteractable();
    }
    void ClearCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.SetOutline(false);
            currentInteractable = null;
        }
    }

    private void Crouch()
    {
        crouching = !crouching;
        
        var target = crouching ? crouchingOffset : standingOffset;
        camTarget.DOLocalMove(target, 0.3f);
        if (crouching)
        {
            speed *= crouchSpeedMultiplier; 
        }
        else
        {
            speed /=  crouchSpeedMultiplier;
        }
    }
    private void HandleHeadBob()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            camNoise.AmplitudeGain = Mathf.Lerp(camNoise.AmplitudeGain, walkingBobAmplitude, Time.deltaTime * bobTransSpeed);
            camNoise.FrequencyGain = Mathf.Lerp(camNoise.FrequencyGain, walkingBobFrequency, Time.deltaTime * bobTransSpeed);
        }
        else
        {
            camNoise.AmplitudeGain = Mathf.Lerp(camNoise.AmplitudeGain, 0, Time.deltaTime * bobTransSpeed);
            camNoise.FrequencyGain = Mathf.Lerp(camNoise.FrequencyGain, 0, Time.deltaTime * bobTransSpeed);
        }
    }
}
