using UnityEngine;

// ─── AttackSO ───────────────────────────────────────────────────────────────
// Abstract base class for all attack ScriptableObjects. Each concrete attack
// type (laser, rocket, bomb, etc.) extends this class and implements Execute()
// to define its own behavior.
//
// The player's PlayerCombat component has slots typed as AttackSO that can be
// filled with any concrete AttackSO subclass. The player calls Execute() and
// the right behavior fires — the player doesn't know or care what kind of
// attack it is.
//
// To create a new attack type:
//   1. Create a new C# class that extends AttackSO
//   2. Override Execute() to define what the attack does
//   3. Add a [CreateAssetMenu] attribute so you can create assets in Unity
//   4. Add fields for whatever data your attack needs (damage, speed, prefab, etc.)
//
// Example: LaserAttackSO, RocketAttackSO, BombAttackSO
public abstract class AttackSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in UI / debug logs")]
    public string attackName = "Unnamed Attack";

    [Header("Timing")]
    [Tooltip("Seconds between automatic uses of this attack (when on auto-fire). " +
             "For special attacks, this is the manual-trigger cooldown.")]
    public float cooldown = 1f;

    // ─── Execute ────────────────────────────────────────────────────────────
    // Called when the attack should fire. Each subclass defines its own behavior.
    //
    // Parameters:
    //   user      — the GameObject performing the attack (player, enemy, etc.)
    //   direction — direction the attack should travel (typically aim or facing direction)
    //   teamLayer — physics layer for spawned projectiles (PlayerProjectile or EnemyProjectile)
    //
    // Returning void instead of bool because firing failure should be handled
    // upstream (cooldowns, validation) before Execute() is even called.
    public abstract void Execute(GameObject user, Vector2 direction, int teamLayer);
}