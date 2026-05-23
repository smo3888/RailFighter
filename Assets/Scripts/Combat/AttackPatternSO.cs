using UnityEngine;

// ─── AttackPatternSO ────────────────────────────────────────────────────────
// Abstract base class for enemy attack patterns.
public abstract class AttackPatternSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in debug logs / future UI")]
    public string patternName = "Unnamed Attack Pattern";

    public abstract void Tick(Enemy enemy);

    // ─── DrawGizmos ─────────────────────────────────────────────────────────
    // Receives the Enemy itself so gizmos can choose anchored vs following.
    // Most attack pattern gizmos (fire direction, detection range, etc.)
    // should follow the enemy via enemy.transform.position.
    public virtual void DrawGizmos(Enemy enemy) { }
}