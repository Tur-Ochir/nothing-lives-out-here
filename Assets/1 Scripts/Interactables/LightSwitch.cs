using System;
using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Light Switch Settings")]
    public ParticleSystem lightParticles;
    public new Light light;
    public bool isOn = false;

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;
    [HideInInspector] public Outline outline;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;

    private void Awake()
    {
        outline = GetComponent<Outline>();
    }

    public void Interact()
    {
        if (!CanInteract) return;

        if (!isOn)
        {
            var held = PlayerManager.Instance != null ? PlayerManager.Instance.heldItem : null;
            bool hasMatch = held is Match;

            if (!hasMatch)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlaySubtitle(reasonNotInteract);
                }
                return;
            }
        }

        isOn = !isOn;

        if (light != null)
        {
            light.enabled = isOn;
        }

        if (lightParticles != null)
        {
            if (isOn) lightParticles.Play();
            else lightParticles.Stop();
        }

        OnInteracted?.Invoke();
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}
