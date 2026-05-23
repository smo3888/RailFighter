using UnityEngine;

// ─── EnemyData ──────────────────────────────────────────────────────────────
// Defines the stats and configuration for a kind of enemy. Each enemy variant
// (basic flyer, asteroid turret, dive enemy, etc.) is its own EnemyData asset.
//
// Pattern A SO: pure data container. The Enemy MonoBehaviour reads this at
// startup and uses the values for its runtime behavior. Multiple Enemy
// GameObjects can share the same EnemyData asset.
//
// To create an asset:
//   Right-click in Project window → Create → Rail Fighter → Enemy Data
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Rail Fighter/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in debug logs / future UI")]
    public string displayName = "Enemy";

    [Header("Stats")]
    [Tooltip("Maximum health. Enemy dies when current health reaches 0.")]
    public float maxHealth = 5f;

    [Header("Combat")]
    [Tooltip("Damage dealt when this enemy collides with the player or other " +
             "damageable target. 0 = no contact damage (safe to touch).")]
    public float contactDamage = 1f;

    [Header("Visual Feedback")]
    [Tooltip("Color the sprite flashes when hit")]
    public Color hitFlashColor = Color.red;

    [Tooltip("Duration of the hit flash in seconds")]
    [Range(0.02f, 0.5f)]
    public float hitFlashDuration = 0.1f;

    [Header("Death")]
    [Tooltip("Optional particle/effect prefab spawned on death")]
    public GameObject deathEffectPrefab;
}