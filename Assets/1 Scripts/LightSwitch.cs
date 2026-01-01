using UnityEngine;

public class LightSwitch : Interactable
{
    public ParticleSystem lightParticles;
    public Light light;
    public bool isOn = false;
    
    public override void Interact()
    {
        base.Interact();
        if (!isOn)
        {
            if (PlayerManager.Instance.heldItem == null) return;
            if (!PlayerManager.Instance.heldItem.TryGetComponent(out Match match)) return;
        }
        
        isOn = !isOn;
        light.enabled = isOn;
        if (isOn)
        {
            lightParticles.Play();  
        }
        else
        {
            
            lightParticles.Stop();  
        }
    }
}
