using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance;
    public Image blackScreen;
    private void Awake()
    {
        Instance = this;
    }

    public void BlackScreen(float duration)
    {
        var seq = DOTween.Sequence();
        seq.Append(blackScreen.DOFade(1f, 0.25f));
        seq.AppendInterval(duration);
        seq.Append(blackScreen.DOFade(0f, 0.25f));
    }
}