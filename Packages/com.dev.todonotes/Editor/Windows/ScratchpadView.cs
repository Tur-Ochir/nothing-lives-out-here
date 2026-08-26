using System;
using UnityEditor;
using UnityEngine;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// View renderer for the Quick Scratchpad tab.
    /// Distraction-free, auto-saving text area for immediate thoughts and clipboard dumps.
    /// </summary>
    public class ScratchpadView
    {
        private readonly TaskNotesDatabase m_Database;
        private readonly EditorWindow m_ParentWindow;
        private Vector2 m_ScrollPos;

        public ScratchpadView(TaskNotesDatabase database, EditorWindow parentWindow)
        {
            m_Database = database;
            m_ParentWindow = parentWindow;
        }

        public void Draw()
        {
            if (m_Database == null) return;

            DrawToolbar();
            EditorGUILayout.Space(4);

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            string currentContent = m_Database.Scratchpad;
            string newContent = EditorGUILayout.TextArea(currentContent, TaskNotesStyles.RichTextAreaStyle, GUILayout.ExpandHeight(true), GUILayout.MinHeight(350));

            if (newContent != currentContent)
            {
                Undo.RecordObject(m_Database, "Edit Scratchpad");
                m_Database.Scratchpad = newContent;
            }

            EditorGUILayout.EndScrollView();

            DrawBottomBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("⚡ Quick Scratchpad (Auto-Saves)", EditorStyles.miniBoldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Convert to Task", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                ConvertToTask();
            }

            if (GUILayout.Button("Convert to Note", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                ConvertToNote();
            }

            if (GUILayout.Button("Copy All", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                EditorGUIUtility.systemCopyBuffer = m_Database.Scratchpad;
                Debug.Log("[Task & Notes Manager] Scratchpad copied to clipboard!");
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                if (EditorUtility.DisplayDialog("Clear Scratchpad", "Are you sure you want to clear the entire scratchpad?", "Yes", "No"))
                {
                    Undo.RecordObject(m_Database, "Clear Scratchpad");
                    m_Database.Scratchpad = "";
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBottomBar()
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string content = m_Database.Scratchpad;
            int charCount = content.Length;
            int lineCount = string.IsNullOrEmpty(content) ? 0 : content.Split('\n').Length;

            GUILayout.Label($"Characters: {charCount}  |  Lines: {lineCount}", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void ConvertToTask()
        {
            string content = m_Database.Scratchpad.Trim();
            if (string.IsNullOrEmpty(content))
            {
                EditorUtility.DisplayDialog("Scratchpad Empty", "There is no text in the scratchpad to convert into a task.", "OK");
                return;
            }

            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string title = lines.Length > 0 ? lines[0] : "New Task from Scratchpad";
            string description = lines.Length > 1 ? string.Join("\n", lines, 1, lines.Length - 1) : "";

            var task = new TaskItem(title, TaskPriority.Medium, "General")
            {
                Description = description
            };

            m_Database.AddTask(task);
            EditorUtility.DisplayDialog("Task Created", $"Created new task: '{title}'", "OK");
        }

        private void ConvertToNote()
        {
            string content = m_Database.Scratchpad.Trim();
            if (string.IsNullOrEmpty(content))
            {
                EditorUtility.DisplayDialog("Scratchpad Empty", "There is no text in the scratchpad to convert into a note.", "OK");
                return;
            }

            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string title = lines.Length > 0 ? lines[0] : "Untitled Scratch Note";

            var note = new NoteItem(title, content, "General");
            m_Database.AddNote(note);
            EditorUtility.DisplayDialog("Note Created", $"Created new note: '{title}'", "OK");
        }
    }
}
