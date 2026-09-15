using System;
using DG.Tweening;
using UnityEngine;

public class Cap : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Cap Settings")]
    public bool canCap = true;
    public Vector3 firstPosition;
    public Vector3 firstRotation;
    public Vector3 secondPosition;
    public Vector3 secondRotation;
    public Lock currentLock;
    
    public float duration = 1f;
    public float jumpPower = 1f;
    public bool isCapped = true;
    public bool useJump = true;

    [Header("SFX")]
    public AudioClip openSFX;
    public AudioClip closeSFX;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Pitch Randomization")]
    public bool randomizePitch = true;
    public float minPitch = 0.88f;
    public float maxPitch = 1.12f;

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;
    [HideInInspector] public Outline outline;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;

    private AudioSource audioSource;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound in world
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 25f;
    }

    private void Start()
    {
        if (SoundManager.Instance != null && audioSource != null)
        {
            SoundManager.Instance.RegisterAudioSource(audioSource, SoundManager.SoundCategory.SFX, sfxVolume);
        }
    }

    private void OnDestroy()
    {
        if (SoundManager.Instance != null && audioSource != null)
        {
            SoundManager.Instance.UnregisterAudioSource(audioSource);
        }
    }

    public void Interact()
    {
        if (currentLock != null && currentLock.isLocked)
        {
            if (GameManager.Instance != null && !string.IsNullOrEmpty(reasonNotInteract))
            {
                GameManager.Instance.PlaySubtitle(reasonNotInteract);
            }
            return;
        }

        if (!canCap || !CanInteract) return;

        isCapped = !isCapped;
        Move(isCapped);
        PlaySFX(isCapped);
        OnInteracted?.Invoke();
    }

    private void PlaySFX(bool capped)
    {
        // When capped is true, it corresponds to closed/capped; when false, open/uncapped.
        var clip = capped ? closeSFX : openSFX;
        if (clip == null) return;

        float pitch = randomizePitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;

        if (audioSource != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, sfxVolume);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX3D(clip, transform.position, sfxVolume);
        }
    }

    private void Move(bool isFirst)
    {
        var target = isFirst ? firstPosition : secondPosition;
        var targetRot = isFirst ? firstRotation : secondRotation;

        transform.DOKill();
        if (useJump)
        {
            transform.DOLocalJump(target, jumpPower, 1, duration);
        }
        else
        {
            transform.DOLocalMove(target, duration);
        }
        transform.DOLocalRotate(targetRot, duration);
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}
