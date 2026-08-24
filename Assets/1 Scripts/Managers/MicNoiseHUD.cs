using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Handles real-time visual feedback for microphone sound detection.
/// Displays loudness meter, threshold danger marker, dynamic color states, and alert animations.
/// </summary>
public class MicNoiseHUD : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Microphone icon image to tint and animate based on sound intensity.")]
    public Image micIcon;
    [Tooltip("Filled Image component representing volume bar (set Image Type to Filled).")]
    public Image volumeBarFill;
    [Tooltip("Marker transform positioned along the meter to show where detection triggers.")]
    public RectTransform thresholdMarker;
    [Tooltip("Optional text element showing mic status or volume.")]
    public TMP_Text statusText;
    [Tooltip("CanvasGroup to control HUD alpha.")]
    public CanvasGroup hudCanvasGroup;

    [Header("Color Themes")]
    public Color safeColor = new Color(0.25f, 0.85f, 0.4f, 1f);       // Subtle green
    public Color detectedColor = new Color(1.0f, 0.82f, 0.15f, 1f);   // Warning yellow
    public Color dangerColor = new Color(0.95f, 0.2f, 0.2f, 1f);       // Aggressive red
    public Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);    // Inactive gray

    [Header("Animation Settings")]
    public bool enableSpikePulse = true;
    public float pulseScale = 1.35f;
    public float pulseDuration = 0.35f;
    public bool autoHideWhenQuiet = false;
    public float quietFadeDelay = 3.0f;
    public float fadeSpeed = 3.0f;

    [Header("Calibration Shortcut")]
    public KeyCode calibrateKey = KeyCode.F1;

    private float quietTimer = 0f;
    private Tween pulseTween;

    private void Start()
    {
        UpdateThresholdMarkerPosition();

        if (MicSoundDetector.Instance != null)
        {
            MicSoundDetector.Instance.OnLoudNoiseSpike += HandleLoudSpike;
            MicSoundDetector.Instance.OnMicrophoneStateChanged += HandleMicStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (MicSoundDetector.Instance != null)
        {
            MicSoundDetector.Instance.OnLoudNoiseSpike -= HandleLoudSpike;
            MicSoundDetector.Instance.OnMicrophoneStateChanged -= HandleMicStateChanged;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(calibrateKey) && MicSoundDetector.Instance != null)
        {
            MicSoundDetector.Instance.CalibrateNoiseFloor(1.5f);
        }

        if (MicSoundDetector.Instance == null) return;

        var detector = MicSoundDetector.Instance;
        float volume = detector.CurrentVolume;

        // Update Volume Bar Fill
        if (volumeBarFill != null)
        {
            volumeBarFill.fillAmount = volume;

            // Gradient color based on intensity
            if (volume >= detector.loudThreshold)
            {
                volumeBarFill.color = dangerColor;
            }
            else if (volume >= detector.noiseThreshold)
            {
                volumeBarFill.color = detectedColor;
            }
            else
            {
                volumeBarFill.color = safeColor;
            }
        }

        // Update Mic Icon Tint
        if (micIcon != null)
        {
            if (!detector.IsRecording)
            {
                micIcon.color = inactiveColor;
            }
            else if (detector.IsLoudSoundDetected)
            {
                micIcon.color = dangerColor;
            }
            else if (detector.IsSoundDetected)
            {
                micIcon.color = detectedColor;
            }
            else
            {
                micIcon.color = safeColor;
            }
        }

        // Update Status Text
        if (statusText != null)
        {
            if (!detector.IsRecording)
            {
                statusText.text = "<color=#888888>[MIC: OFF]</color>";
            }
            else if (detector.IsCalibrating)
            {
                statusText.text = "<color=#FFE866>[CALIBRATING ROOM NOISE...]</color>";
            }
            else if (detector.IsLoudSoundDetected)
            {
                statusText.text = "<color=#FF3333><b>[DANGER: NOISE SPIKE!]</b></color>";
            }
            else if (detector.IsSoundDetected)
            {
                statusText.text = "<color=#FFE866>[MIC: SPEAKING]</color>";
            }
            else
            {
                statusText.text = "<color=#55DD77>[MIC: QUIET]</color>";
            }
        }

        // Auto-fade HUD when silent
        if (autoHideWhenQuiet && hudCanvasGroup != null)
        {
            if (volume > 0.02f || detector.IsCalibrating)
            {
                quietTimer = 0f;
                hudCanvasGroup.alpha = Mathf.MoveTowards(hudCanvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            }
            else
            {
                quietTimer += Time.deltaTime;
                if (quietTimer > quietFadeDelay)
                {
                    hudCanvasGroup.alpha = Mathf.MoveTowards(hudCanvasGroup.alpha, 0.25f, Time.deltaTime * fadeSpeed);
                }
            }
        }
    }

    private void HandleLoudSpike(float volume)
    {
        if (!enableSpikePulse || micIcon == null) return;

        pulseTween?.Kill();
        micIcon.transform.localScale = Vector3.one;
        pulseTween = micIcon.transform.DOPunchScale(Vector3.one * (pulseScale - 1f), pulseDuration, 8, 0.5f);
    }

    private void HandleMicStateChanged(bool active)
    {
        if (statusText != null && !active)
        {
            statusText.text = "<color=#888888>[MIC: DISCONNECTED]</color>";
        }
    }

    /// <summary>
    /// Align the visual threshold marker to match the detector's NoiseThreshold percentage.
    /// </summary>
    public void UpdateThresholdMarkerPosition()
    {
        if (thresholdMarker == null || MicSoundDetector.Instance == null) return;

        float threshold = MicSoundDetector.Instance.noiseThreshold;
        if (thresholdMarker.parent is RectTransform parentRect)
        {
            float width = parentRect.rect.width;
            thresholdMarker.anchoredPosition = new Vector2(width * threshold, thresholdMarker.anchoredPosition.y);
        }
    }
}
