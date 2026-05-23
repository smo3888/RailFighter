#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapEditorWindow : EditorWindow
{
    private const string DefaultMapPath = "Assets/EditorResources/Map.png";
    // Path of the map editor scene that holds the RoomMapButton GameObjects.
    // We keep this scene loaded additively while navigating so the buttons survive.
    private const string MapEditorScenePath = "Assets/Scenes/_MapEditor.unity";

    private Texture2D mapTexture;
    private string mapTexturePath = DefaultMapPath;
    private Vector2 scrollPos;
    private float zoom = 0.5f;
    private const float MapPadding = 600f;

    [MenuItem("Window/Rail Fighter Map")]
    public static void Open()
    {
        var w = GetWindow<MapEditorWindow>("RF Map");
        w.minSize = new Vector2(400, 400);
    }

    void OnEnable()
    {
        LoadMap();
    }

    void LoadMap()
    {
        mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(mapTexturePath);
        if (mapTexture != null)
            Debug.Log($"[RF Map] Loaded {mapTexturePath}  dimensions: {mapTexture.width} x {mapTexture.height}");
        else
            Debug.LogWarning($"[RF Map] No texture found at {mapTexturePath}");
    }

    void OnGUI()
    {
        DrawToolbar();

        if (mapTexture == null)
        {
            EditorGUILayout.HelpBox($"Map image not found at:\n{mapTexturePath}", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField(
            $"Source: {mapTexture.width} x {mapTexture.height} px   |   Drawing at: {(int)(mapTexture.width * zoom)} x {(int)(mapTexture.height * zoom)} px",
            EditorStyles.miniLabel);

        var buttons = FindButtonsInOpenScenes();
        DrawMap(buttons);

        EditorGUILayout.LabelField($"{buttons.Count} room button(s) in scene  |  Middle-click + drag to pan  |  Right-click button to select GameObject", EditorStyles.miniLabel);
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Reload Map", EditorStyles.toolbarButton, GUILayout.Width(90)))
            LoadMap();

        if (GUILayout.Button("Center", EditorStyles.toolbarButton, GUILayout.Width(60)))
            CenterOnMap();

        if (GUILayout.Button("Fit Window", EditorStyles.toolbarButton, GUILayout.Width(80)))
            FitToWindow();

        if (GUILayout.Button("Open Map Editor", EditorStyles.toolbarButton, GUILayout.Width(120)))
            EnsureMapEditorSceneLoaded(true);

        GUILayout.Space(8);
        GUILayout.Label("Zoom", EditorStyles.miniLabel, GUILayout.Width(40));
        zoom = GUILayout.HorizontalSlider(zoom, 0.05f, 2.0f, GUILayout.Width(160));
        GUILayout.Label($"{(int)(zoom * 100)}%", EditorStyles.miniLabel, GUILayout.Width(40));

        GUILayout.FlexibleSpace();

        EditorGUI.BeginChangeCheck();
        var newPath = EditorGUILayout.TextField(mapTexturePath, EditorStyles.toolbarTextField, GUILayout.Width(280));
        if (EditorGUI.EndChangeCheck())
        {
            mapTexturePath = newPath;
            LoadMap();
        }

        EditorGUILayout.EndHorizontal();
    }

    void CenterOnMap()
    {
        scrollPos = new Vector2(MapPadding * 0.5f, MapPadding * 0.5f);
    }

    void FitToWindow()
    {
        float availableW = position.width - 40f;
        float availableH = position.height - 80f;
        float zoomW = availableW / mapTexture.width;
        float zoomH = availableH / mapTexture.height;
        zoom = Mathf.Clamp(Mathf.Min(zoomW, zoomH) * 0.95f, 0.05f, 2.0f);
        CenterOnMap();
    }

    void DrawMap(List<RoomMapButton> buttons)
    {
        float drawWidth = mapTexture.width * zoom;
        float drawHeight = mapTexture.height * zoom;

        float canvasWidth = drawWidth + MapPadding * 2f;
        float canvasHeight = drawHeight + MapPadding * 2f;

        scrollPos = GUILayout.BeginScrollView(scrollPos, true, true);

        Rect canvasRect = GUILayoutUtility.GetRect(
            canvasWidth, canvasHeight,
            GUILayout.Width(canvasWidth),
            GUILayout.Height(canvasHeight)
        );

        Rect mapRect = new Rect(
            canvasRect.x + MapPadding,
            canvasRect.y + MapPadding,
            drawWidth,
            drawHeight
        );

        EditorGUI.DrawRect(canvasRect, new Color(0.18f, 0.18f, 0.18f, 1f));
        EditorGUI.DrawRect(mapRect, new Color(0.1f, 0.1f, 0.1f, 1f));
        GUI.DrawTexture(mapRect, mapTexture, ScaleMode.StretchToFill);

        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            Rect overlay = new Rect(
                mapRect.x + btn.x * zoom,
                mapRect.y + btn.y * zoom,
                btn.width * zoom,
                btn.height * zoom
            );
            DrawButtonOverlay(overlay, btn);
        }

        HandlePanInput(canvasRect);

        GUILayout.EndScrollView();
    }

    void HandlePanInput(Rect canvasRect)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDrag && e.button == 2)
        {
            scrollPos -= e.delta;
            e.Use();
            Repaint();
        }
        if (e.type == EventType.MouseDrag && e.button == 0 && e.alt)
        {
            scrollPos -= e.delta;
            e.Use();
            Repaint();
        }
    }

    void DrawButtonOverlay(Rect overlay, RoomMapButton btn)
    {
        Color fill = btn.gizmoColor;
        fill.a = 0.35f;
        EditorGUI.DrawRect(overlay, fill);

        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(overlay, new Color(0, 0, 0, 0), btn.gizmoColor);
        Handles.EndGUI();

        GUIStyle invisible = new GUIStyle(GUI.skin.label);
        invisible.normal.background = null;

        if (GUI.Button(overlay, GUIContent.none, invisible))
            HandleButtonClick(btn);

        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(overlay, btn.roomName, labelStyle);
    }

    void HandleButtonClick(RoomMapButton btn)
    {
        if (Event.current.button == 1)
        {
            Selection.activeGameObject = btn.gameObject;
            EditorGUIUtility.PingObject(btn.gameObject);
            return;
        }

        string scenePath = ResolveScenePath(btn);
        if (string.IsNullOrEmpty(scenePath))
        {
            EditorUtility.DisplayDialog(
                "No Scene Assigned",
                $"\"{btn.roomName}\" doesn't have a target scene yet.",
                "OK");
            Selection.activeGameObject = btn.gameObject;
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // Open the target room as the primary scene (replaces current gameplay scene)
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Then re-load the map editor scene additively so the buttons remain available
        EnsureMapEditorSceneLoaded(false);
    }

    /// <summary>
    /// Makes sure _MapEditor.unity is loaded additively. If `setActive` is true,
    /// also sets it as the active scene (used when clicking "Open Map Editor").
    /// </summary>
    void EnsureMapEditorSceneLoaded(bool setActive)
    {
        // Already loaded? bail unless we need to make it active
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var s = EditorSceneManager.GetSceneAt(i);
            if (s.path == MapEditorScenePath)
            {
                if (setActive) EditorSceneManager.SetActiveScene(s);
                return;
            }
        }

        // Not loaded - check the file exists, then load it
        if (System.IO.File.Exists(MapEditorScenePath))
        {
            var loaded = EditorSceneManager.OpenScene(MapEditorScenePath, OpenSceneMode.Additive);
            if (setActive) EditorSceneManager.SetActiveScene(loaded);
        }
        else
        {
            Debug.LogWarning($"[RF Map] Map editor scene not found at {MapEditorScenePath}. Update the path constant in MapEditorWindow.cs if you saved it elsewhere.");
        }
    }

    string ResolveScenePath(RoomMapButton btn)
    {
        if (btn.targetScene != null)
            return AssetDatabase.GetAssetPath(btn.targetScene);

        if (!string.IsNullOrEmpty(btn.sceneName))
        {
            string[] guids = AssetDatabase.FindAssets($"t:Scene {btn.sceneName}");
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == btn.sceneName)
                    return p;
            }
        }
        return null;
    }

    List<RoomMapButton> FindButtonsInOpenScenes()
    {
        var list = new List<RoomMapButton>();
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                list.AddRange(root.GetComponentsInChildren<RoomMapButton>(true));
            }
        }
        return list;
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }
}
#endif