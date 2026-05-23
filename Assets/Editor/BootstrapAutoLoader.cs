#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class BootstrapAutoLoader
{
    const string BootstrapPath = "Assets/Scenes/_Bootstrap.unity";

    static BootstrapAutoLoader()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Only run when we're about to enter play mode
        if (state != PlayModeStateChange.ExitingEditMode) return;

        // Check if bootstrap is already loaded — if so, do nothing
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            if (EditorSceneManager.GetSceneAt(i).path == BootstrapPath)
                return;
        }

        // Save current scenes (so we don't lose unsaved changes)
        EditorSceneManager.SaveOpenScenes();

        // Load bootstrap additively underneath whatever scene is open
        EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Additive);
    }
}
#endif