using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Interactive Kanban Board view for managing tasks across columns (To Do, In Progress, Blocked, Done).
    /// </summary>
    public class KanbanBoardView
    {
        private readonly TaskNotesDatabase m_Database;
        private readonly EditorWindow m_ParentWindow;

        private Vector2 m_BoardScrollPos;
        private readonly Dictionary<TaskStatus, Vector2> m_ColumnScrollPositions = new Dictionary<TaskStatus, Vector2>();

        // Column inline quick add state
        private TaskStatus? m_AddingToColumn = null;
        private string m_NewInlineTitle = "";

        private TaskItem m_TaskToDelete = null;

        public KanbanBoardView(TaskNotesDatabase database, EditorWindow parentWindow)
        {
            m_Database = database;
            m_ParentWindow = parentWindow;

            m_ColumnScrollPositions[TaskStatus.ToDo] = Vector2.zero;
            m_ColumnScrollPositions[TaskStatus.InProgress] = Vector2.zero;
            m_ColumnScrollPositions[TaskStatus.Blocked] = Vector2.zero;
            m_ColumnScrollPositions[TaskStatus.Done] = Vector2.zero;
        }

        public void Draw(string searchText, string categoryFilter, int priorityFilterIndex)
        {
            if (m_Database == null) return;

            // Get filtered tasks
            var filteredTasks = FilterTasks(m_Database.Tasks, searchText, categoryFilter, priorityFilterIndex);

            m_BoardScrollPos = EditorGUILayout.BeginScrollView(m_BoardScrollPos, GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginHorizontal();

            // Render 4 Kanban Columns
            DrawColumn("📌 To Do", TaskStatus.ToDo, TaskNotesStyles.ToDoColor, filteredTasks);
            GUILayout.Space(6);
            DrawColumn("⚡ In Progress", TaskStatus.InProgress, TaskNotesStyles.InProgressColor, filteredTasks);
            GUILayout.Space(6);
            DrawColumn("🛑 Blocked", TaskStatus.Blocked, TaskNotesStyles.BlockedColor, filteredTasks);
            GUILayout.Space(6);
            DrawColumn("✅ Done", TaskStatus.Done, TaskNotesStyles.DoneColor, filteredTasks);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            if (m_TaskToDelete != null)
            {
                m_Database.RemoveTask(m_TaskToDelete);
                m_TaskToDelete = null;
                GUIUtility.ExitGUI();
            }
        }

        private void DrawColumn(string columnTitle, TaskStatus status, Color headerColor, List<TaskItem> allFilteredTasks)
        {
            var columnTasks = allFilteredTasks.FindAll(t => t.Status == status);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(220), GUILayout.MaxWidth(320), GUILayout.ExpandHeight(true));

            // Column Header Banner
            DrawColumnHeader(columnTitle, status, headerColor, columnTasks.Count);

            // Inline Quick Add Bar (if active for this column)
            if (m_AddingToColumn == status)
            {
                DrawInlineAddBar(status);
            }

            EditorGUILayout.Space(4);

            // Column Tasks Scroll View
            if (!m_ColumnScrollPositions.ContainsKey(status))
            {
                m_ColumnScrollPositions[status] = Vector2.zero;
            }

            m_ColumnScrollPositions[status] = EditorGUILayout.BeginScrollView(m_ColumnScrollPositions[status], GUILayout.ExpandHeight(true));

            if (columnTasks.Count == 0)
            {
                GUILayout.Space(15);
                GUILayout.Label("No tasks in this column", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(15);
            }
            else
            {
                for (int i = 0; i < columnTasks.Count; i++)
                {
                    DrawKanbanCard(columnTasks[i]);
                    GUILayout.Space(4);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawColumnHeader(string title, TaskStatus status, Color headerColor, int count)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Header Color Stripe
            Rect stripeRect = GUILayoutUtility.GetRect(4, 18, GUILayout.Width(4));
            EditorGUI.DrawRect(stripeRect, headerColor);

            GUILayout.Space(4);

            // Title & Count Badge
            GUILayout.Label($"{title} ({count})", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // Quick Add Button
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(22)))
            {
                if (m_AddingToColumn == status)
                {
                    m_AddingToColumn = null;
                }
                else
                {
                    m_AddingToColumn = status;
                    m_NewInlineTitle = "";
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawInlineAddBar(TaskStatus status)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUI.SetNextControlName("KanbanInlineAdd_" + status);
            m_NewInlineTitle = EditorGUILayout.TextField(m_NewInlineTitle, GUILayout.Height(20));

            if (GUILayout.Button("Add", GUILayout.Width(45), GUILayout.Height(20)) ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "KanbanInlineAdd_" + status))
            {
                if (!string.IsNullOrWhiteSpace(m_NewInlineTitle))
                {
                    var task = new TaskItem(m_NewInlineTitle.Trim(), TaskPriority.Medium, "General")
                    {
                        Status = status
                    };
                    m_Database.AddTask(task);
                    m_NewInlineTitle = "";
                    m_AddingToColumn = null;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(20)))
            {
                m_AddingToColumn = null;
                m_NewInlineTitle = "";
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawKanbanCard(TaskItem task)
        {
            Color priorityColor = TaskNotesStyles.GetPriorityColor(task.Priority);

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = task.IsDone ?
                (EditorGUIUtility.isProSkin ? TaskNotesStyles.CardCompletedDark : TaskNotesStyles.CardCompletedLight) :
                (EditorGUIUtility.isProSkin ? TaskNotesStyles.CardBgDark : TaskNotesStyles.CardBgLight);

            EditorGUILayout.BeginVertical(TaskNotesStyles.CardStyle);
            GUI.backgroundColor = prevBg;

            // Card Top Row: Priority Strip + Title + Context Menu
            EditorGUILayout.BeginHorizontal();

            // Priority Left Border
            Rect priorityRect = GUILayoutUtility.GetRect(4, 20, GUILayout.Width(4));
            EditorGUI.DrawRect(priorityRect, priorityColor);

            GUILayout.Space(4);

            // Title
            GUIStyle titleStyle = task.IsDone ? TaskNotesStyles.TitleDoneStyle : TaskNotesStyles.TitleStyle;
            if (GUILayout.Button(task.Title, titleStyle, GUILayout.ExpandWidth(true)))
            {
                task.IsExpanded = !task.IsExpanded;
            }

            // Options menu
            if (GUILayout.Button("⋮", EditorStyles.miniButton, GUILayout.Width(18), GUILayout.Height(18)))
            {
                ShowCardContextMenu(task);
            }

            EditorGUILayout.EndHorizontal();

            // Card Middle Row: Category Badge + Linked Object Ping
            EditorGUILayout.BeginHorizontal();

            Rect catRect = GUILayoutUtility.GetRect(new GUIContent(task.Category), TaskNotesStyles.BadgeStyle, GUILayout.ExpandWidth(false));
            TaskNotesStyles.DrawBadge(catRect, task.Category, new Color(0.35f, 0.4f, 0.45f, 0.8f));

            GUILayout.FlexibleSpace();

            if (task.LinkedObject != null)
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_ViewToolZoom"), EditorStyles.iconButton, GUILayout.Width(20), GUILayout.Height(18)))
                {
                    EditorGUIUtility.PingObject(task.LinkedObject);
                    Selection.activeObject = task.LinkedObject;
                }
            }

            EditorGUILayout.EndHorizontal();

            // Card Move Transition Buttons Row
            DrawStatusShiftButtons(task);

            // Expanded Details View
            if (task.IsExpanded)
            {
                DrawExpandedCardDetails(task);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusShiftButtons(TaskItem task)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();

            switch (task.Status)
            {
                case TaskStatus.ToDo:
                    if (GUILayout.Button("⚡ Start ▶", EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
                    {
                        Undo.RecordObject(m_Database, "Move Task to In Progress");
                        task.Status = TaskStatus.InProgress;
                        m_Database.MarkDirty();
                    }
                    if (GUILayout.Button("🛑", EditorStyles.miniButton, GUILayout.Width(22)))
                    {
                        Undo.RecordObject(m_Database, "Move Task to Blocked");
                        task.Status = TaskStatus.Blocked;
                        m_Database.MarkDirty();
                    }
                    break;

                case TaskStatus.InProgress:
                    if (GUILayout.Button("◀ To Do", EditorStyles.miniButtonLeft, GUILayout.Width(55)))
                    {
                        Undo.RecordObject(m_Database, "Move Task to To Do");
                        task.Status = TaskStatus.ToDo;
                        m_Database.MarkDirty();
                    }
                    if (GUILayout.Button("🛑 Block", EditorStyles.miniButtonMid, GUILayout.Width(55)))
                    {
                        Undo.RecordObject(m_Database, "Move Task to Blocked");
                        task.Status = TaskStatus.Blocked;
                        m_Database.MarkDirty();
                    }
                    GUI.backgroundColor = new Color(0.35f, 0.75f, 0.45f);
                    if (GUILayout.Button("✓ Done ▶", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true)))
                    {
                        Undo.RecordObject(m_Database, "Move Task to Done");
                        task.Status = TaskStatus.Done;
                        m_Database.MarkDirty();
                    }
                    GUI.backgroundColor = Color.white;
                    break;

                case TaskStatus.Blocked:
                    if (GUILayout.Button("◀ To Do", EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(true)))
                    {
                        Undo.RecordObject(m_Database, "Unblock Task to To Do");
                        task.Status = TaskStatus.ToDo;
                        m_Database.MarkDirty();
                    }
                    if (GUILayout.Button("⚡ Resume ▶", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true)))
                    {
                        Undo.RecordObject(m_Database, "Resume Task");
                        task.Status = TaskStatus.InProgress;
                        m_Database.MarkDirty();
                    }
                    break;

                case TaskStatus.Done:
                    if (GUILayout.Button("◀ Reopen", EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
                    {
                        Undo.RecordObject(m_Database, "Reopen Task");
                        task.Status = TaskStatus.InProgress;
                        m_Database.MarkDirty();
                    }
                    break;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawExpandedCardDetails(TaskItem task)
        {
            GUILayout.Space(4);
            TaskNotesStyles.DrawSplitter();
            GUILayout.Space(4);

            // Title Edit
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Title:", GUILayout.Width(45));
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
            EditorGUILayout.LabelField("Priority:", GUILayout.Width(50));
            var newPriority = (TaskPriority)EditorGUILayout.EnumPopup(task.Priority, GUILayout.Width(75));
            if (newPriority != task.Priority)
            {
                Undo.RecordObject(m_Database, "Edit Task Priority");
                task.Priority = newPriority;
                m_Database.MarkDirty();
            }

            EditorGUILayout.LabelField("Cat:", GUILayout.Width(30));
            var categories = m_Database.CustomCategories.ToArray();
            int catIdx = Mathf.Max(0, Array.IndexOf(categories, task.Category));
            int newCatIdx = EditorGUILayout.Popup(catIdx, categories);
            if (newCatIdx >= 0 && newCatIdx < categories.Length && categories[newCatIdx] != task.Category)
            {
                Undo.RecordObject(m_Database, "Edit Task Category");
                task.Category = categories[newCatIdx];
                m_Database.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // Linked Object
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Asset:", GUILayout.Width(45));
            var newObj = EditorGUILayout.ObjectField(task.LinkedObject, typeof(Object), true);
            if (newObj != task.LinkedObject)
            {
                Undo.RecordObject(m_Database, "Edit Task Linked Object");
                task.LinkedObject = newObj;
                m_Database.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // Description
            EditorGUILayout.LabelField("Notes:", EditorStyles.miniBoldLabel);
            string newDesc = EditorGUILayout.TextArea(task.Description, TaskNotesStyles.RichTextAreaStyle, GUILayout.MinHeight(40));
            if (newDesc != task.Description)
            {
                Undo.RecordObject(m_Database, "Edit Task Description");
                task.Description = newDesc;
                m_Database.MarkDirty();
            }

            // Delete
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                m_TaskToDelete = task;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ShowCardContextMenu(TaskItem task)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Move to/📌 To Do"), task.Status == TaskStatus.ToDo, () =>
            {
                Undo.RecordObject(m_Database, "Change Task Status");
                task.Status = TaskStatus.ToDo;
                m_Database.MarkDirty();
            });
            menu.AddItem(new GUIContent("Move to/⚡ In Progress"), task.Status == TaskStatus.InProgress, () =>
            {
                Undo.RecordObject(m_Database, "Change Task Status");
                task.Status = TaskStatus.InProgress;
                m_Database.MarkDirty();
            });
            menu.AddItem(new GUIContent("Move to/🛑 Blocked"), task.Status == TaskStatus.Blocked, () =>
            {
                Undo.RecordObject(m_Database, "Change Task Status");
                task.Status = TaskStatus.Blocked;
                m_Database.MarkDirty();
            });
            menu.AddItem(new GUIContent("Move to/✅ Done"), task.Status == TaskStatus.Done, () =>
            {
                Undo.RecordObject(m_Database, "Change Task Status");
                task.Status = TaskStatus.Done;
                m_Database.MarkDirty();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Duplicate"), false, () =>
            {
                var clone = task.Clone();
                m_Database.AddTask(clone);
            });
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                m_TaskToDelete = task;
            });
            menu.ShowAsContext();
        }

        private List<TaskItem> FilterTasks(List<TaskItem> tasks, string searchText, string categoryFilter, int priorityFilterIndex)
        {
            var list = new List<TaskItem>(tasks);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string query = searchText.Trim().ToLowerInvariant();
                list = list.FindAll(t =>
                    t.Title.ToLowerInvariant().Contains(query) ||
                    t.Description.ToLowerInvariant().Contains(query) ||
                    t.Category.ToLowerInvariant().Contains(query));
            }

            if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "All")
            {
                list = list.FindAll(t => t.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (priorityFilterIndex > 0)
            {
                var targetPriority = (TaskPriority)(priorityFilterIndex - 1);
                list = list.FindAll(t => t.Priority == targetPriority);
            }

            return list;
        }
    }
}
