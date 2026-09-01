using UnityEngine;

public interface IItemContainer
{
    bool CanContainItems { get; }
    int ItemCount { get; }
    int Capacity { get; }
    bool TryContain(GameObject item);
    void Remove(GameObject item);
}
