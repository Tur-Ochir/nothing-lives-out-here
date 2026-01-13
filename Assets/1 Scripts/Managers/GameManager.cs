using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public Light mainLight;
    public LightBulb gerLight;
    public MonsterManager monsterManager;
    public Door gerDoor;
    [Header("Day/Night")]
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
    public static int EventIndex;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        monsterManager.OnStartWalk += OnMonsterWalk;
    }

    private void Start()
    {
        PlaySubtitle("intro");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            gerDoor.TryOpenAnimation();
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            isNight = !isNight;
            if (isNight)
            {
                SetDay();
            }
            else
            {
                SetNight();
            }
            
        }
    }

    private void OnMonsterWalk()
    {
        gerLight.SetActivate(false);
        StartCoroutine(gerLight.DelayedSetActive(Random.Range(2f, 4f), true));
    }
    public void DoorKnock()
    {
        gerDoor.Knock();
    }
    public void SetNight()
    {
        RenderSettings.fogColor = nightFogColor;
        RenderSettings.skybox.SetFloat("_Exposure", nightSkyboxExposure);
        mainLight.intensity = nightLightIntensity;
    }
    public void SetDay()
    {
        RenderSettings.fogColor = dayFogColor;
        RenderSettings.skybox.SetFloat("_Exposure", daySkyboxExposure);
        mainLight.intensity = dayLightIntensity;
    }

    public void PlaySubtitle(string id)
    {
        SubtitleLine line = database.subtitles.Find(x => x.id == id);
        if (line == null) return;

        string text = currentLanguage == Language.English ? line.english : line.mongolian;
        ShowSubtitle(text, line.duration);
    }

    private void ShowSubtitle(string text, float dur)
    {
        subtitleText.DOKill();
        subtitleText.text = text;
        var seq = DOTween.Sequence();
        seq.Append(subtitleText.DOFade(1f, 0.3f));
        seq.AppendInterval(dur);
        seq.Append(subtitleText.DOFade(0f, 0.3f));
    }
}