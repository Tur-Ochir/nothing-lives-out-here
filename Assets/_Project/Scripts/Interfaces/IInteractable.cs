using System;
using UnityEngine;

public interface IInteractable
{
    bool CanInteract { get; }
    string ReasonCannotInteract { get; }
    void Interact();
    event Action OnInteracted;
}

public interface IUsable
{
    bool CanUse { get; }
    void Use();
}

public interface IHoldable
{
    bool IsHeld { get; }
    bool DropCurrentItemOnInteract { get; }
    void Pickup(Transform holdTransform);
    void Drop();
}

public interface IHighlightable
{
    void SetHighlight(bool active);
}
