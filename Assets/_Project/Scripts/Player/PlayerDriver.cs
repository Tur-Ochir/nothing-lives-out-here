using System;
using UnityEngine;

/// <summary>
/// Component on Player that manages vehicle driving state, input transmission to CarController, 
/// camera transition, and vehicle entry/exit lifecycle.
/// </summary>
public class PlayerDriver : MonoBehaviour
{
    [Header("Current Vehicle State")]
    [SerializeField] private CarController currentCar;
    [SerializeField] private CarSeat currentSeat;

    [Header("Settings")]
    [Tooltip("Whether to hide player renderers/mesh while seated in car.")]
    public bool hidePlayerMeshInCar = false;

    public bool IsDriving => currentCar != null;
    public CarController CurrentCar => currentCar;
    public CarSeat CurrentSeat => currentSeat;

    public event Action<CarController> OnEnterCar;
    public event Action<CarController> OnExitCar;

    private PlayerMovement playerMovement;
    private CharacterController characterController;
    private PlayerManager playerManager;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
        playerManager = GetComponent<PlayerManager>();
    }

    /// <summary>
    /// Enters a car seat as the driver.
    /// </summary>
    public void EnterCar(CarController car, CarSeat seat)
    {
        if (car == null) return;

        currentCar = car;
        currentSeat = seat;

        // Disable player character controller & movement
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (playerMovement != null)
        {
            playerMovement.canMove = false;
            playerMovement.canCrouch = false;
            playerMovement.SetCamControllerActive(false);
        }

        // Attach player to car/seat position
        if (seat != null)
        {
            transform.position = seat.transform.position;
            transform.rotation = seat.transform.rotation;
            transform.SetParent(car.transform);
        }
        playerManager.HideItems(false);
        // Notify car of driver
        car.SetDriver(this);

        OnEnterCar?.Invoke(car);
    }

    /// <summary>
    /// Exits the current car.
    /// </summary>
    public void ExitCar()
    {
        if (currentCar == null) return;

        var exitingCar = currentCar;
        var exitingSeat = currentSeat;

        // Detach player from car hierarchy
        transform.SetParent(null);

        // Notify car
        exitingCar.RemoveDriver();

        // Reposition player at exit point
        Transform exitTarget = (exitingSeat != null && exitingSeat.exitPoint != null) 
            ? exitingSeat.exitPoint 
            : exitingCar.transform;

        transform.position = exitTarget.position;
        transform.rotation = exitTarget.rotation;

        // Re-enable character controller & movement
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (playerMovement != null)
        {
            playerMovement.canMove = true;
            playerMovement.canCrouch = true;
            playerMovement.SetCamControllerActive(true);
        }

        currentCar = null;
        currentSeat = null;
        playerManager.HideItems(true);
        OnExitCar?.Invoke(exitingCar);
    }

    /// <summary>
    /// Processes driver inputs from PlayerManager / PlayerInput.
    /// </summary>
    public void ProcessDriveInput(Vector2 moveInput, bool interactPressed, bool flashlightPressed, bool sprintPressed, bool hornPressed = false)
    {
        if (!IsDriving) return;

        // Exit car on interact press (E key)
        if (interactPressed)
        {
            if (playerManager != null && playerManager.currentOccupied != null)
            {
                playerManager.currentOccupied.Exit();
            }
            else if (currentSeat != null)
            {
                currentSeat.Exit();
            }
            else
            {
                ExitCar();
            }
            return;
        }

        // Headlights toggle
        if (flashlightPressed)
        {
            currentCar.ToggleHeadlights();
        }

        // Horn honk
        if (hornPressed)
        {
            currentCar.PlayHorn();
        }

        // Send drive input to car controller
        bool isBraking = sprintPressed || (moveInput.y < 0 && currentCar.CurrentSpeed > 0.5f);
        currentCar.Drive(moveInput, isBraking);
    }
}
