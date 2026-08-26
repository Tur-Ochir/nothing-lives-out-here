using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Export and import utilities for Task & Notes database (Markdown, JSON, CSV).
    /// </summary>
    public static class TaskNotesExporter
    {
        [Serializable]
        private class JsonExportContainer
        {
            public List<TaskItem> tasks = new List<TaskItem>();
            public List<NoteItem> notes = new List<NoteItem>();
            public string scratchpad = "";
            public List<string> customCategories = new List<string>();
            public string exportedAt = "";
        }

        /// <summary>
        /// Prompts user to save tasks and notes as a Markdown document.
        /// </summary>
        public static void ExportToMarkdownPrompt(TaskNotesDatabase db)
        {
            if (db == null) return;

            string defaultFileName = $"TasksAndNotes_{DateTime.Now:yyyyMMdd_HHmm}.md";
            string path = EditorUtility.SaveFilePanel("Export to Markdown", Application.dataPath, defaultFileName, "md");

            if (!string.IsNullOrEmpty(path))
            {
                string mdContent = GenerateMarkdown(db);
                File.WriteAllText(path, mdContent, Encoding.UTF8);
                EditorUtility.RevealInFinder(path);
                Debug.Log($"[Task & Notes Manager] Successfully exported to Markdown: {path}");
            }
        }

        /// <summary>
        /// Prompts user to save database as JSON.
        /// </summary>
        public static void ExportToJsonPrompt(TaskNotesDatabase db)
        {
            if (db == null) return;

            string defaultFileName = $"TasksAndNotes_Backup_{DateTime.Now:yyyyMMdd_HHmm}.json";
            string path = EditorUtility.SaveFilePanel("Export to JSON", Application.dataPath, defaultFileName, "json");

            if (!string.IsNullOrEmpty(path))
            {
                var container = new JsonExportContainer
                {
                    tasks = new List<TaskItem>(db.Tasks),
                    notes = new List<NoteItem>(db.Notes),
                    scratchpad = db.Scratchpad,
                    customCategories = new List<string>(db.CustomCategories),
                    exportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonUtility.ToJson(container, true);
                File.WriteAllText(path, json, Encoding.UTF8);
                EditorUtility.RevealInFinder(path);
                Debug.Log($"[Task & Notes Manager] Successfully exported to JSON: {path}");
            }
        }

        /// <summary>
        /// Prompts user to import tasks and notes from JSON.
        /// </summary>
        public static void ImportFromJsonPrompt(TaskNotesDatabase db)
        {
            if (db == null) return;

            string path = EditorUtility.OpenFilePanel("Import from JSON Backup", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var container = JsonUtility.FromJson<JsonExportContainer>(json);

                if (container == null || (container.tasks == null && container.notes == null))
                {
                    EditorUtility.DisplayDialog("Import Error", "Selected file is not a valid Task & Notes backup JSON.", "OK");
                    return;
                }

                bool replace = EditorUtility.DisplayDialog(
                    "Import Mode",
                    $"The backup contains {container.tasks?.Count ?? 0} tasks and {container.notes?.Count ?? 0} notes.\n\nDo you want to Merge into current data or Replace all current data?",
                    "Replace", "Merge");

                Undo.RecordObject(db, "Import Task & Notes");

                if (replace)
                {
                    db.Tasks.Clear();
                    db.Notes.Clear();
                    if (container.tasks != null) db.Tasks.AddRange(container.tasks);
                    if (container.notes != null) db.Notes.AddRange(container.notes);
                    if (!string.IsNullOrEmpty(container.scratchpad)) db.Scratchpad = container.scratchpad;
                }
                else
                {
                    // Merge
                    if (container.tasks != null)
                    {
                        foreach (var task in container.tasks)
                        {
                            if (!db.Tasks.Exists(t => t.Id == task.Id))
                                db.Tasks.Add(task);
                        }
                    }
                    if (container.notes != null)
                    {
                        foreach (var note in container.notes)
                        {
                            if (!db.Notes.Exists(n => n.Id == note.Id))
                                db.Notes.Add(note);
                        }
                    }
                }

                if (container.customCategories != null)
                {
                    foreach (var cat in container.customCategories)
                    {
                        if (!db.CustomCategories.Contains(cat))
                            db.CustomCategories.Add(cat);
                    }
                }

                db.MarkDirty();
                EditorUtility.DisplayDialog("Import Complete", "Task & Notes data was successfully imported!", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Import Failed", $"An error occurred during import:\n{ex.Message}", "OK");
                Debug.LogException(ex);
            }
        }

        public static string GenerateMarkdown(TaskNotesDatabase db)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 📋 Project Tasks & Notes");
            sb.AppendLine($"*Exported on: {DateTime.Now:yyyy-MM-dd HH:mm}*");
            sb.AppendLine();

            // Tasks Summary
            int total = db.Tasks.Count;
            int done = db.Tasks.FindAll(t => t.IsDone).Count;
            float pct = total > 0 ? (float)done / total * 100f : 0f;

            sb.AppendLine("## 📊 Summary");
            sb.AppendLine($"- **Total Tasks**: {total}");
            sb.AppendLine($"- **Completed**: {done} ({pct:F0}%)");
            sb.AppendLine($"- **Pending**: {total - done}");
            sb.AppendLine($"- **Total Notes**: {db.Notes.Count}");
            sb.AppendLine();

            // Tasks List
            sb.AppendLine("## 📋 Tasks");
            sb.AppendLine();

            // Group by category
            var categorized = new Dictionary<string, List<TaskItem>>();
            foreach (var t in db.Tasks)
            {
                string cat = string.IsNullOrWhiteSpace(t.Category) ? "General" : t.Category;
                if (!categorized.ContainsKey(cat))
                    categorized[cat] = new List<TaskItem>();
                categorized[cat].Add(t);
            }

            foreach (var kvp in categorized)
            {
                sb.AppendLine($"### 📁 {kvp.Key}");
                foreach (var task in kvp.Value)
                {
                    string check = task.IsDone ? "[x]" : "[ ]";
                    string priorityBadge = $"**[{task.Priority}]**";
                    string statusBadge = task.Status != TaskStatus.ToDo && task.Status != TaskStatus.Done ? $" `({task.Status})`" : "";
                    string objectRef = task.LinkedObject != null ? $" *(Linked: {task.LinkedObject.name})*" : "";

                    sb.AppendLine($"- {check} {priorityBadge}{statusBadge} {task.Title}{objectRef}");
                    if (!string.IsNullOrWhiteSpace(task.Description))
                    {
                        sb.AppendLine($"  > {task.Description.Replace("\n", "\n  > ")}");
                    }
                }
                sb.AppendLine();
            }

            // Notes List
            sb.AppendLine("## 📝 Notes & Documentation");
            sb.AppendLine();

            foreach (var note in db.Notes)
            {
                string pin = note.IsPinned ? "📌 " : "";
                sb.AppendLine($"### {pin}{note.Title} `[{note.Category}]`");
                sb.AppendLine($"*Created: {note.CreatedDate} | Modified: {note.LastModifiedDate}*");
                sb.AppendLine();
                sb.AppendLine(note.Content);
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(db.Scratchpad))
            {
                sb.AppendLine("## ⚡ Scratchpad Content");
                sb.AppendLine("```");
                sb.AppendLine(db.Scratchpad);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
