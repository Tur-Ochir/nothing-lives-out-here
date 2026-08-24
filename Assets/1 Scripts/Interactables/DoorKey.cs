using System;
using DG.Tweening;
using UnityEngine;

public class DoorKey : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Key Movement")]
    public Vector3 posA;
    public Vector3 posB;
    public float toggleSpeed = 0.5f;
    public bool atA = true;

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

    public void Interact()
    {
        if (!CanInteract) return;

        Vector3 target = atA ? posB : posA;
        moveTween?.Kill();
        moveTween = transform.DOLocalMove(target, toggleSpeed).SetEase(Ease.InOutSine);

        atA = !atA;
        OnInteracted?.Invoke();
        Debug.Log($"Key toggled to {(atA ? "Position A" : "Position B")}");
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}
