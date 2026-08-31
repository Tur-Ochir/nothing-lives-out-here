using UnityEngine;

/// <summary>
/// Attach this component to any GameObject/Interactable (like HidingSpot, CarSeat, Item, Door)
/// to automatically display a world-canvas tutorial prompt (e.g. "Press 'E' to Hide") when the player looks at it or enters its trigger area.
/// </summary>
public class TutorialPrompt : MonoBehaviour, IHighlightable
{
    [Header("Prompt Settings")]
    [Tooltip("The message displayed on the Tutorial Canvas.")]
    public string promptMessage = "Press 'E' to Hide";

    [Tooltip("Optional transform to anchor the world canvas to (defaults to this transform).")]
    public Transform anchorPoint;

    [Tooltip("Offset added to the anchor point position.")]
    public Vector3 offset = new Vector3(0f, 0.5f, 0f);

    [Header("Trigger Modes")]
    [Tooltip("Show prompt when the player looks at this object via raycast.")]
    public bool showOnLookAt = true;

    [Tooltip("Show prompt when the player enters this collider trigger.")]
    public bool showOnTrigger = false;

    [Tooltip("Hide prompt automatically once interacted with.")]
    public bool hideOnInteract = true;

    private bool isPlayerLooking = false;
    private bool isPlayerInTrigger = false;
    private IInteractable interactable;

    private void Awake()
    {
        if (anchorPoint == null)
        {
            anchorPoint = transform;
        }

        interactable = GetComponent<IInteractable>();
    }

    private void OnEnable()
    {
        if (hideOnInteract && interactable != null)
        {
            interactable.OnInteracted += HandleInteracted;
        }
    }

    private void OnDisable()
    {
        if (hideOnInteract && interactable != null)
        {
            interactable.OnInteracted -= HandleInteracted;
        }
        Hide();
    }

    private void HandleInteracted()
    {
        Hide();
    }

    public void SetHighlight(bool active)
    {
        if (!showOnLookAt) return;

        isPlayerLooking = active;
        if (active)
        {
            Show();
        }
        else if (!isPlayerInTrigger)
        {
            Hide();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!showOnTrigger) return;

        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
            Show();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!showOnTrigger) return;

        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            if (!isPlayerLooking)
            {
                Hide();
            }
        }
    }

    public void Show()
    {
        if (TutorialManager.Instance != null && !string.IsNullOrEmpty(promptMessage))
        {
            Vector3 pos = anchorPoint != null ? anchorPoint.position + offset : transform.position + offset;
            TutorialManager.Instance.defaultOffset = offset;
            TutorialManager.Instance.ShowTutorial(promptMessage, anchorPoint);
        }
    }

    public void Hide()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.HideTutorial();
        }
    }
}
