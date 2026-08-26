using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Piece Settings")]
    public Rigidbody rb;
    public float moveSpeed = 5f;

    private Outline outline;
    
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }
    
    public void SetSelected(bool isSelected)
    {
        if (outline != null) outline.enabled = isSelected;
        if (rb != null) rb.isKinematic = !isSelected;
    }

    public void Move(Vector3 direction, Transform relativeTo)
    {
        if (rb == null) return;
        rb.linearVelocity = direction * moveSpeed;
    }

    public void StopMoving()
    {
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}
