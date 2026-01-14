using UnityEngine;
using DG.Tweening;

public class Key : Interactable
{
    [Header("Key Movement")]
    public Vector3 posA;
    public Vector3 posB;
    public float toggleSpeed = 0.5f;
    
    public bool atA = true;

    public override void Interact()
    {
        if (!canInteract) return;

        // Toggle position between A and B
        Vector3 target = atA ? posB : posA;
        transform.DOLocalMove(target, toggleSpeed).SetEase(Ease.InOutSine);
        
        atA = !atA;

        // Note: Not calling base.Interact() to avoid pickup logic if unintended
        // If pickup is needed later, this can be adjusted.
        Debug.Log($"Key toggled to {(atA ? "Position A" : "Position B")}");
    }
}
