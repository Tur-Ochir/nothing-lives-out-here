using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class MonsterManager : MonoBehaviour
{
    [Header("Circle Settings")]
    public Transform centerPoint;   // Point to rotate around
    public float radius = 2f;        // Circle radius
    public float speed = 3f; 
    private float angle;
    [Header("SFX")]
    public AudioClip[] snowWalkSFX;
    public AudioClip[] laughingSFX;
    private AudioSource audioSource;
    public UnityAction OnStartWalk;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // StartCoroutine(StartRandomWalk());
    }

    private void Update()
    {
        angle += speed * Time.deltaTime;

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        transform.position = centerPoint.position + new Vector3(x, 0, z);
    }

    private IEnumerator StartRandomWalk()
    {
            OnStartWalk?.Invoke();
            for (int i = 0; i < 4; i++)
            {
                PlaySnowWalkSFX();
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));
            }
            yield return new WaitForSeconds(UnityEngine.Random.Range(7f, 14f));
            StartCoroutine(StartRandomWalk());
    }

    public void PlaySnowWalkSFX()
    {
        audioSource.PlayOneShot(snowWalkSFX[UnityEngine.Random.Range(0, snowWalkSFX.Length)]);
    }
    public void PlayLaughingSFX()
    {
        audioSource.PlayOneShot(laughingSFX[UnityEngine.Random.Range(0, laughingSFX.Length)]);
    }
    
}
