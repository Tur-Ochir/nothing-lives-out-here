using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Interactable")]
    public bool moveToHand;

    public Vector3 inHandRotation;
    public float moveDuration = 0.5f;
    public bool isMovingToHand;
    public bool dropCurrentItem = true;
    public float moveSpeed = 12f;
    public Container container;
    
    [HideInInspector] public Outline outline;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Collider col;

    Transform hand;
    protected virtual void Awake()
    {
        outline = GetComponent<Outline>();
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        
    }

    protected virtual void OnEnable()
    {
        
    }

    protected virtual void OnDisable()
    {
        
    }

    public virtual void Interact()
    {
        Debug.Log($"Interact {transform.name}");

        if (moveToHand && !PlayerManager.Instance.IsHoldingItem)
        {
            SetRbColActive(false);

            hand = PlayerManager.Instance.handPoint;
            PlayerManager.Instance.heldItem = this;
            StartCoroutine(MoveToHand());  
        }
    }

    public void SetRbColActive(bool active)
    {
        if (col)
            col.enabled = active;
        
        if (rb)
        {
            rb.isKinematic = !active;
        }
    }

    public virtual void SetOutline(bool active)
    {
        if (outline == null) return;
        
        outline.enabled = active;
    }
    
    
    public virtual void Drop()
    {
        if (PlayerManager.Instance.heldItem != this)
            return;

        PlayerManager.Instance.heldItem = null;

        transform.SetParent(null);

        SetRbColActive(true);
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
        var r = Quaternion.Euler(inHandRotation);
        transform.localRotation = r;

        isMovingToHand = false;
    }
}