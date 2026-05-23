using UnityEngine;

// ─── IDamageable ────────────────────────────────────────────────────────────
// Anything that can take damage implements this interface. Enemies, the player,
// future destructible objects (barrels, doors, etc.) all use the same contract.
//
// Projectiles don't need to know whether they hit an enemy, the player, or a
// barrel — they just check if the thing they hit implements IDamageable and
// call TakeDamage() on it.
//
// Usage from a projectile:
//   IDamageable target = collision.gameObject.GetComponent<IDamageable>();
//   if (target != null && target.IsAlive) {
//       target.TakeDamage(damagePayload);
//   }
public interface IDamageable
{
    // Apply a damage payload to this object. Implementation handles health
    // reduction, hit reactions, death, knockback, etc.
    void TakeDamage(DamagePayload payload);

    // Whether this object can currently take damage. Returns false if dead,
    // currently invincible, or otherwise unhittable. Lets attackers skip
    // wasted hits on already-dead/invincible targets.
    bool IsAlive { get; }
}