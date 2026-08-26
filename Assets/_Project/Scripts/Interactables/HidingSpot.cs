using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Allows the player to hide inside or under objects (e.g., under bed, wardrobe, chest).
/// Disables movement, switches camera perspective, and dampens microphone noise detection.
/// </summary>
public class HidingSpot : MonoBehaviour, IInteractable, IHighlightable, IUsable
{
    [Header("Hiding Camera & Positions")]
    [Tooltip("Cinemachine camera activated when the player enters this hiding spot.")]
    public CinemachineCamera hidingCam;

    [Tooltip("Target transform where the player is placed when exiting.")]
    public Transform exitPoint;

    [Tooltip("Optional transform where the player character controller is repositioned while hidden.")]
    public Transform hidePoint;

    [Header("Hiding Settings")]
    public bool canEnterHiding = true;
    public bool canExitHiding = true;
    public bool useFadeScreen = true;
    public float fadeDuration = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Multiplier applied to microphone noise suspicion when player is hidden here.")]
    public float noiseDampeningMultiplier = 0.4f;

    [Header("Audio")]
    public AudioClip enterSFX;
    public AudioClip exitSFX;

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;
    [HideInInspector] public Outline outline;

    public event Action OnInteracted;
    public static event Action<HidingSpot, bool> OnPlayerHidingStateChanged;

    public bool CanInteract => canInteract && canEnterHiding;
    public string ReasonCannotInteract => reasonNotInteract;
    public bool CanUse => isHiding && canExitHiding;
    public bool IsHidden => isHiding;

    private bool isHiding = false;
    private AudioSource audioSource;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (hidingCam != null)
        {
            hidingCam.gameObject.SetActive(false);
        }
    }

    public void Interact()
    {
        if (isHiding)
        {
            ExitHiding();
            return;
        }

        if (!CanInteract)
        {
            if (GameManager.Instance != null && !string.IsNullOrEmpty(reasonNotInteract))
            {
                GameManager.Instance.PlaySubtitle(reasonNotInteract);
            }
            return;
        }

        if (PlayerManager.Instance != null)
        {
            EnterHiding(PlayerManager.Instance);
        }
    }

    public void Use()
    {
        if (CanUse)
        {
            ExitHiding();
        }
    }

    public void EnterHiding(PlayerManager player)
    {
        if (player == null || isHiding) return;

        isHiding = true;
        player.currentHidingSpot = this;

        // Disable player movement & crouch
        if (player.movement != null)
        {
            player.movement.canMove = false;
            player.movement.canCrouch = false;
        }

        // Reposition player if hide point is set
        if (hidePoint != null)
        {
            if (player.TryGetComponent<CharacterController>(out var cc))
            {
                cc.enabled = false;
                player.transform.position = hidePoint.position;
                player.transform.rotation = hidePoint.rotation;
                cc.enabled = true;
            }
            else
            {
                player.transform.position = hidePoint.position;
                player.transform.rotation = hidePoint.rotation;
            }
        }

        // Activate hiding camera
        if (hidingCam != null)
        {
            hidingCam.gameObject.SetActive(true);
        }

        // Screen transition
        if (useFadeScreen && CanvasManager.Instance != null)
        {
            CanvasManager.Instance.BlackScreen(fadeDuration);
        }

        // Audio
        if (audioSource != null && enterSFX != null)
        {
            audioSource.PlayOneShot(enterSFX);
        }

        OnPlayerHidingStateChanged?.Invoke(this, true);
        OnInteracted?.Invoke();
    }

    public void ExitHiding()
    {
        if (!isHiding || !canExitHiding) return;

        isHiding = false;

        if (PlayerManager.Instance != null)
        {
            var player = PlayerManager.Instance;
            player.currentHidingSpot = null;

            // Reposition player to exit point
            if (exitPoint != null)
            {
                if (player.TryGetComponent<CharacterController>(out var cc))
                {
                    cc.enabled = false;
                    player.transform.position = exitPoint.position;
                    player.transform.rotation = exitPoint.rotation;
                    cc.enabled = true;
                }
                else
                {
                    player.transform.position = exitPoint.position;
                    player.transform.rotation = exitPoint.rotation;
                }
            }

            // Restore player movement & crouch
            if (player.movement != null)
            {
                player.movement.canMove = true;
                player.movement.canCrouch = true;
            }
        }

        // Deactivate hiding camera
        if (hidingCam != null)
        {
            hidingCam.gameObject.SetActive(false);
        }

        // Screen transition
        if (useFadeScreen && CanvasManager.Instance != null)
        {
            CanvasManager.Instance.BlackScreen(fadeDuration);
        }

        // Audio
        if (audioSource != null && exitSFX != null)
        {
            audioSource.PlayOneShot(exitSFX);
        }

        OnPlayerHidingStateChanged?.Invoke(this, false);
    }

    public void SetHighlight(bool active)
    {
        if (outline != null)
        {
            outline.enabled = active;
        }
    }
}
