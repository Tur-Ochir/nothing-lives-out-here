using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the world-space tutorial canvas to show contextual prompts 
/// (e.g. "Press 'E' to Hide", "Press 'E' to Drive", "Press 'E' to Interact").
/// Can be positioned in world space above/near objects, face the camera, and fade/toggle smoothly.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI & Canvas References")]
    [Tooltip("The World Space Tutorial Canvas.")]
    public Canvas tutorialCanvas;

    [Tooltip("CanvasGroup used for smooth fade in / out.")]
    public CanvasGroup canvasGroup;

    [Tooltip("Text component displaying the tutorial / control message.")]
    public TMP_Text tutorialText;

    [Header("Settings")]
    [Tooltip("Default height offset above target when placed in world space.")]
    public Vector3 defaultOffset = new Vector3(0f, 0.5f, 0f);

    [Tooltip("Fade transition speed.")]
    public float fadeSpeed = 8f;

    [Tooltip("Default auto-hide delay (0 = stays until HideTutorial is called).")]
    public float defaultDuration = 0f;

    private Transform currentTarget;
    private Vector3 currentWorldPosition;
    private bool followTarget = false;
    private bool isVisible = false;
    private Coroutine autoHideCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tutorialCanvas == null)
        {
            tutorialCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (canvasGroup == null && tutorialCanvas != null)
        {
            canvasGroup = tutorialCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tutorialCanvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (tutorialText == null && tutorialCanvas != null)
        {
            tutorialText = tutorialCanvas.GetComponentInChildren<TMP_Text>(true);
        }

        // Hide initially
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        if (tutorialCanvas != null)
        {
            tutorialCanvas.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Follow target position if active
        if (isVisible && followTarget && currentTarget != null && tutorialCanvas != null)
        {
            tutorialCanvas.transform.position = currentTarget.position + defaultOffset;
        }

        // Smooth alpha fading
        if (canvasGroup != null && tutorialCanvas != null && tutorialCanvas.gameObject.activeSelf)
        {
            float targetAlpha = isVisible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

            if (!isVisible && canvasGroup.alpha <= 0.01f)
            {
                tutorialCanvas.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Shows a tutorial message at a specific world position or following a target Transform.
    /// </summary>
    /// <param name="message">The text to display (e.g. "Press 'E' to Hide").</param>
    /// <param name="target">The object/transform to position the tutorial at (optional).</param>
    /// <param name="duration">How long to show the message before auto-hiding (0 = stay visible).</param>
    public void ShowTutorial(string message, Transform target = null, float duration = 0f)
    {
        if (tutorialText != null)
        {
            tutorialText.text = message;
        }

        if (target != null)
        {
            currentTarget = target;
            followTarget = true;
            if (tutorialCanvas != null)
            {
                tutorialCanvas.transform.position = target.position + defaultOffset;
            }
        }
        else
        {
            followTarget = false;
        }

        if (tutorialCanvas != null)
        {
            tutorialCanvas.gameObject.SetActive(true);
        }

        isVisible = true;

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        float displayTime = duration > 0f ? duration : defaultDuration;
        if (displayTime > 0f)
        {
            autoHideCoroutine = StartCoroutine(AutoHideRoutine(displayTime));
        }
    }

    /// <summary>
    /// Shows a tutorial message at a specific static world position.
    /// </summary>
    public void ShowTutorialAtPosition(string message, Vector3 worldPosition, float duration = 0f)
    {
        currentTarget = null;
        followTarget = false;

        if (tutorialCanvas != null)
        {
            tutorialCanvas.transform.position = worldPosition + defaultOffset;
        }

        ShowTutorial(message, null, duration);
    }

    /// <summary>
    /// Hides the tutorial canvas.
    /// </summary>
    public void HideTutorial()
    {
        isVisible = false;
        currentTarget = null;
        followTarget = false;

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    private IEnumerator AutoHideRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideTutorial();
    }
}
