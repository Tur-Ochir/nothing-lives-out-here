using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// ScriptableObject database storing all tasks, notes, and scratchpad data.
    /// Supports team version control (Git), undo/redo, and auto-saving.
    /// </summary>
    public class TaskNotesDatabase : ScriptableObject
    {
        private const string k_DefaultAssetPath = "Assets/_Project/Data/TaskNotesDatabase.asset";
        private const string k_FallbackAssetPath = "Assets/TaskNotesDatabase.asset";

        [SerializeField] private List<TaskItem> m_Tasks = new List<TaskItem>();
        [SerializeField] private List<NoteItem> m_Notes = new List<NoteItem>();
        [SerializeField, TextArea(5, 20)] private string m_Scratchpad = "";
        [SerializeField] private List<string> m_CustomCategories = new List<string>
        {
            "General",
            "Gameplay",
            "UI",
            "Art",
            "Audio",
            "Bug",
            "Optimization",
            "Refactor"
        };

        [Header("Preferences")]
        [SerializeField] private bool m_AutoSave = true;
        [SerializeField] private bool m_ShowDoneTasksAtBottom = true;
        [SerializeField] private bool m_ShowProgressBar = true;
        [SerializeField] private TaskSortMode m_SortMode = TaskSortMode.CustomOrder;
        [SerializeField] private TaskViewMode m_ViewMode = TaskViewMode.List;

        public List<TaskItem> Tasks => m_Tasks ?? (m_Tasks = new List<TaskItem>());
        public List<NoteItem> Notes => m_Notes ?? (m_Notes = new List<NoteItem>());

        public TaskViewMode ViewMode
        {
            get => m_ViewMode;
            set
            {
                if (m_ViewMode != value)
                {
                    m_ViewMode = value;
                    MarkDirty();
                }
            }
        }

        public string Scratchpad
        {
            get => m_Scratchpad ?? "";
            set
            {
                if (m_Scratchpad != value)
                {
                    m_Scratchpad = value;
                    MarkDirty();
                }
            }
        }

        public List<string> CustomCategories
        {
            get
            {
                if (m_CustomCategories == null || m_CustomCategories.Count == 0)
                {
                    m_CustomCategories = new List<string>
                    {
                        "General", "Gameplay", "UI", "Art", "Audio", "Bug", "Optimization", "Refactor"
                    };
                }
                return m_CustomCategories;
            }
        }

        public bool AutoSave
        {
            get => m_AutoSave;
            set => m_AutoSave = value;
        }

        public bool ShowDoneTasksAtBottom
        {
            get => m_ShowDoneTasksAtBottom;
            set => m_ShowDoneTasksAtBottom = value;
        }

        public bool ShowProgressBar
        {
            get => m_ShowProgressBar;
            set => m_ShowProgressBar = value;
        }

        public TaskSortMode SortMode
        {
            get => m_SortMode;
            set => m_SortMode = value;
        }

        public void MarkDirty()
        {
            EditorUtility.SetDirty(this);
            if (m_AutoSave)
            {
                AssetDatabase.SaveAssetIfDirty(this);
            }
        }

        public void AddTask(TaskItem task)
        {
            if (task == null) return;
            Undo.RecordObject(this, "Add Task");
            Tasks.Add(task);
            MarkDirty();
        }

        public void RemoveTask(TaskItem task)
        {
            if (task == null) return;
            Undo.RecordObject(this, "Remove Task");
            Tasks.Remove(task);
            MarkDirty();
        }

        public void AddNote(NoteItem note)
        {
            if (note == null) return;
            Undo.RecordObject(this, "Add Note");
            Notes.Add(note);
            MarkDirty();
        }

        public void RemoveNote(NoteItem note)
        {
            if (note == null) return;
            Undo.RecordObject(this, "Remove Note");
            Notes.Remove(note);
            MarkDirty();
        }

        public void AddCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;
            category = category.Trim();
            if (!CustomCategories.Contains(category))
            {
                Undo.RecordObject(this, "Add Category");
                CustomCategories.Add(category);
                MarkDirty();
            }
        }

        public void RemoveCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || category == "General") return;
            if (CustomCategories.Contains(category))
            {
                Undo.RecordObject(this, "Remove Category");
                CustomCategories.Remove(category);
                MarkDirty();
            }
        }

        /// <summary>
        /// Finds the active database or creates one if it doesn't exist yet.
        /// </summary>
        public static TaskNotesDatabase GetOrCreateDatabase()
        {
            // First search project assets
            string[] guids = AssetDatabase.FindAssets("t:TaskNotesDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var db = AssetDatabase.LoadAssetAtPath<TaskNotesDatabase>(path);
                if (db != null) return db;
            }

            // Create new asset in project
            string targetPath = k_DefaultAssetPath;
            string targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir))
            {
                try
                {
                    Directory.CreateDirectory(targetDir);
                    AssetDatabase.Refresh();
                }
                catch
                {
                    targetPath = k_FallbackAssetPath;
                }
            }

            var newDb = CreateInstance<TaskNotesDatabase>();
            newDb.InitializeDefaultSamples();

            AssetDatabase.CreateAsset(newDb, targetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newDb;
        }

        private void InitializeDefaultSamples()
        {
            m_Tasks.Add(new TaskItem("Explore To-Do & Notes features", TaskPriority.High, "General")
            {
                Description = "Welcome to your new in-editor task & note manager! Link assets, create notes, and pin 3D markers in your scene.",
                Status = TaskStatus.InProgress
            });
            m_Tasks.Add(new TaskItem("Link a Game Object or Prefab to this task", TaskPriority.Medium, "Gameplay")
            {
                Description = "Drag and drop any asset or GameObject into the Linked Object slot and click Ping!"
            });

            var sampleNote = new NoteItem("Welcome & Quick Tips",
                "# 📝 Welcome to Tasks & Notes Manager!\n\n" +
                "- **Tasks Tab**: Track bugs, features, and to-dos with priority badges.\n" +
                "- **Notes Tab**: Store game design docs, architecture notes, and snippets.\n" +
                "- **Scratchpad**: Instant auto-saved scratch buffer for notes and logs.\n" +
                "- **Scene Pins**: Drop `SceneTaskMarker` components in your scenes to leave 3D sticky notes!",
                "General")
            {
                IsPinned = true,
                ColorTag = NoteColorTag.Teal
            };
            m_Notes.Add(sampleNote);

            m_Scratchpad = "// Quick scratchpad for notes, code snippets, or clipboard items.\n// Everything here is automatically saved!";
        }
    }
}
