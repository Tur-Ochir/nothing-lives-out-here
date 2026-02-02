using UnityEngine;

public class Tag : Interactable
{
    public Togoo togoo;
    public override void Interact()
    {
        if (togoo != null && togoo.steamingDumpling)
        {
            GameManager.Instance.PlaySubtitle("raw-warning");
            return;
        }
        base.Interact();

        if (togoo != null)
        {   
            togoo.tag = null;
        }
    }
}
