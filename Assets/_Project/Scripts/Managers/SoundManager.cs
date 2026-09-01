using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public enum SoundCategory
    {
        Master,
        SFX,
        Ambient
    }

    [Header("Audio Mixer (Optional)")]
    public AudioMixer audioMixer;
    public string masterVolumeParam = "MasterVolume";
    public string sfxVolumeParam = "SFXVolume";
    public string ambientVolumeParam = "AmbientVolume";
    public AudioMixerGroup masterGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup ambientGroup;

    [Header("Default Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource ambientSource;

    [Header("Volume Levels (0.0 - 1.0)")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 1f;

    private const string MasterPrefKey = "Sound_MasterVolume";
    private const string SfxPrefKey = "Sound_SFXVolume";
    private const string AmbientPrefKey = "Sound_AmbientVolume";

    // Registry for external AudioSources across the scene
    private class RegisteredSource
    {
        public AudioSource source;
        public SoundCategory category;
        public float baseVolume;
    }

    private readonly List<RegisteredSource> _registeredSources = new List<RegisteredSource>();
    private readonly Queue<AudioSource> _sfx3DPool = new Queue<AudioSource>();
    private const int PoolInitialSize = 6;
    private Tween _ambientFadeTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureDefaultSources();
        LoadVolumes();
        InitializePool();
    }

    private void OnEnable()
    {
        Settings.OnMainSoundChangedAction += SetMasterVolume;
        Settings.OnSfxChangedAction += SetSFXVolume;
        Settings.OnAmbientChangedAction += SetAmbientVolume;
    }

    private void OnDisable()
    {
        Settings.OnMainSoundChangedAction -= SetMasterVolume;
        Settings.OnSfxChangedAction -= SetSFXVolume;
        Settings.OnAmbientChangedAction -= SetAmbientVolume;
    }

    private void Start()
    {
        ApplyVolumes();
    }

    private void EnsureDefaultSources()
    {
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource_2D");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
        }

        if (ambientSource == null)
        {
            GameObject ambObj = new GameObject("AmbientSource_2D");
            ambObj.transform.SetParent(transform);
            ambientSource = ambObj.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            if (ambientGroup != null) ambientSource.outputAudioMixerGroup = ambientGroup;
        }
    }

    private void InitializePool()
    {
        GameObject poolContainer = new GameObject("SFX_3D_Pool");
        poolContainer.transform.SetParent(transform);

        for (int i = 0; i < PoolInitialSize; i++)
        {
            AudioSource src = poolContainer.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f; // 3D sound
            src.rolloffMode = AudioRolloffMode.Linear;
            if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup;
            _sfx3DPool.Enqueue(src);
        }
    }

    #region Volume & Settings

    public void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterPrefKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxPrefKey, 1f);
        ambientVolume = PlayerPrefs.GetFloat(AmbientPrefKey, 1f);
    }

    public void SaveVolumes()
    {
        PlayerPrefs.SetFloat(MasterPrefKey, masterVolume);
        PlayerPrefs.SetFloat(SfxPrefKey, sfxVolume);
        PlayerPrefs.SetFloat(AmbientPrefKey, ambientVolume);
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SaveVolumes();
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SaveVolumes();
        ApplyVolumes();
    }

    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        SaveVolumes();
        ApplyVolumes();
    }

    public void ApplyVolumes()
    {
        // 1. If AudioMixer is connected, update exposed dB parameters
        if (audioMixer != null)
        {
            SetMixerVolume(masterVolumeParam, masterVolume);
            SetMixerVolume(sfxVolumeParam, sfxVolume);
            SetMixerVolume(ambientVolumeParam, ambientVolume);
        }

        // 2. Direct internal source volumes
        if (sfxSource != null)
        {
            sfxSource.volume = GetEffectiveVolume(SoundCategory.SFX);
        }

        if (ambientSource != null)
        {
            ambientSource.volume = GetEffectiveVolume(SoundCategory.Ambient);
        }

        // 3. Update all registered scene AudioSources
        UpdateRegisteredSources();
    }

    private void SetMixerVolume(string paramName, float linearVolume)
    {
        if (string.IsNullOrEmpty(paramName)) return;
        // Convert 0.0-1.0 linear volume to dB (-80dB to 0dB)
        float clamped = Mathf.Clamp(linearVolume, 0.0001f, 1f);
        float dB = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(paramName, dB);
    }

    public float GetEffectiveVolume(SoundCategory category)
    {
        switch (category)
        {
            case SoundCategory.Master:
                return masterVolume;
            case SoundCategory.SFX:
                return masterVolume * sfxVolume;
            case SoundCategory.Ambient:
                return masterVolume * ambientVolume;
            default:
                return masterVolume;
        }
    }

    #endregion

    #region Audio Source Registration

    /// <summary>
    /// Register any GameObject's AudioSource to automatically follow SoundManager volume channels.
    /// </summary>
    public void RegisterAudioSource(AudioSource source, SoundCategory category, float baseVolume = 1f)
    {
        if (source == null) return;

        // Auto assign mixer group if available
        if (source.outputAudioMixerGroup == null)
        {
            if (category == SoundCategory.SFX && sfxGroup != null) source.outputAudioMixerGroup = sfxGroup;
            else if (category == SoundCategory.Ambient && ambientGroup != null) source.outputAudioMixerGroup = ambientGroup;
            else if (category == SoundCategory.Master && masterGroup != null) source.outputAudioMixerGroup = masterGroup;
        }

        _registeredSources.RemoveAll(r => r.source == null || r.source == source);
        _registeredSources.Add(new RegisteredSource
        {
            source = source,
            category = category,
            baseVolume = baseVolume
        });

        // Set initial volume
        source.volume = baseVolume * GetEffectiveVolume(category);
    }

    public void UnregisterAudioSource(AudioSource source)
    {
        if (source == null) return;
        _registeredSources.RemoveAll(r => r.source == null || r.source == source);
    }

    private void UpdateRegisteredSources()
    {
        for (int i = _registeredSources.Count - 1; i >= 0; i--)
        {
            RegisteredSource reg = _registeredSources[i];
            if (reg.source == null)
            {
                _registeredSources.RemoveAt(i);
                continue;
            }

            reg.source.volume = reg.baseVolume * GetEffectiveVolume(reg.category);
        }
    }

    #endregion

    #region Playback Methods

    /// <summary>
    /// Play a 2D one-shot sound effect.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volumeScale * GetEffectiveVolume(SoundCategory.SFX));
    }

    /// <summary>
    /// Play a 3D spatial sound effect at a specific world position using pooled audio sources.
    /// </summary>
    public void PlaySFX3D(AudioClip clip, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 25f)
    {
        if (clip == null) return;

        AudioSource src = GetPooled3DSource();
        src.transform.position = position;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.volume = volumeScale * GetEffectiveVolume(SoundCategory.SFX);
        src.clip = clip;
        src.Play();

        StartCoroutine(ReturnSourceToPool(src, clip.length));
    }

    private AudioSource GetPooled3DSource()
    {
        if (_sfx3DPool.Count > 0)
        {
            return _sfx3DPool.Dequeue();
        }

        // Expand pool if necessary
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup;
        return src;
    }

    private System.Collections.IEnumerator ReturnSourceToPool(AudioSource src, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (src != null)
        {
            src.Stop();
            src.clip = null;
            _sfx3DPool.Enqueue(src);
        }
    }

    /// <summary>
    /// Play or crossfade looping ambient audio.
    /// </summary>
    public void PlayAmbient(AudioClip clip, bool loop = true, float fadeDuration = 1f)
    {
        if (clip == null || ambientSource == null) return;

        if (ambientSource.clip == clip && ambientSource.isPlaying) return;

        _ambientFadeTween?.Kill();

        if (fadeDuration > 0f && ambientSource.isPlaying)
        {
            _ambientFadeTween = ambientSource.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    ambientSource.clip = clip;
                    ambientSource.loop = loop;
                    ambientSource.Play();
                    ambientSource.DOFade(GetEffectiveVolume(SoundCategory.Ambient), fadeDuration).SetUpdate(true);
                });
        }
        else
        {
            ambientSource.clip = clip;
            ambientSource.loop = loop;
            ambientSource.volume = GetEffectiveVolume(SoundCategory.Ambient);
            ambientSource.Play();
        }
    }

    public void StopAmbient(float fadeDuration = 1f)
    {
        if (ambientSource == null || !ambientSource.isPlaying) return;

        _ambientFadeTween?.Kill();

        if (fadeDuration > 0f)
        {
            _ambientFadeTween = ambientSource.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    ambientSource.Stop();
                    ambientSource.clip = null;
                });
        }
        else
        {
            ambientSource.Stop();
            ambientSource.clip = null;
        }
    }

    #endregion
}
