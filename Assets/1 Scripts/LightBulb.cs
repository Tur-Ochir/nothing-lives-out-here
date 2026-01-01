using System.Collections;
using UnityEngine;

public class LightBulb : Interactable
{
    public Light[] lights;
    public MeshRenderer meshRenderer;
    public int lightMatIndex;
    public bool isOn = false;

    public override void Interact()
    {
        base.Interact();
        isOn = !isOn;
        SetActivate(isOn);
    }

    public void SetActivate(bool active)
    {
        if (active)
        {
            meshRenderer.materials[lightMatIndex].EnableKeyword("_EMISSION");
        }
        else
        {
            meshRenderer.materials[lightMatIndex].DisableKeyword("_EMISSION");
        }

        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].enabled = active;
        }
    }

    public IEnumerator DelayedSetActive(float delay, bool active)
    {
        yield return new WaitForSeconds(delay);
        SetActivate(active);
    }
}
