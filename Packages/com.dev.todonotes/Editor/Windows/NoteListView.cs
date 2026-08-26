using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// View renderer for the Notes & Documentation tab.
    /// Master-detail 2-pane layout for browsing, creating, and editing rich notes.
    /// </summary>
    public class NoteListView
    {
        private readonly TaskNotesDatabase m_Database;
        private readonly EditorWindow m_ParentWindow;

        private string m_SearchText = "";
        private string m_CategoryFilter = "All";
        private Vector2 m_SidebarScrollPos;
        private Vector2 m_ContentScrollPos;

        private NoteItem m_SelectedNote;
        private NoteItem m_NoteToDelete;

        public NoteListView(TaskNotesDatabase database, EditorWindow parentWindow)
        {
            m_Database = database;
            m_ParentWindow = parentWindow;
        }

        public void Draw()
        {
            if (m_Database == null) return;

            // Ensure selection validity
            if (m_SelectedNote != null && !m_Database.Notes.Contains(m_SelectedNote))
            {
                m_SelectedNote = m_Database.Notes.Count > 0 ? m_Database.Notes[0] : null;
            }
            if (m_SelectedNote == null && m_Database.Notes.Count > 0)
            {
                m_SelectedNote = m_Database.Notes[0];
            }

            EditorGUILayout.BeginHorizontal();

            // Left Sidebar: Note list
            DrawNoteSidebar();

            // Vertical splitter separator
            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));

            // Right Pane: Selected Note Editor
            DrawNoteContent();

            EditorGUILayout.EndHorizontal();

            if (m_NoteToDelete != null)
            {
                m_Database.RemoveNote(m_NoteToDelete);
                if (m_SelectedNote == m_NoteToDelete)
                {
                    m_SelectedNote = m_Database.Notes.Count > 0 ? m_Database.Notes[0] : null;
                }
                m_NoteToDelete = null;
                GUIUtility.ExitGUI();
            }
        }

        private void DrawNoteSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240), GUILayout.MinWidth(180), GUILayout.MaxWidth(320));

            // Top Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_SearchText = EditorGUILayout.TextField(m_SearchText, EditorStyles.toolbarSearchField);
            if (!string.IsNullOrEmpty(m_SearchText))
            {
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(18)))
                {
                    m_SearchText = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Filter by category
            var catOptions = new List<string> { "All Categories" };
            catOptions.AddRange(m_Database.CustomCategories);
            int selectedCatIdx = m_CategoryFilter == "All" ? 0 : catOptions.IndexOf(m_CategoryFilter);
            if (selectedCatIdx < 0) selectedCatIdx = 0;
            int newCatIdx = EditorGUILayout.Popup(selectedCatIdx, catOptions.ToArray(), EditorStyles.toolbarPopup);
            m_CategoryFilter = newCatIdx == 0 ? "All" : catOptions[newCatIdx];

            // New Note Button
            GUI.backgroundColor = new Color(0.25f, 0.65f, 0.95f);
            if (GUILayout.Button("+ New Note", EditorStyles.miniButton, GUILayout.Height(22)))
            {
                var newNote = new NoteItem("Untitled Note", "", m_CategoryFilter != "All" ? m_CategoryFilter : "General");
                m_Database.AddNote(newNote);
                m_SelectedNote = newNote;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            // Notes list
            var notes = GetFilteredNotes();
            m_SidebarScrollPos = EditorGUILayout.BeginScrollView(m_SidebarScrollPos);

            if (notes.Count == 0)
            {
                GUILayout.Space(20);
                GUILayout.Label(m_Database.Notes.Count == 0 ? "No notes created yet." : "No notes found.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var note in notes)
                {
                    DrawSidebarNoteItem(note);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSidebarNoteItem(NoteItem note)
        {
            bool isSelected = m_SelectedNote == note;

            Color tagColor = TaskNotesStyles.GetNoteTagColor(note.ColorTag);
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? new Color(0.3f, 0.6f, 1f, 0.4f) : Color.clear;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            // Color bar
            Rect colorRect = GUILayoutUtility.GetRect(4, 32, GUILayout.Width(4));
            EditorGUI.DrawRect(colorRect, tagColor);

            GUILayout.Space(4);

            // Title & Category
            EditorGUILayout.BeginVertical();
            string pinPrefix = note.IsPinned ? "📌 " : "";
            string titleDisplay = pinPrefix + (string.IsNullOrWhiteSpace(note.Title) ? "Untitled Note" : note.Title);

            var titleStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
            if (GUILayout.Button(titleDisplay, titleStyle, GUILayout.ExpandWidth(true)))
            {
                m_SelectedNote = note;
                GUI.FocusControl(null);
            }

            EditorGUILayout.LabelField(note.Category, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoteContent()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            if (m_SelectedNote == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a note on the left or create a new one to begin editing.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            m_ContentScrollPos = EditorGUILayout.BeginScrollView(m_ContentScrollPos);

            // Note Header Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Pin toggle
            bool newPin = GUILayout.Toggle(m_SelectedNote.IsPinned, m_SelectedNote.IsPinned ? "📌 Pinned" : "Pin Note", EditorStyles.toolbarButton, GUILayout.Width(75));
            if (newPin != m_SelectedNote.IsPinned)
            {
                Undo.RecordObject(m_Database, "Toggle Note Pin");
                m_SelectedNote.IsPinned = newPin;
                m_Database.MarkDirty();
            }

            // Category
            GUILayout.Label("Category:", EditorStyles.miniLabel, GUILayout.Width(55));
            var categories = m_Database.CustomCategories.ToArray();
            int catIdx = Mathf.Max(0, Array.IndexOf(categories, m_SelectedNote.Category));
            int newCatIdx = EditorGUILayout.Popup(catIdx, categories, EditorStyles.toolbarPopup, GUILayout.Width(110));
            if (newCatIdx >= 0 && newCatIdx < categories.Length && categories[newCatIdx] != m_SelectedNote.Category)
            {
                Undo.RecordObject(m_Database, "Change Note Category");
                m_SelectedNote.Category = categories[newCatIdx];
                m_Database.MarkDirty();
            }

            // Color tag
            GUILayout.Label("Color:", EditorStyles.miniLabel, GUILayout.Width(35));
            var newColorTag = (NoteColorTag)EditorGUILayout.EnumPopup(m_SelectedNote.ColorTag, EditorStyles.toolbarPopup, GUILayout.Width(80));
            if (newColorTag != m_SelectedNote.ColorTag)
            {
                Undo.RecordObject(m_Database, "Change Note Color Tag");
                m_SelectedNote.ColorTag = newColorTag;
                m_Database.MarkDirty();
            }

            GUILayout.FlexibleSpace();

            // Duplicate Note
            if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                var clone = m_SelectedNote.Clone();
                m_Database.AddNote(clone);
                m_SelectedNote = clone;
            }

            // Delete Note
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                if (EditorUtility.DisplayDialog("Delete Note", $"Are you sure you want to delete note '{m_SelectedNote.Title}'?", "Yes", "No"))
                {
                    m_NoteToDelete = m_SelectedNote;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Title input
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Title:", EditorStyles.boldLabel, GUILayout.Width(45));
            string newTitle = EditorGUILayout.TextField(m_SelectedNote.Title, EditorStyles.boldLabel);
            if (newTitle != m_SelectedNote.Title)
            {
                Undo.RecordObject(m_Database, "Edit Note Title");
                m_SelectedNote.Title = newTitle;
                m_Database.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Linked Assets Section
            DrawLinkedAssetsSection();

            EditorGUILayout.Space(4);
            TaskNotesStyles.DrawSplitter();
            EditorGUILayout.Space(4);

            // Content Body Text Area
            EditorGUILayout.LabelField("Content / Markdown:", EditorStyles.miniBoldLabel);
            string newContent = EditorGUILayout.TextArea(m_SelectedNote.Content, TaskNotesStyles.RichTextAreaStyle, GUILayout.ExpandHeight(true), GUILayout.MinHeight(260));
            if (newContent != m_SelectedNote.Content)
            {
                Undo.RecordObject(m_Database, "Edit Note Content");
                m_SelectedNote.Content = newContent;
                m_Database.MarkDirty();
            }

            EditorGUILayout.Space(4);

            // Timestamps
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Created: {m_SelectedNote.CreatedDate}  |  Modified: {m_SelectedNote.LastModifiedDate}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawLinkedAssetsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📎 Linked Assets ({m_SelectedNote.LinkedObjects.Count}):", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("+ Link Asset", EditorStyles.miniButton, GUILayout.Width(90)))
            {
                Undo.RecordObject(m_Database, "Add Linked Asset Slot");
                m_SelectedNote.LinkedObjects.Add(null);
                m_Database.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            int removeIdx = -1;
            for (int i = 0; i < m_SelectedNote.LinkedObjects.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var currentObj = m_SelectedNote.LinkedObjects[i];
                var newObj = EditorGUILayout.ObjectField(currentObj, typeof(Object), true);
                if (newObj != currentObj)
                {
                    Undo.RecordObject(m_Database, "Modify Linked Asset");
                    m_SelectedNote.LinkedObjects[i] = newObj;
                    m_Database.MarkDirty();
                }

                if (currentObj != null && GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(45)))
                {
                    EditorGUIUtility.PingObject(currentObj);
                    Selection.activeObject = currentObj;
                }

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    removeIdx = i;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (removeIdx >= 0)
            {
                Undo.RecordObject(m_Database, "Remove Linked Asset");
                m_SelectedNote.LinkedObjects.RemoveAt(removeIdx);
                m_Database.MarkDirty();
            }

            EditorGUILayout.EndVertical();
        }

        private List<NoteItem> GetFilteredNotes()
        {
            var list = new List<NoteItem>(m_Database.Notes);

            // Filter search
            if (!string.IsNullOrWhiteSpace(m_SearchText))
            {
                string query = m_SearchText.Trim().ToLowerInvariant();
                list = list.FindAll(n =>
                    n.Title.ToLowerInvariant().Contains(query) ||
                    n.Content.ToLowerInvariant().Contains(query) ||
                    n.Category.ToLowerInvariant().Contains(query));
            }

            // Filter Category
            if (m_CategoryFilter != "All")
            {
                list = list.FindAll(n => n.Category.Equals(m_CategoryFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Sort: Pinned first, then by last modified desc
            return list.OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.LastModifiedDate).ToList();
        }
    }
}
