using System;
using DG.Tweening;
using UnityEngine;

public class DoorLock : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Key Movement")]
    public bool isLocked = true;
    public Vector3 lockedPoint;
    public Vector3 unlockedPoint;
    public float toggleDuration = 0.5f;

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;
    [HideInInspector] public Outline outline;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;

    private Tween moveTween;

    private void Awake()
    {
        outline = GetComponent<Outline>();
    }

    private void Start()
    {
        SetLock(isLocked);
    }

    public void Interact()
    {
        if (!CanInteract) return;

        SetLock(!isLocked);

        OnInteracted?.Invoke();
    }

    public void SetLock(bool isOn)
    {
        isLocked = isOn;
        Vector3 target = isLocked ? lockedPoint : unlockedPoint;
        moveTween?.Kill();
        moveTween = transform.DOLocalMove(target, toggleDuration).SetEase(Ease.InOutSine);
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}
