using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Represents a design or architecture note document.
    /// </summary>
    [Serializable]
    public class NoteItem
    {
        [SerializeField] private string m_Id;
        [SerializeField] private string m_Title;
        [SerializeField] private string m_Content;
        [SerializeField] private string m_Category;
        [SerializeField] private NoteColorTag m_ColorTag;
        [SerializeField] private bool m_IsPinned;
        [SerializeField] private string m_CreatedDate;
        [SerializeField] private string m_LastModifiedDate;
        [SerializeField] private List<Object> m_LinkedObjects = new List<Object>();
        [SerializeField] private List<string> m_Tags = new List<string>();

        public NoteItem()
        {
            m_Id = Guid.NewGuid().ToString();
            m_Title = "New Note";
            m_Content = "";
            m_Category = "General";
            m_ColorTag = NoteColorTag.Default;
            m_IsPinned = false;
            m_CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            m_LastModifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            m_LinkedObjects = new List<Object>();
            m_Tags = new List<string>();
        }

        public NoteItem(string title, string content = "", string category = "General")
        {
            m_Id = Guid.NewGuid().ToString();
            m_Title = title;
            m_Content = content;
            m_Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
            m_ColorTag = NoteColorTag.Default;
            m_IsPinned = false;
            m_CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            m_LastModifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            m_LinkedObjects = new List<Object>();
            m_Tags = new List<string>();
        }

        public string Id
        {
            get => string.IsNullOrEmpty(m_Id) ? (m_Id = Guid.NewGuid().ToString()) : m_Id;
            set => m_Id = value;
        }

        public string Title
        {
            get => m_Title ?? "";
            set
            {
                m_Title = value;
                MarkModified();
            }
        }

        public string Content
        {
            get => m_Content ?? "";
            set
            {
                m_Content = value;
                MarkModified();
            }
        }

        public string Category
        {
            get => string.IsNullOrWhiteSpace(m_Category) ? "General" : m_Category;
            set => m_Category = string.IsNullOrWhiteSpace(value) ? "General" : value;
        }

        public NoteColorTag ColorTag
        {
            get => m_ColorTag;
            set => m_ColorTag = value;
        }

        public bool IsPinned
        {
            get => m_IsPinned;
            set => m_IsPinned = value;
        }

        public string CreatedDate
        {
            get => m_CreatedDate ?? "";
            set => m_CreatedDate = value;
        }

        public string LastModifiedDate
        {
            get => m_LastModifiedDate ?? "";
            set => m_LastModifiedDate = value;
        }

        public List<Object> LinkedObjects
        {
            get => m_LinkedObjects ?? (m_LinkedObjects = new List<Object>());
            set => m_LinkedObjects = value;
        }

        public List<string> Tags
        {
            get => m_Tags ?? (m_Tags = new List<string>());
            set => m_Tags = value;
        }

        public void MarkModified()
        {
            m_LastModifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }

        public NoteItem Clone()
        {
            return new NoteItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = m_Title + " (Copy)",
                Content = m_Content,
                Category = m_Category,
                ColorTag = m_ColorTag,
                IsPinned = false,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                LastModifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                LinkedObjects = new List<Object>(m_LinkedObjects ?? new List<Object>()),
                Tags = new List<string>(m_Tags ?? new List<string>())
            };
        }
    }
}
