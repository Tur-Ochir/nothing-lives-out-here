using DG.Tweening;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Furnace : Container
{
    [Header("Furnace")] public ParticleSystem fireParticles;
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
        }
    }

    public override bool TryContain(Interactable item)
    {
        if (!canContainItems) return false;
        if (!am.isOpen) return false;

        if (item.TryGetComponent(out Argal argal))
        {
            var d = argal.burnDur;
            burnTime += d;
            argal.transform.SetParent(itemPoints[currentCounter]);
            argal.transform.DOLocalMove(Vector3.zero, 0.5f).OnComplete((() =>
            {
                argal.gameObject.SetActive(false);
            }));
            argal.transform.DOLocalRotate(Vector3.zero, 0.5f);
            // currentCounter++;
            item.container = this;
            items.Add(item);
            return true;
        }

        if (item.TryGetComponent(out Match match))
        {
            if (burnTime > 0)
            {
                SetFire(true);
            }
            return true;
        }

        // SetFire(true);
        // Remove(item);
        return false;
    }

    public override bool TryGet(Container container)
    {
        if (!base.TryGet(container)) return false;

        if (container.TryGetComponent(out Togoo togoo))
        {
            if (cap.isCapped) return false;

            togoo.transform.SetParent(transform);
            togoo.transform.DOLocalMove(togooPoint, 0.5f).OnComplete((() => { togoo.SetActivateCollider(true); }));
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
            isBurning = true;
            fireParticles.Play();
        }
        else
        {
            fireParticles.Stop();
            isBurning = false;
        }
    }
}