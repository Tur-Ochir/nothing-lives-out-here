using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public CanvasGroup menuPanel;
    public CanvasGroup optionPanel;
    public bool isInGameSettings;
    public TMP_Dropdown fpsDropdown;
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown displayModeDropdown;
    public GameManager.Language currentLanguage;
    public Slider  mainSoundSlider;
    public Slider  sfxSlider;
    public Slider  ambientSlider;

    public static UnityAction<GameManager.Language> OnLanguageChangedEvent;
    public static UnityAction<float> OnMainSoundChangedAction;
    public static UnityAction<float> OnSfxChangedAction;
    public static UnityAction<float> OnAmbientChangedAction;
    public float mainSoundVolume;
    public float sfxVolume;
    public float ambientVolume;

    private void Awake()
    {
        EnsureDropdownPauseFixers();
    }

    private void OnEnable()
    {
        EnsureDropdownPauseFixers();
    }

    private void Start()
    {
        InitializeAudioSliders();
    }

    private void InitializeAudioSliders()
    {
        if (mainSoundSlider != null)
        {
            mainSoundVolume = PlayerPrefs.GetFloat("Sound_MasterVolume", 1f);
            mainSoundSlider.SetValueWithoutNotify(mainSoundVolume);
        }

        if (sfxSlider != null)
        {
            sfxVolume = PlayerPrefs.GetFloat("Sound_SFXVolume", 1f);
            sfxSlider.SetValueWithoutNotify(sfxVolume);
        }

        if (ambientSlider != null)
        {
            ambientVolume = PlayerPrefs.GetFloat("Sound_AmbientVolume", 1f);
            ambientSlider.SetValueWithoutNotify(ambientVolume);
        }
    }

    private void EnsureDropdownPauseFixers()
    {
        TMP_Dropdown[] dropdowns = GetComponentsInChildren<TMP_Dropdown>(true);
        foreach (TMP_Dropdown dd in dropdowns)
        {
            if (dd != null && dd.GetComponent<DropdownPauseFixer>() == null)
            {
                dd.gameObject.AddComponent<DropdownPauseFixer>();
            }
        }
    }

    public void OnBackButtonClicked()
    {
        optionPanel.DOFade(0, .3f).SetUpdate(true).OnComplete(() => optionPanel.gameObject.SetActive(false));
        
        if (!isInGameSettings)
        {
            menuPanel.gameObject.SetActive(true);
            menuPanel.DOFade(1, .3f).From(0).SetUpdate(true);
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
    public void OnLanguageChanged()
    {
        switch (languageDropdown.value)
        {
            case 0:
                currentLanguage = GameManager.Language.English;
                break;
            case 1:
                currentLanguage = GameManager.Language.Mongolian;
                break;
        }
        OnLanguageChangedEvent.Invoke(currentLanguage);
    }

    public void OnMainSoundChanged()
    {
        mainSoundVolume = mainSoundSlider.value;
        OnMainSoundChangedAction.Invoke(mainSoundVolume);
    }
    public void OnSfxChanged()
    {
        sfxVolume = sfxSlider.value;
        OnSfxChangedAction.Invoke(sfxVolume);
    }
    public void OnAmbientChanged()
    {
        ambientVolume = ambientSlider.value;
        OnAmbientChangedAction.Invoke(ambientVolume);
    }
}
