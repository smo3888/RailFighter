#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-shot helper. Run this once via the menu to create 12 starter
/// RoomMapButton GameObjects in the currently open scene, parented under
/// a "MapButtons" container so the Hierarchy stays tidy.
///
/// After running, you'll position/resize each one to match its room on the map.
/// </summary>
public static class RoomMapButtonSpawner
{
    [MenuItem("Tools/Rail Fighter/Spawn 12 Map Buttons")]
    public static void Spawn12()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            EditorUtility.DisplayDialog("No Scene", "Open the map editor scene first.", "OK");
            return;
        }

        // Find or create container
        GameObject container = GameObject.Find("MapButtons");
        if (container == null)
        {
            container = new GameObject("MapButtons");
            Undo.RegisterCreatedObjectUndo(container, "Create MapButtons");
        }

        // Spread the 12 buttons in a 4x3 grid as starting positions so they
        // don't overlap. You'll move each one onto its real room afterwards.
        int created = 0;
        for (int i = 0; i < 12; i++)
        {
            string name = $"Room {i + 1:00}";
            // Skip if a child with this name already exists
            if (container.transform.Find(name) != null) continue;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create RoomMapButton");
            go.transform.SetParent(container.transform, false);

            var btn = go.AddComponent<RoomMapButton>();
            btn.roomName = name;
            btn.width = 200f;
            btn.height = 200f;
            // Lay them out in a 4x3 grid in the top-left corner of the map
            int col = i % 4;
            int row = i / 4;
            btn.x = 50f + col * 220f;
            btn.y = 50f + row * 220f;
            btn.gizmoColor = Color.HSVToRGB((i / 12f), 0.7f, 1f);
            // Force OnValidate to run so transform mirrors the rect
            EditorUtility.SetDirty(btn);

            created++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Created {created} room buttons under 'MapButtons'.");
    }
}
#endif