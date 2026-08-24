using System;
using Unity.Cinemachine;
using UnityEngine;

public class Bed : MonoBehaviour, IInteractable, IUsable
{
    [Header("Bed Settings")]
    public bool canEnterBed = true;
    public bool canExitBed = true;
    public bool canSleep;
    public CinemachineCamera inBedCam;

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;
    public bool CanUse => isInBed && canExitBed;

    private bool isInBed = false;

    private void OnEnable()
    {
        GameManager.OnPlayerEatFill += HandlePlayerEatFill;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerEatFill -= HandlePlayerEatFill;
    }

    private void HandlePlayerEatFill()
    {
        canEnterBed = true;
        canSleep = true;
    }

    public void Interact()
    {
        if (isInBed)
        {
            Use();
            return;
        }

        if (!canEnterBed || !CanInteract)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlaySubtitle(reasonNotInteract);
            }
            return;
        }

        EnterBed();
        OnInteracted?.Invoke();
    }

    public void Use()
    {
        if (!CanUse) return;
        ExitBed();
    }

    private void EnterBed()
    {
        isInBed = true;

        if (inBedCam != null)
        {
            inBedCam.gameObject.SetActive(true);
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.canMove = false;
            PlayerManager.Instance.canCrouch = false;
        }

        if (CanvasManager.Instance != null)
        {
            CanvasManager.Instance.BlackScreen(0.8f);
        }

        if (canSleep)
        {
            canExitBed = false;
            GameManager.OnPlayerSleep?.Invoke();
        }
    }

    private void ExitBed()
    {
        isInBed = false;

        if (inBedCam != null)
        {
            inBedCam.gameObject.SetActive(false);
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.canMove = true;
            PlayerManager.Instance.canCrouch = true;
        }

        if (CanvasManager.Instance != null)
        {
            CanvasManager.Instance.BlackScreen(0.8f);
        }
    }
}
