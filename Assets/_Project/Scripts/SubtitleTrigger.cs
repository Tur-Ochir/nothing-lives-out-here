using UnityEngine;

/// <summary>
/// Triggers subtitle dialog either on player trigger enter or on interactable activation.
/// </summary>
public class SubtitleTrigger : MonoBehaviour
{
    [Header("Subtitle Settings")]
    public string subtitleId;
    public int eventIndex;
    public float delay;
    
    [Header("Trigger Modes")]
    public bool useTrigger = true;
    public bool useInteractable = true;

    private void OnEnable()
    {
        if (useInteractable)
        {
            if (TryGetComponent(out IInteractable interactable))
            {
                interactable.OnInteracted += DelayedTryPlaySub;
            }   
        }
    }

    private void OnDisable()
    {
        if (useInteractable)
        {
            if (TryGetComponent(out IInteractable interactable))
            {
                interactable.OnInteracted -= DelayedTryPlaySub;
            }   
        }
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (!useTrigger) return;
        
        if (other.CompareTag("Player"))
        {
            TryPlaySubtitle();
        }
    }

    public void TryPlaySubtitle()
    {
        if (eventIndex == GameManager.EventIndex)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlaySubtitle(subtitleId);
            }
            GameManager.EventIndex++;
        }
    }

    private void DelayedTryPlaySub()
    {
        Invoke(nameof(TryPlaySubtitle), delay);
    }
}
