using System.Threading;
using DG.Tweening;
using UnityEngine;

public class Togoo : Container
{
    public Vector3 tagPoint;
    private Tag tag;
    private bool steamingDumpling;
    public ParticleSystem steamingParticle;
    public float steamingDuration;

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
        if (item.TryGetComponent(out Tag tag))
        {
            tag.transform.SetParent(transform);
            tag.transform.DOLocalMove(tagPoint, 0.5f).OnComplete(() => item.col.enabled = true);
            tag.transform.DOLocalRotate(Vector3.zero, 0.5f);

            PlayerManager.Instance.heldItem = null;

            if (currentCounter >= 3)
            {
                //TODO BUUZ CHANAJ EHLEH
                SetSteaming(true);
            }
            else
            {
                //TODO TSOOHON BUUZ IDEED TSADAHGUI BAIH GEJ HELEH
            }
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
            tag.SetRbColActive(true);
        }
    }

    public override void SetActivateCollider(bool activate)
    {
        base.SetActivateCollider(activate);
        
        if (tag != null)
        {
            tag.SetRbColActive(activate);
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
}