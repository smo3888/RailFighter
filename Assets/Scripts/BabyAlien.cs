using UnityEngine;

public class BabyAlien : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Health")]
    public int maxHealth = 1;
    private int currentHealth;

    [Header("Debuff Settings")]
    public float slowDuration = 3f;
    public float slowPercentage = 0.5f;
    public Color debuffColor = new Color(0, 1, 0, 0.5f);

    private Transform player;
    private WaveManager waveManager;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.EnemySpawned();
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            if (spriteRenderer != null)
            {
                if (direction.x > 0)
                {
                    spriteRenderer.flipX = false;
                }
                else if (direction.x < 0)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }
    }

    void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Baby Alien hit! Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (waveManager != null)
        {
            waveManager.EnemyDestroyed();
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerControllerRailFighter playerController = other.GetComponent<PlayerControllerRailFighter>();
            if (playerController != null)
            {
                playerController.ApplySlowDebuff(slowDuration, slowPercentage, debuffColor);
                Debug.Log("Baby Alien applied slowness debuff!");
            }

            if (waveManager != null)
            {
                waveManager.EnemyDestroyed();
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Projectile"))
        {
            PlayerLaserScript laser = other.GetComponent<PlayerLaserScript>();
            if (laser != null && !laser.isEnemyLaser)
            {
                TakeDamage(1);
                Destroy(other.gameObject);
            }
        }
    }
}