using System;
using System.Collections;
using UnityEngine;

public class Tag : MonoBehaviour, IInteractable, IHoldable, IHighlightable
{
    [Header("Tag / Lid Settings")]
    public Togoo togoo;

    [Header("Interactable")]
    public bool canInteract = true;
    public HoldType holdType = HoldType.OneHand;
    public bool moveToHand = true;
    public Vector3 inHandPositionOffset = Vector3.zero;
    public Vector3 inHandRotation;
    public float moveSpeed = 12f;
    public bool dropCurrentItem = true;
    public string reasonNotInteract;

    [Header("Throw / Drop Settings")]
    public bool applyThrowOnDrop = false;
    public float throwForce = 3f;

    [HideInInspector] public Outline outline;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Collider col;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;
    public HoldType HoldType => holdType;
    public bool IsHeld => PlayerManager.Instance != null && PlayerManager.Instance.heldItem == (IHoldable)this;
    public bool DropCurrentItemOnInteract => dropCurrentItem;

    private Transform hand;
    private Coroutine moveToHandCoroutine;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    public void Interact()
    {
        if (togoo != null && togoo.steamingDumpling)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlaySubtitle("raw-warning");
            }
            return;
        }

        if (!CanInteract) return;

        if (togoo != null)
        {   
            togoo.tag = null;
        }

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

        if (applyThrowOnDrop && rb != null && hand != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(hand.forward * throwForce, ForceMode.Impulse);
        }
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
        while (hand != null && Vector3.Distance(transform.position, hand.TransformPoint(inHandPositionOffset)) > 0.05f)
        {
            Vector3 targetPosition = hand.TransformPoint(inHandPositionOffset);
            Quaternion targetRotation = hand.rotation * Quaternion.Euler(inHandRotation);
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (hand != null)
        {
            transform.SetParent(hand);
            transform.localPosition = inHandPositionOffset;
            transform.localRotation = Quaternion.Euler(inHandRotation);
        }

        moveToHandCoroutine = null;
    }
}
