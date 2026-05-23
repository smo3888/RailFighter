using System.Collections.Generic;
using UnityEngine;

// ─── WanderMovementSO ───────────────────────────────────────────────────────
// Bounds are now defined as min/max offsets from the spawn position rather
// than symmetric half-extents. This lets you set up enemies that wander
// asymmetrically — e.g., a Pebble against the right wall that should only
// wander LEFT into the room, with a tight cap on the right side.
//
// Default values produce a 5x3 area centered on spawn, equivalent to the
// old (5, 3) half-extents.
[CreateAssetMenu(fileName = "MovementWander", menuName = "Rail Fighter/Movement/Wander")]
public class WanderMovementSO : MovementPatternSO
{
    public enum PlayerAwarenessMode
    {
        Ignore,
        Approach,
        Flee,
    }

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Wander Bounds (offsets from spawn position)")]
    [Tooltip("How far in each direction the enemy can wander from its spawn point. " +
             "X = left/right, Y = down/up. Set asymmetric values to bias the wander " +
             "area in a specific direction (e.g., min.x = -8, max.x = 1 for an enemy " +
             "near the right wall that should mostly wander left).")]
    public Vector2 boundsMin = new Vector2(-5f, -3f);
    public Vector2 boundsMax = new Vector2(5f, 3f);

    [Header("Wander Mode — Idle Periods")]
    public float minIdleTime = 0.5f;
    public float maxIdleTime = 2f;

    [Header("Wander Mode — Move Periods")]
    public float minMoveTime = 0.5f;
    public float maxMoveTime = 2f;

    [Header("Allowed Movement Axes")]
    public bool moveHorizontal = true;
    public bool moveVertical = false;

    [Header("Player Awareness")]
    public PlayerAwarenessMode awareness = PlayerAwarenessMode.Ignore;
    public float awarenessRange = 5f;

    [Range(0f, 1f)]
    public float awarenessStrength = 1f;

    public string playerTag = "Player";

    private class WanderState
    {
        public bool isMoving;
        public float stateTimer;
        public Vector2 currentDirection;
        public float lastTickTime;
    }

    private Dictionary<int, WanderState> states = new Dictionary<int, WanderState>();

    public override void Tick(Enemy enemy)
    {
        int id = enemy.GetInstanceID();
        if (!states.TryGetValue(id, out var state))
        {
            state = new WanderState { lastTickTime = Time.time };
            ChooseNewWanderState(state, isInitial: true);
            states[id] = state;
        }

        float dt = Time.time - state.lastTickTime;
        state.lastTickTime = Time.time;
        dt = Mathf.Clamp(dt, 0f, 0.1f);

        bool engaged = false;
        Vector2 awareDir = Vector2.zero;
        if (awareness != PlayerAwarenessMode.Ignore && awarenessStrength > 0f)
        {
            awareDir = GetAwarenessDirection(enemy);
            engaged = (awareDir != Vector2.zero);
        }

        if (engaged)
        {
            TickEngaged(enemy, state, awareDir, dt);
        }
        else
        {
            TickWander(enemy, state, dt);
        }
    }

    public override FacingIntent GetFacingIntent(Enemy enemy)
    {
        int id = enemy.GetInstanceID();
        if (!states.TryGetValue(id, out var state)) return FacingIntent.None;

        if (!state.isMoving) return FacingIntent.None;
        if (Mathf.Abs(state.currentDirection.x) < 0.01f) return FacingIntent.None;

        return state.currentDirection.x > 0f ? FacingIntent.Right : FacingIntent.Left;
    }

    void TickEngaged(Enemy enemy, WanderState state, Vector2 awareDir, float dt)
    {
        Vector2 direction;

        if (awarenessStrength >= 0.99f)
        {
            direction = awareDir;
        }
        else
        {
            if (state.currentDirection.sqrMagnitude < 0.01f)
            {
                state.currentDirection = ChooseRandomDirection();
            }

            direction = Vector2.Lerp(state.currentDirection, awareDir, awarenessStrength);
            if (direction.sqrMagnitude > 0.001f) direction.Normalize();
            else direction = awareDir;
        }

        state.currentDirection = direction;
        state.isMoving = true;

        ApplyMovement(enemy, direction, dt);
    }

