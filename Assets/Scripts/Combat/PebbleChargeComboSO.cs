using System.Collections.Generic;
using UnityEngine;

// ─── ChargeProfile ──────────────────────────────────────────────────────────
// Bundles the four parameters that define one "mode" of a charge attack.
// PebbleChargeComboSO uses two of these so aggressive and passive charges
// can feel distinct (passive can be lazier, slower, shorter — aggressive snaps).
[System.Serializable]
public class ChargeProfile
{
    [Tooltip("How long to telegraph (face the target) before committing the charge")]
    public float telegraphDuration = 0.4f;

    [Tooltip("Peak speed of the charge during the active phase")]
    public float speed = 18f;

    [Tooltip("How long to hold peak speed before decay begins")]
    public float activeDuration = 0.4f;

    [Tooltip("How long the speed decays from peak down to driftSpeed (linear)")]
    public float decayDuration = 0.7f;

    [Tooltip("Speed the Pebble retains after the charge decays. Persists in the charge " +
             "direction until the next charge overrides it. 0 = abrupt stop, higher = floatier.")]
    public float driftSpeed = 4f;
}

// ─── PebbleChargeComboSO ────────────────────────────────────────────────────
// The Pebble's only behavior: telegraph → charge → telegraph → charge, forever.
// Targeting varies by aggro state — when aggro is active, the telegraph tracks
// the player; when passive, it picks a horizontal target (left/right) inside
// the Pebble's territory. Mode is locked at the start of each telegraph cycle.
//
// Velocity is persistent — after each charge decays to driftSpeed, the Pebble
// keeps drifting in that direction until the next charge sets a new velocity.
// This gives charges momentum and a floaty feel between attacks.
//
// Awareness uses the WanderMovementSO's awareness circle (not the box bounds).
// Aggro persists for a configurable duration after the player leaves awareness,
// so the Pebble's own movement doesn't accidentally drop aggro.
//
// Wander is assigned to the Pebble for its bounds (territory pacing) and
// awareness range (detection) — motion is fully owned by this SO via
// Enemy.movementSuppressed = true at all times.
[CreateAssetMenu(fileName = "AttackPebbleChargeCombo", menuName = "Rail Fighter/Attack Pattern/Pebble Charge Combo")]
public class PebbleChargeComboSO : AttackPatternSO
{
    [Header("Aggressive Charge (player in awareness)")]
    public ChargeProfile aggressive = new ChargeProfile
    {
        telegraphDuration = 0.4f,
        speed = 18f,
        activeDuration = 0.4f,
        decayDuration = 0.7f,
        driftSpeed = 4f
    };

    [Header("Passive Charge (no player in awareness)")]
    public ChargeProfile passive = new ChargeProfile
    {
        telegraphDuration = 0.6f,
        speed = 8f,
        activeDuration = 0.25f,
        decayDuration = 0.5f,
        driftSpeed = 2f
    };

    [Header("Awareness")]
    [Tooltip("Once the Pebble notices the player, it stays aggressive for this many seconds " +
             "after the player leaves the awareness range. Prevents instant de-aggro from " +
             "the Pebble's own movement carrying the awareness circle off the player.")]
    public float aggroPersistenceDuration = 3f;

    [Header("Facing")]
    [Tooltip("Extra rotation (degrees) applied after pointing the sprite at the target. " +
             "Use this when the sprite isn't drawn facing right by default. " +
             "Common values: 0 (sprite faces right), 180 (faces left), 90 (faces up), -90 (faces down).")]
    public float spriteRotationOffset = -45f;

    [Header("Targeting")]
    public string playerTag = "Player";

    private enum AggroState { Telegraph, Charge }

    private class State
    {
        public AggroState aggroState = AggroState.Telegraph;
        public float stateTimer;
        public Vector2 commitDirection;
        public Vector2 randomTarget;
        public Vector2 velocity;             // persists across states, carries the Pebble between charges
        public float aggroTimer;             // time remaining of aggressive mode — refreshed while in awareness
        public bool telegraphIsTracking;
        public bool telegraphInitialized;
        public float lastTickTime;
    }
    private Dictionary<int, State> states = new Dictionary<int, State>();

