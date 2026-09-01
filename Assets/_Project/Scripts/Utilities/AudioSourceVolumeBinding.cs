using UnityEngine;

/// <summary>
/// Automatically binds any GameObject's AudioSource to the SoundManager's volume channels (Master, SFX, Ambient).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioSourceVolumeBinding : MonoBehaviour
{
    [Tooltip("The audio channel this AudioSource belongs to.")]
    public SoundManager.SoundCategory category = SoundManager.SoundCategory.SFX;

    [Range(0f, 1f)]
    [Tooltip("The default volume multiplier for this specific AudioSource.")]
    public float baseVolume = 1f;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource != null && baseVolume == 1f)
        {
            baseVolume = _audioSource.volume;
        }
    }

    private void Start()
    {
        Register();
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        if (SoundManager.Instance != null && _audioSource != null)
        {
            SoundManager.Instance.UnregisterAudioSource(_audioSource);
        }
    }

    private void Register()
    {
        if (SoundManager.Instance != null && _audioSource != null)
        {
            SoundManager.Instance.RegisterAudioSource(_audioSource, category, baseVolume);
        }
    }
}
