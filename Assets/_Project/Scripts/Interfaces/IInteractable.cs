using System;
using UnityEngine;

public interface IInteractable
{
    bool CanInteract { get; }
    string ReasonCannotInteract { get; }
    void Interact();
    event Action OnInteracted;
}

/// <summary>
/// Represents an object or spot that the player can enter/occupy (e.g. CarSeat, HidingSpot) and exit with the interact key (E).
/// </summary>
public interface IOccupiable : IInteractable
{
    bool IsOccupied { get; }
    void Enter(PlayerManager player);
    void Exit();
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
