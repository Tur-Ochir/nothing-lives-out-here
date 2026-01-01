using System;
using UnityEngine;
using Random = UnityEngine.Random;


public class GameManager : MonoBehaviour
{
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

    private void OnEnable()
    {
        monsterManager.OnStartWalk += OnMonsterWalk;
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
}