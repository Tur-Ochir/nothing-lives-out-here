using System;
using System.Collections;
using UnityEngine;

public class Dumpling : MonoBehaviour, IInteractable, IHoldable, ICookable, IEatable, IUsable, IHighlightable
{
    [Header("Dumpling Settings")]
    public bool cooked;
    public Color cookedColor;
    public GameObject eatenVer1;
    public int counter = 2;

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

    public bool IsCooked => cooked;
    public bool CanEat => cooked;
    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;
    public bool CanUse => true;
    public bool IsHeld => PlayerManager.Instance != null && PlayerManager.Instance.heldItem == (IHoldable)this;
    public bool DropCurrentItemOnInteract => dropCurrentItem;

    private MeshRenderer meshRenderer;
    private Transform hand;
    private Coroutine moveToHandCoroutine;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        outline = GetComponent<Outline>();
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    public void Cook()
    {
        cooked = true;

        if (meshRenderer != null)
        {
            meshRenderer.material.color = cookedColor;
        }

        if (eatenVer1 != null && eatenVer1.TryGetComponent(out MeshRenderer eatenRenderer))
        {
            eatenRenderer.material.color = cookedColor;
        }

        if (col != null)
        {
            col.enabled = true;
        }
    }

    public void Interact()
    {
        if (!CanInteract) return;
        OnInteracted?.Invoke();
    }

    public void Use()
    {
        Eat();
    }

    public void Eat()
    {
        if (!CanEat)
        {
            Debug.Log("Can't eat raw dumpling.");
            return;
        }

        Debug.Log("Eaten dumpling piece.");
        counter--;

        if (counter == 1)
        {
            if (meshRenderer != null) meshRenderer.enabled = false;
            if (eatenVer1 != null) eatenVer1.SetActive(true);
        }
        else if (counter < 1)
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.heldItem = null;
                PlayerManager.Instance.Eat();
            }
            Destroy(gameObject);
        }
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
