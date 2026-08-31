using UnityEngine;

/// <summary>
/// Billboard script that makes the GameObject always face towards the active/main camera.
/// </summary>
[ExecuteAlways]
public class FaceToCamera : MonoBehaviour
{
    public enum BillboardMode
    {
        LookAtCamera,   // Rotates directly towards camera position
        CameraForward,  // Matches camera orientation (best for UI/Sprites without perspective distortion)
        YAxisOnly       // Only rotates horizontally around Y axis (good for world-space nameplates/trees)
    }

    [Header("Settings")]
    [Tooltip("Target camera to face. If null, Camera.main will be used.")]
    public Camera targetCamera;

    [Tooltip("How the object aligns to the camera.")]
    public BillboardMode mode = BillboardMode.CameraForward;

    [Tooltip("Check if the model/sprite appears backwards by default.")]
    public bool reverseDirection = false;

    private Transform camTransform;

    private void Awake()
    {
        FindCamera();
    }

    private void OnEnable()
    {
        FindCamera();
    }

    private void FindCamera()
    {
        if (targetCamera != null)
        {
            camTransform = targetCamera.transform;
        }
        else if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (camTransform == null)
        {
            FindCamera();
            if (camTransform == null) return;
        }

        switch (mode)
        {
            case BillboardMode.CameraForward:
                if (reverseDirection)
                {
                    transform.rotation = camTransform.rotation * Quaternion.Euler(0f, 180f, 0f);
                }
                else
                {
                    transform.rotation = camTransform.rotation;
                }
                break;

            case BillboardMode.LookAtCamera:
                Vector3 lookDir = reverseDirection 
                    ? transform.position - camTransform.position 
                    : camTransform.position - transform.position;

                if (lookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir, camTransform.up);
                }
                break;

            case BillboardMode.YAxisOnly:
                Vector3 yLookDir = reverseDirection 
                    ? transform.position - camTransform.position 
                    : camTransform.position - transform.position;

                yLookDir.y = 0f; // Constrain to horizontal rotation

                if (yLookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(yLookDir, Vector3.up);
                }
                break;
        }
    }
}
