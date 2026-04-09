using UnityEngine;

public class MimicEgg : MonoBehaviour
{
    [Header("Egg Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float hatchTime = 2f;
    [SerializeField] private Sprite crackedSprite;
    [SerializeField] private GameObject babyAlienPrefab;

    [Header("Sprite Scaling")]
    [SerializeField] private Vector3 normalScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 crackedScale = new Vector3(10f, 10f, 1f);

    [Header("Float Animation")]
    [SerializeField] private float floatBobSpeed = 1f;
    [SerializeField] private float floatBobAmount = 0.2f;

    private int currentHealth;
    private float hatchTimer;
    private bool hasCracked = false;
    private float floatStartY;
    private SpriteRenderer spriteRenderer;
    private Mimic boss;

    void Start()
    {
        currentHealth = maxHealth;
        hatchTimer = 0f;
        floatStartY = transform.position.y;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Set initial scale
        transform.localScale = normalScale;
    }

    void Update()
    {
        // FLOATING ANIMATION
        float newY = floatStartY + Mathf.Sin(Time.time * floatBobSpeed) * floatBobAmount;
        transform.position = new Vector3(transform.position.x, newY, 0);

        // HATCH TIMER
        hatchTimer += Time.deltaTime;

        // CRACK AT HALFWAY POINT
        if (!hasCracked && hatchTimer >= hatchTime / 2f && crackedSprite)
        {
            hasCracked = true;
            spriteRenderer.sprite = crackedSprite;
            transform.localScale = crackedScale;
            Debug.Log("Egg cracked!");
        }

        // HATCH AT FULL TIME
        if (hatchTimer >= hatchTime)
        {
            Hatch();
        }
    }

    void Hatch()
    {
        Debug.Log("Egg hatched!");

        // SPAWN BABY ALIEN
        if (babyAlienPrefab)
        {
            Instantiate(babyAlienPrefab, transform.position, Quaternion.identity);
            Debug.Log("Baby alien spawned!");
        }

        if (boss) boss.OnEggDestroyed();
        Destroy(gameObject);
    }

    public void SetBoss(Mimic mimicBoss)
    {
        boss = mimicBoss;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            PlayerLaserScript laser = other.GetComponent<PlayerLaserScript>();
            if (laser && !laser.isEnemyLaser)
            {
                TakeDamage(1);
                Destroy(other.gameObject);
            }
        }
    }

    void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Egg hit! Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log("Egg destroyed!");
            if (boss) boss.OnEggDestroyed();
            Destroy(gameObject);
        }
    }
}