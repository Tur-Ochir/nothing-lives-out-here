using System;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class Puzzle : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Puzzle Settings")]
    public CinemachineCamera focusCamera;
    public PuzzlePiece[] pieces;
    
    [Header("Input Actions")]
    public InputActionProperty moveAction;
    public InputActionProperty scrollAction;
    public InputActionProperty selectAction;
    public InputActionProperty exitAction;

    [Header("Parameters")]
    public float scrollSensitivity = 0.01f;

    [Header("Raycast Settings")]
    public LayerMask pieceLayer;

    [Header("Interactable")]
    public bool canInteract = true;
    public string reasonNotInteract;
    [HideInInspector] public Outline outline;

    public event Action OnInteracted;

    public bool CanInteract => canInteract;
    public string ReasonCannotInteract => reasonNotInteract;

    private bool isFocused = false;
    private PuzzlePiece selectedPiece;
    private Camera mainCam;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        mainCam = Camera.main;
    }

    private void OnEnable()
    {
        moveAction.action?.Enable();
        scrollAction.action?.Enable();
        selectAction.action?.Enable();
        exitAction.action?.Enable();
    }

    public void Interact()
    {
        if (!CanInteract) return;

        isFocused = !isFocused;

        if (isFocused) EnterFocus();
        else ExitFocus();

        OnInteracted?.Invoke();
    }

    private void EnterFocus()
    {
        if (focusCamera != null) focusCamera.gameObject.SetActive(true);
        
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.canMove = false;
            PlayerManager.Instance.canCrouch = false;
            PlayerManager.Instance.DisableCam();
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ExitFocus()
    {
        if (focusCamera != null) focusCamera.gameObject.SetActive(false);
        
        if (selectedPiece != null)
        {
            selectedPiece.SetSelected(false);
            selectedPiece.StopMoving();
            selectedPiece = null;
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.canMove = true;
            PlayerManager.Instance.canCrouch = true;
            PlayerManager.Instance.EnableCam();
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!isFocused) return;

        if (exitAction.action != null && exitAction.action.WasPressedThisFrame())
        {
            isFocused = false;
            ExitFocus();
            return;
        }

        if (selectAction.action != null && selectAction.action.WasPressedThisFrame())
        {
            HandleSelection();
        }

        if (selectedPiece != null)
        {
            Vector2 moveInput = moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            Vector2 scrollDelta = scrollAction.action != null ? scrollAction.action.ReadValue<Vector2>() : Vector2.zero;

            Vector3 combinedInput = new Vector3(moveInput.y, scrollDelta.y * scrollSensitivity, moveInput.x);

            if (combinedInput.magnitude > 0.01f)
            {
                Transform refTransform = focusCamera != null ? focusCamera.transform : transform;
                selectedPiece.Move(combinedInput, refTransform);
            }
            else
            {
                selectedPiece.StopMoving();
            }
        }
    }

    private void HandleSelection()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || Mouse.current == null) return;

        Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, pieceLayer))
        {
            if (hit.transform.TryGetComponent(out PuzzlePiece piece))
            {
                if (selectedPiece != null) selectedPiece.SetSelected(false);
                selectedPiece = piece;  
                selectedPiece.SetSelected(true);
            }
        }
        else
        {
            if (selectedPiece != null)
            {
                selectedPiece.SetSelected(false);
                selectedPiece.StopMoving();
                selectedPiece = null;
            }
        }
    }

    public void SetHighlight(bool active)
    {
        if (outline != null) outline.enabled = active;
    }
}