using System;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

public class LinkButton : MonoBehaviour
{
    public string link;
    
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(() => Application.OpenURL(link));
    }
}
