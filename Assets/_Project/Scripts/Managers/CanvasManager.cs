using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// Manages canvas visual transitions, black screen fades, and player sleep sequence choreography.
/// </summary>
public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance { get; private set; }

    [Header("UI")]
    public Image blackScreen;
    public CanvasGroup settingsCanvas;
    public CanvasGroup pauseMenuCanvas;
    public bool isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameManager.OnPlayerSleep += StartSleep;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerSleep -= StartSleep;
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasReleasedThisFrame)
        {
            TogglePause();
        }
    }

    public void BlackScreen(float duration)
    {
        if (blackScreen == null) return;

        blackScreen.DOKill();
        var seq = DOTween.Sequence();
        seq.Append(blackScreen.DOFade(1f, 0.25f));
        seq.AppendInterval(duration);
        seq.Append(blackScreen.DOFade(0f, 0.25f));
    }

    private void StartSleep()
    {
        StartCoroutine(SleepAnimationRoutine());
    }

    private IEnumerator SleepAnimationRoutine()
    {
        float t0 = Random.Range(1.5f, 3f);
        yield return new WaitForSeconds(t0);

        float d1 = Random.Range(0.5f, 1f);
        BlackScreen(d1);
        yield return new WaitForSeconds(d1);

        float t1 = Random.Range(1.5f, 2.5f);
        yield return new WaitForSeconds(t1);

        float d2 = Random.Range(0.5f, 2f);
        BlackScreen(d2);
        yield return new WaitForSeconds(d2);

        float t2 = Random.Range(0.5f, 2f);
        yield return new WaitForSeconds(t2);

        float d3 = Random.Range(0.5f, 2f);
        BlackScreen(d3);
        yield return new WaitForSeconds(d3);

        float t3 = Random.Range(1.5f, 2f);
        yield return new WaitForSeconds(t3);

        BlackScreen(10f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetNight();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        
        pauseMenuCanvas.gameObject.SetActive(isPaused);
        pauseMenuCanvas.DOFade(isPaused ? 1 : 0, .3f).From(0).SetUpdate(true).OnComplete(() =>
        {
            Cursor.visible = isPaused;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Time.timeScale = isPaused ? 0 : 1;
        });
    }
    public void OnResumeButtonClicked()
    {
        TogglePause();
    }
    public void OnSettingsButtonClicked()
    {
        pauseMenuCanvas.DOFade(0, .2f).From(1).SetUpdate(true);
        settingsCanvas.gameObject.SetActive(true);
        settingsCanvas.DOFade(1, .3f).From(0).SetUpdate(true);
    }
    public void OnSettingsCloseButtonClicked()
    {
        settingsCanvas.gameObject.SetActive(false);
        settingsCanvas.DOFade(0, .2f).From(1).SetUpdate(true);
        pauseMenuCanvas.DOFade(1, .3f).From(0).SetUpdate(true);
    }

    public void OnQuitButtonClicked()
    {
        TogglePause();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        DOVirtual.DelayedCall(0.3f, () =>
        {
            SceneManager.LoadScene("_Project/Scenes/Menu");
        });
    }
}