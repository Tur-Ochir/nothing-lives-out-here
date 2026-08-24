using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Movement Settings")]
    public bool canMove = true;
    public bool canCrouch = true;
    public float speed = 5f;
    public float crouchSpeedMultiplier = 0.5f;
    public Vector3 standingOffset;
    public Vector3 crouchingOffset;

    [Header("Camera & HeadBob")]
    public Transform camTarget;
    public CinemachineVirtualCameraBase vcam;
    public CinemachineBasicMultiChannelPerlin camNoise;
    public bool useHeadBob = true;
    public float bobTransSpeed = 5f;
    public float walkingBobAmplitude = 2f;
    public float walkingBobFrequency = 0.02f;

    [Header("Interaction & Hands")]
    public Transform handPoint;
    public Transform twoHandPoint;
    public float maxDistance = 5f;
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
        movement.canMove = canMove;
        movement.canCrouch = canCrouch;
        movement.speed = speed;
        movement.crouchSpeedMultiplier = crouchSpeedMultiplier;
        movement.standingOffset = standingOffset;
        movement.crouchingOffset = crouchingOffset;
        movement.camTarget = camTarget;
        movement.vcam = vcam;
        movement.camNoise = camNoise;
        movement.useHeadBob = useHeadBob;
        movement.bobTransSpeed = bobTransSpeed;
        movement.walkingBobAmplitude = walkingBobAmplitude;
        movement.walkingBobFrequency = walkingBobFrequency;
        movement.Initialize(controller, camTransform);

        // 2. Interaction Subsystem
        if (interaction == null)
        {
            interaction = GetComponent<PlayerInteractionHandler>();
            if (interaction == null) interaction = gameObject.AddComponent<PlayerInteractionHandler>();
        }
        interaction.maxDistance = maxDistance;
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
        // Sync dynamic inspector flags
        if (movement != null)
        {
            movement.canMove = canMove;
            movement.canCrouch = canCrouch;
        }

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