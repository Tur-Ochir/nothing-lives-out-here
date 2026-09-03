using System;
using Unity.Cinemachine;
using UnityEngine;

public class CarSeat : MonoBehaviour, IOccupiable, IHighlightable
{
    [Header("Car & Seat Setup")]
    public CarController carController;
    public bool isDrivingSeat = true;
    public bool isOccupied = false;
    public Door carDoor;

    [Header("Camera & Exit")]
    public CinemachineCamera inCarCam;
    public Transform exitPoint;

    [Header("Highlight Outline")]
    public Outline outline;

    [Header("Tutorial Prompt Settings")]
    public bool showTutorialPrompt = true;
    public string enterPrompt = "Press 'E' to Drive";
    public string exitPrompt = "Press 'E' to Exit Car";
    public Vector3 promptOffset = new Vector3(0f, 0.4f, 0f);

    public bool CanInteract => true;
    public bool hidePlayerOnEnter = true;
    public string ReasonCannotInteract => string.Empty;
    public event Action OnInteracted;
    public bool IsOccupied => isOccupied;

    private void Awake()
    {
        if (outline == null)
        {
            outline = GetComponent<Outline>();
        }

        if (carController == null)
        {
            carController = GetComponentInParent<CarController>();
        }

        if (inCarCam != null)
        {
            inCarCam.gameObject.SetActive(false);
        }
    }

    public void Enter(PlayerManager player)
    {
        EnterCarSeat();
    }

    public void Exit()
    {
        ExitCarSeat();
    }

    private void EnterCarSeat()
    {
        isOccupied = true;

        if (inCarCam != null)
        {
            inCarCam.gameObject.SetActive(true);
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.currentOccupied = this;

            if (isDrivingSeat)
            {
                PlayerManager.Instance.SetDrivingState(true, this);
            }
            else
            {
                // Passenger seat logic if needed
                PlayerManager.Instance.movement.canMove = false;
                PlayerManager.Instance.movement.SetCamControllerActive(false);
            }
        }

        // Show exit prompt while driving
        if (showTutorialPrompt && TutorialManager.Instance != null && !string.IsNullOrEmpty(exitPrompt))
        {
            TutorialManager.Instance.defaultOffset = promptOffset;
            TutorialManager.Instance.ShowTutorial(exitPrompt, transform);
        }

        if (carDoor != null)
        {
            carDoor.SetOpen(false);
        }

        OnInteracted?.Invoke();
    }

    public void ExitCarSeat()
    {
        isOccupied = false;
        if (carDoor != null)
        {
            carDoor.SetOpen(true);
        }

        // Hide tutorial prompt
        if (showTutorialPrompt && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.HideTutorial();
        }

        if (inCarCam != null)
        {
            inCarCam.gameObject.SetActive(false);
        }

        if (PlayerManager.Instance != null)
        {
            if (PlayerManager.Instance.currentOccupied == (IOccupiable)this)
            {
                PlayerManager.Instance.currentOccupied = null;
            }

            if (isDrivingSeat)
            {
                PlayerManager.Instance.SetDrivingState(false, this);
            }
            else
            {
                var player = PlayerManager.Instance;
                if (player.TryGetComponent<CharacterController>(out var cc))
                {
                    cc.enabled = false;
                    player.transform.position = exitPoint != null ? exitPoint.position : transform.position;
                    player.transform.rotation = exitPoint != null ? exitPoint.rotation : transform.rotation;
                    cc.enabled = true;
                }
                player.movement.canMove = true;
                player.movement.SetCamControllerActive(true);
            }
        }

        OnInteracted?.Invoke();
    }

    private void HandleInteraction()
    {
        if (isOccupied)
        {
            ExitCarSeat();
        }
        else
        {
            EnterCarSeat();
        }
    }

    public void Interact()
    {
        HandleInteraction();
    }

    public void SetHighlight(bool active)
    {
        if (outline != null)
        {
            outline.enabled = active;
        }

        if (showTutorialPrompt && TutorialManager.Instance != null)
        {
            if (active && !isOccupied && !string.IsNullOrEmpty(enterPrompt))
            {
                TutorialManager.Instance.defaultOffset = promptOffset;
                TutorialManager.Instance.ShowTutorial(enterPrompt, transform);
            }
            else if (!active && !isOccupied)
            {
                TutorialManager.Instance.HideTutorial();
            }
        }
    }
}
