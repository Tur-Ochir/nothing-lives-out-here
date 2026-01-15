using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class Puzzle : Interactable
{
    [Header("Puzzle Settings")]
    public CinemachineCamera focusCamera;
    public PuzzlePiece[] pieces;
    
    [Header("Input Actions")]
    public InputActionProperty moveAction;
    public InputActionProperty selectAction;
    public InputActionProperty exitAction;

    [Header("Raycast Settings")]
    public LayerMask pieceLayer;

    private bool isFocused = false;
    private PuzzlePiece selectedPiece;
    private Camera mainCam;

    protected override void Awake()
    {
        base.Awake();
        mainCam = Camera.main;
    }

    private void OnEnable()
    {
        moveAction.action?.Enable();
        selectAction.action?.Enable();
        exitAction.action?.Enable();
    }

    public override void Interact()
    {
        if (!canInteract) return;

        isFocused = !isFocused;

        if (isFocused) EnterFocus();
        else ExitFocus();
    }

    private void EnterFocus()
    {
        if (focusCamera != null) focusCamera.gameObject.SetActive(true);
        
        // Disable player controls
        PlayerManager.Instance.canMove = false;
        PlayerManager.Instance.canCrouch = false;
        PlayerManager.Instance.DisableCam();

        // Unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ExitFocus()
    {
        if (focusCamera != null) focusCamera.gameObject.SetActive(false);
        
        // Deselect piece
        if (selectedPiece != null)
        {
            selectedPiece.SetSelected(false);
            selectedPiece.StopMoving();
            selectedPiece = null;
        }

        // Enable player controls
        PlayerManager.Instance.canMove = true;
        PlayerManager.Instance.canCrouch = true;
        PlayerManager.Instance.EnableCam();

        // Lock cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!isFocused) return;

        // Exit handling
        if (exitAction.action != null && exitAction.action.WasPressedThisFrame())
        {
            isFocused = false;
            ExitFocus();
            return;
        }

        // Piece Selection
        if (selectAction.action != null && selectAction.action.WasPressedThisFrame())
        {
            HandleSelection();
        }

        // Piece Movement
        if (selectedPiece != null && moveAction.action != null)
        {
            Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
            if (moveInput.magnitude > 0.1f)
            {
                selectedPiece.Move(moveInput, focusCamera.transform);
            }
            else
            {
                selectedPiece.StopMoving();
            }
        }
    }

    private void HandleSelection()
    {
        Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, pieceLayer))
        {
            if (hit.transform.TryGetComponent(out PuzzlePiece piece))
            {
                // Deselect current
                if (selectedPiece != null) selectedPiece.SetSelected(false);

                // Select new
                selectedPiece = piece;
                selectedPiece.SetSelected(true);
            }
        }
        else
        {
            // Deselect if clicking on empty space
            if (selectedPiece != null)
            {
                selectedPiece.SetSelected(false);
                selectedPiece.StopMoving();
                selectedPiece = null;
            }
        }
    }
}