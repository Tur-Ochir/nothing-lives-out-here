using UnityEngine;

/// <summary>
/// Represents an item that can be cooked in cookware (e.g. dumplings).
/// </summary>
public interface ICookable
{
    bool IsCooked { get; }
    void Cook();
}

/// <summary>
/// Represents an item that can be eaten/consumed by the player.
/// </summary>
public interface IEatable
{
    bool CanEat { get; }
    void Eat();
}

/// <summary>
/// Represents a lockable object that requires a key or code.
/// </summary>
public interface ILockable
{
    bool IsLocked { get; }
    string LockId { get; }
    bool TryUnlock(string keyId);
}

/// <summary>
/// Represents an object with custom initialization when spawned by Spawner.
/// </summary>
public interface ISpawnable
{
    void OnSpawned();
}
