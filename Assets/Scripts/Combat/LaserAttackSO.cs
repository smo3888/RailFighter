using UnityEngine;

// ─── LaserAttackSO ──────────────────────────────────────────────────────────
// Basic laser attack. Spawns a single projectile that flies in the given
// direction at the configured speed and deals the configured damage.
//
// This is the player's default basic attack. Multiple LaserAttackSO assets
// can be created with different stats (basic laser, post-boss laser,
// charged laser, etc.) — same script, different inspector values.
//
// To create an asset:
//   Right-click in Project window → Create → Rail Fighter → Attacks → Laser
[CreateAssetMenu(fileName = "NewLaserAttack", menuName = "Rail Fighter/Attacks/Laser")]
public class LaserAttackSO : AttackSO
{
    [Header("Projectile")]
    [Tooltip("The projectile prefab to spawn. Must have a Projectile component.")]
    public GameObject projectilePrefab;

    [Tooltip("How far in front of the user the projectile spawns. Prevents the " +
             "projectile from immediately self-colliding with the user's collider.")]
    public float spawnOffset = 0.5f;

    [Header("Stats")]
    [Tooltip("Damage dealt by each projectile")]
    public float damage = 1f;

    [Tooltip("Travel speed of the projectile in units per second")]
    public float speed = 10f;

    [Header("Damage Type")]
    [Tooltip("What kind of damage this attack deals. Useful for resistances/immunities later.")]
    public DamageType damageType = DamageType.Basic;

    // ─── Execute ────────────────────────────────────────────────────────────
    // Spawns a projectile, configures it, and lets it fly.
    public override void Execute(GameObject user, Vector2 direction, int teamLayer)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{attackName}] No projectile prefab assigned.");
            return;
        }

        Vector2 dir = direction.normalized;
        Vector3 spawnPos = user.transform.position + (Vector3)(dir * spawnOffset);

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Set the team layer so the physics matrix filters collisions correctly.
        projectileObj.layer = teamLayer;

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(
                direction: dir,
                speed: speed,
                damage: damage,
                type: damageType,
                source: user
            );
        }
        else
        {
            Debug.LogError($"[{attackName}] Spawned projectile is missing the Projectile component.");
        }
    }
}