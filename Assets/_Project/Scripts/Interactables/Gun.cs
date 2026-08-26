using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Gun : MonoBehaviour, IInteractable, IHoldable, IUsable, IHighlightable
{
    [Header("Gun Settings")]
    public Transform muzzlePoint;
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
    public AudioClip fireSound;
    public AudioSource audioSource;
    public Animator animator;
    
    [Header("Parameters")]
    public float range = 100f;
    public float fireRate = 0.5f;
    public float damage = 10f;
    
    [Header("Recoil")]
    public float recoilZ = -0.1f;
    public float recoilXRotation = -5f;
    public float recoilDuration = 0.1f;

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
    public bool CanUse => true;
    public bool IsHeld => PlayerManager.Instance != null && PlayerManager.Instance.heldItem == (IHoldable)this;
    public bool DropCurrentItemOnInteract => dropCurrentItem;

    private float nextFireTime;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Transform hand;
    private Coroutine moveToHandCoroutine;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        originalLocalPos = new Vector3(0, 0, 0);
        originalLocalRot = Quaternion.Euler(0, 90, 0);
    }

    public void Interact()
    {
        if (!CanInteract) return;
        OnInteracted?.Invoke();
    }

    public void Use()
    {
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireRate;
        Shoot();
    }

    private void Shoot()
    {
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
        }

        if (fireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fireSound);
        }

        ApplyRecoil();

        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                Debug.Log($"Gun hit: {hit.transform.name}");

                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
        }
    }

    private void ApplyRecoil()
    {
        transform.DOKill();

        transform.DOLocalMoveZ(originalLocalPos.z + recoilZ, recoilDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            transform.DOLocalMoveZ(originalLocalPos.z, recoilDuration * 2f).SetEase(Ease.InOutSine);
        });

        transform.DOLocalRotate(new Vector3(0, 90, recoilXRotation), recoilDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            transform.DOLocalRotate(new Vector3(0, 90, 0), recoilDuration * 2f).SetEase(Ease.InOutSine);
        });

        if (animator != null)
        {
            animator.CrossFade("BoltAction", 0.1f);
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
        transform.DOKill();

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
