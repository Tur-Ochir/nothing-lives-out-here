using System;
using UnityEngine;

/// <summary>
/// Handles player raycasting, target highlighting, picking up, dropping, and interacting with objects.
/// </summary>
public class PlayerInteractionHandler : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Transform handPoint;
    public Transform twoHandPoint;
    public float maxDistance = 5f;

    [Tooltip("Layer mask to filter which objects can be raycasted for interaction.")]
    public LayerMask interactionLayer = ~0;

    private Transform camTransform;
    private IHighlightable currentHighlight;

    private void Awake()
    {
        camTransform = Camera.main != null ? Camera.main.transform : null;
    }

    public void ProcessLookAtTarget()
    {
        if (camTransform == null) return;

        if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit hit, maxDistance, interactionLayer, QueryTriggerInteraction.Ignore))
        {
            var highlightable = hit.transform.GetComponent<IHighlightable>() ?? hit.transform.GetComponentInParent<IHighlightable>();
            if (highlightable != null)
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

        if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit hit, maxDistance, interactionLayer, QueryTriggerInteraction.Ignore))
        {
            // 1. Container Interaction
            var itemContainer = hit.transform.GetComponent<IItemContainer>() ?? hit.transform.GetComponentInParent<IItemContainer>();
            if (itemContainer != null)
            {
                // Put held item into target container
                if (player.IsHoldingItem && player.heldItem is MonoBehaviour heldItemMb)
                {
                    if (itemContainer.TryContain(heldItemMb.gameObject))
                    {
                        return true;
                    }
                }
            }

            // 2. Interactable Interaction
            var interactable = hit.transform.GetComponent<IInteractable>() ?? hit.transform.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                if (player.IsHoldingItem && !(player.heldItem is IItemContainer) && interactable is IHoldable targetHoldable && targetHoldable.DropCurrentItemOnInteract)
                {
                    player.heldItem?.Drop();
                }

                interactable.Interact();

                // Pick up holdable item if player hand is free
                if (!player.IsHoldingItem && interactable is IHoldable pickupable)
                {
                    Transform targetHoldPoint = pickupable.HoldType == HoldType.TwoHands ? twoHandPoint : handPoint;
                    pickupable.Pickup(targetHoldPoint);
                }

                return true;
            }

            // 3. Direct Holdable Interaction (for items/containers that implement IHoldable directly)
            var holdable = hit.transform.GetComponent<IHoldable>() ?? hit.transform.GetComponentInParent<IHoldable>();
            if (holdable != null && !player.IsHoldingItem)
            {
                Transform targetHoldPoint = holdable.HoldType == HoldType.TwoHands ? twoHandPoint : handPoint;
                holdable.Pickup(targetHoldPoint);
                return true;
            }
        }

        return false;
    }

    public void HandleDrop(PlayerManager player)
    {
        if (player == null) return;

        if (player.IsHoldingItem)
        {
            player.heldItem.Drop();
        }
    }
}
