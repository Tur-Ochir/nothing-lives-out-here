using UnityEngine;

public class LightSwitch : Interactable
{
    public ParticleSystem lightParticles;
    public Light light;
    public bool isOn = false;
    
    public override void Interact()
    {
        base.Interact();
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
