using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class AIPlaygroundWindow : EditorWindow
{
    [MenuItem("Tools/AI Playground")]
    public static void Open() => GetWindow<AIPlaygroundWindow>("AI Playground");

    private const string PLAYGROUND_FOLDER = "Assets/_AIPlayground";
    private const string PLAYGROUND_SCENE_PATH = "Assets/_AIPlayground/AIPlayground.unity";
    private const string PLAYGROUND_SCENE_NAME = "AIPlayground";

    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private bool playerFollowsCursor = false;
    [SerializeField] private bool showAllGizmos = false;
    [SerializeField] private bool showDamagePreview = true;
    [SerializeField] private bool showContactBounds = true;
    [SerializeField] private float simulationSpeed = 1f;
    [SerializeField] private float spawnSpacing = 3f;

    private List<GameObject> spawned = new List<GameObject>();
    private GameObject spawnedPlayer;
    private bool isSimulating = false;
    private Vector2 scrollPos;

    private class DamageTextEvent
    {
        public Vector3 spawnWorldPos;
        public float amount;
        public float spawnTime;
    }
    private List<DamageTextEvent> activeDamageTexts = new List<DamageTextEvent>();
    private HashSet<int> currentlyOverlapping = new HashSet<int>();
    private const float DAMAGE_TEXT_LIFETIME = 1.2f;
    private const float DAMAGE_TEXT_FLOAT_DISTANCE = 1.5f;

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        SceneView.duringSceneGui += OnSceneGUI;
        Enemy.ShowAllGizmos = showAllGizmos;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        SceneView.duringSceneGui -= OnSceneGUI;
        Enemy.ShowAllGizmos = false;
        ClearSpawned();
        DespawnPlayer();
    }

    void OnSceneChanged(Scene previous, Scene current)
    {
        if (current.name != PLAYGROUND_SCENE_NAME)
        {
            isSimulating = false;
            ClearSpawned();
            DespawnPlayer();
            Enemy.ShowAllGizmos = false;
        }
        else
        {
            Enemy.ShowAllGizmos = showAllGizmos;
        }
        Repaint();
    }

    bool IsInPlayground => SceneManager.GetActiveScene().name == PLAYGROUND_SCENE_NAME;

    void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("AI Playground", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Tinker with enemy movement and attack patterns in a dedicated scene.", EditorStyles.miniLabel);
        EditorGUILayout.Space(8);

        DrawSceneStatus();

        if (!IsInPlayground)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "You're not currently in the playground scene. " +
                "Click 'Open Playground Scene' above to start using the tool.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space(8);
        DrawPrefabList();
        EditorGUILayout.Space(8);
        DrawSpawnControls();
        EditorGUILayout.Space(8);
        DrawPlayerControls();
        EditorGUILayout.Space(8);
        DrawSimulationControls();
        EditorGUILayout.Space(8);
        DrawVisualizationControls();
        EditorGUILayout.Space(8);
        DrawStatus();
    }

    void DrawSceneStatus()
    {
        EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);

        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = IsInPlayground ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.85f, 0.4f);
        string label = IsInPlayground
            ? "✓ In Playground Scene"
            : $"✗ Active scene: {SceneManager.GetActiveScene().name}";
        EditorGUILayout.LabelField(label, EditorStyles.helpBox);
        GUI.backgroundColor = prevColor;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(IsInPlayground ? "Reload Playground Scene" : "Open Playground Scene"))
        {
            OpenPlaygroundScene();
        }
        EditorGUI.BeginDisabledGroup(!IsInPlayground);
        if (GUILayout.Button("Save Playground"))
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    void DrawPrefabList()
    {
        EditorGUILayout.LabelField("Enemy Prefab Slots", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MinHeight(80), GUILayout.MaxHeight(180));
        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            enemyPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                $"Slot {i + 1}", enemyPrefabs[i], typeof(GameObject), false);
            if (GUILayout.Button("✕", GUILayout.Width(28)))
            {
                enemyPrefabs.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Slot")) enemyPrefabs.Add(null);
        if (GUILayout.Button("Clear Slots"))
        {
            if (EditorUtility.DisplayDialog("Clear all slots?",
                "This empties the prefab list. Spawned enemies stay until Clear All.",
                "Clear", "Cancel"))
            {
                enemyPrefabs.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawSpawnControls()
    {
        EditorGUILayout.LabelField("Enemy Spawn", EditorStyles.boldLabel);
        spawnSpacing = EditorGUILayout.Slider("Spacing", spawnSpacing, 1f, 10f);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn All", GUILayout.Height(28))) SpawnAll();
        if (GUILayout.Button("Clear All", GUILayout.Height(28))) ClearSpawned();
        EditorGUILayout.EndHorizontal();
    }

    void DrawPlayerControls()
    {
        EditorGUILayout.LabelField("Player Stand-In", EditorStyles.boldLabel);

        playerPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Optional Prefab", playerPrefab, typeof(GameObject), false);
        EditorGUILayout.LabelField(
            "Leave empty to use a generated stand-in (magenta square, tagged \"Player\").",
            EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(spawnedPlayer != null);
        if (GUILayout.Button("Spawn Player", GUILayout.Height(26))) SpawnPlayer();
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(spawnedPlayer == null);
        if (GUILayout.Button("Despawn Player", GUILayout.Height(26))) DespawnPlayer();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(spawnedPlayer == null);
        playerFollowsCursor = EditorGUILayout.ToggleLeft(
            "Follow Cursor (drag player around scene view)",
            playerFollowsCursor);
        EditorGUI.EndDisabledGroup();

        if (spawnedPlayer != null && playerFollowsCursor)
        {
            EditorGUILayout.HelpBox(
                "Move your cursor over the Scene view. The player will follow it.",
                MessageType.Info);
        }
    }

    void DrawSimulationControls()
    {
        EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        string playLabel = isSimulating ? "⏸  Pause" : "▶  Play";
        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = isSimulating ? new Color(1f, 0.7f, 0.4f) : new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button(playLabel, GUILayout.Height(36)))
        {
            isSimulating = !isSimulating;
        }
        GUI.backgroundColor = prevColor;

        if (GUILayout.Button("⟲  Reset", GUILayout.Height(36), GUILayout.Width(100)))
        {
            ResetSpawned();
        }
        EditorGUILayout.EndHorizontal();

        simulationSpeed = EditorGUILayout.Slider("Speed (approx)", simulationSpeed, 0.1f, 5f);
    }

    void DrawVisualizationControls()
    {
        EditorGUILayout.LabelField("Visualization", EditorStyles.boldLabel);

        bool prevShowAll = showAllGizmos;
        showAllGizmos = EditorGUILayout.ToggleLeft(
            "Show All Gizmos (every enemy's bounds visible at once)",
            showAllGizmos);

        if (showAllGizmos != prevShowAll)
        {
            Enemy.ShowAllGizmos = showAllGizmos;
            SceneView.RepaintAll();
        }

        showDamagePreview = EditorGUILayout.ToggleLeft(
            "Show Damage Preview (floating numbers on contact AND projectile hit)",
            showDamagePreview);

        showContactBounds = EditorGUILayout.ToggleLeft(
            "Show Contact Bounds (visualize the actual collision rectangles)",
            showContactBounds);
    }

    void DrawStatus()
    {
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Spawned enemies: {spawned.Count}");
        EditorGUILayout.LabelField($"Player spawned: {(spawnedPlayer != null ? "yes" : "no")}");

        if (isSimulating && spawned.Count > 0)
            EditorGUILayout.HelpBox("Simulation running.", MessageType.Info);
        else if (spawned.Count > 0 && !isSimulating)
            EditorGUILayout.HelpBox("Paused. Hit Play to resume.", MessageType.None);
        else
            EditorGUILayout.HelpBox("Add prefabs to slots, then Spawn All.", MessageType.None);
    }

    void OpenPlaygroundScene()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            bool confirmed = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            if (!confirmed) return;
        }

        if (!Directory.Exists(PLAYGROUND_FOLDER))
        {
            Directory.CreateDirectory(PLAYGROUND_FOLDER);
            AssetDatabase.Refresh();
        }

        if (!File.Exists(PLAYGROUND_SCENE_PATH))
        {
            CreatePlaygroundScene();
        }

        EditorSceneManager.OpenScene(PLAYGROUND_SCENE_PATH);
        EnsurePlaygroundCamera();
    }

    void CreatePlaygroundScene()
    {
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(newScene, PLAYGROUND_SCENE_PATH);
        AssetDatabase.Refresh();
        Debug.Log($"[AI Playground] Created new playground scene at {PLAYGROUND_SCENE_PATH}");
    }

    void EnsurePlaygroundCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            EditorUtility.SetDirty(cam.gameObject);
        }
    }

    // ─── Spawn All — now uses Enemy.OnSpawn() ───────────────────────────────
    void SpawnAll()
    {
        if (!IsInPlayground) { Debug.LogWarning("[AI Playground] Not in playground scene."); return; }

        ClearSpawned();

        int validCount = 0;
        foreach (var p in enemyPrefabs) if (p != null) validCount++;
        if (validCount == 0) { Debug.LogWarning("[AI Playground] No prefabs assigned."); return; }

        int index = 0;
        foreach (var prefab in enemyPrefabs)
        {
            if (prefab == null) continue;

            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (obj == null) continue;

            float x = (index - (validCount - 1) / 2f) * spawnSpacing;
            obj.transform.position = new Vector3(x, 0f, 0f);
            obj.hideFlags = HideFlags.DontSaveInEditor;

            // Use OnSpawn() — sets spawnPosition AND flags isSpawned = true
            // so gizmos anchor to spawn position from this point on.
            Enemy enemy = obj.GetComponent<Enemy>();
            if (enemy != null) enemy.OnSpawn();

            spawned.Add(obj);
            index++;
        }

        SceneView.RepaintAll();
        Debug.Log($"[AI Playground] Spawned {spawned.Count} enemy/enemies.");
    }

    void ClearSpawned()
    {
        foreach (var obj in spawned)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        spawned.Clear();
        DestroyStrayProjectiles();
        currentlyOverlapping.Clear();
        SceneView.RepaintAll();
    }

    void DestroyStrayProjectiles()
    {
        var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        foreach (var proj in projectiles)
        {
            if (proj != null) DestroyImmediate(proj.gameObject);
        }
    }

    void ResetSpawned()
    {
        foreach (var obj in spawned)
        {
            if (obj == null) continue;
            Enemy enemy = obj.GetComponent<Enemy>();
            if (enemy != null) obj.transform.position = enemy.spawnPosition;
        }
        DestroyStrayProjectiles();
        currentlyOverlapping.Clear();
        SceneView.RepaintAll();
    }

    void SpawnPlayer()
    {
        if (!IsInPlayground) { Debug.LogWarning("[AI Playground] Not in playground scene."); return; }
        if (spawnedPlayer != null) return;

        if (playerPrefab != null)
        {
            spawnedPlayer = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        }
        else
        {
            spawnedPlayer = CreateGeneratedPlayerStandIn();
        }

        if (spawnedPlayer == null) return;

        spawnedPlayer.hideFlags = HideFlags.DontSaveInEditor;
        spawnedPlayer.transform.position = new Vector3(0, -3, 0);

        try { spawnedPlayer.tag = "Player"; }
        catch { Debug.LogWarning("[AI Playground] Could not set 'Player' tag."); }

        SceneView.RepaintAll();
        Debug.Log("[AI Playground] Player spawned.");
    }

    void DespawnPlayer()
    {
        if (spawnedPlayer != null)
        {
            DestroyImmediate(spawnedPlayer);
            spawnedPlayer = null;
        }
        playerFollowsCursor = false;
        currentlyOverlapping.Clear();
        SceneView.RepaintAll();
    }

    GameObject CreateGeneratedPlayerStandIn()
    {
        GameObject go = new GameObject("Player Stand-In (generated)");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidColorSprite(Color.magenta);
        sr.color = Color.magenta;
        sr.sortingOrder = 10;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        col.isTrigger = true;

        go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        return go;
    }

    Sprite MakeSolidColorSprite(Color color)
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!IsInPlayground) return;

        if (playerFollowsCursor && spawnedPlayer != null)
        {
            Event e = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            Plane zPlane = new Plane(Vector3.forward, Vector3.zero);
            if (zPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                worldPos.z = 0f;
                spawnedPlayer.transform.position = worldPos;
                sceneView.Repaint();
            }
        }

        if (showContactBounds) DrawContactBounds();
        if (showDamagePreview) DrawDamageTexts();
    }

    void DrawContactBounds()
    {
        if (spawnedPlayer != null)
        {
            DrawBoundsRect(GetWorldBounds(spawnedPlayer), new Color(0.3f, 0.6f, 1f, 0.9f));
        }

        foreach (var enemyObj in spawned)
        {
            if (enemyObj == null) continue;
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy == null) continue;
            DrawBoundsRect(GetWorldBounds(enemyObj), new Color(1f, 0.3f, 0.3f, 0.9f));
        }
    }

    void DrawBoundsRect(Bounds bounds, Color color)
    {
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;

        Vector3 tl = new Vector3(c.x - e.x, c.y + e.y, 0);
        Vector3 tr = new Vector3(c.x + e.x, c.y + e.y, 0);
        Vector3 br = new Vector3(c.x + e.x, c.y - e.y, 0);
        Vector3 bl = new Vector3(c.x - e.x, c.y - e.y, 0);

        Handles.color = color;
        Handles.DrawAAPolyLine(2.5f, new Vector3[] { tl, tr, br, bl, tl });

        Color fill = color;
        fill.a = 0.08f;
        Handles.DrawSolidRectangleWithOutline(new Vector3[] { tl, tr, br, bl }, fill, Color.clear);
    }

    void DetectDamagePreview()
    {
        if (!showDamagePreview) return;
        if (spawnedPlayer == null) { currentlyOverlapping.Clear(); return; }

        Bounds playerBounds = GetWorldBounds(spawnedPlayer);
        HashSet<int> nowOverlapping = new HashSet<int>();

        foreach (var enemyObj in spawned)
        {
            if (enemyObj == null) continue;
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy == null || enemy.data == null) continue;
            if (enemy.data.contactDamage <= 0f) continue;

            Bounds enemyBounds = GetWorldBounds(enemyObj);
            if (!enemyBounds.Intersects(playerBounds)) continue;

            int id = enemyObj.GetInstanceID();
            nowOverlapping.Add(id);

            if (!currentlyOverlapping.Contains(id))
            {
                activeDamageTexts.Add(new DamageTextEvent
                {
                    spawnWorldPos = spawnedPlayer.transform.position,
                    amount = enemy.data.contactDamage,
                    spawnTime = (float)EditorApplication.timeSinceStartup,
                });
            }
        }

        int enemyProjLayer = LayerMask.NameToLayer("EnemyProjectile");
        var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        foreach (var proj in projectiles)
        {
            if (proj == null) continue;
            if (enemyProjLayer >= 0 && proj.gameObject.layer != enemyProjLayer) continue;

            Bounds projBounds = GetWorldBounds(proj.gameObject);
            if (!projBounds.Intersects(playerBounds)) continue;

            int id = proj.gameObject.GetInstanceID();
            nowOverlapping.Add(id);

            if (!currentlyOverlapping.Contains(id))
            {
                activeDamageTexts.Add(new DamageTextEvent
                {
                    spawnWorldPos = spawnedPlayer.transform.position,
                    amount = proj.Damage,
                    spawnTime = (float)EditorApplication.timeSinceStartup,
                });
            }
        }

        currentlyOverlapping = nowOverlapping;
    }

    void DrawDamageTexts()
    {
        float now = (float)EditorApplication.timeSinceStartup;

        GUIStyle style = new GUIStyle();
        style.fontSize = 22;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        for (int i = activeDamageTexts.Count - 1; i >= 0; i--)
        {
            var dt = activeDamageTexts[i];
            float age = now - dt.spawnTime;

            if (age > DAMAGE_TEXT_LIFETIME)
            {
                activeDamageTexts.RemoveAt(i);
                continue;
            }

            float t = age / DAMAGE_TEXT_LIFETIME;
            Vector3 pos = dt.spawnWorldPos + Vector3.up * (t * DAMAGE_TEXT_FLOAT_DISTANCE);

            float alpha = Mathf.Clamp01(1f - t * 1.2f);

            style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.6f);
            Handles.Label(pos + new Vector3(0.04f, -0.04f, 0), $"-{dt.amount}", style);

            style.normal.textColor = new Color(1f, 0.25f, 0.25f, alpha);
            Handles.Label(pos, $"-{dt.amount}", style);
        }
    }

    Bounds GetWorldBounds(GameObject obj)
    {
        var col = obj.GetComponent<Collider2D>();
        if (col != null) return col.bounds;

        var rend = obj.GetComponent<Renderer>();
        if (rend != null) return rend.bounds;

        return new Bounds(obj.transform.position, new Vector3(0.5f, 0.5f, 0.5f));
    }

    private double tickAccumulator = 0;
    private double lastUpdateTime = 0;

    void OnEditorUpdate()
    {
        if (!IsInPlayground) return;

        DetectDamagePreview();

        if (activeDamageTexts.Count > 0 || (showContactBounds && spawnedPlayer != null))
        {
            SceneView.RepaintAll();
        }

        if (!isSimulating) return;
        if (spawned.Count == 0) return;

        double now = EditorApplication.timeSinceStartup;
        double delta = now - lastUpdateTime;
        lastUpdateTime = now;

        tickAccumulator += delta * simulationSpeed;

        const double tickInterval = 1.0 / 60.0;
        while (tickAccumulator >= tickInterval)
        {
            tickAccumulator -= tickInterval;
            TickAll();
        }

        SceneView.RepaintAll();
        Repaint();
    }

    void TickAll()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawned[i];
            if (obj == null)
            {
                spawned.RemoveAt(i);
                continue;
            }

            Enemy enemy = obj.GetComponent<Enemy>();
            if (enemy == null) continue;

            enemy.RunFrame();
        }
    }
}