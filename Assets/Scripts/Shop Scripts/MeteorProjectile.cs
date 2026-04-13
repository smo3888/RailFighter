using UnityEngine;

public class MeteorProjectile : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject impactEffectPrefab;

    private float fallSpeed;
    private int damage;
    private bool initialized = false;

    void Start()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(float speed, int dmg)
    {
        fallSpeed = speed;
        damage = dmg;
        initialized = true;

        Debug.Log($"Meteor initialized: Speed {fallSpeed}, Damage {damage}");
    }

    void Update()
    {
        if (!initialized) return;

        // Fall downward
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // Destroy if off-screen
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerControllerRailFighter player = other.GetComponent<PlayerControllerRailFighter>();
            if (player)
            {
                player.TakeDamage(damage);
                Debug.Log($"Meteor hit player! Damage: {damage}");
            }

            SpawnImpactEffect();
            Destroy(gameObject);
        }

        if (other.CompareTag("Rail"))
        {
            SpawnImpactEffect();
            Destroy(gameObject);
        }
    }

    void SpawnImpactEffect()
    {
        if (impactEffectPrefab)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }
    }
}