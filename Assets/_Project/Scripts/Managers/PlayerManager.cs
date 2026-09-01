using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public bool IsHoldingItem => heldItem != null;
    public IHoldable heldItem;

    [Header("Flashlight")]
    public bool canUseflashlight = true;
    public bool flashlightOn = true;
    public Light flashlight;
    public GameObject lightCone;

    [Header("Stats")]
    public int eatenDumplings = 0;

    [Header("Occupied / Hiding State")]
    public IOccupiable currentOccupied;
    public HidingSpot currentHidingSpot;
    public bool IsHidden => currentHidingSpot != null;
    public bool IsOccupying => currentOccupied != null;

    [Header("Components")]
    public PlayerMovement movement;
    public PlayerInteractionHandler interaction;
    public PlayerDriver driver;
    public CinemachineCamera playerCam;

    public bool IsDriving => driver != null && driver.IsDriving;

    private PlayerInput input;
    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction dropAction;
    private InputAction crouchAction;
    private InputAction useAction;
    private InputAction flashlightAction;
    private InputAction sprintAction;

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
        driver = GetComponent<PlayerDriver>();
        if (driver == null)
        {
            driver = gameObject.AddComponent<PlayerDriver>();
        }
        
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
            sprintAction = input.actions["Sprint"];
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
        // 1. Process Driving Mode
        if (IsDriving)
        {
            Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            bool interactPressed = interactAction != null && interactAction.WasPressedThisFrame();
            bool flashlightPressed = flashlightAction != null && flashlightAction.WasPressedThisFrame();
            bool sprintPressed = sprintAction != null && sprintAction.IsPressed();

            driver.ProcessDriveInput(moveInput, interactPressed, flashlightPressed, sprintPressed);
            return;
        }

        // 2. Process Normal Walking Movement
        if (moveAction != null && movement != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            movement.ProcessMove(moveInput);
            movement.ProcessHeadBob();
        }

        // 3. Process Interactions & Exit Occupied Spots (Hiding Spot, Car Seat, etc.)
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            if (currentOccupied != null)
            {
                currentOccupied.Exit();
            }
            else if (IsHidden)
            {
                currentHidingSpot.ExitHiding();
            }
            else
            {
                interaction.TryInteract(this);
            }
        }

        // 4. Process Drop
        if (dropAction != null && dropAction.WasPressedThisFrame())
        {
            interaction.HandleDrop(this);
        }

        // 5. Process Crouch
        if (crouchAction != null && crouchAction.WasPressedThisFrame())
        {
            movement.ToggleCrouch();
        }

        // 6. Process Fire / Item Use
        if (useAction != null && useAction.WasPressedThisFrame())
        {
            if (heldItem is IUsable usable && usable.CanUse)
            {
                usable.Use();
            }
        }

        // 7. Highlight look-at target
        if (interaction != null && !IsHidden && currentOccupied == null)
        {
            interaction.ProcessLookAtTarget();
        }

        // 8. Flashlight Toggle
        if (flashlightAction != null && flashlightAction.WasPressedThisFrame())
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
        if (!canUseflashlight) return;
        
        flashlightOn = state;
        if (flashlight != null)
        {
            flashlight.enabled = flashlightOn;
        }
        if (lightCone != null)
        {
            lightCone.SetActive(flashlightOn);
        }
    }

    public void SetDrivingState(bool state, CarSeat seat = null)
    {
        if (driver == null) return;

        if (state)
        {
            if (seat != null)
            {
                var car = seat.carController != null ? seat.carController : seat.GetComponentInParent<CarController>();
                if (car != null)
                {
                    driver.EnterCar(car, seat);
                }
            }
        }
        else
        {
            driver.ExitCar();
        }
    }
    public void HideItems(bool hide)
    {
        SetFlashlightState(false);
        canUseflashlight = !hide;
        
        for (int i = 0; i < playerCam.transform.childCount; i++)
        {
            var child = playerCam.transform.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(!hide);
            }
        }
    }
}