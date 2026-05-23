using UnityEngine;

// ─── Projectile ─────────────────────────────────────────────────────────────
// Team-agnostic projectile MonoBehaviour. Forces its Rigidbody2D into
// Kinematic mode AND its Collider2D into Trigger mode so behavior is
// predictable regardless of how a given prefab is configured.
//
// [ExecuteAlways] lets this script's Update() run in edit mode too.
[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("How long the projectile flies before auto-destroying (seconds)")]
    public float lifetime = 5f;

    [Tooltip("If true, projectile passes through targets instead of being destroyed on hit")]
    public bool piercing = false;

    [Tooltip("If true, the projectile rotates to face its direction of travel.")]
    public bool alignToDirection = true;

    [Tooltip("Extra rotation offset applied after alignment, in degrees.")]
    public float rotationOffset = 0f;

    [Header("Debug")]
    [Tooltip("Log to console when this projectile collides with anything. Helps diagnose pass-through issues.")]
    public bool debugLog = false;

    // ─── Runtime State ──────────────────────────────────────────────────────
    private Vector2 direction;
    private float speed;
    private float damage;
    private DamageType damageType;
    private GameObject source;
    private float age;

    private bool initialized;
    private float lastEditorTickTime;

    public float Damage => damage;
    public DamageType Type => damageType;

    // ─── Reset ──────────────────────────────────────────────────────────────
    void Reset()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.gravityScale = 0f;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    // ─── Awake ──────────────────────────────────────────────────────────────
    // Defensive runtime enforcement. Ensures every projectile that ever
    // spawns has the right Rigidbody2D AND Collider2D config, regardless of
    // how the prefab was set up.
    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    // ─── Initialization ─────────────────────────────────────────────────────
    public void Initialize(Vector2 direction, float speed, float damage, DamageType type, GameObject source)
    {
        this.direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        this.speed = speed;
        this.damage = damage;
        this.damageType = type;
        this.source = source;
        this.age = 0f;
        this.initialized = true;
        this.lastEditorTickTime = Time.realtimeSinceStartup;

        if (alignToDirection)
        {
            float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }
    }

    // ─── Update ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (!initialized) return;

        float dt;
        if (Application.isPlaying)
        {
            dt = Time.deltaTime;
        }
        else
        {
            float now = Time.realtimeSinceStartup;
            dt = now - lastEditorTickTime;
            lastEditorTickTime = now;
            if (dt > 0.1f) dt = 0.1f;
        }

        age += dt;
        if (age >= lifetime)
        {
            DestroySafely();
            return;
        }

        transform.Translate((Vector3)(direction * speed * dt), Space.World);
    }

    // ─── Collision ──────────────────────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (debugLog)
        {
            Debug.Log($"[Projectile] OnTriggerEnter2D: {other.name} (layer {LayerMask.LayerToName(other.gameObject.layer)})");
        }

        if (source != null && other.gameObject == source) return;
        if (other.GetComponent<Projectile>() != null) return;
        if (other.gameObject.layer == gameObject.layer) return;

        // Look for IDamageable on the hit object OR any of its parents.
        // Common pattern: Enemy script on parent, collider on visual child.
        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null) target = other.GetComponentInParent<IDamageable>();

        if (debugLog)
        {
            Debug.Log($"[Projectile] IDamageable on {other.name}: {(target != null ? "FOUND" : "NULL")}");
        }

        if (target != null && target.IsAlive)
        {
            DamagePayload payload = new DamagePayload(damage, damageType, source);
            target.TakeDamage(payload);

            if (!piercing) DestroySafely();
        }
    }

    void DestroySafely()
    {
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }
}