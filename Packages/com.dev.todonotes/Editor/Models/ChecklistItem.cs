using System;
using UnityEngine;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Represents a sub-task or checklist item within a Task's notes/details.
    /// </summary>
    [Serializable]
    public class ChecklistItem
    {
        [SerializeField] private string m_Id;
        [SerializeField] private string m_Text;
        [SerializeField] private bool m_IsDone;

        public ChecklistItem()
        {
            m_Id = Guid.NewGuid().ToString();
            m_Text = "";
            m_IsDone = false;
        }

        public ChecklistItem(string text, bool isDone = false)
        {
            m_Id = Guid.NewGuid().ToString();
            m_Text = text ?? "";
            m_IsDone = isDone;
        }

        public string Id
        {
            get => string.IsNullOrEmpty(m_Id) ? (m_Id = Guid.NewGuid().ToString()) : m_Id;
            set => m_Id = value;
        }

        public string Text
        {
            get => m_Text ?? "";
            set => m_Text = value;
        }

        public bool IsDone
        {
            get => m_IsDone;
            set => m_IsDone = value;
        }

        public ChecklistItem Clone()
        {
            return new ChecklistItem
            {
                Id = Guid.NewGuid().ToString(),
                Text = m_Text,
                IsDone = m_IsDone
            };
        }
    }
}
