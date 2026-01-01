using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Container : MonoBehaviour
{
    [Header("Container")]
    public bool canContainItems;
    public Transform[] itemPoints;
    public int currentCounter;
    [Header("Hold")]
    public bool canHold;
    public float moveDuration = 0.5f;
    public bool isMovingToHand;
    public float moveSpeed = 12f;
    public float groundCheckDistance = 1f;
    public List<Interactable> items = new List<Interactable>();
    private Transform hand;
    private bool activateCollider;
    [SerializeField] private LayerMask groundLayer; 

    public Rigidbody rb;
    public Collider[] colliders;

    protected virtual void Awake()
    {
        colliders = GetComponents<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void Update()
    {
        if (IsGrounded() && activateCollider)
        {
            activateCollider = false;
            SetActivateCollider(true);
        }
    }
    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }


    public virtual bool TryContain(Interactable item)
    {
        if (!canContainItems) return false;
        if (itemPoints.Length <= currentCounter) return false;
        
        item.container = this;
        currentCounter++;
        items.Add(item);
        Debug.Log($"{transform.name} contains {item.transform.name} size: {itemPoints.Length} count: {currentCounter}");
        return true;
    }
    public virtual void Remove(Interactable item)
    {
        currentCounter--;
        item.container = null;
        
        if (items.Contains(item))
        {
            items.Remove(item);
        }
    }

    public virtual void Hold()
    {
        if (!canHold) return;
        
        SetActivateCollider(false);
        hand = PlayerManager.Instance.twoHandPoint;
        PlayerManager.Instance.currentContainer = this;

        for (int i = 0; i < items.Count; i++)
        {
            items[i].col.enabled = false;
        }

        rb.isKinematic = true;
        StartCoroutine(MoveToHand());
    }

    public virtual void Release()
    {
        if (!canHold) return;
        
        activateCollider = true;
        rb.isKinematic = false;
        transform.SetParent(null);
        PlayerManager.Instance.currentContainer = null;
        
        for (int i = 0; i < items.Count; i++)
        {
            items[i].col.enabled = true;
        }
    }

    public virtual bool TryGet(Container container)
    {
        return true;
    }

    public virtual void SetActivateCollider(bool activate)
    {
        foreach (var c in colliders)
        {
            c.enabled = activate;
        }
    }

    IEnumerator MoveToHand()
    {
        isMovingToHand = true;

        while (Vector3.Distance(transform.position, hand.position) > 0.1f)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                hand.position,
                moveSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                hand.rotation,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        // Final snap
        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        isMovingToHand = false;
    }
}