using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public bool IsHoldingItem => heldItem != null;
    public IHoldable heldItem;
    public IHoldableContainer currentContainer;
    [Header("Flashlight")]
    public bool flashlightOn = true;
    public Light flashlight;

    [Header("Stats")]
    public int eatenDumplings = 0;

    [Header("Hiding State")]
    public HidingSpot currentHidingSpot;
    public bool IsHidden => currentHidingSpot != null;

    [Header("Components")]
    public PlayerMovement movement;
    public PlayerInteractionHandler interaction;

    private PlayerInput input;
    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction dropAction;
    private InputAction crouchAction;
    private InputAction useAction;
    private InputAction flashlightAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        input = GetComponent<PlayerInput>();
        movement = GetComponent<PlayerMovement>();
        interaction = GetComponent<PlayerInteractionHandler>();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        if (input != null && input.actions != null)
        {
            input.actions.Enable();

            moveAction = input.actions["Move"];
            interactAction = input.actions["Use"];
            useAction = input.actions["Fire"];
            dropAction = input.actions["Drop"];
            crouchAction = input.actions["Crouch"];
            flashlightAction = input.actions["Flashlight"];
        }
    }

    private void OnDisable()
    {
        if (input != null && input.actions != null)
        {
            input.actions.Disable();
        }
    }

    private void Start()
    {
        SetFlashlightState(flashlightOn);
    }

    private void Update()
    {
        // Process Movement
        if (moveAction != null && movement != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            movement.ProcessMove(moveInput);
            movement.ProcessHeadBob();
        }

        // Process Interactions
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            if (IsHidden)
            {
                currentHidingSpot.ExitHiding();
            }
            else
            {
                interaction.TryInteract(this);
            }
        }

        // Process Drop
        if (dropAction != null && dropAction.WasPressedThisFrame())
        {
            interaction.HandleDrop(this);
        }

        // Process Crouch
        if (crouchAction != null && crouchAction.WasPressedThisFrame())
        {
            movement.ToggleCrouch();
        }

        // Process Fire / Item Use
        if (useAction.WasPressedThisFrame())
        {
            if (heldItem is IUsable usable && usable.CanUse)
            {
                usable.Use();
            }
        }

        // Highlight look-at target
        if (interaction != null && !IsHidden)
        {
            interaction.ProcessLookAtTarget();
        }

        if (flashlightAction.WasPressedThisFrame())
        {
            SetFlashlightState(!flashlightOn);
        }
    }

    public void Eat()
    {
        eatenDumplings++;

        if (eatenDumplings >= 3)
        {
            GameManager.OnPlayerEatFill?.Invoke();
        }
    }

    public void DisableCam()
    {
        if (movement != null)
        {
            movement.SetCamControllerActive(false);
        }
    }

    public void EnableCam()
    {
        if (movement != null)
        {
            movement.SetCamControllerActive(true);
        }
    }
    public void SetFlashlightState(bool state)
    {
        flashlightOn = state;
        if (flashlight != null)
        {
            flashlight.enabled = flashlightOn;
        }
    }
}