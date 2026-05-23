using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackAimedShot", menuName = "Rail Fighter/Attack Pattern/Aimed Shot")]
public class AimedShotSO : AttackPatternSO
{
    [Header("Attack")]
    public AttackSO attack;

    [Header("Detection")]
    public float detectionRange = 8f;
    public string playerTag = "Player";

    [Header("Timing")]
    public float fireInterval = 1.5f;
    public float aimUpDelay = 0.3f;

    private class State
    {
        public float lastFireTime;
        public bool wasInRange;
    }
    private Dictionary<int, State> states = new Dictionary<int, State>();

    public override void Tick(Enemy enemy)
    {
        if (attack == null) return;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        Vector2 toPlayer = (Vector2)(player.transform.position - enemy.transform.position);
        float dist = toPlayer.magnitude;
        bool inRange = dist <= detectionRange && dist > 0.001f;

        int id = enemy.GetInstanceID();
        if (!states.TryGetValue(id, out var state))
        {
            state = new State { lastFireTime = -fireInterval, wasInRange = false };
            states[id] = state;
        }

        if (!inRange)
        {
            state.wasInRange = false;
            return;
        }

        if (!state.wasInRange)
        {
            state.lastFireTime = Time.time - fireInterval + aimUpDelay;
            state.wasInRange = true;
            return;
        }

        if (Time.time - state.lastFireTime < fireInterval) return;

        Vector2 dir = toPlayer / dist;
        int teamLayer = LayerMask.NameToLayer("EnemyProjectile");
        if (teamLayer < 0) teamLayer = 0;

        attack.Execute(enemy.gameObject, dir, teamLayer);
        state.lastFireTime = Time.time;
    }

    // ─── Gizmos ─────────────────────────────────────────────────────────────
    // Detection range FOLLOWS the enemy — it's the distance from THIS enemy's
    // position that triggers the aimed shot.
    public override void DrawGizmos(Enemy enemy)
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(enemy.transform.position, detectionRange);
    }
}