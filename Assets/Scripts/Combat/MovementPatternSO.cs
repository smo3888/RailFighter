using UnityEngine;

// ─── MovementPatternSO ──────────────────────────────────────────────────────
// Abstract base class for enemy movement patterns.
public abstract class MovementPatternSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in debug logs / future UI")]
    public string patternName = "Unnamed Pattern";

    public enum FacingIntent { None, Right, Left }

    public abstract void Tick(Enemy enemy);

    public virtual FacingIntent GetFacingIntent(Enemy enemy) => FacingIntent.None;

    // ─── DrawGizmos ─────────────────────────────────────────────────────────
    // Receives the Enemy itself so individual gizmos can choose whether to
    // anchor at spawn position (enemy.GizmoAnchor) or follow the enemy
    // (enemy.transform.position) based on what they represent.
    //   - Bounds, patrol ranges → enemy.GizmoAnchor (fixed reference frame)
    //   - Awareness, detection, attack range → enemy.transform.position (follows)
    public virtual void DrawGizmos(Enemy enemy) { }
}