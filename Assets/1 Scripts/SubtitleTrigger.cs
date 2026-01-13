using System;
using UnityEngine;

public class SubtitleTrigger : MonoBehaviour
{
    public string subtitleId;
    public int eventIndex;
    public float delay;
    
    public bool useTrigger = true;
    public bool useInteractable = true;

    private void OnEnable()
    {
        if (useInteractable)
        {
            var interactable = GetComponent<Interactable>();
            interactable.OnInteract += DelayedTryPlaySub;
            // interactable.OnInteract += (() => { Destroy(this);});
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

    private void TryPlaySubtitle()
    {
        if (eventIndex == GameManager.EventIndex)
        {
            GameManager.Instance.PlaySubtitle(subtitleId);
            GameManager.EventIndex++;
            // Destroy(gameObject);
        }
    }

    private void DelayedTryPlaySub()
    {
        Invoke(nameof(TryPlaySubtitle), delay);
    }
}
