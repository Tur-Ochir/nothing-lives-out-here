using UnityEngine;

public class Dumpling : Interactable
{
    public bool cooked;
    private MeshRenderer meshRenderer;

    protected override void Start()
    {
        base.Start();
        
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Cook()
    {
        cooked = true;
    }

    public bool Eat()
    {
        if (!cooked)
        {
            Debug.Log("Can't eat raw dumpling.");
            return false;
        }
        
        Debug.Log("Eaten dumpling.");
        meshRenderer.material.color = Color.peru;
        return true;
    }
}
