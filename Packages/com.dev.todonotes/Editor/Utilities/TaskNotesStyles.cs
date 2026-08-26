using UnityEditor;
using UnityEngine;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// UI styling, colors, and drawing utilities for Task & Notes Editor Window.
    /// </summary>
    public static class TaskNotesStyles
    {
        // Colors
        public static readonly Color UrgentColor = new Color(0.92f, 0.25f, 0.25f);
        public static readonly Color HighColor = new Color(0.96f, 0.55f, 0.15f);
        public static readonly Color MediumColor = new Color(0.25f, 0.65f, 0.95f);
        public static readonly Color LowColor = new Color(0.55f, 0.60f, 0.65f);

        public static readonly Color DoneColor = new Color(0.35f, 0.75f, 0.45f);
        public static readonly Color InProgressColor = new Color(0.20f, 0.65f, 0.95f);
        public static readonly Color BlockedColor = new Color(0.85f, 0.25f, 0.25f);
        public static readonly Color ToDoColor = new Color(0.60f, 0.60f, 0.65f);

        public static readonly Color CardBgDark = new Color(0.22f, 0.22f, 0.24f, 0.9f);
        public static readonly Color CardBgLight = new Color(0.85f, 0.85f, 0.87f, 0.9f);

        public static readonly Color CardCompletedDark = new Color(0.18f, 0.20f, 0.19f, 0.6f);
        public static readonly Color CardCompletedLight = new Color(0.88f, 0.91f, 0.88f, 0.6f);

        public static readonly Color ToolbarBg = EditorGUIUtility.isProSkin ? new Color(0.16f, 0.16f, 0.18f) : new Color(0.75f, 0.75f, 0.77f);
        public static readonly Color AccentColor = new Color(0.3f, 0.6f, 1f);

        private static GUIStyle s_CardStyle;
        private static GUIStyle s_CardCompletedStyle;
        private static GUIStyle s_TitleStyle;
        private static GUIStyle s_TitleDoneStyle;
        private static GUIStyle s_BadgeStyle;
        private static GUIStyle s_TabButtonStyle;
        private static GUIStyle s_HeaderTitleStyle;
        private static GUIStyle s_SubHeaderStyle;
        private static GUIStyle s_RichTextAreaStyle;
        private static GUIStyle s_SearchFieldStyle;

        private static Texture2D s_WhiteTexture;

        public static Texture2D WhiteTexture
        {
            get
            {
                if (s_WhiteTexture == null)
                {
                    s_WhiteTexture = new Texture2D(1, 1);
                    s_WhiteTexture.SetPixel(0, 0, Color.white);
                    s_WhiteTexture.Apply();
                }
                return s_WhiteTexture;
            }
        }

        public static GUIStyle CardStyle
        {
            get
            {
                if (s_CardStyle == null)
                {
                    s_CardStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(10, 10, 8, 8),
                        margin = new RectOffset(4, 4, 3, 3)
                    };
                }
                return s_CardStyle;
            }
        }

        public static GUIStyle TitleStyle
        {
            get
            {
                if (s_TitleStyle == null)
                {
                    s_TitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 13,
                        wordWrap = true
                    };
                }
                return s_TitleStyle;
            }
        }

        public static GUIStyle TitleDoneStyle
        {
            get
            {
                if (s_TitleDoneStyle == null)
                {
                    s_TitleDoneStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Italic,
                        wordWrap = true
                    };
                    s_TitleDoneStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.45f, 0.45f, 0.45f);
                }
                return s_TitleDoneStyle;
            }
        }

        public static GUIStyle BadgeStyle
        {
            get
            {
                if (s_BadgeStyle == null)
                {
                    s_BadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = 10,
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    s_BadgeStyle.normal.textColor = Color.white;
                }
                return s_BadgeStyle;
            }
        }

        public static GUIStyle HeaderTitleStyle
        {
            get
            {
                if (s_HeaderTitleStyle == null)
                {
                    s_HeaderTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 15,
                        alignment = TextAnchor.MiddleLeft
                    };
                }
                return s_HeaderTitleStyle;
            }
        }

        public static GUIStyle SubHeaderStyle
        {
            get
            {
                if (s_SubHeaderStyle == null)
                {
                    s_SubHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft
                    };
                    s_SubHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.3f, 0.3f, 0.3f);
                }
                return s_SubHeaderStyle;
            }
        }

        public static GUIStyle RichTextAreaStyle
        {
            get
            {
                if (s_RichTextAreaStyle == null)
                {
                    s_RichTextAreaStyle = new GUIStyle(EditorStyles.textArea)
                    {
                        wordWrap = true,
                        fontSize = 12,
                        padding = new RectOffset(8, 8, 8, 8)
                    };
                }
                return s_RichTextAreaStyle;
            }
        }

        public static Color GetPriorityColor(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Urgent => UrgentColor,
                TaskPriority.High => HighColor,
                TaskPriority.Medium => MediumColor,
                TaskPriority.Low => LowColor,
                _ => LowColor
            };
        }

        public static Color GetStatusColor(TaskStatus status)
        {
            return status switch
            {
                TaskStatus.Done => DoneColor,
                TaskStatus.InProgress => InProgressColor,
                TaskStatus.Blocked => BlockedColor,
                TaskStatus.ToDo => ToDoColor,
                _ => ToDoColor
            };
        }

        public static Color GetNoteTagColor(NoteColorTag tag)
        {
            return tag switch
            {
                NoteColorTag.Blue => new Color(0.25f, 0.65f, 0.95f),
                NoteColorTag.Green => new Color(0.35f, 0.75f, 0.45f),
                NoteColorTag.Yellow => new Color(0.95f, 0.75f, 0.2f),
                NoteColorTag.Orange => new Color(0.96f, 0.55f, 0.15f),
                NoteColorTag.Red => new Color(0.92f, 0.25f, 0.25f),
                NoteColorTag.Purple => new Color(0.7f, 0.4f, 0.9f),
                NoteColorTag.Teal => new Color(0.15f, 0.75f, 0.75f),
                _ => EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.55f) : new Color(0.4f, 0.4f, 0.45f)
            };
        }

        public static void DrawBadge(Rect rect, string text, Color backgroundColor)
        {
            var prevColor = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(rect, WhiteTexture, ScaleMode.StretchToFill, true, 0, backgroundColor, 0, 4f);
            GUI.color = prevColor;

            GUI.Label(rect, text, BadgeStyle);
        }

        public static void DrawProgressBar(float progress, string label, float height = 18f)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            DrawProgressBar(rect, progress, label);
        }

        public static void DrawProgressBar(Rect rect, float progress, string label)
        {
            progress = Mathf.Clamp01(progress);

            // Background
            Color bgColor = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.20f) : new Color(0.78f, 0.78f, 0.80f);
            EditorGUI.DrawRect(rect, bgColor);

            // Fill
            if (progress > 0.001f)
            {
                Rect fillRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
                Color fillColor = Color.Lerp(new Color(0.2f, 0.6f, 0.9f), DoneColor, progress);
                EditorGUI.DrawRect(fillRect, fillColor);
            }

            // Outline
            Color borderColor = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.14f) : new Color(0.6f, 0.6f, 0.65f);
            Handles.color = borderColor;
            Handles.DrawLines(new Vector3[]
            {
                new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin),
                new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, rect.yMax),
                new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax),
                new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMin, rect.yMin)
            });

            // Text Label
            var textStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };
            textStyle.normal.textColor = Color.white;
            GUI.Label(rect, label, textStyle);
        }

        public static void DrawSplitter(float thickness = 1f)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(thickness), GUILayout.ExpandWidth(true));
            Color splitterColor = EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.17f) : new Color(0.70f, 0.70f, 0.72f);
            EditorGUI.DrawRect(rect, splitterColor);
        }
    }
}
