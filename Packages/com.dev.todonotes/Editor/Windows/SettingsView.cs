using UnityEditor;
using UnityEngine;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// View renderer for the Settings & Backup tab.
    /// Manages custom categories, preferences, database assets, and export/import operations.
    /// </summary>
    public class SettingsView
    {
        private readonly TaskNotesDatabase m_Database;
        private readonly EditorWindow m_ParentWindow;
        private Vector2 m_ScrollPos;

        private string m_NewCategoryName = "";

        public SettingsView(TaskNotesDatabase database, EditorWindow parentWindow)
        {
            m_Database = database;
            m_ParentWindow = parentWindow;
        }

        public void Draw()
        {
            if (m_Database == null) return;

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            // 1. General Preferences
            DrawPreferencesSection();
            EditorGUILayout.Space(8);

            // 2. Custom Categories Management
            DrawCategoriesSection();
            EditorGUILayout.Space(8);

            // 3. Database Location & Info
            DrawDatabaseInfoSection();
            EditorGUILayout.Space(8);

            // 4. Backup & Export Tools
            DrawBackupSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawPreferencesSection()
        {
            EditorGUILayout.LabelField("⚙️ General Preferences", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool autoSave = EditorGUILayout.ToggleLeft("Auto-Save changes immediately to disk", m_Database.AutoSave);
            if (autoSave != m_Database.AutoSave)
            {
                Undo.RecordObject(m_Database, "Toggle AutoSave");
                m_Database.AutoSave = autoSave;
                m_Database.MarkDirty();
            }

            bool showDoneAtBottom = EditorGUILayout.ToggleLeft("Move completed tasks to the bottom of the list", m_Database.ShowDoneTasksAtBottom);
            if (showDoneAtBottom != m_Database.ShowDoneTasksAtBottom)
            {
                Undo.RecordObject(m_Database, "Toggle Show Done at Bottom");
                m_Database.ShowDoneTasksAtBottom = showDoneAtBottom;
                m_Database.MarkDirty();
            }

            bool showProgressBar = EditorGUILayout.ToggleLeft("Show visual progress completion bar", m_Database.ShowProgressBar);
            if (showProgressBar != m_Database.ShowProgressBar)
            {
                Undo.RecordObject(m_Database, "Toggle Show Progress Bar");
                m_Database.ShowProgressBar = showProgressBar;
                m_Database.MarkDirty();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCategoriesSection()
        {
            EditorGUILayout.LabelField("🏷️ Manage Task & Note Categories", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Add new category row
            EditorGUILayout.BeginHorizontal();
            m_NewCategoryName = EditorGUILayout.TextField(m_NewCategoryName);
            if (GUILayout.Button("+ Add Category", GUILayout.Width(110)))
            {
                if (!string.IsNullOrWhiteSpace(m_NewCategoryName))
                {
                    m_Database.AddCategory(m_NewCategoryName.Trim());
                    m_NewCategoryName = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Active categories list
            string toRemove = null;
            for (int i = 0; i < m_Database.CustomCategories.Count; i++)
            {
                string cat = m_Database.CustomCategories[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {cat}", EditorStyles.label);

                if (cat != "General")
                {
                    if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(55)))
                    {
                        toRemove = cat;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("(Default)", EditorStyles.miniLabel, GUILayout.Width(55));
                }

                EditorGUILayout.EndHorizontal();
            }

            if (toRemove != null)
            {
                m_Database.RemoveCategory(toRemove);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDatabaseInfoSection()
        {
            EditorGUILayout.LabelField("💾 Database Asset", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField("Active Database:", m_Database, typeof(TaskNotesDatabase), false);
            if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(m_Database);
                Selection.activeObject = m_Database;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Total Tasks: {m_Database.Tasks.Count}  |  Total Notes: {m_Database.Notes.Count}", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawBackupSection()
        {
            EditorGUILayout.LabelField("📦 Export & Backup Tools", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Markdown Summary (.md)", GUILayout.Height(26)))
            {
                TaskNotesExporter.ExportToMarkdownPrompt(m_Database);
            }
            if (GUILayout.Button("Export JSON Backup (.json)", GUILayout.Height(26)))
            {
                TaskNotesExporter.ExportToJsonPrompt(m_Database);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Import from JSON Backup...", GUILayout.Height(26)))
            {
                TaskNotesExporter.ImportFromJsonPrompt(m_Database);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
