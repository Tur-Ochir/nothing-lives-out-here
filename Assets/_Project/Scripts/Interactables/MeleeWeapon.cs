using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class MeleeWeapon : MonoBehaviour, IInteractable, IHoldable, IUsable, IHighlightable
{
    public enum AttackAnimationType
    {
        Animator,       // Uses Unity Animator (trigger or CrossFade state)
        ProceduralSwing // Procedural swing using DOTween if no animator clip is setup
    }

    [Header("Interactable Settings")]
    public bool canInteract = true;
    public string reasonNotInteract;
    public bool dropCurrentItem = true;

    [Header("Hold Settings")]
    public HoldType holdType = HoldType.OneHand;
    public bool moveToHand = true;
    public float moveSpeed = 12f;
    public Vector3 inHandPositionOffset = Vector3.zero;
    public Vector3 inHandRotationOffset = Vector3.zero;

    [Header("Melee / Attack Settings")]
    public AttackAnimationType animationType = AttackAnimationType.Animator;
    public Animator animator;
    public string attackTriggerName = "Attack";
    public string attackStateName = "Attack";
    public float attackRate = 0.5f;
    public float attackRange = 2.5f;
    public float attackRadius = 0.5f;
    public float damage = 20f;
    public LayerMask hitLayers = ~0;

    [Header("Procedural Swing (DOTween)")]
    public Vector3 swingPunchRotation = new Vector3(45f, -30f, 20f);
    public Vector3 swingPunchPosition = new Vector3(0f, -0.05f, 0.15f);
    public float swingDuration = 0.25f;

    [Header("Effects & Sounds")]
    public AudioClip attackSwingSound;
    public AudioClip hitSound;
    public GameObject hitEffectPrefab;

    [Header("Throw / Drop Settings")]
    public bool applyThrowOnDrop = false;
    public float throwForce = 3f;
    public AudioClip pickupSound;
    public AudioClip dropSound;

    [Header("Pitch Randomization")]
    public bool randomizePitch = true;
    public float minPitch = 0.88f;
    public float maxPitch = 1.12f;

    [Header("Events")]
    public UnityEvent onAttack;
    public UnityEvent<RaycastHit> onHitTarget;
    public UnityEvent onPickup;
    public UnityEvent onDrop;

    [HideInInspector] public Outline outline;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Collider col;
    [HideInInspector] public AudioSource audioSource;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;
    public bool CanUse => IsHeld && Time.time >= nextAttackTime;
    public HoldType HoldType => holdType;
    public bool IsHeld => PlayerManager.Instance != null && PlayerManager.Instance.heldItem == (IHoldable)this;
    public bool DropCurrentItemOnInteract => dropCurrentItem;

    private Transform hand;
    private Coroutine moveToHandCoroutine;
    private float nextAttackTime;
    private Tween currentSwingTween;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        if (SoundManager.Instance != null && audioSource != null)
        {
            SoundManager.Instance.RegisterAudioSource(audioSource, SoundManager.SoundCategory.SFX);
        }
    }

    private void OnDisable()
    {
        if (SoundManager.Instance != null && audioSource != null)
        {
            SoundManager.Instance.UnregisterAudioSource(audioSource);
        }
    }

    public void Interact()
    {
        if (!CanInteract) return;
        OnInteracted?.Invoke();
    }

    public void Use()
    {
        if (!CanUse) return;

        nextAttackTime = Time.time + attackRate;
        Attack();
    }

    private void Attack()
    {
        PlayAttackAnimation();

        if (attackSwingSound != null)
        {
            float pitch = randomizePitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;
            if (audioSource != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(attackSwingSound);
            }
            else if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(attackSwingSound, 1f, pitch);
            }
        }

        onAttack?.Invoke();
        PerformHitDetection();
    }

    private void PlayAttackAnimation()
    {
        if (animationType == AttackAnimationType.Animator && animator != null)
        {
            if (!string.IsNullOrEmpty(attackTriggerName))
            {
                animator.SetTrigger(attackTriggerName);
            }
            else if (!string.IsNullOrEmpty(attackStateName))
            {
                animator.CrossFade(attackStateName, 0.05f);
            }
        }
        else
        {
            // Procedural swing fallback using DOTween
            transform.DOKill();
            transform.localPosition = inHandPositionOffset;
            transform.localRotation = Quaternion.Euler(inHandRotationOffset);

            Sequence swingSeq = DOTween.Sequence();
            swingSeq.Append(transform.DOLocalRotate(inHandRotationOffset + swingPunchRotation, swingDuration * 0.4f).SetEase(Ease.OutQuad));
            swingSeq.Join(transform.DOLocalMove(inHandPositionOffset + swingPunchPosition, swingDuration * 0.4f).SetEase(Ease.OutQuad));
            swingSeq.Append(transform.DOLocalRotate(inHandRotationOffset, swingDuration * 0.6f).SetEase(Ease.InSine));
            swingSeq.Join(transform.DOLocalMove(inHandPositionOffset, swingDuration * 0.6f).SetEase(Ease.InSine));
            currentSwingTween = swingSeq;
        }
    }

    private void PerformHitDetection()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        bool hitSomething = Physics.SphereCast(ray, attackRadius, out RaycastHit hit, attackRange, hitLayers);

        if (hitSomething)
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            if (hitSound != null)
            {
                float pitch = randomizePitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX(hitSound, 1f, pitch);
                }
                else if (audioSource != null)
                {
                    audioSource.pitch = pitch;
                    audioSource.PlayOneShot(hitSound);
                }
            }

            onHitTarget?.Invoke(hit);
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

        if (pickupSound != null && audioSource != null)
        {
            audioSource.pitch = randomizePitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;
            audioSource.PlayOneShot(pickupSound);
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

        if (applyThrowOnDrop && rb != null && hand != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(hand.forward * throwForce, ForceMode.Impulse);
        }

        if (dropSound != null && audioSource != null)
        {
            audioSource.pitch = randomizePitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;
            audioSource.PlayOneShot(dropSound);
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

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
