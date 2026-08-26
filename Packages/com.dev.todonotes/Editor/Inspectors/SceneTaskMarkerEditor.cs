using UnityEditor;
using UnityEngine;
using Dev.TodoNotes;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Custom Inspector for SceneTaskMarker component.
    /// </summary>
    [CustomEditor(typeof(SceneTaskMarker))]
    public class SceneTaskMarkerEditor : UnityEditor.Editor
    {
        private SerializedProperty m_TitleProp;
        private SerializedProperty m_DescriptionProp;
        private SerializedProperty m_PriorityProp;
        private SerializedProperty m_IsCompletedProp;
        private SerializedProperty m_CategoryProp;
        private SerializedProperty m_ShowInSceneProp;
        private SerializedProperty m_CustomColorProp;
        private SerializedProperty m_IconScaleProp;

        private void OnEnable()
        {
            m_TitleProp = serializedObject.FindProperty("m_Title");
            m_DescriptionProp = serializedObject.FindProperty("m_Description");
            m_PriorityProp = serializedObject.FindProperty("m_Priority");
            m_IsCompletedProp = serializedObject.FindProperty("m_IsCompleted");
            m_CategoryProp = serializedObject.FindProperty("m_Category");
            m_ShowInSceneProp = serializedObject.FindProperty("m_ShowInScene");
            m_CustomColorProp = serializedObject.FindProperty("m_CustomColor");
            m_IconScaleProp = serializedObject.FindProperty("m_IconScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var marker = (SceneTaskMarker)target;

            // Status Banner
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            bool completed = m_IsCompletedProp.boolValue;
            GUI.backgroundColor = completed ? new Color(0.35f, 0.75f, 0.45f) : new Color(0.9f, 0.5f, 0.2f);
            if (GUILayout.Button(completed ? "✓ Completed" : "○ Incomplete (Click to complete)", GUILayout.Height(26)))
            {
                m_IsCompletedProp.boolValue = !completed;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // Properties
            EditorGUILayout.PropertyField(m_TitleProp);
            EditorGUILayout.PropertyField(m_PriorityProp);
            EditorGUILayout.PropertyField(m_CategoryProp);
            EditorGUILayout.PropertyField(m_DescriptionProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Scene Visualization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ShowInSceneProp);
            EditorGUILayout.PropertyField(m_CustomColorProp);
            EditorGUILayout.PropertyField(m_IconScaleProp);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            TaskNotesStyles.DrawSplitter();
            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Task Manager", GUILayout.Height(24)))
            {
                TaskNotesWindow.OpenWindow();
            }

            if (GUILayout.Button("+ Copy to Global Tasks", GUILayout.Height(24)))
            {
                var db = TaskNotesDatabase.GetOrCreateDatabase();
                var task = new TaskItem(marker.Title, marker.Priority, marker.Category)
                {
                    Description = marker.Description,
                    LinkedObject = marker.gameObject,
                    IsDone = marker.IsCompleted
                };
                db.AddTask(task);
                EditorUtility.DisplayDialog("Task Added", $"Added '{marker.Title}' to project tasks database!", "OK");
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
