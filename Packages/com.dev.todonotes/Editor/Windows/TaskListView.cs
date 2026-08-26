using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// View renderer for the To-Do List & Task Management tab.
    /// </summary>
    public class TaskListView
    {
        private readonly TaskNotesDatabase m_Database;
        private readonly EditorWindow m_ParentWindow;
        private readonly KanbanBoardView m_KanbanBoardView;

        // Quick add state
        private string m_NewTaskTitle = "";
        private TaskPriority m_NewTaskPriority = TaskPriority.Medium;
        private string m_NewTaskCategory = "General";

        // Filter & Search state
        private string m_SearchText = "";
        private TaskFilterStatus m_StatusFilter = TaskFilterStatus.All;
        private string m_CategoryFilter = "All";
        private int m_PriorityFilterIndex = 0; // 0 = All, 1..4 = Low..Urgent

        private Vector2 m_ScrollPos;
        private TaskItem m_TaskToDelete = null;

        public TaskListView(TaskNotesDatabase database, EditorWindow parentWindow)
        {
            m_Database = database;
            m_ParentWindow = parentWindow;
            m_KanbanBoardView = new KanbanBoardView(database, parentWindow);
        }

        public void Draw()
        {
            if (m_Database == null) return;

            DrawQuickAddBar();
            EditorGUILayout.Space(4);
            DrawFilterToolbar();
            EditorGUILayout.Space(4);
            DrawProgressBar();
            EditorGUILayout.Space(6);

            if (m_Database.ViewMode == TaskViewMode.Kanban)
            {
                m_KanbanBoardView.Draw(m_SearchText, m_CategoryFilter, m_PriorityFilterIndex);
            }
            else
            {
                DrawTaskList();

                if (m_TaskToDelete != null)
                {
                    m_Database.RemoveTask(m_TaskToDelete);
                    m_TaskToDelete = null;
                    GUIUtility.ExitGUI();
                }

                DrawBottomBar();
            }
        }

        private void DrawQuickAddBar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // Quick Add Input
            GUI.SetNextControlName("QuickAddTitleField");
            m_NewTaskTitle = EditorGUILayout.TextField(m_NewTaskTitle, EditorStyles.textField, GUILayout.Height(24));

            // Show placeholder if empty
            if (string.IsNullOrEmpty(m_NewTaskTitle) && GUI.GetNameOfFocusedControl() != "QuickAddTitleField")
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                var placeholderStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Italic
                };
                placeholderStyle.normal.textColor = Color.gray;
                GUI.Label(new Rect(lastRect.x + 6, lastRect.y + 3, lastRect.width, lastRect.height), "Add new task... (Press Enter or click Add)", placeholderStyle);
            }

            // Category selector
            var categories = m_Database.CustomCategories.ToArray();
            int currentCatIdx = Mathf.Max(0, Array.IndexOf(categories, m_NewTaskCategory));
            int newCatIdx = EditorGUILayout.Popup(currentCatIdx, categories, GUILayout.Width(90), GUILayout.Height(24));
            if (newCatIdx >= 0 && newCatIdx < categories.Length)
            {
                m_NewTaskCategory = categories[newCatIdx];
            }

            // Priority selector
            m_NewTaskPriority = (TaskPriority)EditorGUILayout.EnumPopup(m_NewTaskPriority, GUILayout.Width(75), GUILayout.Height(24));

            // Add Button
            GUI.backgroundColor = new Color(0.3f, 0.75f, 0.35f);
            if (GUILayout.Button("+ Add Task", GUILayout.Width(90), GUILayout.Height(24)) ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "QuickAddTitleField"))
            {
                if (!string.IsNullOrWhiteSpace(m_NewTaskTitle))
                {
                    var task = new TaskItem(m_NewTaskTitle.Trim(), m_NewTaskPriority, m_NewTaskCategory);
                    m_Database.AddTask(task);
                    m_NewTaskTitle = "";
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawFilterToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // View Mode Toggle (List vs Kanban)
            int currentViewIdx = (int)m_Database.ViewMode;
            string[] viewLabels = { "📋 List", "📊 Kanban" };
            int newViewIdx = GUILayout.Toolbar(currentViewIdx, viewLabels, EditorStyles.toolbarButton, GUILayout.Width(130));
            if (newViewIdx != currentViewIdx)
            {
                m_Database.ViewMode = (TaskViewMode)newViewIdx;
            }

            GUILayout.Space(6);

            // Search text
            m_SearchText = EditorGUILayout.TextField(m_SearchText, EditorStyles.toolbarSearchField, GUILayout.Width(140));
            if (!string.IsNullOrEmpty(m_SearchText))
            {
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(18)))
                {
                    m_SearchText = "";
                    GUI.FocusControl(null);
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Filter:", EditorStyles.miniLabel, GUILayout.Width(35));

            // Status Filter (Only in List view since Kanban displays all columns)
            if (m_Database.ViewMode == TaskViewMode.List)
            {
                m_StatusFilter = (TaskFilterStatus)EditorGUILayout.EnumPopup(m_StatusFilter, EditorStyles.toolbarPopup, GUILayout.Width(95));
            }

            // Category Filter
            var catOptions = new List<string> { "All Categories" };
            catOptions.AddRange(m_Database.CustomCategories);
            int selectedCatIdx = m_CategoryFilter == "All" ? 0 : catOptions.IndexOf(m_CategoryFilter);
            if (selectedCatIdx < 0) selectedCatIdx = 0;
            int newCatIdx = EditorGUILayout.Popup(selectedCatIdx, catOptions.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(110));
            m_CategoryFilter = newCatIdx == 0 ? "All" : catOptions[newCatIdx];

            // Priority Filter
            string[] priorityLabels = { "All Priorities", "Low", "Medium", "High", "Urgent" };
            m_PriorityFilterIndex = EditorGUILayout.Popup(m_PriorityFilterIndex, priorityLabels, EditorStyles.toolbarPopup, GUILayout.Width(95));

            GUILayout.FlexibleSpace();

            // Sort Mode (Only in List view)
            if (m_Database.ViewMode == TaskViewMode.List)
            {
                GUILayout.Label("Sort:", EditorStyles.miniLabel, GUILayout.Width(32));
                var newSort = (TaskSortMode)EditorGUILayout.EnumPopup(m_Database.SortMode, EditorStyles.toolbarPopup, GUILayout.Width(110));
                if (newSort != m_Database.SortMode)
                {
                    m_Database.SortMode = newSort;
                    m_Database.MarkDirty();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawProgressBar()
        {
            if (!m_Database.ShowProgressBar) return;

            int total = m_Database.Tasks.Count;
            if (total == 0) return;

            int done = m_Database.Tasks.Count(t => t.IsDone);
            float progress = (float)done / total;

            string label = $"{done} / {total} Completed ({progress * 100f:0}%)";
            TaskNotesStyles.DrawProgressBar(progress, label, 16f);
        }

        private void DrawTaskList()
        {
            var filteredTasks = GetFilteredAndSortedTasks();

            if (filteredTasks.Count == 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Space(20);
                GUILayout.Label(m_Database.Tasks.Count == 0 ? "✨ No tasks yet! Add your first task above." : "🔍 No tasks match the active filters.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(20);
                EditorGUILayout.EndVertical();
                return;
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            for (int i = 0; i < filteredTasks.Count; i++)
            {
                var task = filteredTasks[i];
                DrawTaskCard(task, i, filteredTasks.Count);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTaskCard(TaskItem task, int index, int totalCount)
        {
            bool isDone = task.IsDone;

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = isDone ?
                (EditorGUIUtility.isProSkin ? TaskNotesStyles.CardCompletedDark : TaskNotesStyles.CardCompletedLight) :
                (EditorGUIUtility.isProSkin ? TaskNotesStyles.CardBgDark : TaskNotesStyles.CardBgLight);

            EditorGUILayout.BeginVertical(TaskNotesStyles.CardStyle);
            GUI.backgroundColor = prevBg;

            // Main Row
            EditorGUILayout.BeginHorizontal();

            // 1. Checkbox toggle
            bool newDone = EditorGUILayout.Toggle(isDone, GUILayout.Width(20));
            if (newDone != isDone)
            {
                Undo.RecordObject(m_Database, "Toggle Task Completion");
                task.IsDone = newDone;
                m_Database.MarkDirty();
            }

            // 2. Priority indicator dot / badge
            Color priorityColor = TaskNotesStyles.GetPriorityColor(task.Priority);
            Rect priorityRect = GUILayoutUtility.GetRect(12, 18, GUILayout.Width(12));
            priorityRect.y += 3;
            priorityRect.height = 12;
            EditorGUI.DrawRect(priorityRect, priorityColor);

            // 3. Title (Foldable or Editable on click)
            GUIStyle titleStyle = isDone ? TaskNotesStyles.TitleDoneStyle : TaskNotesStyles.TitleStyle;
            if (GUILayout.Button(task.Title, titleStyle, GUILayout.ExpandWidth(true)))
            {
                task.IsExpanded = !task.IsExpanded;
            }

            // 4. Category Tag Badge
            Rect catRect = GUILayoutUtility.GetRect(new GUIContent(task.Category), TaskNotesStyles.BadgeStyle, GUILayout.ExpandWidth(false));
            TaskNotesStyles.DrawBadge(catRect, task.Category, new Color(0.35f, 0.4f, 0.45f, 0.8f));

            GUILayout.Space(4);

            // 5. Status dropdown
            var newStatus = (TaskStatus)EditorGUILayout.EnumPopup(task.Status, EditorStyles.miniPullDown, GUILayout.Width(75));
            if (newStatus != task.Status)
            {
                Undo.RecordObject(m_Database, "Change Task Status");
                task.Status = newStatus;
                m_Database.MarkDirty();
            }

            // 6. Linked Object Quick Ping icon (if any)
            if (task.LinkedObject != null)
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_ViewToolZoom"), EditorStyles.iconButton, GUILayout.Width(20), GUILayout.Height(18)))
                {
                    EditorGUIUtility.PingObject(task.LinkedObject);
                    Selection.activeObject = task.LinkedObject;
                }
            }

            // 7. Reorder buttons (only when Custom Order is active)
            if (m_Database.SortMode == TaskSortMode.CustomOrder)
            {
                GUI.enabled = index > 0;
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(18), GUILayout.Height(18)))
                {
                    SwapTasks(index, index - 1);
                }

                GUI.enabled = index < totalCount - 1;
                if (GUILayout.Button("▼", EditorStyles.miniButtonRight, GUILayout.Width(18), GUILayout.Height(18)))
                {
                    SwapTasks(index, index + 1);
                }
                GUI.enabled = true;
            }

            // 8. Options Menu / Delete
            if (GUILayout.Button("⋮", EditorStyles.miniButton, GUILayout.Width(18), GUILayout.Height(18)))
            {
                ShowTaskContextMenu(task);
            }

            EditorGUILayout.EndHorizontal();

            // Expanded Details Row
            if (task.IsExpanded)
            {
                DrawTaskDetails(task);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTaskDetails(TaskItem task)
        {
            GUILayout.Space(4);
            TaskNotesStyles.DrawSplitter();
            GUILayout.Space(4);

            // Editable Title
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Title:", GUILayout.Width(65));
            string newTitle = EditorGUILayout.TextField(task.Title);
            if (newTitle != task.Title)
            {
                Undo.RecordObject(m_Database, "Edit Task Title");
                task.Title = newTitle;
                m_Database.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // Priority & Category
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Priority:", GUILayout.Width(65));
            var newPriority = (TaskPriority)EditorGUILayout.EnumPopup(task.Priority, GUILayout.Width(100));
            if (newPriority != task.Priority)
            {
                Undo.RecordObject(m_Database, "Edit Task Priority");
                task.Priority = newPriority;
                m_Database.MarkDirty();
            }

            EditorGUILayout.LabelField("Category:", GUILayout.Width(65));
            var categories = m_Database.CustomCategories.ToArray();
            int catIdx = Mathf.Max(0, Array.IndexOf(categories, task.Category));
            int newCatIdx = EditorGUILayout.Popup(catIdx, categories, GUILayout.Width(120));
            if (newCatIdx >= 0 && newCatIdx < categories.Length && categories[newCatIdx] != task.Category)
            {
                Undo.RecordObject(m_Database, "Edit Task Category");
                task.Category = categories[newCatIdx];
                m_Database.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // Linked Object Reference Field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Link Asset:", GUILayout.Width(65));
            var newObj = EditorGUILayout.ObjectField(task.LinkedObject, typeof(Object), true);
            if (newObj != task.LinkedObject)
            {
                Undo.RecordObject(m_Database, "Edit Task Linked Object");
                task.LinkedObject = newObj;
                m_Database.MarkDirty();
            }

            if (task.LinkedObject != null && GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(45)))
            {
                EditorGUIUtility.PingObject(task.LinkedObject);
                Selection.activeObject = task.LinkedObject;
            }
            EditorGUILayout.EndHorizontal();

            // Description / Multiline Notes
            EditorGUILayout.LabelField("Details & Notes:", EditorStyles.miniBoldLabel);
            string newDesc = EditorGUILayout.TextArea(task.Description, TaskNotesStyles.RichTextAreaStyle, GUILayout.MinHeight(45));
            if (newDesc != task.Description)
            {
                Undo.RecordObject(m_Database, "Edit Task Description");
                task.Description = newDesc;
                m_Database.MarkDirty();
            }

            // Metadata / Timestamps
            EditorGUILayout.BeginHorizontal();
            string metaText = $"Created: {task.CreatedDate}";
            if (!string.IsNullOrEmpty(task.CompletedDate))
                metaText += $"  |  Completed: {task.CompletedDate}";
            EditorGUILayout.LabelField(metaText, EditorStyles.miniLabel);

            if (GUILayout.Button("Delete Task", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                m_TaskToDelete = task;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ShowTaskContextMenu(TaskItem task)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Duplicate"), false, () =>
            {
                var clone = task.Clone();
                m_Database.AddTask(clone);
            });
            menu.AddItem(new GUIContent("Mark as To Do"), task.Status == TaskStatus.ToDo, () =>
            {
                task.Status = TaskStatus.ToDo;
                m_Database.MarkDirty();
            });
            menu.AddItem(new GUIContent("Mark as In Progress"), task.Status == TaskStatus.InProgress, () =>
            {
                task.Status = TaskStatus.InProgress;
                m_Database.MarkDirty();
            });
            menu.AddItem(new GUIContent("Mark as Done"), task.Status == TaskStatus.Done, () =>
            {
                task.Status = TaskStatus.Done;
                m_Database.MarkDirty();
            });
            menu.AddItem(new GUIContent("Mark as Blocked"), task.Status == TaskStatus.Blocked, () =>
            {
                task.Status = TaskStatus.Blocked;
                m_Database.MarkDirty();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                m_TaskToDelete = task;
            });
            menu.ShowAsContext();
        }

        private void SwapTasks(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= m_Database.Tasks.Count || indexB < 0 || indexB >= m_Database.Tasks.Count) return;
            Undo.RecordObject(m_Database, "Reorder Tasks");
            var temp = m_Database.Tasks[indexA];
            m_Database.Tasks[indexA] = m_Database.Tasks[indexB];
            m_Database.Tasks[indexB] = temp;
            m_Database.MarkDirty();
        }

        private List<TaskItem> GetFilteredAndSortedTasks()
        {
            var list = new List<TaskItem>(m_Database.Tasks);

            // Filter Search Text
            if (!string.IsNullOrWhiteSpace(m_SearchText))
            {
                string query = m_SearchText.Trim().ToLowerInvariant();
                list = list.FindAll(t =>
                    t.Title.ToLowerInvariant().Contains(query) ||
                    t.Description.ToLowerInvariant().Contains(query) ||
                    t.Category.ToLowerInvariant().Contains(query));
            }

            // Filter Status
            if (m_StatusFilter == TaskFilterStatus.ActiveOnly)
            {
                list = list.FindAll(t => !t.IsDone);
            }
            else if (m_StatusFilter == TaskFilterStatus.CompletedOnly)
            {
                list = list.FindAll(t => t.IsDone);
            }
            else if (m_StatusFilter == TaskFilterStatus.BlockedOnly)
            {
                list = list.FindAll(t => t.Status == TaskStatus.Blocked);
            }

            // Filter Category
            if (m_CategoryFilter != "All")
            {
                list = list.FindAll(t => t.Category.Equals(m_CategoryFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filter Priority
            if (m_PriorityFilterIndex > 0)
            {
                var targetPriority = (TaskPriority)(m_PriorityFilterIndex - 1);
                list = list.FindAll(t => t.Priority == targetPriority);
            }

            // Sorting
            switch (m_Database.SortMode)
            {
                case TaskSortMode.PriorityDesc:
                    list = list.OrderByDescending(t => t.Priority).ToList();
                    break;
                case TaskSortMode.PriorityAsc:
                    list = list.OrderBy(t => t.Priority).ToList();
                    break;
                case TaskSortMode.DateCreatedDesc:
                    list = list.OrderByDescending(t => t.CreatedDate).ToList();
                    break;
                case TaskSortMode.DateCreatedAsc:
                    list = list.OrderBy(t => t.CreatedDate).ToList();
                    break;
                case TaskSortMode.Category:
                    list = list.OrderBy(t => t.Category).ThenByDescending(t => t.Priority).ToList();
                    break;
                case TaskSortMode.Alphabetical:
                    list = list.OrderBy(t => t.Title).ToList();
                    break;
            }

            // Optionally push completed tasks to bottom
            if (m_Database.ShowDoneTasksAtBottom && m_StatusFilter == TaskFilterStatus.All)
            {
                list = list.OrderBy(t => t.IsDone ? 1 : 0).ToList();
            }

            return list;
        }

        private void DrawBottomBar()
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Clear Completed", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                if (EditorUtility.DisplayDialog("Clear Completed Tasks", "Are you sure you want to remove all completed tasks?", "Yes", "No"))
                {
                    Undo.RecordObject(m_Database, "Clear Completed Tasks");
                    m_Database.Tasks.RemoveAll(t => t.IsDone);
                    m_Database.MarkDirty();
                }
            }

            if (GUILayout.Button("Mark All Done", EditorStyles.toolbarButton, GUILayout.Width(95)))
            {
                Undo.RecordObject(m_Database, "Mark All Tasks Done");
                foreach (var t in m_Database.Tasks) t.IsDone = true;
                m_Database.MarkDirty();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Export Markdown", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                TaskNotesExporter.ExportToMarkdownPrompt(m_Database);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
