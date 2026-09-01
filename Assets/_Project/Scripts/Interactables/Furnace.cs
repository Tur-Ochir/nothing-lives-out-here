using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Furnace : MonoBehaviour, IItemContainer, IHighlightable
{
    [Header("Container Settings")]
    public bool canContainItems = true;
    public Transform[] itemPoints;
    public int currentCounter;
    public List<GameObject> items = new List<GameObject>();

    [Header("Furnace Settings")]
    public ParticleSystem fireParticles;
    public float burnTime = 0f;
    public bool isBurning = false;
    public Vector3 togooPoint;
    public Cap cap;
    public Door am;

    [HideInInspector] public Outline outline;
    private Togoo currentTogoo;

    public bool CanContainItems => canContainItems;
    public int ItemCount => currentCounter;
    public int Capacity => itemPoints != null ? itemPoints.Length : 0;

    private void Awake()
    {
        outline = GetComponent<Outline>();
    }

    private void Update()
    {
        if (!isBurning) return;

        burnTime -= Time.deltaTime;
        if (burnTime <= 0f)
        {
            SetFire(false);
            burnTime = 0f;
        }
    }

    public bool TryContain(GameObject item)
    {
        if (!CanContainItems || item == null) return false;

        if (item.TryGetComponent(out Togoo togoo))
        {
            if (cap != null && cap.isCapped) return false;

            togoo.transform.SetParent(transform);
            togoo.transform.DOLocalMove(togooPoint, 0.5f).OnComplete(() => togoo.SetActivateCollider(true));
            togoo.transform.DOLocalRotate(Vector3.zero, 0.5f);

            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.heldItem = null;
            }

            if (cap != null)
            {
                cap.canCap = false;
            }

            togoo.furnace = this;
            currentTogoo = togoo;
            return true;
        }
        
        // 1. Fuel items (Argal)
        if (item.TryGetComponent(out Argal argal))
        {
            if (am != null && !am.isOpen) return false;
            
            burnTime += argal.burnDur;

            Transform targetParent = (itemPoints != null && itemPoints.Length > 0) ? itemPoints[0] : transform;
            item.transform.SetParent(targetParent);
            item.transform.DOLocalRotate(Vector3.zero, 0.5f);
            item.transform.DOLocalMove(Vector3.zero, 0.5f).OnComplete(() =>
            {
                item.gameObject.SetActive(false);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlaySubtitle("match");
                }
            });

            items.Add(item);
            return true;
        }

        // 2. Igniter items (Match)
        if (item.TryGetComponent(out Match _))
        {
            if (am != null && !am.isOpen) return false;
            
            if (burnTime > 0f)
            {
                SetFire(true);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlaySubtitle("fire");
                }
            }
            return true;
        }

        return false;
    }
    

    public void Remove(GameObject item)
    {
        if (item != null)
        {
            items.Remove(item);
            Destroy(item);
        }
    }

    public void SetFire(bool active)
    {
        isBurning = active;

        if (fireParticles != null)
        {
            if (active) fireParticles.Play();
            else fireParticles.Stop();
        }

        if (active && currentTogoo != null)
        {
            currentTogoo.TryCook();
        }
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}