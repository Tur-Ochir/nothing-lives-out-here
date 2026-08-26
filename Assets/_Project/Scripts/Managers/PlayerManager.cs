using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Interaction & Hands")]
    public Transform handPoint;
    public Transform twoHandPoint;
    public bool IsHoldingItem => heldItem != null;
    public IHoldable heldItem;
    public IHoldableContainer currentContainer;

    [Header("Stats")]
    public int eatenDumplings = 0;

    [Header("Components")]
    public PlayerMovement movement;
    public PlayerInteractionHandler interaction;

    private PlayerInput input;
    private CharacterController controller;
    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction dropAction;
    private InputAction crouchAction;
    private InputAction eatAction;
    private InputAction fireAction;
    private Transform camTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();

        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        SetupSubsystems();
    }

    private void SetupSubsystems()
    {
        // 1. Movement & Headbob Subsystem
        if (movement == null)
        {
            movement = GetComponent<PlayerMovement>();
            if (movement == null) movement = gameObject.AddComponent<PlayerMovement>();
        }

        // 2. Interaction Subsystem
        if (interaction == null)
        {
            interaction = GetComponent<PlayerInteractionHandler>();
            if (interaction == null) interaction = gameObject.AddComponent<PlayerInteractionHandler>();
        }
        interaction.Initialize(camTransform);
    }

    private void OnEnable()
    {
        if (input != null && input.actions != null)
        {
            input.actions.Enable();

            moveAction = input.actions["Move"];
            interactAction = input.actions["Interact"];
            dropAction = input.actions["Drop"];
            crouchAction = input.actions["Crouch"];
            eatAction = input.actions["Eat"];
            fireAction = input.actions["Fire"];
        }
    }

    private void OnDisable()
    {
        if (input != null && input.actions != null)
        {
            input.actions.Disable();
        }
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
            interaction.TryInteract(this);
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
        if (fireAction != null && fireAction.WasPressedThisFrame())
        {
            if (heldItem is IUsable usable && usable.CanUse)
            {
                usable.Use();
            }
        }

        // Highlight look-at target
        if (interaction != null)
        {
            interaction.ProcessLookAtTarget();
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
}