    void TickWander(Enemy enemy, WanderState state, float dt)
    {
        state.stateTimer -= dt;
        if (state.stateTimer <= 0f)
        {
            ChooseNewWanderState(state, isInitial: false);
        }

        if (state.isMoving && state.currentDirection.sqrMagnitude > 0.001f)
        {
            ApplyMovement(enemy, state.currentDirection, dt);
        }
    }

    void ApplyMovement(Enemy enemy, Vector2 direction, float dt)
    {
        Vector3 newPos = enemy.transform.position + (Vector3)(direction * moveSpeed * dt);

        // Clamp to asymmetric bounds, both relative to spawn
        float dx = Mathf.Clamp(newPos.x - enemy.spawnPosition.x, boundsMin.x, boundsMax.x);
        float dy = Mathf.Clamp(newPos.y - enemy.spawnPosition.y, boundsMin.y, boundsMax.y);
        newPos.x = enemy.spawnPosition.x + dx;
        newPos.y = enemy.spawnPosition.y + dy;

        enemy.transform.position = newPos;
    }

    void ChooseNewWanderState(WanderState state, bool isInitial)
    {
        if (isInitial) state.isMoving = Random.value < 0.5f;
        else state.isMoving = !state.isMoving;

        if (state.isMoving && !moveHorizontal && !moveVertical)
        {
            state.isMoving = false;
        }

        if (state.isMoving)
        {
            state.stateTimer = Random.Range(minMoveTime, maxMoveTime);
            state.currentDirection = ChooseRandomDirection();
        }
        else
        {
            state.stateTimer = Random.Range(minIdleTime, maxIdleTime);
            state.currentDirection = Vector2.zero;
        }
    }

    Vector2 ChooseRandomDirection()
    {
        bool hOnly = moveHorizontal && !moveVertical;
        bool vOnly = !moveHorizontal && moveVertical;

        if (hOnly) return Random.value < 0.5f ? Vector2.left : Vector2.right;
        if (vOnly) return Random.value < 0.5f ? Vector2.down : Vector2.up;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    Vector2 GetAwarenessDirection(Enemy enemy)
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return Vector2.zero;

        Vector2 toPlayer = (Vector2)(player.transform.position - enemy.transform.position);
        float dist = toPlayer.magnitude;

        if (dist > awarenessRange || dist < 0.001f) return Vector2.zero;

        Vector2 dir = toPlayer / dist;
        return awareness == PlayerAwarenessMode.Approach ? dir : -dir;
    }

    // ─── Gizmos ─────────────────────────────────────────────────────────────
    public override void DrawGizmos(Enemy enemy)
    {
        // Wander bounds — draw as an asymmetric box from min to max offsets,
        // anchored at spawn position
        Vector3 anchor = enemy.GizmoAnchor;
        Vector3 boundsCenter = anchor + new Vector3((boundsMin.x + boundsMax.x) * 0.5f, (boundsMin.y + boundsMax.y) * 0.5f, 0f);
        Vector3 boundsSize = new Vector3(boundsMax.x - boundsMin.x, boundsMax.y - boundsMin.y, 0f);

        Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(boundsCenter, boundsSize);

        // Awareness range — follows the enemy
        if (awareness != PlayerAwarenessMode.Ignore)
        {
            Color awarenessColor = awareness == PlayerAwarenessMode.Approach
                ? new Color(1f, 0.5f, 0.2f, 0.7f)
                : new Color(0.4f, 0.7f, 1f, 0.7f);
            Gizmos.color = awarenessColor;
            Gizmos.DrawWireSphere(enemy.transform.position, awarenessRange);
        }
    }
}