using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Arag : MonoBehaviour, IItemContainer, IHoldable, IHighlightable
{
    [Header("Container Settings")]
    public bool canContainItems = true;
    public Transform[] itemPoints;
    public int currentCounter;
    public List<GameObject> items = new List<GameObject>();

    [Header("Hold Settings")]
    public bool canHold = true;
    public HoldType holdType = HoldType.TwoHands;
    public float moveSpeed = 12f;
    public Vector3 inHandPositionOffset = Vector3.zero;
    public Vector3 inHandRotation;

    [Header("Throw / Drop Settings")]
    public bool applyThrowOnDrop = false;
    public float throwForce = 3f;

    [Header("Audio (Optional)")]
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public bool randomizePitch = true;
    public float minPitch = 0.88f;
    public float maxPitch = 1.12f;
    [HideInInspector] public AudioSource audioSource;

    public Rigidbody rb;
    public Collider[] colliders;
    public List<Collider> itemColliders = new List<Collider>();
    [HideInInspector] public Outline outline;

    private List<Outline> itemOutlines = new List<Outline>();
    private Transform hand;
    private Coroutine moveToHandCoroutine;

    public bool CanContainItems => canContainItems;
    public int ItemCount => currentCounter;
    public int Capacity => itemPoints != null ? itemPoints.Length : 0;
    public bool CanHold => canHold;
    public HoldType HoldType => holdType;
    public bool DropCurrentItemOnInteract => false;
    public bool IsHeld => PlayerManager.Instance != null && PlayerManager.Instance.heldItem == (IHoldable)this;

    private void Awake()
    {
        colliders = GetComponents<Collider>();
        rb = GetComponent<Rigidbody>();
        outline = GetComponent<Outline>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
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

    public bool TryContain(GameObject item)
    {
        if (!CanContainItems || item == null) return false;
        if (itemPoints == null || currentCounter >= itemPoints.Length) return false;

        if (item.transform.parent.TryGetComponent(out Argal argal))
        {
            argal.SetRbColActive(false);
            argal.PlaySFX(argal.pickupSound);
        }

        Transform targetParent = itemPoints[currentCounter];
        item.transform.SetParent(targetParent);
        item.transform.DOLocalMove(Vector3.zero, 0.5f).OnComplete(() =>
        {
            if (item.TryGetComponent(out Collider c)) c.enabled = true;
        });
        item.transform.DOLocalRotate(Vector3.zero, 0.5f);

        if (item.TryGetComponent(out Outline itemOutline))
        {
            itemOutline.OutlineMode = Outline.Mode.OutlineVisible;
            itemOutlines.Add(itemOutline);
        }

        if (PlayerManager.Instance != null && PlayerManager.Instance.heldItem != (IHoldable)this)
        {
            PlayerManager.Instance.heldItem = null;
        }

        currentCounter++;
        items.Add(item);
        return true;
    }

    public void AddItemOutline(Outline itemOutline)
    {
        if (itemOutline != null && !itemOutlines.Contains(itemOutline))
        {
            itemOutlines.Add(itemOutline);
        }
    }

    public void Remove(GameObject item)
    {
        if (item != null)
        {
            currentCounter = Mathf.Max(0, currentCounter - 1);
            items.Remove(item);

            if (item.TryGetComponent(out Outline itemOutline))
            {
                itemOutlines.Remove(itemOutline);
            }
        }
    }

    public void Pickup(Transform holdTransform)
    {
        if (!CanHold || holdTransform == null) return;

        SetActivateCollider(false);
        hand = holdTransform;

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.heldItem = this;
        }

        SetContainedColliders(false);

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (pickupSound != null && audioSource != null)
        {
            audioSource.pitch = randomizePitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;
            audioSource.PlayOneShot(pickupSound);
        }

        if (moveToHandCoroutine != null) StopCoroutine(moveToHandCoroutine);
        moveToHandCoroutine = StartCoroutine(MoveToHandRoutine());
    }

    public void Drop()
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

        if (PlayerManager.Instance != null && PlayerManager.Instance.heldItem == (IHoldable)this)
        {
            PlayerManager.Instance.heldItem = null;
        }

        SetContainedColliders(true);
        SetActivateCollider(true);

        if (dropSound != null && audioSource != null)
        {
            audioSource.pitch = randomizePitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;
            audioSource.PlayOneShot(dropSound);
        }

        if (applyThrowOnDrop && rb != null && hand != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(hand.forward * throwForce, ForceMode.Impulse);
        }
        // Debug.Log("Release Arag");
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

        foreach (var c in itemColliders)
        {
            c.enabled = activate;
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

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;

        foreach (var io in itemOutlines)
        {
            if (io != null) io.enabled = active;
        }
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
