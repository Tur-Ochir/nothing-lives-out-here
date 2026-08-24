using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Argal : MonoBehaviour, IInteractable, IHoldable, ISpawnable, IHighlightable
{
    [Header("Argal Settings")]
    public float burnDur = 60f;

    [Header("Interactable")]
    public bool canInteract = true;
    public bool moveToHand = true;
    public Vector3 inHandRotation;
    public float moveSpeed = 12f;
    public bool dropCurrentItem = true;
    public string reasonNotInteract;

    [HideInInspector] public Outline outline;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Collider col;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;
    public bool IsHeld => PlayerManager.Instance != null && PlayerManager.Instance.heldItem == (IHoldable)this;
    public bool DropCurrentItemOnInteract => dropCurrentItem;

    private Transform hand;
    private Coroutine moveToHandCoroutine;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();
    }

    public void OnSpawned()
    {
        ActivateRandomChild();
    }

    public void ActivateRandomChild()
    {
        if (transform.childCount == 0) return;

        int r = UnityEngine.Random.Range(0, transform.childCount);
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i == r);
        }

        Transform activeChild = transform.GetChild(r);
        activeChild.localPosition = Vector3.zero;
        col = activeChild.GetComponent<Collider>();

        if (outline != null)
        {
            outline.AddMaterials();
            outline.enabled = false;
        }
    }

    public void Interact()
    {
        // Container pickup (e.g. into Arag basket)
        if (PlayerManager.Instance != null && PlayerManager.Instance.currentContainer is Arag arag)
        {
            if (arag.currentCounter >= arag.itemPoints.Length)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlaySubtitle("arag-full");
                }
                return;
            }

            SetRbColActive(false);
            transform.SetParent(arag.itemPoints[arag.currentCounter]);
            transform.DOLocalJump(Vector3.zero, 2.5f, 1, 0.5f);
            transform.DOLocalRotate(Vector3.zero, 0.5f);
            arag.currentCounter++;
            arag.items.Add(gameObject);
            OnInteracted?.Invoke();
            return;
        }

        if (!CanInteract) return;
        OnInteracted?.Invoke();
    }

    public void Pickup(Transform holdTransform)
    {
        if (holdTransform == null) return;

        SetRbColActive(false);
        hand = holdTransform;

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.heldItem = this;
        }

        if (moveToHandCoroutine != null) StopCoroutine(moveToHandCoroutine);
        moveToHandCoroutine = StartCoroutine(MoveToHandRoutine());
    }

    public void Drop()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.heldItem == (IHoldable)this)
        {
            PlayerManager.Instance.heldItem = null;
        }

        if (moveToHandCoroutine != null)
        {
            StopCoroutine(moveToHandCoroutine);
            moveToHandCoroutine = null;
        }

        transform.SetParent(null);
        SetRbColActive(true);
    }

    public void SetRbColActive(bool active)
    {
        if (col != null) col.enabled = active;
        if (rb != null) rb.isKinematic = !active;
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
