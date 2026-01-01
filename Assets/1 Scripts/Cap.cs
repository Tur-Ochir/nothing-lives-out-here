using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class Cap : Interactable
{
    [Header("Cap")]
    public bool canCap = true;
    public Vector3 firstPosition;
    public Vector3 firstRotation;
    public Vector3 secondPosition;
    public Vector3 secondRotation;
    
    public float duration = 1f;
    public float jumpPower = 1f;
    
    public bool isCapped = true;

    public override void Interact()
    {
        base.Interact();
        if (!canCap) return;
        
        isCapped = !isCapped;
        Move(isCapped);
    }

    private void Move(bool isFirst)
    {
        var target = isFirst ? firstPosition : secondPosition;
        var targetRot = isFirst ? firstRotation : secondRotation;
        
        transform.DOLocalJump(target, jumpPower, 1, duration);
        transform.DOLocalRotate(targetRot, duration);
    }
}
