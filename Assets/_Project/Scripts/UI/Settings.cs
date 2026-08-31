using DG.Tweening;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public CanvasGroup menuPanel;
    public CanvasGroup optionPanel;
    public bool isInGameSettings;
    public Dropdown fpsDropdown;
    public Dropdown languageDropdown;
    public Dropdown displayModeDropdown;
    
    public void OnBackButtonClicked()
    {
        optionPanel.DOFade(0, .3f).OnComplete(() => optionPanel.gameObject.SetActive(false));
        
        if (!isInGameSettings)
        {
            menuPanel.gameObject.SetActive(true);
            menuPanel.DOFade(1, .3f).From(0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1;
        }
    }

    public void OnMaxFpsChanged()
    {
        switch (fpsDropdown.value)
        {
            case 0:
                SetMaxFps(30);
                break;
            case 1:
                SetMaxFps(60);
                break;
            case 2:
                SetMaxFps(120);
                break;
            case 3:
                SetMaxFps(240);
                break;
            case 4:
                SetMaxFps(0);
                break;
        }
    }
    public void SetMaxFps(int fps)
    {
        Application.targetFrameRate = fps;
    }

    public void ToggleVsync()
    {
        QualitySettings.vSyncCount = QualitySettings.vSyncCount == 0 ? 1 : 0;
    }

    public void OnDisplayModeChanged()
    {
        switch (displayModeDropdown.value)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
        }
    }
}
