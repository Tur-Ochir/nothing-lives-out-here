using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Handles character movement, crouching, and camera headbobbing.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public bool canMove = true;
    public bool canCrouch = true;
    public float speed = 5f;
    public float crouchSpeedMultiplier = 0.5f;
    public Vector3 standingOffset;
    public Vector3 crouchingOffset;

    [Header("Camera & Headbob")]
    public Transform camTarget;
    public CinemachineVirtualCameraBase vcam;
    public CinemachineBasicMultiChannelPerlin camNoise;
    public bool useHeadBob = true;
    public float bobTransSpeed = 5f;
    public float walkingBobAmplitude = 2f;
    public float walkingBobFrequency = 0.02f;

    private CharacterController controller;
    private Transform camTransform;
    private bool isCrouching;
    private Vector3 moveDirection;

    public bool IsCrouching => isCrouching;
    public Vector3 MoveDirection => moveDirection;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main != null ? Camera.main.transform : null;
    }

    public void ProcessMove(Vector2 input)
    {
        if (!canMove || controller == null || camTransform == null)
        {
            moveDirection = Vector3.zero;
            return;
        }

        moveDirection = camTransform.right * input.x + camTransform.forward * input.y;
        controller.SimpleMove(moveDirection * speed);
    }

    public void ProcessHeadBob()
    {
        if (camNoise == null) return;

        if (!useHeadBob)
        {
            camNoise.AmplitudeGain = 0;
            camNoise.FrequencyGain = 0;
            return;
        }

        if (moveDirection.magnitude > 0.1f)
        {
            camNoise.AmplitudeGain = Mathf.Lerp(camNoise.AmplitudeGain, walkingBobAmplitude, Time.deltaTime * bobTransSpeed);
            camNoise.FrequencyGain = Mathf.Lerp(camNoise.FrequencyGain, walkingBobFrequency, Time.deltaTime * bobTransSpeed);
        }
        else
        {
            camNoise.AmplitudeGain = Mathf.Lerp(camNoise.AmplitudeGain, 0, Time.deltaTime * bobTransSpeed);
            camNoise.FrequencyGain = Mathf.Lerp(camNoise.FrequencyGain, 0, Time.deltaTime * bobTransSpeed);
        }
    }

    public void ToggleCrouch()
    {
        if (!canCrouch || camTarget == null) return;

        isCrouching = !isCrouching;

        Vector3 target = isCrouching ? crouchingOffset : standingOffset;
        camTarget.DOLocalMove(target, 0.3f);

        if (isCrouching)
        {
            speed *= crouchSpeedMultiplier;
        }
        else
        {
            speed /= crouchSpeedMultiplier;
        }
    }

    public void SetCamControllerActive(bool active)
    {
        if (vcam == null) return;
        var axisController = vcam.GetComponent<CinemachineInputAxisController>();
        if (axisController != null)
        {
            axisController.enabled = active;
        }
    }
}
