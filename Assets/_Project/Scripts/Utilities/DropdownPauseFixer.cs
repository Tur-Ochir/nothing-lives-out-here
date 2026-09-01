using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownPauseFixer : MonoBehaviour
{
    private TMP_Dropdown _dropdown;
    private Canvas _rootCanvas;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();
        if (_dropdown != null)
        {
            _dropdown.alphaFadeSpeed = 0f;
            if (_dropdown.template != null)
            {
                var cg = _dropdown.template.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    Destroy(cg);
                }
            }
        }
        CacheCanvas();
        
    }

    private void OnEnable()
    {
        if (_dropdown != null)
        {
            _dropdown.onValueChanged.AddListener(OnValueChanged);
        }
    }

    private void OnDisable()
    {
        if (_dropdown != null)
        {
            _dropdown.onValueChanged.RemoveListener(OnValueChanged);
        }
    }

    private void CacheCanvas()
    {
        if (_rootCanvas == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _rootCanvas = canvas.rootCanvas;
            }
        }
    }

    private void OnValueChanged(int _)
    {
        if (Time.timeScale == 0f)
        {
            CloseStuckDropdownList();
        }
    }

    private void LateUpdate()
    {
        if (_dropdown == null) return;

        // When paused (Time.timeScale == 0), TMP_Dropdown's internal Hide coroutine freezes
        // because it waits on scaled time (WaitForSeconds).
        Transform dropdownList = transform.Find("Dropdown List");
        if (dropdownList != null)
        {
            CacheCanvas();

            bool blockerExists = false;
            if (_rootCanvas != null)
            {
                Transform blocker = _rootCanvas.transform.Find("Blocker");
                if (blocker != null)
                {
                    blockerExists = true;
                }
            }

            if (!blockerExists && transform.parent != null)
            {
                Transform blocker = transform.parent.Find("Blocker");
                if (blocker != null)
                {
                    blockerExists = true;
                }
            }

            // If the dropdown list is active but the blocker has been destroyed,
            // TMP_Dropdown was asked to Hide() but is stuck waiting on scaled time.
            if (!blockerExists)
            {
                CloseStuckDropdownList();
            }
        }
    }

    private void CloseStuckDropdownList()
    {
        Transform dropdownList = transform.Find("Dropdown List");
        if (dropdownList != null)
        {
            Destroy(dropdownList.gameObject);
        }

        // Also check root canvas in case TMP_Dropdown parented it there
        if (_rootCanvas != null)
        {
            Transform canvasDropdownList = _rootCanvas.transform.Find("Dropdown List");
            if (canvasDropdownList != null)
            {
                Destroy(canvasDropdownList.gameObject);
            }
        }
    }
}