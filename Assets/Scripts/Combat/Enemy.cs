using System.Collections;
using UnityEngine;

// ─── Enemy ──────────────────────────────────────────────────────────────────
// Implements IDamageable, runs MovementPatternSO + AttackPatternSO, handles
// hit flash/death/contact damage, sprite flipping, and gizmo anchoring.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    public static bool ShowAllGizmos = false;

    [Header("Configuration")]
    [Tooltip("EnemyData asset that defines this enemy's stats. Required.")]
    public EnemyData data;

    [Tooltip("Movement pattern SO. How this enemy moves each frame. Leave null for stationary.")]
    public MovementPatternSO movement;

    [Tooltip("Attack pattern SO. How this enemy fights. Leave null for non-attacking.")]
    public AttackPatternSO attackPattern;

    [Header("Facing & Visual")]
    [Tooltip("Automatically flip the enemy horizontally based on movement direction.")]
    public bool flipWithMovement = true;

    [Tooltip("Which direction the sprite is drawn facing by default.")]
    public FacingDirection defaultFacing = FacingDirection.Right;

    [Tooltip("Fallback minimum horizontal movement to trigger a flip when the pattern doesn't " +
             "broadcast its facing intent.")]
    public float flipMovementThreshold = 0.001f;

    [Header("Debug")]
    public bool debugLog = true;

    public enum FacingDirection { Right, Left }

    // ─── Runtime State ──────────────────────────────────────────────────────
    [HideInInspector] public Vector3 spawnPosition;
    [HideInInspector] public bool isSpawned = false;
    [Tooltip("Set true by attack patterns to suppress the movement pattern's Tick. " +
         "Used during aggro phases where the attack drives motion directly.")]
    public bool movementSuppressed = false;
    private float currentHealth;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine activeFlashCoroutine;

    public bool IsAlive => currentHealth > 0;

    // ─── GizmoAnchor ────────────────────────────────────────────────────────
    // The position gizmos should anchor to for "fixed reference frame" things
    // like bounds, patrol ranges. After spawn, this is spawnPosition. Before
    // spawn (placing in scene), it's the current transform position.
    public Vector3 GizmoAnchor => isSpawned ? spawnPosition : transform.position;

    // ─── Lifecycle ──────────────────────────────────────────────────────────
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (data == null)
        {
            Debug.LogError($"[Enemy:{name}] No EnemyData assigned. Defaulting to 1 HP.");
            currentHealth = 1f;
        }
        else
        {
            currentHealth = data.maxHealth;
        }

        OnSpawn();
    }

    public void OnSpawn()
    {
        spawnPosition = transform.position;
        isSpawned = true;
    }

    void Update()
    {
        if (!IsAlive) return;
        RunFrame();
    }

    public void RunFrame()
    {
        Vector3 prevPosition = transform.position;

        if (movement != null && !movementSuppressed) movement.Tick(this);
        if (attackPattern != null) attackPattern.Tick(this);

        if (flipWithMovement) UpdateFacing(prevPosition);
    }

    // ─── Facing ─────────────────────────────────────────────────────────────
    void UpdateFacing(Vector3 prevPosition)
    {
        // Attack patterns drive motion AND facing during suppression — skip entirely
        if (movementSuppressed) return;

        int targetSign = 0;

        if (movement != null)
        {
            var intent = movement.GetFacingIntent(this);
            if (intent == MovementPatternSO.FacingIntent.Right) targetSign = 1;
            else if (intent == MovementPatternSO.FacingIntent.Left) targetSign = -1;
        }

        if (targetSign == 0)
        {
            float xDelta = transform.position.x - prevPosition.x;
            if (Mathf.Abs(xDelta) >= flipMovementThreshold)
            {
                targetSign = xDelta > 0f ? 1 : -1;
            }
        }

        if (targetSign == 0) return;

        if (defaultFacing == FacingDirection.Left) targetSign = -targetSign;

        Vector3 scale = transform.localScale;
        float currentSign = Mathf.Sign(scale.x);
        if (Mathf.Approximately(currentSign, targetSign)) return;

        scale.x = Mathf.Abs(scale.x) * targetSign;
        transform.localScale = scale;
    }
    // ─── Damage Handling ────────────────────────────────────────────────────
    public void TakeDamage(DamagePayload payload)
    {
        if (!IsAlive) return;

        currentHealth -= payload.amount;

        if (debugLog)
        {
            string n = data != null ? data.displayName : gameObject.name;
            Debug.Log($"[{n}] took {payload.amount} {payload.type} damage. " +
                      $"HP: {currentHealth}/{(data != null ? data.maxHealth : 0)}");
        }

        if (data != null && spriteRenderer != null)
        {
            if (activeFlashCoroutine != null) StopCoroutine(activeFlashCoroutine);
            activeFlashCoroutine = StartCoroutine(HitFlash());
        }

        if (currentHealth <= 0) Die();
    }

    IEnumerator HitFlash()
    {
        spriteRenderer.color = data.hitFlashColor;
        yield return new WaitForSeconds(data.hitFlashDuration);
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        activeFlashCoroutine = null;
    }

    void Die()
    {
        if (debugLog)
        {
            string n = data != null ? data.displayName : gameObject.name;
            Debug.Log($"[{n}] died.");
        }

        if (data != null && data.deathEffectPrefab != null)
        {
            Instantiate(data.deathEffectPrefab, transform.position, Quaternion.identity);
        }

        WaveManager wm = FindAnyObjectByType<WaveManager>();
        if (wm != null) wm.EnemyDestroyed();

        Destroy(gameObject);
    }

    // ─── Contact Damage ─────────────────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other) => TryDealContactDamage(other.gameObject);
    void OnCollisionEnter2D(Collision2D collision) => TryDealContactDamage(collision.gameObject);

    void TryDealContactDamage(GameObject other)
    {
        if (!IsAlive) return;
        if (data == null || data.contactDamage <= 0f) return;
        if (other == gameObject) return;
        if (other.GetComponent<Enemy>() != null) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null) target = other.GetComponentInParent<IDamageable>();
        if (target == null || !target.IsAlive) return;

        DamagePayload payload = new DamagePayload(
            data.contactDamage,
            DamageType.Contact,
            gameObject);
        target.TakeDamage(payload);
    }

    // ─── Gizmos ─────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (!ShowAllGizmos) return;
        DrawPatternGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (ShowAllGizmos) return;
        DrawPatternGizmos();
    }

    void DrawPatternGizmos()
    {
        // Pass the Enemy itself so individual gizmos can choose whether to
        // use GizmoAnchor (fixed) or transform.position (follows).
        if (movement != null) movement.DrawGizmos(this);
        if (attackPattern != null) attackPattern.DrawGizmos(this);
    }
}