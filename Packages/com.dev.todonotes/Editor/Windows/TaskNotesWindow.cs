using UnityEditor;
using UnityEngine;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Main Editor Window for Tasks, Notes, Scratchpad, and Scene Pins.
    /// Access via Tools > Task & Notes Manager or Window > General > Task & Notes Manager (Ctrl+Alt+T / Cmd+Alt+T).
    /// </summary>
    public class TaskNotesWindow : EditorWindow
    {
        private static readonly string[] k_TabNames = { "📋 Tasks", "📝 Notes", "⚡ Scratchpad", "📌 Scene Pins", "⚙️ Settings" };

        [SerializeField] private int m_SelectedTab = 0;

        private TaskNotesDatabase m_Database;
        private TaskListView m_TaskListView;
        private NoteListView m_NoteListView;
        private ScratchpadView m_ScratchpadView;
        private ScenePinsView m_ScenePinsView;
        private SettingsView m_SettingsView;

        [MenuItem("Tools/Task & Notes Manager %&t", priority = 100)]
        [MenuItem("Window/General/Task & Notes Manager", priority = 20)]
        public static void OpenWindow()
        {
            var window = GetWindow<TaskNotesWindow>("Tasks & Notes", true, typeof(EditorWindow));
            window.minSize = new Vector2(480, 360);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Tasks & Notes", EditorGUIUtility.IconContent("d_CustomTool").image);
            InitializeViews();

            Undo.undoRedoPerformed += OnUndoRedo;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnUndoRedo()
        {
            Repaint();
        }

        private void OnSelectionChanged()
        {
            if (m_SelectedTab == 3 && m_ScenePinsView != null)
            {
                m_ScenePinsView.RefreshMarkers();
                Repaint();
            }
        }

        private void InitializeViews()
        {
            if (m_Database == null)
            {
                m_Database = TaskNotesDatabase.GetOrCreateDatabase();
            }

            m_TaskListView = new TaskListView(m_Database, this);
            m_NoteListView = new NoteListView(m_Database, this);
            m_ScratchpadView = new ScratchpadView(m_Database, this);
            m_ScenePinsView = new ScenePinsView(m_Database, this);
            m_SettingsView = new SettingsView(m_Database, this);
        }

        private void OnGUI()
        {
            if (m_Database == null)
            {
                m_Database = TaskNotesDatabase.GetOrCreateDatabase();
                InitializeViews();
            }

            DrawHeaderTabBar();
            EditorGUILayout.Space(4);

            switch (m_SelectedTab)
            {
                case 0:
                    m_TaskListView?.Draw();
                    break;
                case 1:
                    m_NoteListView?.Draw();
                    break;
                case 2:
                    m_ScratchpadView?.Draw();
                    break;
                case 3:
                    m_ScenePinsView?.Draw();
                    break;
                case 4:
                    m_SettingsView?.Draw();
                    break;
            }
        }

        private void DrawHeaderTabBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            m_SelectedTab = GUILayout.Toolbar(m_SelectedTab, k_TabNames, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
        }
    }
}
