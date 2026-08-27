using System;
using System.Collections;
using UnityEngine;

public class LightBulb : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Bulb Settings")]
    public Light[] lights;
    public MeshRenderer meshRenderer;
    public int lightMatIndex;
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

    private void Start()
    {
        SetActivate(isOn);
    }

    public void Interact()
    {
        if (!CanInteract) return;

        isOn = !isOn;
        SetActivate(isOn);
        OnInteracted?.Invoke();
    }

    public void SetActivate(bool active)
    {
        if (meshRenderer != null && meshRenderer.materials != null && lightMatIndex < meshRenderer.materials.Length)
        {
            if (active)
            {
                meshRenderer.materials[lightMatIndex].EnableKeyword("_EMISSION");
            }
            else
            {
                meshRenderer.materials[lightMatIndex].DisableKeyword("_EMISSION");
            }
        }

        if (lights != null)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].enabled = active;
                }
            }
        }
    }

    public IEnumerator DelayedSetActive(float delay, bool active)
    {
        yield return new WaitForSeconds(delay);
        SetActivate(active);
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}
