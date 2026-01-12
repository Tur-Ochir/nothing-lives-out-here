using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class Argal : Interactable
{
    private int r;
    public float burnDur = 60f;
    protected override void Awake()
    {
        base.Awake();
        
        // ActivateRandomChild();
        
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();
    }

    private void ActivateRandomChild()
    {
        r = Random.Range(0, transform.childCount);
        transform.GetChild(r).gameObject.SetActive(true);
        transform.GetChild(r).localPosition = Vector3.zero;
    }

    public override void Interact()
    {
        base.Interact();

        if (container != null)
        {
            container.Remove(this);
        }
    }
}
