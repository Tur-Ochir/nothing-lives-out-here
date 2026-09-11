using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GeneralItem : MonoBehaviour, IInteractable, IHoldable, IHighlightable
{
    [Header("Interactable Settings")]
    public bool canInteract = true;
    public string reasonNotInteract;
    public bool dropCurrentItem = true;

    [Header("Hold Settings")]
    public bool canHold = true;
    public HoldType holdType = HoldType.OneHand;
    public bool moveToHand = true;
    public float moveSpeed = 12f;
    public Vector3 inHandPositionOffset = Vector3.zero;
    public Vector3 inHandRotationOffset = Vector3.zero;

    [Header("Throw / Drop Settings")]
    public bool applyThrowOnDrop = false;
    public float throwForce = 3f;

    [Header("Audio (Optional)")]
    public AudioClip pickupSound;
    public AudioClip dropSound;

    [Header("Events")]
    public UnityEvent onPickup;
    public UnityEvent onDrop;

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
        if (!CanInteract) return;
        OnInteracted?.Invoke();
    }

    public void Pickup(Transform holdTransform)
    {
        if (!canHold)
        {
            GameManager.Instance.PlaySubtitle(reasonNotInteract);
            return;
        }
        if (holdTransform == null) return;

        SetRbColActive(false);
        hand = holdTransform;

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.heldItem = this;
        }

        if (pickupSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(pickupSound);
        }

        onPickup?.Invoke();

        if (moveToHandCoroutine != null) StopCoroutine(moveToHandCoroutine);

        if (moveToHand)
        {
            moveToHandCoroutine = StartCoroutine(MoveToHandRoutine());
        }
        else
        {
            AttachToHandDirectly();
        }
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

        if (dropSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(dropSound);
        }

        onDrop?.Invoke();
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

    private void AttachToHandDirectly()
    {
        if (hand == null) return;
        transform.SetParent(hand);
        transform.localPosition = inHandPositionOffset;
        transform.localRotation = Quaternion.Euler(inHandRotationOffset);
    }

    private IEnumerator MoveToHandRoutine()
    {
        while (hand != null && Vector3.Distance(transform.position, hand.TransformPoint(inHandPositionOffset)) > 0.05f)
        {
            Vector3 targetPosition = hand.TransformPoint(inHandPositionOffset);
            Quaternion targetRotation = hand.rotation * Quaternion.Euler(inHandRotationOffset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, moveSpeed * Time.deltaTime);
            yield return null;
        }

        AttachToHandDirectly();
        moveToHandCoroutine = null;
    }
}
