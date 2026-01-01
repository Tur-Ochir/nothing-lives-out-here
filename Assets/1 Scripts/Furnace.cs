using DG.Tweening;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Furnace : Container
{
    [Header("Furnace")]
    public ParticleSystem fireParticles;
    public float burnTime = 0f;
    private bool isBurning = false;
    public Vector3 togooPoint;
    public Cap cap;
    public Door am;

    protected override void Update()
    {
        base.Update();

        if (!isBurning) return;
        
        burnTime -= Time.deltaTime;
        if (burnTime <= 0)
        {
            SetFire(false);
            burnTime = 0f;
            isBurning = false;
        }
    }

    public override bool TryContain(Interactable item)
    {
        if (base.TryContain(item))
        {
            if (!am.isOpen) return false;
            
            var d = item.GetComponent<Argal>().burnDur;
            burnTime += d;
            SetFire(true);
            isBurning = true;
            Remove(item);
            return true;
        }

        return false;
    }

    public override bool TryGet(Container container)
    {
        if (!base.TryGet(container)) return false;
        
        if (container.TryGetComponent(out Togoo togoo))
        {
            if (cap.isCapped) return false;
                
            togoo.transform.SetParent(transform);
            togoo.transform.DOLocalMove(togooPoint, 0.5f).OnComplete((() =>
            {
                togoo.SetActivateCollider(true);
            }));
            togoo.transform.DOLocalRotate(Vector3.zero, 0.5f);
            PlayerManager.Instance.currentContainer = null;
            cap.canCap = false;
            return true;
        }
        return false;
    }

    public override void Remove(Interactable item)
    {
        base.Remove(item);
        Destroy(item.gameObject);
        
    }
    
    public void SetFire(bool active)
    {
        if (active)
        {
            fireParticles.Play();   
        }
        else
        {
            fireParticles.Stop();  
        }
    }
}
