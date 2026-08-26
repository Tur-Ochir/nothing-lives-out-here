using System;
using UnityEngine;

namespace Dev.TodoNotes
{
    /// <summary>
    /// Enum representing the priority of a task or scene marker.
    /// </summary>
    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }

    /// <summary>
    /// In-scene sticky note and task pin component.
    /// Place on GameObjects to attach visual 3D tasks & reminders directly in your Unity scenes.
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Todo & Notes/Scene Task Marker")]
    [DisallowMultipleComponent]
    public class SceneTaskMarker : MonoBehaviour
    {
        [Header("Task Info")]
        [SerializeField] private string m_Title = "New Scene Task";
        [SerializeField, TextArea(2, 6)] private string m_Description = "";
        [SerializeField] private TaskPriority m_Priority = TaskPriority.Medium;
        [SerializeField] private bool m_IsCompleted = false;
        [SerializeField] private string m_Category = "Level Design";

        [Header("Visualization")]
        [SerializeField] private bool m_ShowInScene = true;
        [SerializeField] private Color m_CustomColor = new Color(0.2f, 0.7f, 1f, 1f);
        [SerializeField] private float m_IconScale = 1.0f;

        public string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        public string Description
        {
            get => m_Description;
            set => m_Description = value;
        }

        public TaskPriority Priority
        {
            get => m_Priority;
            set => m_Priority = value;
        }

        public bool IsCompleted
        {
            get => m_IsCompleted;
            set => m_IsCompleted = value;
        }

        public string Category
        {
            get => m_Category;
            set => m_Category = value;
        }

        public bool ShowInScene
        {
            get => m_ShowInScene;
            set => m_ShowInScene = value;
        }

        public Color CustomColor
        {
            get => m_CustomColor;
            set => m_CustomColor = value;
        }

        public float IconScale
        {
            get => m_IconScale;
            set => m_IconScale = Mathf.Clamp(value, 0.2f, 5f);
        }

        public Color GetPriorityColor()
        {
            if (m_IsCompleted)
                return new Color(0.4f, 0.75f, 0.4f, 0.9f); // Soft green

            return m_Priority switch
            {
                TaskPriority.Urgent => new Color(0.95f, 0.26f, 0.21f, 1f), // Red
                TaskPriority.High => new Color(1f, 0.6f, 0.0f, 1f),       // Orange
                TaskPriority.Medium => new Color(0.2f, 0.65f, 1f, 1f),     // Blue/Cyan
                TaskPriority.Low => new Color(0.55f, 0.6f, 0.65f, 1f),    // Grey
                _ => m_CustomColor
            };
        }

        private void Reset()
        {
            m_Title = gameObject.name + " Note";
            m_Category = "Scene";
        }
    }
}
