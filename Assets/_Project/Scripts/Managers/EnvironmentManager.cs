using UnityEngine;

/// <summary>
/// Handles environment lighting, fog, and skybox transitions between day and night.
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    [Header("Lights & Scene")]
    public Light mainLight;

    [Header("Day Settings")]
    public float dayLightIntensity = 0.01f;
    public float daySkyboxExposure = 0.01f;
    public Color dayFogColor;

    [Header("Night Settings")]
    public float nightLightIntensity = 0f;
    public float nightSkyboxExposure = 0.01f;
    public Color nightFogColor;

    [Header("State")]
    public bool isNight = false;

    public void SetNight()
    {
        isNight = true;
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

    public void SetDay()
    {
        isNight = false;
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
}
