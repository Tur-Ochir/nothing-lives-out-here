using System;
using UnityEditor;
using UnityEngine;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Reusable GUI component for rendering and managing a Task's checklist / subtasks.
    /// </summary>
    public static class TaskChecklistDrawer
    {
        private static GUIStyle s_ItemDoneStyle;
        private static GUIStyle s_ItemActiveStyle;

        private static GUIStyle ItemDoneStyle
        {
            get
            {
                if (s_ItemDoneStyle == null)
                {
                    s_ItemDoneStyle = new GUIStyle(EditorStyles.textField)
                    {
                        fontStyle = FontStyle.Italic
                    };
                    s_ItemDoneStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.45f, 0.45f, 0.45f);
                }
                return s_ItemDoneStyle;
            }
        }

        private static GUIStyle ItemActiveStyle
        {
            get
            {
                if (s_ItemActiveStyle == null)
                {
                    s_ItemActiveStyle = new GUIStyle(EditorStyles.textField);
                }
                return s_ItemActiveStyle;
            }
        }

        /// <summary>
        /// Draws a compact checklist progress badge (e.g. "☑ 2/5") for card summaries.
        /// </summary>
        public static void DrawChecklistBadge(TaskItem task)
        {
            if (task == null || !task.HasChecklist) return;

            int done = task.ChecklistDoneCount;
            int total = task.ChecklistTotalCount;
            string text = $"{done}/{total}";
            Color badgeBg = (done == total) ?
                new Color(0.25f, 0.65f, 0.35f, 0.85f) :
                new Color(0.25f, 0.45f, 0.65f, 0.85f);

            GUIContent content = new GUIContent($"☑ {text}");
            Rect badgeRect = GUILayoutUtility.GetRect(content, TaskNotesStyles.BadgeStyle, GUILayout.ExpandWidth(false));
            TaskNotesStyles.DrawBadge(badgeRect, $"☑ {text}", badgeBg);
        }

        /// <summary>
        /// Draws the complete checklist section inside an expanded task card / details view.
        /// </summary>
        public static void DrawChecklistSection(TaskItem task, TaskNotesDatabase database, ref string newChecklistInputText, string controlIdPrefix = "Checklist_")
        {
            if (task == null || database == null) return;

            var checklist = task.Checklist;
            int total = checklist.Count;
            int done = task.ChecklistDoneCount;

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row with progress
            EditorGUILayout.BeginHorizontal();
            string headerText = total > 0 ? $"☑ Checklist ({done}/{total})" : "☑ Checklist";
            EditorGUILayout.LabelField(headerText, EditorStyles.miniBoldLabel);

            if (total > 0)
            {
                float progress = (float)done / total;
                // Mini progress text
                EditorGUILayout.LabelField($"{progress * 100f:0}%", EditorStyles.miniLabel, GUILayout.Width(35));
            }
            EditorGUILayout.EndHorizontal();

            // Progress bar if items exist
            if (total > 0)
            {
                float progress = (float)done / total;
                string progLabel = $"{done} of {total} completed";
                TaskNotesStyles.DrawProgressBar(progress, progLabel, 12f);
                EditorGUILayout.Space(2);
            }

            // Checklist items list
            int itemToDelete = -1;
            int swapA = -1;
            int swapB = -1;

            for (int i = 0; i < checklist.Count; i++)
            {
                var item = checklist[i];
                if (item == null) continue;

                EditorGUILayout.BeginHorizontal();

                // 1. Completion Toggle
                bool newDone = EditorGUILayout.Toggle(item.IsDone, GUILayout.Width(18));
                if (newDone != item.IsDone)
                {
                    Undo.RecordObject(database, "Toggle Checklist Item");
                    item.IsDone = newDone;
                    database.MarkDirty();
                }

                // 2. Text Input
                GUIStyle textStyle = item.IsDone ? ItemDoneStyle : ItemActiveStyle;
                string newText = EditorGUILayout.TextField(item.Text, textStyle, GUILayout.ExpandWidth(true));
                if (newText != item.Text)
                {
                    Undo.RecordObject(database, "Edit Checklist Item");
                    item.Text = newText;
                    database.MarkDirty();
                }

                // 3. Move Up / Down
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(18), GUILayout.Height(18)))
                {
                    swapA = i;
                    swapB = i - 1;
                }

                GUI.enabled = i < checklist.Count - 1;
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(18), GUILayout.Height(18)))
                {
                    swapA = i;
                    swapB = i + 1;
                }
                GUI.enabled = true;

                // 4. Delete item
                if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(20), GUILayout.Height(18)))
                {
                    itemToDelete = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Apply swaps or deletions
            if (swapA >= 0 && swapB >= 0 && swapA < checklist.Count && swapB < checklist.Count)
            {
                Undo.RecordObject(database, "Reorder Checklist Item");
                var temp = checklist[swapA];
                checklist[swapA] = checklist[swapB];
                checklist[swapB] = temp;
                database.MarkDirty();
            }

            if (itemToDelete >= 0 && itemToDelete < checklist.Count)
            {
                Undo.RecordObject(database, "Delete Checklist Item");
                checklist.RemoveAt(itemToDelete);
                database.MarkDirty();
                GUIUtility.ExitGUI();
            }

            // Quick Add Row
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();

            string controlName = $"{controlIdPrefix}_{task.Id}";
            GUI.SetNextControlName(controlName);
            newChecklistInputText = EditorGUILayout.TextField(newChecklistInputText, EditorStyles.textField, GUILayout.Height(20));

            // Show placeholder if empty
            if (string.IsNullOrEmpty(newChecklistInputText) && GUI.GetNameOfFocusedControl() != controlName)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                var placeholderStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Italic
                };
                placeholderStyle.normal.textColor = Color.gray;
                GUI.Label(new Rect(lastRect.x + 4, lastRect.y + 1, lastRect.width, lastRect.height), "Add checklist item... (Enter)", placeholderStyle);
            }

            bool enterPressed = Event.current.type == EventType.KeyDown &&
                               Event.current.keyCode == KeyCode.Return &&
                               GUI.GetNameOfFocusedControl() == controlName;

            if (GUILayout.Button("+ Add", EditorStyles.miniButton, GUILayout.Width(50), GUILayout.Height(20)) || enterPressed)
            {
                if (!string.IsNullOrWhiteSpace(newChecklistInputText))
                {
                    Undo.RecordObject(database, "Add Checklist Item");
                    task.Checklist.Add(new ChecklistItem(newChecklistInputText.Trim(), false));
                    database.MarkDirty();
                    newChecklistInputText = "";
                    GUI.FocusControl(controlName);
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