    public override void Tick(Enemy enemy)
    {
        // Pebble's only motion is its own charges — wander is suppressed at all times
        enemy.movementSuppressed = true;

        int id = enemy.GetInstanceID();
        if (!states.TryGetValue(id, out var state))
        {
            state = new State { lastTickTime = Time.time };
            states[id] = state;

            // One-time: clear any wander flip so rotation has a clean scale
            Vector3 scale = enemy.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            enemy.transform.localScale = scale;
        }

        float dt = Time.time - state.lastTickTime;
        state.lastTickTime = Time.time;
        dt = Mathf.Clamp(dt, 0f, 0.1f);

        // Awareness with persistence: refresh the timer while the player is in range,
        // decay it when they're out. Pebble stays aggressive while timer > 0.
        if (IsPlayerInAwareness(enemy))
        {
            state.aggroTimer = aggroPersistenceDuration;
        }
        else
        {
            state.aggroTimer = Mathf.Max(0f, state.aggroTimer - dt);
        }
        bool isAggro = state.aggroTimer > 0f;

        switch (state.aggroState)
        {
            case AggroState.Telegraph: TickTelegraph(enemy, state, dt, isAggro); break;
            case AggroState.Charge: TickCharge(enemy, state, dt); break;
        }

        // Apply persistent velocity every frame — drift carries the Pebble between charges
        enemy.transform.position += (Vector3)(state.velocity * dt);
    }

    // ─── State Behaviors ───────────────────────────────────────────────────

    void TickTelegraph(Enemy enemy, State state, float dt, bool isAggro)
    {
        // Lock in the mode for this whole cycle on the first tick
        if (!state.telegraphInitialized)
        {
            state.telegraphIsTracking = isAggro;
            state.randomTarget = PickRandomTargetDirection(enemy);
            state.telegraphInitialized = true;
        }

        // Pick the profile that matches this cycle's mode
        ChargeProfile profile = state.telegraphIsTracking ? aggressive : passive;

        // Resolve target direction this frame
        Vector2 targetDir = state.randomTarget;
        if (state.telegraphIsTracking)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                Vector2 toPlayer = (Vector2)(player.transform.position - enemy.transform.position);
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    targetDir = toPlayer.normalized;
                }
            }
        }

        // Face the target
        if (targetDir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
            enemy.transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteRotationOffset);
        }

        // Velocity isn't touched during telegraph — drift from previous charge persists

        // Advance timer; commit when telegraph completes
        state.stateTimer += dt;
        if (state.stateTimer >= profile.telegraphDuration)
        {
            state.commitDirection = targetDir;
            state.aggroState = AggroState.Charge;
            state.stateTimer = 0f;
        }
    }

    void TickCharge(Enemy enemy, State state, float dt)
    {
        // Same profile this cycle started with — committed
        ChargeProfile profile = state.telegraphIsTracking ? aggressive : passive;

        state.stateTimer += dt;

        float chargeTotal = profile.activeDuration + profile.decayDuration;

        if (state.stateTimer < profile.activeDuration)
        {
            // Active phase: snap velocity to commit direction at peak speed
            state.velocity = state.commitDirection * profile.speed;
        }
        else if (state.stateTimer < chargeTotal)
        {
            // Decay phase: lerp from peak down to driftSpeed (not zero)
            float decayProgress = (state.stateTimer - profile.activeDuration) / profile.decayDuration;
            float currentSpeed = Mathf.Lerp(profile.speed, profile.driftSpeed, decayProgress);
            state.velocity = state.commitDirection * currentSpeed;
        }
        else
        {
            // Charge complete — velocity stays at driftSpeed in commitDirection
            // and will carry the Pebble through the next telegraph
            state.telegraphInitialized = false;
            state.aggroState = AggroState.Telegraph;
            state.stateTimer = 0f;
            return;
        }

        float chargeAngle = Mathf.Atan2(state.commitDirection.y, state.commitDirection.x) * Mathf.Rad2Deg;
        enemy.transform.rotation = Quaternion.Euler(0f, 0f, chargeAngle + spriteRotationOffset);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    bool IsPlayerInAwareness(Enemy enemy)
    {
        // Detection uses the WanderMovementSO's awareness circle (radius around the
        // Pebble), not the box bounds — bounds are for territory pacing, awareness
        // is for player detection.
        var wander = enemy.movement as WanderMovementSO;
        if (wander == null) return false;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return false;

        Vector2 toPlayer = (Vector2)(player.transform.position - enemy.transform.position);
        return toPlayer.sqrMagnitude <= wander.awarenessRange * wander.awarenessRange;
    }

    Vector2 PickRandomTargetDirection(Enemy enemy)
    {
        // Passive charges move purely horizontally — left or right toward a random X
        // inside the bounds. Keeps the Pebble pacing back and forth in its territory
        // like an idle creature.
        var wander = enemy.movement as WanderMovementSO;
        if (wander == null)
        {
            // No bounds — fall back to a plain left/right coin flip
            return Random.value < 0.5f ? Vector2.left : Vector2.right;
        }

        float randomX = Random.Range(wander.boundsMin.x, wander.boundsMax.x);
        float targetWorldX = enemy.spawnPosition.x + randomX;
        float dx = targetWorldX - enemy.transform.position.x;

        if (Mathf.Abs(dx) < 0.001f)
        {
            return Random.value < 0.5f ? Vector2.left : Vector2.right;
        }
        return dx > 0f ? Vector2.right : Vector2.left;
    }
}