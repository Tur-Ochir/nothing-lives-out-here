using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages canvas visual transitions, black screen fades, and player sleep sequence choreography.
/// </summary>
public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance { get; private set; }

    [Header("UI")]
    public Image blackScreen;

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
}