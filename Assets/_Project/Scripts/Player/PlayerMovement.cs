using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Handles Rigidbody-based character movement, slope projection, crouching, and camera headbobbing.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public bool canMove = true;
    public bool canCrouch = true;
    public float speed = 5f;
    public float crouchSpeedMultiplier = 0.5f;
    public Vector3 standingOffset;
    public Vector3 crouchingOffset;

    [Header("Rigidbody Physics")]
    public float mass = 70f;
    public float acceleration = 60f;
    public float deceleration = 50f;
    public float airAcceleration = 15f;
    public float maxSlopeAngle = 45f;
    public float groundStickForce = 6f;
    public float gravityMultiplier = 1.2f;
    public LayerMask groundLayers = ~0;

    [Header("Step Climbing")]
    public bool enableStepClimb = true;
    public float stepOffset = 0.3f;
    public float stepSmooth = 3f;

    [Header("Collider Settings")]
    public float standingHeight = 1.8f;
    public float crouchHeight = 1.0f;
    public float colliderRadius = 0.3f;
    public Vector3 standingCenter = Vector3.zero;
    public Vector3 crouchingCenter = new Vector3(0f, -0.4f, 0f);

    [Header("Camera & Headbob")]
    public Transform camTarget;
    public CinemachineVirtualCameraBase vcam;
    public CinemachineBasicMultiChannelPerlin camNoise;
    public bool useHeadBob = true;
    public float bobTransSpeed = 5f;
    public float walkingBobAmplitude = 2f;
    public float walkingBobFrequency = 0.02f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private CharacterController legacyController;
    private Transform camTransform;

    private bool isCrouching;
    private bool isGrounded;
    private Vector3 moveDirection;
    private Vector2 lastInput;
    private Vector3 groundNormal = Vector3.up;
    private float baseSpeed;

    public Rigidbody Rigidbody => rb;
    public CapsuleCollider CapsuleCollider => capsuleCollider;
    public bool IsCrouching => isCrouching;
    public bool IsGrounded => isGrounded;
    public Vector3 MoveDirection => moveDirection;
    public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
    public float CurrentSpeed => isCrouching ? baseSpeed * crouchSpeedMultiplier : baseSpeed;

    private void Awake()
    {
        baseSpeed = speed;
        camTransform = Camera.main != null ? Camera.main.transform : null;

        // 1. Setup Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.mass = mass;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = true;
        rb.isKinematic = false;

        // 2. Setup CapsuleCollider
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        }
        capsuleCollider.height = standingHeight;
        capsuleCollider.radius = colliderRadius;
        capsuleCollider.center = standingCenter;

        // Zero-friction material prevents sticking to walls and geometry
        PhysicsMaterial frictionlessMat = new PhysicsMaterial("PlayerFrictionlessMaterial")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        capsuleCollider.material = frictionlessMat;

        // 3. Disable legacy CharacterController if attached to prevent physics conflicts
        legacyController = GetComponent<CharacterController>();
        if (legacyController != null && legacyController.enabled)
        {
            legacyController.enabled = false;
        }
    }

    public void ProcessMove(Vector2 input)
    {
        lastInput = input;

        if (camTransform == null && Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        if (!canMove || camTransform == null || input.sqrMagnitude <= 0.001f)
        {
            moveDirection = Vector3.zero;
            return;
        }

        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        moveDirection = (right * input.x + forward * input.y).normalized;
    }

    private void FixedUpdate()
    {
        if (rb == null || rb.isKinematic) return;

        isGrounded = PerformGroundCheck(out RaycastHit groundHit);
        groundNormal = isGrounded ? groundHit.normal : Vector3.up;

        float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
        bool isWalkableSlope = isGrounded && slopeAngle <= maxSlopeAngle;
        float targetSpeed = CurrentSpeed;

        if (canMove)
        {
            if (isWalkableSlope)
            {
                if (lastInput.sqrMagnitude < 0.001f)
                {
                    // Decelerate and hold position on slopes without sliding down
                    Vector3 currentVel = rb.linearVelocity;
                    rb.linearVelocity = Vector3.MoveTowards(currentVel, Vector3.zero, deceleration * Time.fixedDeltaTime);
                    if (rb.linearVelocity.sqrMagnitude < 0.01f)
                    {
                        rb.linearVelocity = Vector3.zero;
                    }
                    rb.useGravity = false;
                }
                else
                {
                    rb.useGravity = true;
                    Vector3 slopeMoveDir = Vector3.ProjectOnPlane(moveDirection, groundNormal).normalized;
                    Vector3 targetVelocity = slopeMoveDir * (targetSpeed * Mathf.Clamp01(lastInput.magnitude));
                    Vector3 newVel = Vector3.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

                    // Downward stick force to keep grounded on minor slopes and steps
                    if (newVel.y <= 0.05f)
                    {
                        newVel.y -= groundStickForce * Time.fixedDeltaTime;
                    }
                    rb.linearVelocity = newVel;

                    if (enableStepClimb)
                    {
                        HandleStepClimb();
                    }
                }
            }
            else
            {
                // Airborne or unwalkable steep slope
                rb.useGravity = true;

                if (isGrounded)
                {
                    // Slide down steep slope
                    Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                    rb.AddForce(slideDir * (Physics.gravity.magnitude * 0.5f), ForceMode.Acceleration);
                }
                else
                {
                    // Air control
                    Vector3 currentH = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                    Vector3 targetH = new Vector3(moveDirection.x, 0f, moveDirection.z) * targetSpeed;
                    Vector3 newH = Vector3.MoveTowards(currentH, targetH, airAcceleration * Time.fixedDeltaTime);

                    float yVel = rb.linearVelocity.y;
                    if (gravityMultiplier != 1f)
                    {
                        yVel += Physics.gravity.y * (gravityMultiplier - 1f) * Time.fixedDeltaTime;
                    }
                    rb.linearVelocity = new Vector3(newH.x, yVel, newH.z);
                }
            }
        }
        else
        {
            // Decelerate horizontal velocity when movement is disabled
            Vector3 currentVel = rb.linearVelocity;
            rb.linearVelocity = Vector3.MoveTowards(currentVel, new Vector3(0f, currentVel.y, 0f), deceleration * Time.fixedDeltaTime);
            moveDirection = Vector3.zero;
            lastInput = Vector2.zero;
        }
    }

    private bool PerformGroundCheck(out RaycastHit groundHit)
    {
        groundHit = default;
        if (capsuleCollider == null) return false;

        Vector3 centerWorld = transform.TransformPoint(capsuleCollider.center);
        float halfHeight = capsuleCollider.height * 0.5f;
        Vector3 bottomSphereCenter = centerWorld + Vector3.down * (halfHeight - capsuleCollider.radius);

        float checkRadius = capsuleCollider.radius * 0.85f;
        float castDistance = 0.25f;

        RaycastHit[] hits = Physics.SphereCastAll(bottomSphereCenter, checkRadius, Vector3.down, castDistance, groundLayers, QueryTriggerInteraction.Ignore);
        float closestDist = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.gameObject == gameObject || hits[i].collider.transform.IsChildOf(transform))
                continue;

            if (hits[i].distance < closestDist)
            {
                closestDist = hits[i].distance;
                groundHit = hits[i];
                found = true;
            }
        }

        return found;
    }

    private void HandleStepClimb()
    {
        if (!isGrounded || moveDirection.sqrMagnitude < 0.01f || capsuleCollider == null) return;

        Vector3 centerWorld = transform.TransformPoint(capsuleCollider.center);
        Vector3 feetPos = centerWorld + Vector3.down * (capsuleCollider.height * 0.5f);
        Vector3 lowerRayOrigin = feetPos + Vector3.up * 0.05f;
        Vector3 upperRayOrigin = feetPos + Vector3.up * stepOffset;

        if (Physics.Raycast(lowerRayOrigin, moveDirection, out RaycastHit lowerHit, capsuleCollider.radius + 0.15f, groundLayers, QueryTriggerInteraction.Ignore))
        {
            if (lowerHit.collider.gameObject != gameObject && !lowerHit.collider.transform.IsChildOf(transform))
            {
                if (Vector3.Angle(Vector3.up, lowerHit.normal) > maxSlopeAngle)
                {
                    if (!Physics.Raycast(upperRayOrigin, moveDirection, out RaycastHit upperHit, capsuleCollider.radius + 0.25f, groundLayers, QueryTriggerInteraction.Ignore))
                    {
                        rb.position += Vector3.up * (stepSmooth * Time.fixedDeltaTime);
                    }
                }
            }
        }
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

        float horizontalSpeed = rb != null ? new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude : moveDirection.magnitude;

        if (horizontalSpeed > 0.1f && isGrounded)
        {
            camNoise.AmplitudeGain = Mathf.Lerp(camNoise.AmplitudeGain, walkingBobAmplitude, Time.deltaTime * bobTransSpeed);
            camNoise.FrequencyGain = Mathf.Lerp(camNoise.FrequencyGain, walkingBobFrequency, Time.deltaTime * bobTransSpeed);
        }
        else
        {
            camNoise.AmplitudeGain = Mathf.Lerp(camNoise.AmplitudeGain, 0f, Time.deltaTime * bobTransSpeed);
            camNoise.FrequencyGain = Mathf.Lerp(camNoise.FrequencyGain, 0f, Time.deltaTime * bobTransSpeed);
        }
    }

    public void ToggleCrouch()
    {
        if (!canCrouch || camTarget == null) return;

        if (isCrouching)
        {
            // Trying to stand up: verify ceiling clearance
            if (!CanStandUp()) return;
        }

        isCrouching = !isCrouching;

        Vector3 target = isCrouching ? crouchingOffset : standingOffset;
        camTarget.DOLocalMove(target, 0.3f);

        if (capsuleCollider != null)
        {
            capsuleCollider.height = isCrouching ? crouchHeight : standingHeight;
            capsuleCollider.center = isCrouching ? crouchingCenter : standingCenter;
        }

        speed = CurrentSpeed;
    }

    private bool CanStandUp()
    {
        if (capsuleCollider == null) return true;

        Vector3 bottom = transform.position + standingCenter + Vector3.down * (standingHeight * 0.5f - colliderRadius);
        Vector3 top = transform.position + standingCenter + Vector3.up * (standingHeight * 0.5f - colliderRadius);

        Collider[] colliders = Physics.OverlapCapsule(bottom, top, colliderRadius * 0.9f, groundLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject == gameObject || colliders[i].transform.IsChildOf(transform))
                continue;
            return false;
        }
        return true;
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
        }
        transform.position = position;
        transform.rotation = rotation;
    }

    public void SetKinematic(bool kinematic)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = kinematic;
            rb.useGravity = !kinematic;
        }

        if (capsuleCollider != null)
        {
            capsuleCollider.enabled = !kinematic;
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
