using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public CanvasGroup menuPanel;
    public CanvasGroup optionPanel;
    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene("_Project/Scenes/Main");
    }
    public void OnOptionButtonClicked()
    {
        menuPanel.DOFade(0, .3f).OnComplete(() => menuPanel.gameObject.SetActive(false));
        optionPanel.gameObject.SetActive(true);
        optionPanel.DOFade(1, .3f).From(0);
    }
    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    private void OnEnable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
