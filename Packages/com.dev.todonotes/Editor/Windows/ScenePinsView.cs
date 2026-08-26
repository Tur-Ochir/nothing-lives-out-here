using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Dev.TodoNotes;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// View renderer for the In-Scene Sticky Pins tab.
    /// Lists, manages, and navigates to 3D task pins placed directly in active scenes.
    /// </summary>
    public class ScenePinsView
    {
        private readonly TaskNotesDatabase m_Database;
        private readonly EditorWindow m_ParentWindow;
        private Vector2 m_ScrollPos;

        private List<SceneTaskMarker> m_CachedMarkers = new List<SceneTaskMarker>();

        public ScenePinsView(TaskNotesDatabase database, EditorWindow parentWindow)
        {
            m_Database = database;
            m_ParentWindow = parentWindow;
            RefreshMarkers();
        }

        public void RefreshMarkers()
        {
            m_CachedMarkers = Object.FindObjectsByType<SceneTaskMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        }

        public void Draw()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            // Clean up nulls
            m_CachedMarkers.RemoveAll(m => m == null);

            if (m_CachedMarkers.Count == 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Space(20);
                GUILayout.Label("📍 No Scene Task Markers found in the active scene.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(6);
                if (GUILayout.Button("+ Place Sticky Note Pin at Scene View Center", GUILayout.Height(26)))
                {
                    CreateMarkerAtSceneView();
                }
                GUILayout.Space(20);
                EditorGUILayout.EndVertical();
            }
            else
            {
                for (int i = 0; i < m_CachedMarkers.Count; i++)
                {
                    var marker = m_CachedMarkers[i];
                    if (marker == null) continue;
                    DrawMarkerCard(marker);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label($"📌 Scene Task Markers ({m_CachedMarkers.Count})", EditorStyles.miniBoldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(65)))
            {
                RefreshMarkers();
            }

            if (GUILayout.Button("+ Add Scene Pin", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                CreateMarkerAtSceneView();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMarkerCard(SceneTaskMarker marker)
        {
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = marker.IsCompleted ?
                (EditorGUIUtility.isProSkin ? TaskNotesStyles.CardCompletedDark : TaskNotesStyles.CardCompletedLight) :
                (EditorGUIUtility.isProSkin ? TaskNotesStyles.CardBgDark : TaskNotesStyles.CardBgLight);

            EditorGUILayout.BeginVertical(TaskNotesStyles.CardStyle);
            GUI.backgroundColor = prevBg;

            EditorGUILayout.BeginHorizontal();

            // Checkbox
            bool newDone = EditorGUILayout.Toggle(marker.IsCompleted, GUILayout.Width(20));
            if (newDone != marker.IsCompleted)
            {
                Undo.RecordObject(marker, "Toggle Marker Completion");
                marker.IsCompleted = newDone;
                EditorUtility.SetDirty(marker);
            }

            // Priority dot
            Rect priorityRect = GUILayoutUtility.GetRect(12, 18, GUILayout.Width(12));
            priorityRect.y += 3;
            priorityRect.height = 12;
            EditorGUI.DrawRect(priorityRect, marker.GetPriorityColor());

            // Title
            var titleStyle = marker.IsCompleted ? TaskNotesStyles.TitleDoneStyle : TaskNotesStyles.TitleStyle;
            GUILayout.Label(string.IsNullOrWhiteSpace(marker.Title) ? marker.gameObject.name : marker.Title, titleStyle, GUILayout.ExpandWidth(true));

            // Category badge
            Rect catRect = GUILayoutUtility.GetRect(new GUIContent(marker.Category), TaskNotesStyles.BadgeStyle, GUILayout.ExpandWidth(false));
            TaskNotesStyles.DrawBadge(catRect, marker.Category, new Color(0.35f, 0.4f, 0.45f, 0.8f));

            GUILayout.Space(4);

            // Frame / Focus Button
            if (GUILayout.Button("Focus", EditorStyles.miniButtonLeft, GUILayout.Width(50), GUILayout.Height(18)))
            {
                Selection.activeGameObject = marker.gameObject;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }

            // Select button
            if (GUILayout.Button("Select", EditorStyles.miniButtonRight, GUILayout.Width(50), GUILayout.Height(18)))
            {
                Selection.activeGameObject = marker.gameObject;
                EditorGUIUtility.PingObject(marker.gameObject);
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(marker.Description))
            {
                GUILayout.Space(2);
                EditorGUILayout.LabelField(marker.Description, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Object: {marker.gameObject.name} | Pos: {marker.transform.position:F1}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void CreateMarkerAtSceneView()
        {
            Vector3 spawnPos = Vector3.zero;
            if (SceneView.lastActiveSceneView != null)
            {
                spawnPos = SceneView.lastActiveSceneView.pivot;
            }

            var go = new GameObject("SceneTaskPin");
            go.transform.position = spawnPos;
            var marker = go.AddComponent<SceneTaskMarker>();
            marker.Title = "New Scene Note";
            marker.Category = "Level Design";

            Undo.RegisterCreatedObjectUndo(go, "Create Scene Task Pin");
            Selection.activeGameObject = go;
            RefreshMarkers();
        }
    }
}
