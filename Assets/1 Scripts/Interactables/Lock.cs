using System;
using UnityEngine;

public class Lock : MonoBehaviour, IInteractable, ILockable, IHighlightable
{
    [Header("Lock Settings")]
    public bool isLocked = true;
    public string lockId;

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;
    [HideInInspector] public Outline outline;

    public event Action OnInteracted;

    public bool IsLocked => isLocked;
    public string LockId => lockId;
    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;

    private void Awake()
    {
        outline = GetComponent<Outline>();
    }

    public void Interact()
    {
        if (PlayerManager.Instance == null || !PlayerManager.Instance.IsHoldingItem) return;

        if (PlayerManager.Instance.heldItem is Key key)
        {
            if (TryUnlock(key.keyId))
            {
                OnInteracted?.Invoke();
                Debug.Log($"Lock {lockId} unlocked!");
            }
        }
    }

    public bool TryUnlock(string key)
    {
        if (key != lockId) return false;

        isLocked = !isLocked;
        gameObject.SetActive(isLocked);
        return true;
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}
