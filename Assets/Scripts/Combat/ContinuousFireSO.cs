using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackContinuousFire", menuName = "Rail Fighter/Attack Pattern/Continuous Fire")]
public class ContinuousFireSO : AttackPatternSO
{
    public enum FireDirection
    {
        Right,
        Left,
        Up,
        Down,
        Custom,
        TowardPlayer,
        AwayFromPlayer,
    }

    [Header("Attack")]
    [Tooltip("The attack to fire (drag a LaserAttackSO or other AttackSO asset here)")]
    public AttackSO attack;

    [Header("Timing")]
    public float fireInterval = 1.5f;
    public float initialDelay = 0f;

    [Header("Direction")]
    public FireDirection direction = FireDirection.Left;
    public Vector2 customDirection = Vector2.left;
    public string playerTag = "Player";

    private class State { public float lastFireTime; }
    private Dictionary<int, State> states = new Dictionary<int, State>();

    public override void Tick(Enemy enemy)
    {
        if (attack == null) return;

        int id = enemy.GetInstanceID();
        if (!states.TryGetValue(id, out var state))
        {
            state = new State { lastFireTime = Time.time + initialDelay - fireInterval };
            states[id] = state;
        }

        if (Time.time - state.lastFireTime < fireInterval) return;

        Vector2 dir = GetFireDirection(enemy);
        int teamLayer = LayerMask.NameToLayer("EnemyProjectile");
        if (teamLayer < 0) teamLayer = 0;

        attack.Execute(enemy.gameObject, dir, teamLayer);
        state.lastFireTime = Time.time;
    }

    Vector2 GetFireDirection(Enemy enemy)
    {
        switch (direction)
        {
            case FireDirection.Right: return Vector2.right;
            case FireDirection.Left: return Vector2.left;
            case FireDirection.Up: return Vector2.up;
            case FireDirection.Down: return Vector2.down;
            case FireDirection.Custom:
                return customDirection.sqrMagnitude > 0.001f ? customDirection.normalized : Vector2.right;
            case FireDirection.TowardPlayer: return GetPlayerDir(enemy, away: false);
            case FireDirection.AwayFromPlayer: return GetPlayerDir(enemy, away: true);
            default: return Vector2.right;
        }
    }

    Vector2 GetPlayerDir(Enemy enemy, bool away)
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return Vector2.right;

        Vector2 toPlayer = (Vector2)(player.transform.position - enemy.transform.position);
        if (toPlayer.sqrMagnitude < 0.001f) return Vector2.right;

        Vector2 dir = toPlayer.normalized;
        return away ? -dir : dir;
    }

    // ─── Gizmos ─────────────────────────────────────────────────────────────
    // Fire-direction arrow FOLLOWS the enemy — it shows where THIS enemy is
    // currently aiming.
    public override void DrawGizmos(Enemy enemy)
    {
        Vector2 dir = Vector2.zero;
        switch (direction)
        {
            case FireDirection.Right: dir = Vector2.right; break;
            case FireDirection.Left: dir = Vector2.left; break;
            case FireDirection.Up: dir = Vector2.up; break;
            case FireDirection.Down: dir = Vector2.down; break;
            case FireDirection.Custom:
                if (customDirection.sqrMagnitude > 0.001f) dir = customDirection.normalized;
                break;
        }

        if (dir == Vector2.zero) return;

        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);

        Vector3 origin = enemy.transform.position;
        Vector3 end = origin + (Vector3)(dir * 1.5f);
        Gizmos.DrawLine(origin, end);

        Vector3 perp = new Vector3(-dir.y, dir.x, 0f) * 0.2f;
        Gizmos.DrawLine(end, end - (Vector3)(dir * 0.3f) + perp);
        Gizmos.DrawLine(end, end - (Vector3)(dir * 0.3f) - perp);
    }
}