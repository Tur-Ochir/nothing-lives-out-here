using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class Door : Interactable
{
    [Header("Animation")]
    public float rotationDuration = 1;
    public Vector3 closedRotation;
    public Vector3 openRotation;
    [Header("SFX")]
    public AudioClip openSFX;
    public AudioClip closeSFX;
    public AudioClip[] knockSFX;
    public bool isOpen;
    private AudioSource audioSource;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }

    public override void Interact()
    {
        base.Interact();
        // Debug.Log($"Haalga interacting");
        isOpen = !isOpen;
        HandleRotate(isOpen);
        PlaySFX(isOpen);
    }

    private void HandleRotate(bool open)
    {
        Vector3 targetRotation = open ? openRotation : closedRotation;
        transform.DORotate(targetRotation, rotationDuration);
        // transform
    }

    private void PlaySFX(bool open)
    {
        if (audioSource == null) return;
        
        var target = open ? openSFX : closeSFX;
        audioSource.PlayOneShot(target);
    }

    public void Knock()
    {
        StartCoroutine(StartKnockingSFX());
    }
    private IEnumerator StartKnockingSFX()
    {
        for (int i = 0; i < 4; i++)
        {
            PlayKnockSFX();
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 1f));
        }
    }

    private void PlayKnockSFX()
    {
        audioSource.PlayOneShot(knockSFX[UnityEngine.Random.Range(0, knockSFX.Length)]);
    }

    public void TryOpenAnimation()
    {
        var sequence = DOTween.Sequence();

        for (int i = 0; i < 8; i++)
        {
            float target = Random.Range(1f, 5f);
            if (i  % 2 == 1)
            {
                target = 0;
            }
            float dur = Random.Range(0.1f, 0.25f);
            sequence.Append(transform.DORotate(new Vector3(openRotation.x, target, openRotation.z), dur));
        }
    }
}
