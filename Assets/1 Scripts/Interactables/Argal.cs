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

    public void ActivateRandomChild()
    {
        r = Random.Range(0, transform.childCount);
        transform.GetChild(r).gameObject.SetActive(true);
        transform.GetChild(r).localPosition = Vector3.zero;
        col = transform.GetChild(r).GetComponent<Collider>();
    }

    public override void Interact()
    {
        if (PlayerManager.Instance.currentContainer != null && PlayerManager.Instance.currentContainer.TryGetComponent(out Arag arag))
        {
            SetRbColActive(false);
            transform.SetParent(arag.itemPoints[arag.currentCounter]);
            transform.DOLocalJump(Vector3.zero, 2.5f, 1, 0.5f);
            transform.DOLocalRotate(Vector3.zero, 0.5f);
            arag.currentCounter++;
            arag.items.Add(this);
            return;
        }
        
        base.Interact();

        if (container != null)
        {
            container.Remove(this);
        }
    }
}
