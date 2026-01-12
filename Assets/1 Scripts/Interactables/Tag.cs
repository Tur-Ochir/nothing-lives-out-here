using UnityEngine;

public class Tag : Interactable
{
    public Togoo togoo;
    public override void Interact()
    {
        base.Interact();

        if (togoo != null)
        {
            togoo.tag = null;
        }
    }
}
