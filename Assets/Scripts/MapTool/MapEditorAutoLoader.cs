#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the _MapEditor scene loaded additively at all times in the editor.
///
/// Hooks into scene-open events: whenever any scene opens, this script checks
/// whether _MapEditor is already among the loaded scenes. If not, it loads it
/// additively underneath. Result: the RF Map window always has its buttons.
///
/// To disable: comment out the [InitializeOnLoad] attribute, or delete this file.
/// </summary>
[InitializeOnLoad]
public static class MapEditorAutoLoader
{
    // Path to the _MapEditor scene that holds the RoomMapButton GameObjects.
    // Must match the path in MapEditorWindow.cs.
    const string MapEditorScenePath = "Assets/Scenes/_MapEditor.unity";

    // Toggle this off to disable the autoloader without deleting the file.
    // (Stored in EditorPrefs so it persists across editor sessions.)
    const string EnabledPrefKey = "RFMap_AutoLoaderEnabled";

    static MapEditorAutoLoader()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        // Run once on editor load too, in case the editor opened with no _MapEditor loaded
        EditorApplication.delayCall += EnsureLoaded;
    }

    static bool IsEnabled
    {
        get => EditorPrefs.GetBool(EnabledPrefKey, true);
    }

    [MenuItem("Tools/Rail Fighter/Toggle Map Editor Auto-Load")]
    public static void Toggle()
    {
        bool now = !IsEnabled;
        EditorPrefs.SetBool(EnabledPrefKey, now);
        UnityEngine.Debug.Log($"[RF Map] Auto-loader {(now ? "ENABLED" : "DISABLED")}");
        if (now) EnsureLoaded();
    }

    [MenuItem("Tools/Rail Fighter/Toggle Map Editor Auto-Load", true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked("Tools/Rail Fighter/Toggle Map Editor Auto-Load", IsEnabled);
        return true;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!IsEnabled) return;
        // Ignore the case where _MapEditor itself was just opened
        if (scene.path == MapEditorScenePath) return;
        EnsureLoaded();
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // When entering play mode, _MapEditor would just sit there as dead weight in
        // the running game. Unload it before play, reload it after returning to edit.
        if (!IsEnabled) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            UnloadMapEditorScene();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EnsureLoaded();
        }
    }

    static void EnsureLoaded()
    {
        if (!IsEnabled) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Already loaded? bail
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            if (EditorSceneManager.GetSceneAt(i).path == MapEditorScenePath)
                return;
        }

        // File exists? load it additively
        if (System.IO.File.Exists(MapEditorScenePath))
        {
            EditorSceneManager.OpenScene(MapEditorScenePath, OpenSceneMode.Additive);
        }
        // Don't warn here  if the file genuinely doesn't exist (e.g. fresh project),
        // we don't want to spam logs every scene open. The MapEditorWindow already warns.
    }

    static void UnloadMapEditorScene()
    {
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var s = EditorSceneManager.GetSceneAt(i);
            if (s.path == MapEditorScenePath)
            {
                EditorSceneManager.CloseScene(s, true);
                return;
            }
        }
    }
}
#endif