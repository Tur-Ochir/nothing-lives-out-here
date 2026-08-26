using System;
using DG.Tweening;
using UnityEngine;

public class Cap : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Cap Settings")]
    public bool canCap = true;
    public Vector3 firstPosition;
    public Vector3 firstRotation;
    public Vector3 secondPosition;
    public Vector3 secondRotation;
    public Lock currentLock;
    
    public float duration = 1f;
    public float jumpPower = 1f;
    public bool isCapped = true;
    public bool useJump = true;

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;
    [HideInInspector] public Outline outline;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;

    private void Awake()
    {
        outline = GetComponent<Outline>();
    }

    public void Interact()
    {
        if (currentLock != null && currentLock.isLocked) return;
        if (!canCap || !CanInteract) return;

        isCapped = !isCapped;
        Move(isCapped);
        OnInteracted?.Invoke();
    }

    private void Move(bool isFirst)
    {
        var target = isFirst ? firstPosition : secondPosition;
        var targetRot = isFirst ? firstRotation : secondRotation;

        transform.DOKill();
        if (useJump)
        {
            transform.DOLocalJump(target, jumpPower, 1, duration);
        }
        else
        {
            transform.DOLocalMove(target, duration);
        }
        transform.DOLocalRotate(targetRot, duration);
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}
