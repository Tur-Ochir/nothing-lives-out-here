using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the monster's circular pathing and audio cues outside the Ger.
/// </summary>
public class MonsterManager : MonoBehaviour
{
    [Header("Circle Settings")]
    public Transform centerPoint;
    public float radius = 2f;
    public float speed = 3f; 

    [Header("SFX")]
    public AudioClip[] snowWalkSFX;
    public AudioClip[] laughingSFX;

    public UnityAction OnStartWalk;

    private float angle;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (centerPoint == null) return;

        angle += speed * Time.deltaTime;

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        transform.position = centerPoint.position + new Vector3(x, 0, z);
    }

    public void PlaySnowWalkSFX()
    {
        if (audioSource == null || snowWalkSFX == null || snowWalkSFX.Length == 0) return;
        audioSource.PlayOneShot(snowWalkSFX[Random.Range(0, snowWalkSFX.Length)]);
    }

    public void PlayLaughingSFX()
    {
        if (audioSource == null || laughingSFX == null || laughingSFX.Length == 0) return;
        audioSource.PlayOneShot(laughingSFX[Random.Range(0, laughingSFX.Length)]);
    }
}
