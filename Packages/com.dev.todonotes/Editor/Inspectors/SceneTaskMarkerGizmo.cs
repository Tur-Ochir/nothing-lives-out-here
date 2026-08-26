using UnityEditor;
using UnityEngine;
using Dev.TodoNotes;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Gizmo and SceneView renderer for SceneTaskMarker components.
    /// Draws floating in-scene sticky pins and text labels in 3D world space.
    /// </summary>
    public static class SceneTaskMarkerGizmo
    {
        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        private static void DrawMarkerGizmo(SceneTaskMarker marker, GizmoType gizmoType)
        {
            if (marker == null || !marker.ShowInScene) return;

            Vector3 pos = marker.transform.position;
            Color markerColor = marker.GetPriorityColor();
            float scale = marker.IconScale;

            // Draw 3D wire sphere / disc pin
            Gizmos.color = markerColor;
            Gizmos.DrawWireSphere(pos, 0.35f * scale);
            Gizmos.DrawSphere(pos, 0.12f * scale);

            // Draw SceneView text label
            if (SceneView.currentDrawingSceneView != null)
            {
                Camera cam = SceneView.currentDrawingSceneView.camera;
                if (cam != null)
                {
                    float distance = Vector3.Distance(cam.transform.position, pos);
                    if (distance < 50f) // Only draw label if close enough to camera
                    {
                        var style = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 11
                        };
                        style.normal.textColor = markerColor;

                        string check = marker.IsCompleted ? "✓ " : "📌 ";
                        string labelText = $"{check}{marker.Title}\n[{marker.Category}]";

                        Handles.Label(pos + Vector3.up * (0.6f * scale), labelText, style);
                    }
                }
            }
        }
    }
}
