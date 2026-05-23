using UnityEngine;

// ─── PlayerCombat ───────────────────────────────────────────────────────────
// The player's combat component. Holds slots for a basic attack and a special
// attack (both AttackSO references), handles auto-fire timing for the basic
// attack, and listens for input on the special attack.
//
// This component does NOT contain attack logic itself — that all lives in the
// AttackSO assets. PlayerCombat just decides WHEN to call Execute() based on
// cooldowns, input, and toggles.
//
// Attach to the player GameObject. Assign AttackSO assets to the slots in the
// inspector.
[DisallowMultipleComponent]
public class PlayerCombat : MonoBehaviour
{
    [Header("Equipped Attacks")]
    [Tooltip("Basic attack — auto-fires on cooldown when enabled. Drop a LaserAttackSO (or other AttackSO) asset here.")]
    public AttackSO basicAttack;

    [Tooltip("Special attack — typically manual-trigger via specialAttackKey. Optional auto-fire toggle below.")]
    public AttackSO specialAttack;

    [Header("Auto-Fire Settings")]
    [Tooltip("When true, basic attack fires automatically every cooldown seconds.")]
    public bool autoFireBasic = true;

    [Tooltip("When true, special attack also fires automatically on cooldown (in addition to manual trigger).")]
    public bool autoFireSpecial = false;

    [Header("Manual Input")]
    [Tooltip("Key/button to manually fire the special attack.")]
    public KeyCode specialAttackKey = KeyCode.Space;

    [Header("Targeting")]
    [Tooltip("Layer mask for enemies — used to find the auto-target direction.")]
    public LayerMask enemyLayer;

    [Tooltip("Maximum range to look for auto-targets. Beyond this, the player fires in defaultDirection.")]
    public float autoTargetRange = 20f;

    [Tooltip("Direction the player fires when no enemy is in auto-target range.")]
    public Vector2 defaultDirection = Vector2.right;

    [Header("Combat State")]
    [Tooltip("When false, all combat is disabled (used during cutscenes, QTE, dialogue, etc.).")]
    public bool combatEnabled = true;

    // ─── Internal State ─────────────────────────────────────────────────────
    private float lastBasicTime = -999f;
    private float lastSpecialTime = -999f;
    private int playerProjectileLayer;

    void Start()
    {
        playerProjectileLayer = LayerMask.NameToLayer("PlayerProjectile");
        if (playerProjectileLayer < 0)
        {
            Debug.LogError("[PlayerCombat] 'PlayerProjectile' layer not found. " +
                           "Add it in Project Settings → Tags and Layers.");
        }
    }

    void Update()
    {
        if (!combatEnabled) return;

        UpdateBasicAttack();
        UpdateSpecialAttack();
    }

    // ─── Basic Attack (auto-fire) ───────────────────────────────────────────
    void UpdateBasicAttack()
    {
        if (!autoFireBasic) return;
        if (basicAttack == null) return;
        if (Time.time < lastBasicTime + basicAttack.cooldown) return;

        Vector2 direction = GetFireDirection();
        basicAttack.Execute(gameObject, direction, playerProjectileLayer);
        lastBasicTime = Time.time;
    }

    // ─── Special Attack (manual or auto) ────────────────────────────────────
    void UpdateSpecialAttack()
    {
        if (specialAttack == null) return;
        if (Time.time < lastSpecialTime + specialAttack.cooldown) return;

        bool shouldFire = false;

        if (autoFireSpecial)
        {
            shouldFire = true;
        }
        else if (Input.GetKeyDown(specialAttackKey))
        {
            shouldFire = true;
        }

        if (shouldFire)
        {
            Vector2 direction = GetFireDirection();
            specialAttack.Execute(gameObject, direction, playerProjectileLayer);
            lastSpecialTime = Time.time;
        }
    }

    // ─── Targeting ──────────────────────────────────────────────────────────
    // Returns the direction to fire. If an enemy is in range, aims at the
    // nearest one. Otherwise falls back to defaultDirection.
    Vector2 GetFireDirection()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, autoTargetRange, enemyLayer);
        if (enemies.Length == 0) return defaultDirection.normalized;

        Collider2D nearest = null;
        float nearestDist = float.MaxValue;
        foreach (Collider2D c in enemies)
        {
            if (c == null) continue;
            float dist = Vector2.Distance(transform.position, c.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = c;
            }
        }

        if (nearest == null) return defaultDirection.normalized;
        return ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;
    }

    // ─── Public API ─────────────────────────────────────────────────────────
    // Call DisableCombat() during cutscenes, QTEs, or other moments where the
    // player shouldn't be able to attack. Call EnableCombat() to restore.
    public void DisableCombat() => combatEnabled = false;
    public void EnableCombat() => combatEnabled = true;

    // ─── Cooldown Queries ───────────────────────────────────────────────────
    // Useful for HUD elements that show cooldown progress.
    public float BasicCooldownRemaining =>
        basicAttack == null ? 0f : Mathf.Max(0f, (lastBasicTime + basicAttack.cooldown) - Time.time);

    public float SpecialCooldownRemaining =>
        specialAttack == null ? 0f : Mathf.Max(0f, (lastSpecialTime + specialAttack.cooldown) - Time.time);

    public bool IsBasicReady => basicAttack != null && BasicCooldownRemaining <= 0f;
    public bool IsSpecialReady => specialAttack != null && SpecialCooldownRemaining <= 0f;

    // ─── Editor Visualization ───────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        // Draw the auto-target range as a circle in the scene view when player is selected.
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, autoTargetRange);

        // Draw the default firing direction as an arrow.
        Gizmos.color = Color.cyan;
        Vector3 from = transform.position;
        Vector3 to = from + (Vector3)defaultDirection.normalized * 2f;
        Gizmos.DrawLine(from, to);
    }
}