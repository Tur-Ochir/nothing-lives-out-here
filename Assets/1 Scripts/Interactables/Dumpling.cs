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
        meshRenderer.material.color = cookedColor;
        eatenVer1.GetComponent<MeshRenderer>().material.color = cookedColor;
        col.enabled = true;
    }

    public override void Use()
    {
        base.Use();
        
        if (!cooked)
        {
            Debug.Log("Can't eat raw dumpling.");
            return;
        }
        
        Debug.Log("Eaten dumpling.");
        counter--;

        if (counter == 1)
        {
            meshRenderer.enabled = false;
            eatenVer1.SetActive(true);
        }
        if (counter < 1)
        {
            Destroy(gameObject);
            PlayerManager.Instance.heldItem = null;
            PlayerManager.Instance.Eat();
        }
    }
}
