using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Connects microphone sound detection with monster AI and horror events.
/// Suspicion increases when player speaks/makes noise.
/// Loud screams or sustained noise trigger monster approach, snow footsteps, light flickers, and door attacks.
/// </summary>
public class MonsterSoundReactor : MonoBehaviour
{
    public static MonsterSoundReactor Instance { get; private set; }

    [Header("Activation")]
    [Tooltip("If true, reactions only occur when GameManager.isNight is active.")]
    public bool onlyReactAtNight = false;
    [Tooltip("Global multiplier for suspicion increase rate.")]
    public float suspicionSensitivity = 1.0f;

    [Header("Suspicion Meter")]
    [Range(0f, 100f)]
    public float currentSuspicion = 0f;
    public float maxSuspicion = 100f;
    [Tooltip("Rate at which suspicion increases per second when speaking (scaled by volume).")]
    public float suspicionBuildSpeed = 40f;
    [Tooltip("Rate at which suspicion decreases per second during total silence.")]
    public float suspicionDecaySpeed = 10f;

    [Header("Reaction Thresholds")]
    [Tooltip("Suspicion level that triggers monster approaching / snow walking SFX.")]
    public float alertSuspicionThreshold = 45f;
    [Tooltip("Suspicion level that triggers light flickering and closer circling.")]
    public float highSuspicionThreshold = 75f;

    [Header("Monster Behavior Modifiers")]
    public float baseMonsterRadius = 15f;
    public float alertedMonsterRadius = 7f;
    public float aggressiveMonsterRadius = 4f;
    public float monsterApproachSpeed = 2.5f;

    [Header("Cooldowns")]
    public float soundReactionInterval = 2.5f;
    public float loudSpikeCooldown = 4.0f;

    [Header("Events")]
    public UnityEvent<float> OnSuspicionChanged;
    public UnityEvent OnMonsterAlerted;
    public UnityEvent OnMonsterDoorAttack;

    private float nextReactionTime = 0f;
    private float nextLoudReactionTime = 0f;
    private MonsterManager monsterManager;
    private GameManager gameManager;
    private float initialMonsterRadius;
    private float targetRadius;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.monsterManager != null)
        {
            monsterManager = gameManager.monsterManager;
            initialMonsterRadius = monsterManager.radius;
            targetRadius = initialMonsterRadius;
        }

        if (MicSoundDetector.Instance != null)
        {
            MicSoundDetector.Instance.OnLoudNoiseSpike += OnLoudNoiseDetected;
        }
    }

    private void OnDestroy()
    {
        if (MicSoundDetector.Instance != null)
        {
            MicSoundDetector.Instance.OnLoudNoiseSpike -= OnLoudNoiseDetected;
        }
    }

    private void Update()
    {
        if (MicSoundDetector.Instance == null) return;

        bool canReact = !onlyReactAtNight || (gameManager != null && gameManager.isNight);
        float volume = MicSoundDetector.Instance.CurrentVolume;
        bool isSoundDetected = MicSoundDetector.Instance.IsSoundDetected;

        if (canReact && isSoundDetected)
        {
            float gain = volume * suspicionBuildSpeed * suspicionSensitivity * Time.deltaTime;
            currentSuspicion = Mathf.Min(maxSuspicion, currentSuspicion + gain);
        }
        else
        {
            currentSuspicion = Mathf.Max(0f, currentSuspicion - (suspicionDecaySpeed * Time.deltaTime));
        }

        OnSuspicionChanged?.Invoke(currentSuspicion);

        if (canReact)
        {
            HandleSuspicionBehavior();
        }

        if (monsterManager != null)
        {
            monsterManager.radius = Mathf.Lerp(monsterManager.radius, targetRadius, Time.deltaTime * monsterApproachSpeed);
        }
    }

    private void HandleSuspicionBehavior()
    {
        if (Time.time < nextReactionTime) return;

        if (currentSuspicion >= highSuspicionThreshold)
        {
            TriggerHighAlertReaction();
            nextReactionTime = Time.time + soundReactionInterval;
        }
        else if (currentSuspicion >= alertSuspicionThreshold)
        {
            TriggerMediumAlertReaction();
            nextReactionTime = Time.time + (soundReactionInterval * 1.5f);
        }
        else
        {
            targetRadius = initialMonsterRadius;
        }
    }

    private void TriggerMediumAlertReaction()
    {
        targetRadius = alertedMonsterRadius;

        if (monsterManager != null)
        {
            monsterManager.PlaySnowWalkSFX();
        }

        OnMonsterAlerted?.Invoke();
        Debug.Log("[MonsterSoundReactor] Monster heard a sound and is stalking closer outside...");
    }

    private void TriggerHighAlertReaction()
    {
        targetRadius = aggressiveMonsterRadius;

        if (monsterManager != null)
        {
            monsterManager.PlaySnowWalkSFX();

            if (gameManager != null && gameManager.gerLight != null)
            {
                gameManager.gerLight.SetActivate(false);
                StartCoroutine(gameManager.gerLight.DelayedSetActive(Random.Range(1.0f, 2.5f), true));
            }

            if (Random.value < 0.4f)
            {
                monsterManager.PlayLaughingSFX();
            }
        }

        OnMonsterAlerted?.Invoke();
        Debug.Log("[MonsterSoundReactor] Monster is aggressively circling right outside the Ger!");
    }

    private void OnLoudNoiseDetected(float volume)
    {
        bool canReact = !onlyReactAtNight || (gameManager != null && gameManager.isNight);
        if (!canReact) return;

        if (Time.time < nextLoudReactionTime) return;
        nextLoudReactionTime = Time.time + loudSpikeCooldown;

        currentSuspicion = Mathf.Min(maxSuspicion, currentSuspicion + 45f);
        StartCoroutine(ExecuteDoorAttackSequence());
    }

    private IEnumerator ExecuteDoorAttackSequence()
    {
        Debug.Log("<color=red>[MonsterSoundReactor] LOUD NOISE SPIKE DETECTED! Monster attacking Ger!</color>");
        OnMonsterDoorAttack?.Invoke();

        targetRadius = aggressiveMonsterRadius;

        if (monsterManager != null)
        {
            monsterManager.PlayLaughingSFX();
        }

        yield return new WaitForSeconds(0.4f);

        if (gameManager != null && gameManager.gerDoor != null)
        {
            gameManager.gerDoor.Knock();
        }

        if (gameManager != null && gameManager.gerLight != null)
        {
            gameManager.gerLight.SetActivate(false);
            yield return new WaitForSeconds(1.2f);
            gameManager.gerLight.SetActivate(true);
        }

        yield return new WaitForSeconds(0.8f);

        if (gameManager != null && gameManager.gerDoor != null)
        {
            gameManager.gerDoor.TryOpenAnimation();
        }
    }

    public void ResetSuspicion()
    {
        currentSuspicion = 0f;
        targetRadius = initialMonsterRadius;
    }
}
