using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Lock / Key Reference")]
    public DoorLock doorLock;

    [Header("Animation")]
    public float rotationDuration = 1f;
    public Vector3 closedRotation;
    public Vector3 openRotation;

    [Header("SFX")]
    public AudioClip openSFX;
    public AudioClip closeSFX;
    public AudioClip[] knockSFX;

    [Header("State")]
    public bool isOpen;

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;
    [HideInInspector] public Outline outline;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;

    private AudioSource audioSource;
    private Tween rotateTween;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void Interact()
    {
        if (doorLock != null && doorLock.isLocked)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlaySubtitle(reasonNotInteract);
            }
            return;
        }

        if (!CanInteract) return;

        isOpen = !isOpen;
        HandleRotate(isOpen);
        PlaySFX(isOpen);
        OnInteracted?.Invoke();
    }

    private void HandleRotate(bool open)
    {
        Vector3 targetRotation = open ? openRotation : closedRotation;
        rotateTween?.Kill();
        rotateTween = transform.DOLocalRotate(targetRotation, rotationDuration);
    }

    private void PlaySFX(bool open)
    {
        if (audioSource == null) return;
        var clip = open ? openSFX : closeSFX;
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void Knock()
    {
        StartCoroutine(StartKnockingSFXRoutine());
    }

    private IEnumerator StartKnockingSFXRoutine()
    {
        for (int i = 0; i < 4; i++)
        {
            PlayKnockSFX();
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 1f));
        }   
    }

    private void PlayKnockSFX()
    {
        if (audioSource == null || knockSFX == null || knockSFX.Length == 0) return;
        audioSource.PlayOneShot(knockSFX[UnityEngine.Random.Range(0, knockSFX.Length)]);
    }

    public void TryOpenAnimation()
    {
        var sequence = DOTween.Sequence();
        for (int i = 0; i < 8; i++)
        {
            float target = (i % 2 == 1) ? 0f : UnityEngine.Random.Range(1f, 5f);
            float dur = UnityEngine.Random.Range(0.1f, 0.25f);
            sequence.Append(transform.DORotate(new Vector3(openRotation.x, target, openRotation.z), dur));
        }
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}
