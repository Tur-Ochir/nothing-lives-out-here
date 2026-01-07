using UnityEngine;

public class Dumpling : Interactable
{
    public bool cooked;
    public Color cookedColor;
    private MeshRenderer meshRenderer;
    public GameObject eatenVer1;
    public int counter = 2;

    protected override void Start()
    {
        base.Start();
        
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Cook()
    {
        cooked = true;
        col.enabled = true;
        meshRenderer.material.color = cookedColor;
    }

    public bool Eat()
    {
        if (!cooked)
        {
            Debug.Log("Can't eat raw dumpling.");
            return false;
        }
        
        Debug.Log("Eaten dumpling.");
        counter--;

        if (counter < 2)
        {
            meshRenderer.enabled = false;
            eatenVer1.SetActive(true);
        }
        else if (counter < 1)
        {
            Destroy(gameObject);
            PlayerManager.Instance.heldItem = null;
        }
        
        return true;
    }
}
