using System.Threading;
using DG.Tweening;
using UnityEngine;

public class Togoo : Container
{
    public Vector3 tagPoint;
    public Tag tag;
    public bool steamingDumpling;
    public ParticleSystem steamingParticle;
    public float steamingDuration;
    public int minDumplings = 3;
    public Furnace furnace;

    protected override void Awake()
    {
        base.Awake();

        tag = GetComponentInChildren<Tag>();
    }

    protected override void Update()
    {
        base.Update();

        if (steamingDumpling)
        {
            steamingDuration -= Time.deltaTime;

            if (steamingDuration <= 0)
            {
                SetSteaming(false);
                CookDumplings();
            }
        }
    }

    public override bool TryContain(Interactable item)
    {
        if (item.TryGetComponent(out Tag newTag))
        {
            newTag.transform.SetParent(transform);
            newTag.transform.DOLocalMove(tagPoint, 0.5f).OnComplete(() => item.col.enabled = true);
            newTag.transform.DOLocalRotate(Vector3.zero, 0.5f);
            newTag.togoo = this;
            tag = newTag;

            PlayerManager.Instance.heldItem = null;
            TryCook();
            return true;
        }

        if (item.TryGetComponent(out Dumpling dumpling))
        {
            item.SetRbColActive(false);

            item.transform.SetParent(itemPoints[currentCounter]);
            item.transform.DOLocalMove(Vector3.zero, 0.5f).OnComplete(() => item.col.enabled = true);
            item.transform.DOLocalRotate(Vector3.zero, 0.5f);
            item.outline.OutlineMode = Outline.Mode.OutlineVisible;
            // itemOutline.Add(item.outline);
            PlayerManager.Instance.heldItem = null;
            item.container = this;
            currentCounter++;
            items.Add(item);
            return true;
        }

        return false;
    }

    private void SetSteaming(bool active)
    {
        steamingDumpling = active;
        if (active)
        {
            steamingParticle.Play();
        }
        else
        {
            steamingParticle.Stop();
        }
        
    }

    public override void Hold()
    {
        base.Hold();

        if (tag != null)
        {
            tag.SetRbColActive(false);
        }
    }

    public override void Release()
    {
        base.Release();
        
        if (tag != null)
        {
            tag.col.enabled = true;
        }

        for (int i = 0; i < items.Count; i++)
        {
            items[i].col.enabled = true;
        }
    }

    private void CookDumplings()
    {
        foreach (var item in items)
        {
            if (item.TryGetComponent(out Dumpling dumpling))
            {
                dumpling.Cook();
            }
        }
    }

    public void TryCook()
    {
        if (currentCounter == 0) return;
        if (currentCounter < minDumplings)
        {
            GameManager.Instance.PlaySubtitle("min-req-dumplings");
            return;
        }
        if (currentCounter >= minDumplings && furnace != null && furnace.isBurning && tag != null)
        {
            SetSteaming(true);
        }
    }

    public override void SetActivateCollider(bool activate)
    {
        base.SetActivateCollider(activate);

        if (tag != null)
        {
            tag.col.enabled = activate;
        }
    }
}