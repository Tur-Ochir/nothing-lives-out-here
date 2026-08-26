using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Environment")]
    public EnvironmentManager environmentManager;
    public Light mainLight;
    public LightBulb gerLight;
    public MonsterManager monsterManager;
    public Door gerDoor;

    [Header("Day/Night Settings")]
    public float dayLightIntensity = 0.01f;
    public float daySkyboxExposure = 0.01f;
    public Color dayFogColor;
    public float nightLightIntensity = 0;
    public float nightSkyboxExposure = 0.01f;
    public Color nightFogColor;
    public bool isNight = false;

    [Header("Subtitles")]
    public SubtitleDatabase database;
    public TMP_Text subtitleText;
    public enum Language { English, Mongolian }
    public Language currentLanguage = Language.English;

    [Header("Game State")]
    public static int EventIndex;
    public static UnityAction OnPlayerEatFill;
    public static UnityAction OnPlayerSleep;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupEnvironmentManager();
    }

    private void SetupEnvironmentManager()
    {
        if (environmentManager == null)
        {
            environmentManager = GetComponent<EnvironmentManager>();
            if (environmentManager == null)
            {
                environmentManager = gameObject.AddComponent<EnvironmentManager>();
            }
        }

        environmentManager.mainLight = mainLight;
        environmentManager.dayLightIntensity = dayLightIntensity;
        environmentManager.daySkyboxExposure = daySkyboxExposure;
        environmentManager.dayFogColor = dayFogColor;
        environmentManager.nightLightIntensity = nightLightIntensity;
        environmentManager.nightSkyboxExposure = nightSkyboxExposure;
        environmentManager.nightFogColor = nightFogColor;
        environmentManager.isNight = isNight;
    }

    private void OnEnable()
    {
        if (monsterManager != null)
        {
            monsterManager.OnStartWalk += OnMonsterWalk;
        }
        OnPlayerEatFill += HandlePlayerEatFill;
    }

    private void OnDisable()
    {
        if (monsterManager != null)
        {
            monsterManager.OnStartWalk -= OnMonsterWalk;
        }
        OnPlayerEatFill -= HandlePlayerEatFill;
    }

    private void Update()
    {
        // Debug inputs
        if (Input.GetKeyDown(KeyCode.V) && gerDoor != null)
        {
            gerDoor.TryOpenAnimation();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            ToggleDayNight();
        }
    }

    private void HandlePlayerEatFill()
    {
        PlaySubtitle("eat-fill");
    }

    private void OnMonsterWalk()
    {
        if (gerLight != null)
        {
            gerLight.SetActivate(false);
            StartCoroutine(gerLight.DelayedSetActive(Random.Range(2f, 4f), true));
        }
    }

    public void DoorKnock()
    {
        if (gerDoor != null)
        {
            gerDoor.Knock();
        }
    }

    public void SetNight()
    {
        isNight = true;
        if (environmentManager != null)
        {
            environmentManager.SetNight();
        }
        else
        {
            RenderSettings.fogColor = nightFogColor;
            if (RenderSettings.skybox != null)
            {
                RenderSettings.skybox.SetFloat("_Exposure", nightSkyboxExposure);
            }
            if (mainLight != null)
            {
                mainLight.intensity = nightLightIntensity;
            }
        }
    }

    public void SetDay()
    {
        isNight = false;
        if (environmentManager != null)
        {
            environmentManager.SetDay();
        }
        else
        {
            RenderSettings.fogColor = dayFogColor;
            if (RenderSettings.skybox != null)
            {
                RenderSettings.skybox.SetFloat("_Exposure", daySkyboxExposure);
            }
            if (mainLight != null)
            {
                mainLight.intensity = dayLightIntensity;
            }
        }
    }

    public void ToggleDayNight()
    {
        if (isNight)
        {
            SetDay();
        }
        else
        {
            SetNight();
        }
    }

    public void PlaySubtitle(string id)
    {
        if (database == null || database.subtitles == null) return;

        SubtitleLine line = database.subtitles.Find(x => x.id == id);
        if (line == null) return;

        string text = currentLanguage == Language.English ? line.english : line.mongolian;
        ShowSubtitle(text, line.duration);
    }

    private void ShowSubtitle(string text, float dur)
    {
        if (subtitleText == null) return;

        subtitleText.DOKill();
        subtitleText.text = text;
        var seq = DOTween.Sequence();
        seq.Append(subtitleText.DOFade(1f, 0.3f));
        seq.AppendInterval(dur);
        seq.Append(subtitleText.DOFade(0f, 0.3f));
    }
}