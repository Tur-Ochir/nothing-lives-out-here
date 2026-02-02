using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Bed : Interactable
{
    public bool canEnterBed = true;
    public bool canExitBed = true;
    public bool canSleep;
    public CinemachineCamera inBedCam;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        GameManager.OnPlayerEatFill += (() =>
        {
            canEnterBed = true;
            canSleep = true;
        });
    }

    public override void Interact()
    {
        base.Interact();
        if (!canEnterBed)
        {
            GameManager.Instance.PlaySubtitle(reasonNotInteract);
            return;
        }
        
        EnterBed();
    }
    public override void Use()
    {
        base.Interact();
        if (!canExitBed) return;
        
        ExitBed();
    }

    private void EnterBed()
    {
        inBedCam.gameObject.SetActive(true);
        PlayerManager.Instance.canMove = false;
        PlayerManager.Instance.canCrouch = false;
        CanvasManager.Instance.BlackScreen(0.8f);
        PlayerManager.Instance.heldItem = this;

        if (canSleep)
        {
            canExitBed = false;
            GameManager.OnPlayerSleep?.Invoke();
        }
    }
    private void ExitBed()
    {
        inBedCam.gameObject.SetActive(false);
        PlayerManager.Instance.canMove = true;
        PlayerManager.Instance.canCrouch = true;
        CanvasManager.Instance.BlackScreen(0.8f);
        PlayerManager.Instance.heldItem = null;
    }
}
