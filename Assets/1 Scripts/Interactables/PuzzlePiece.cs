using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public Rigidbody rb;
    private Outline outline;
    
    [Header("Piece Settings")]
    public float moveSpeed = 5f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }
    
    public void SetSelected(bool isSelected)
    {
        if (outline != null) outline.enabled = isSelected;
        rb.isKinematic = !isSelected;
    }

    public void Move(Vector2 direction, Transform relativeTo)
    {
        if (rb == null) return;

        // Move relative to the camera/focus point
        Vector3 moveDir = relativeTo.right * direction.x + relativeTo.up * direction.y;
        rb.linearVelocity = new Vector3(direction.y, 0 , direction.x) * moveSpeed;
    }

    public void StopMoving()
    {
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}
