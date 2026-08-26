using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Represents an individual task item in the To-Do list.
    /// </summary>
    [Serializable]
    public class TaskItem
    {
        [SerializeField] private string m_Id;
        [SerializeField] private string m_Title;
        [SerializeField] private string m_Description;
        [SerializeField] private TaskPriority m_Priority;
        [SerializeField] private TaskStatus m_Status;
        [SerializeField] private string m_Category;
        [SerializeField] private Object m_LinkedObject;
        [SerializeField] private string m_CreatedDate;
        [SerializeField] private string m_CompletedDate;
        [SerializeField] private string m_DueDate;
        [SerializeField] private List<string> m_Tags = new List<string>();
        [SerializeField] private bool m_IsExpanded;

        public TaskItem()
        {
            m_Id = Guid.NewGuid().ToString();
            m_Title = "New Task";
            m_Description = "";
            m_Priority = TaskPriority.Medium;
            m_Status = TaskStatus.ToDo;
            m_Category = "General";
            m_CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            m_CompletedDate = "";
            m_DueDate = "";
            m_Tags = new List<string>();
            m_IsExpanded = false;
        }

        public TaskItem(string title, TaskPriority priority = TaskPriority.Medium, string category = "General")
        {
            m_Id = Guid.NewGuid().ToString();
            m_Title = title;
            m_Description = "";
            m_Priority = priority;
            m_Status = TaskStatus.ToDo;
            m_Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
            m_CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            m_CompletedDate = "";
            m_DueDate = "";
            m_Tags = new List<string>();
            m_IsExpanded = false;
        }

        public string Id
        {
            get => string.IsNullOrEmpty(m_Id) ? (m_Id = Guid.NewGuid().ToString()) : m_Id;
            set => m_Id = value;
        }

        public string Title
        {
            get => m_Title ?? "";
            set => m_Title = value;
        }

        public string Description
        {
            get => m_Description ?? "";
            set => m_Description = value;
        }

        public TaskPriority Priority
        {
            get => m_Priority;
            set => m_Priority = value;
        }

        public TaskStatus Status
        {
            get => m_Status;
            set
            {
                m_Status = value;
                if (m_Status == TaskStatus.Done && string.IsNullOrEmpty(m_CompletedDate))
                {
                    m_CompletedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                }
                else if (m_Status != TaskStatus.Done)
                {
                    m_CompletedDate = "";
                }
            }
        }

        public bool IsDone
        {
            get => m_Status == TaskStatus.Done;
            set => Status = value ? TaskStatus.Done : TaskStatus.ToDo;
        }

        public string Category
        {
            get => string.IsNullOrWhiteSpace(m_Category) ? "General" : m_Category;
            set => m_Category = string.IsNullOrWhiteSpace(value) ? "General" : value;
        }

        public Object LinkedObject
        {
            get => m_LinkedObject;
            set => m_LinkedObject = value;
        }

        public string CreatedDate
        {
            get => m_CreatedDate ?? "";
            set => m_CreatedDate = value;
        }

        public string CompletedDate
        {
            get => m_CompletedDate ?? "";
            set => m_CompletedDate = value;
        }

        public string DueDate
        {
            get => m_DueDate ?? "";
            set => m_DueDate = value;
        }

        public List<string> Tags
        {
            get => m_Tags ?? (m_Tags = new List<string>());
            set => m_Tags = value;
        }

        public bool IsExpanded
        {
            get => m_IsExpanded;
            set => m_IsExpanded = value;
        }

        public TaskItem Clone()
        {
            return new TaskItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = m_Title + " (Copy)",
                Description = m_Description,
                Priority = m_Priority,
                Status = TaskStatus.ToDo,
                Category = m_Category,
                LinkedObject = m_LinkedObject,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                CompletedDate = "",
                DueDate = m_DueDate,
                Tags = new List<string>(m_Tags ?? new List<string>()),
                IsExpanded = m_IsExpanded
            };
        }
    }
}
