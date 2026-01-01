using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Arag : Container
{
    private List<Outline> itemOutline = new List<Outline>();
    

    protected override void Awake()
    {
        base.Awake();
    }

    public override bool TryContain(Interactable item)
    {
            
        if (base.TryContain(item))
        {
            item.SetRbColActive(false);
            
            item.transform.SetParent(itemPoints[currentCounter-1]);
            item.transform.DOLocalMove(Vector3.zero, 0.5f).OnComplete(() => item.col.enabled = true);
            item.transform.DOLocalRotate(Vector3.zero, 0.5f);
            item.outline.OutlineMode = Outline.Mode.OutlineVisible;
            itemOutline.Add(item.outline);
            PlayerManager.Instance.heldItem = null;
            return true;
        }

        return false;
    }

    public void SetOutline(bool active)
    {
        foreach (var io in itemOutline)
        {
            io.enabled = active;
        }
    }

    public override void Remove(Interactable item)
    {
        base.Remove(item);
        
        if (itemOutline.Contains(item.outline))
        {
            itemOutline.Remove(item.outline);
        }
    }

    public override void Hold()
    {
        base.Hold(); ;
    }

    public override void Release()
    {
        base.Release();
        
        rb.isKinematic = false;
        Debug.Log($"Release Arag");
    }
}
