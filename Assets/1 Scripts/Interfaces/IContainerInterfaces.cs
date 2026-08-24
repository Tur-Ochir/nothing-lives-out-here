using UnityEngine;

public interface IItemContainer
{
    bool CanContainItems { get; }
    int ItemCount { get; }
    int Capacity { get; }
    bool TryContain(GameObject item);
    void Remove(GameObject item);
}

public interface IHoldableContainer
{
    bool CanHold { get; }
    bool IsHeld { get; }
    void Hold(Transform holdTransform);
    void Release();
    bool TryGet(GameObject otherContainer);
}
