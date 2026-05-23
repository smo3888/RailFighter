using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(Enemy))]
public class EnemyEditor : Editor
{
    private Editor movementEditor;
    private Editor dataEditor;
    private Editor attackPatternEditor;

    private BoxBoundsHandle wanderBoundsHandle;

    void OnEnable()
    {
        wanderBoundsHandle = new BoxBoundsHandle();
        wanderBoundsHandle.handleColor = new Color(0.3f, 0.9f, 0.3f, 1f);
        wanderBoundsHandle.wireframeColor = Color.clear;
        wanderBoundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Enemy enemy = (Enemy)target;

        if (enemy.data != null)
        {
            EditorGUILayout.Space(8);
            DrawInlineSO($"Enemy Data: {enemy.data.name}", enemy.data, ref dataEditor, false);
        }

        if (enemy.movement != null)
        {
            EditorGUILayout.Space(8);
            DrawInlineSO($"Movement Pattern: {enemy.movement.name}", enemy.movement,
                ref movementEditor, true, () => DuplicateMovementAsUnique(enemy));
        }

        if (enemy.attackPattern != null)
        {
            EditorGUILayout.Space(8);
            DrawInlineSO($"Attack Pattern: {enemy.attackPattern.name}", enemy.attackPattern,
                ref attackPatternEditor, true, () => DuplicateAttackPatternAsUnique(enemy));
        }
    }

    // ─── OnSceneGUI ─────────────────────────────────────────────────────────
    void OnSceneGUI()
    {
        Enemy enemy = (Enemy)target;

        if (enemy.movement is WanderMovementSO wander)
        {
            DrawWanderBoundsHandle(enemy, wander);
        }
    }

    // ─── Wander Bounds Handle ───────────────────────────────────────────────
    // Asymmetric bounds editing. Each side of the box can be dragged
    // independently. Center may move as a result, and that's intentional —
    // the box represents the wander area, not a centered halo around spawn.
    void DrawWanderBoundsHandle(Enemy enemy, WanderMovementSO wander)
    {
        Vector3 anchor = enemy.GizmoAnchor;

        // Convert min/max offsets into a world-space center + size for the handle
        Vector3 worldCenter = anchor + new Vector3(
            (wander.boundsMin.x + wander.boundsMax.x) * 0.5f,
            (wander.boundsMin.y + wander.boundsMax.y) * 0.5f,
            0f);
        Vector3 worldSize = new Vector3(
            wander.boundsMax.x - wander.boundsMin.x,
            wander.boundsMax.y - wander.boundsMin.y,
            0.01f);

        wanderBoundsHandle.center = worldCenter;
        wanderBoundsHandle.size = worldSize;

        EditorGUI.BeginChangeCheck();
        wanderBoundsHandle.DrawHandle();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(wander, "Edit Wander Bounds");

            // Convert the handle's new world center + size back into min/max
            // offsets relative to the spawn anchor.
            Vector3 newCenter = wanderBoundsHandle.center;
            Vector3 newSize = wanderBoundsHandle.size;

            Vector2 newMin = new Vector2(
                (newCenter.x - newSize.x * 0.5f) - anchor.x,
                (newCenter.y - newSize.y * 0.5f) - anchor.y
            );
            Vector2 newMax = new Vector2(
                (newCenter.x + newSize.x * 0.5f) - anchor.x,
                (newCenter.y + newSize.y * 0.5f) - anchor.y
            );

            // Sanity floors so the bounds always include the spawn point with
            // a tiny margin (keeps the enemy from being clamped out of its own
            // area). If the user dragged a side past spawn, push it back to 0.
            newMin.x = Mathf.Min(newMin.x, -0.1f);
            newMin.y = Mathf.Min(newMin.y, -0.1f);
            newMax.x = Mathf.Max(newMax.x, 0.1f);
            newMax.y = Mathf.Max(newMax.y, 0.1f);

            wander.boundsMin = newMin;
            wander.boundsMax = newMax;

            EditorUtility.SetDirty(wander);
        }
    }

    // ─── Inline SO Helper ───────────────────────────────────────────────────
    void DrawInlineSO(string title, Object target, ref Editor editorRef,
                       bool showDuplicateButton, System.Action onDuplicate = null)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        CreateCachedEditor(target, null, ref editorRef);

        EditorGUI.indentLevel++;
        editorRef.OnInspectorGUI();
        EditorGUI.indentLevel--;

        if (showDuplicateButton)
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Duplicate as Unique Variant"))
            {
                onDuplicate?.Invoke();
            }
            EditorGUILayout.HelpBox(
                "Editing values above modifies the shared SO and affects ALL enemies using it. " +
                "Click 'Duplicate as Unique Variant' to give this enemy its own copy.",
                MessageType.Info);
        }
    }

    void DuplicateMovementAsUnique(Enemy enemy)
    {
        DuplicateSOAsUnique(enemy, enemy.movement,
            (newAsset) => enemy.movement = (MovementPatternSO)newAsset,
            ref movementEditor, "movement");
    }

    void DuplicateAttackPatternAsUnique(Enemy enemy)
    {
        DuplicateSOAsUnique(enemy, enemy.attackPattern,
            (newAsset) => enemy.attackPattern = (AttackPatternSO)newAsset,
            ref attackPatternEditor, "attack pattern");
    }

    void DuplicateSOAsUnique(Enemy enemy, ScriptableObject currentSO,
                             System.Action<ScriptableObject> assignFunc,
                             ref Editor cachedEditor, string label)
    {
        if (currentSO == null) return;

        string path = AssetDatabase.GetAssetPath(currentSO);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"{label} SO has no asset path — cannot duplicate.");
            return;
        }

        string newPath = AssetDatabase.GenerateUniqueAssetPath(path);

        if (!AssetDatabase.CopyAsset(path, newPath))
        {
            Debug.LogError($"Failed to duplicate asset at {path}");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var newAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(newPath);
        if (newAsset == null)
        {
            Debug.LogError($"Failed to load duplicated asset at {newPath}");
            return;
        }

        Undo.RecordObject(enemy, $"Make {label} Unique");
        assignFunc(newAsset);
        EditorUtility.SetDirty(enemy);

        if (cachedEditor != null)
        {
            DestroyImmediate(cachedEditor);
            cachedEditor = null;
        }

        Debug.Log($"[EnemyEditor] Created unique {label} variant: {newPath}");
    }
}