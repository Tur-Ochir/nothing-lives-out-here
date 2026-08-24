using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Togoo : MonoBehaviour, IItemContainer, IHoldableContainer, IHighlightable
{
    [Header("Container Settings")]
    public bool canContainItems = true;
    public Transform[] itemPoints;
    public int currentCounter;
    public List<GameObject> items = new List<GameObject>();

    [Header("Hold Settings")]
    public bool canHold = true;
    public float moveSpeed = 12f;
    public Vector3 inHandRotation;

    [Header("Togoo Settings")]
    public Vector3 tagPoint;
    public new Tag tag;
    public bool steamingDumpling;
    public ParticleSystem steamingParticle;
    public float steamingDuration;
    public int minDumplings = 3;
    public Furnace furnace;

    public Rigidbody rb;
    public Collider[] colliders;
    [HideInInspector] public Outline outline;

    private Transform hand;
    private Coroutine moveToHandCoroutine;

    public bool CanContainItems => canContainItems;
    public int ItemCount => currentCounter;
    public int Capacity => itemPoints != null ? itemPoints.Length : 0;
    public bool CanHold => canHold;
    public bool IsHeld => PlayerManager.Instance != null && PlayerManager.Instance.currentContainer == (IHoldableContainer)this;

    private void Awake()
    {
        colliders = GetComponents<Collider>();
        rb = GetComponent<Rigidbody>();
        outline = GetComponent<Outline>();
        tag = GetComponentInChildren<Tag>();
    }

    private void Update()
    {
        if (steamingDumpling)
        {
            steamingDuration -= Time.deltaTime;

            if (steamingDuration <= 0f)
            {
                SetSteaming(false);
                CookDumplings();
            }
        }
    }

    public bool TryContain(GameObject item)
    {
        if (item == null) return false;

        // 1. Tag (lid) placement
        if (item.TryGetComponent(out Tag newTag))
        {
            newTag.transform.SetParent(transform);
            newTag.transform.DOLocalMove(tagPoint, 0.5f).OnComplete(() =>
            {
                if (newTag.col != null) newTag.col.enabled = true;
            });
            newTag.transform.DOLocalRotate(Vector3.zero, 0.5f);
            newTag.togoo = this;
            tag = newTag;

            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.heldItem = null;
            }

            TryCook();
            return true;
        }

        // 2. Cookable items (e.g. Dumpling)
        if (item.TryGetComponent(out ICookable _))
        {
            if (itemPoints == null || currentCounter >= itemPoints.Length) return false;

            if (item.TryGetComponent(out Dumpling dumpling))
            {
                dumpling.SetRbColActive(false);
            }

            item.transform.SetParent(itemPoints[currentCounter]);
            item.transform.DOLocalMove(Vector3.zero, 0.5f).OnComplete(() =>
            {
                if (item.TryGetComponent(out Collider c)) c.enabled = true;
            });
            item.transform.DOLocalRotate(Vector3.zero, 0.5f);

            if (item.TryGetComponent(out Outline itemOutline))
            {
                itemOutline.OutlineMode = Outline.Mode.OutlineVisible;
            }

            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.heldItem = null;
            }

            currentCounter++;
            items.Add(item);
            return true;
        }

        return false;
    }

    public void Remove(GameObject item)
    {
        if (item != null)
        {
            currentCounter = Mathf.Max(0, currentCounter - 1);
            items.Remove(item);
        }
    }

    public void Hold(Transform holdTransform)
    {
        if (!CanHold || holdTransform == null) return;

        SetActivateCollider(false);
        hand = holdTransform;

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.currentContainer = this;
        }

        if (tag != null)
        {
            tag.SetRbColActive(false);
        }

        SetContainedColliders(false);

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (moveToHandCoroutine != null) StopCoroutine(moveToHandCoroutine);
        moveToHandCoroutine = StartCoroutine(MoveToHandRoutine());
    }

    public void Release()
    {
        if (!CanHold) return;

        if (moveToHandCoroutine != null)
        {
            StopCoroutine(moveToHandCoroutine);
            moveToHandCoroutine = null;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        transform.SetParent(null);

        if (PlayerManager.Instance != null && PlayerManager.Instance.currentContainer == (IHoldableContainer)this)
        {
            PlayerManager.Instance.currentContainer = null;
        }

        if (tag != null && tag.col != null)
        {
            tag.col.enabled = true;
        }

        SetContainedColliders(true);
        SetActivateCollider(true);
    }

    public bool TryGet(GameObject otherContainer)
    {
        return true;
    }

    public void SetActivateCollider(bool activate)
    {
        if (colliders != null)
        {
            foreach (var c in colliders)
            {
                if (c != null) c.enabled = activate;
            }
        }
        if (tag != null && tag.col != null)
        {
            tag.col.enabled = activate;
        }
    }

    private void SetContainedColliders(bool active)
    {
        foreach (var item in items)
        {
            if (item != null && item.TryGetComponent(out Collider c))
            {
                c.enabled = active;
            }
        }
    }

    private void SetSteaming(bool active)
    {
        steamingDumpling = active;
        if (steamingParticle != null)
        {
            if (active) steamingParticle.Play();
            else steamingParticle.Stop();
        }
    }

    private void CookDumplings()
    {
        foreach (var item in items)
        {
            if (item != null && item.TryGetComponent(out ICookable cookable))
            {
                cookable.Cook();
            }
        }
    }

    public void TryCook()
    {
        if (currentCounter == 0) return;

        if (currentCounter < minDumplings)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlaySubtitle("min-req-dumplings");
            }
            return;
        }

        if (currentCounter >= minDumplings && furnace != null && furnace.isBurning && tag != null)
        {
            SetSteaming(true);
        }
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }

    private IEnumerator MoveToHandRoutine()
    {
        while (hand != null && Vector3.Distance(transform.position, hand.position) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, hand.position, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, hand.rotation, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (hand != null)
        {
            transform.SetParent(hand);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(inHandRotation);
        }

        moveToHandCoroutine = null;
    }
}