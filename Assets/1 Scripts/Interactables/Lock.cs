using UnityEngine;

public class Lock : Interactable
{
    public bool isLocked = true;
    public string lockId;
    public override void Interact()
    {
        if (!PlayerManager.Instance.IsHoldingItem) return;
        if (!PlayerManager.Instance.heldItem.TryGetComponent(out Key key)) return;
        if (key.keyId != lockId) return;
        
        isLocked = !isLocked;
        gameObject.SetActive(isLocked);
    }
}
