using UnityEngine;

/// <summary>
/// Constrains this object's X/Z position to follow a target transform while keeping its own Y altitude.
/// </summary>
public class LosPosition : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    private void Update()
    {
        if (target == null) return;
        transform.position = new Vector3(target.position.x, transform.position.y, target.position.z);
    }
}
