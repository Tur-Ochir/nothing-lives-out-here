using UnityEngine;

/// <summary>
/// Handles player raycasting, target highlighting, picking up, dropping, and interacting with objects.
/// </summary>
public class PlayerInteractionHandler : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float maxDistance = 5f;

    private Transform camTransform;
    private IHighlightable currentHighlight;

    public void Initialize(Transform mainCameraTransform)
    {
        camTransform = mainCameraTransform;
    }

    public void ProcessLookAtTarget()
    {
        if (camTransform == null) return;

        if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit hit, maxDistance))
        {
            if (hit.transform.TryGetComponent(out IHighlightable highlightable))
            {
                if (currentHighlight != highlightable)
                {
                    ClearHighlight();
                    currentHighlight = highlightable;
                    currentHighlight.SetHighlight(true);
                }
                return;
            }
        }

        ClearHighlight();
    }

    public void ClearHighlight()
    {
        if (currentHighlight != null)
        {
            currentHighlight.SetHighlight(false);
            currentHighlight = null;
        }
    }

    public bool TryInteract(PlayerManager player)
    {
        if (camTransform == null || player == null) return false;

        if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit hit, maxDistance))
        {
            // 1. Container Interaction
            if (hit.transform.TryGetComponent(out IItemContainer itemContainer))
            {
                // Transfer between containers if holding a container
                if (player.currentContainer is MonoBehaviour heldContainerMb && itemContainer is IHoldableContainer targetHoldableContainer)
                {
                    if (targetHoldableContainer.TryGet(heldContainerMb.gameObject))
                    {
                        return true;
                    }
                }

                // Put held item into target container
                if (player.IsHoldingItem && player.heldItem is MonoBehaviour heldItemMb)
                {
                    if (itemContainer.TryContain(heldItemMb.gameObject))
                    {
                        return true;
                    }
                }
            }

            if (hit.transform.TryGetComponent(out IHoldableContainer holdableContainer) && !player.IsHoldingItem && player.currentContainer == null)
            {
                holdableContainer.Hold(player.twoHandPoint);
                return true;
            }

            // 2. Interactable Interaction
            if (hit.transform.TryGetComponent(out IInteractable interactable))
            {
                if (player.IsHoldingItem && interactable is IHoldable targetHoldable && targetHoldable.DropCurrentItemOnInteract)
                {
                    player.heldItem?.Drop();
                }

                interactable.Interact();

                // Pick up holdable item if player hand is free
                if (!player.IsHoldingItem && interactable is IHoldable pickupable)
                {
                    pickupable.Pickup(player.handPoint);
                }

                return true;
            }
        }

        return false;
    }

    public void HandleDrop(PlayerManager player)
    {
        if (player == null) return;

        if (player.currentContainer != null)
        {
            player.currentContainer.Release();
        }

        if (player.IsHoldingItem)
        {
            player.heldItem.Drop();
        }
    }
}
