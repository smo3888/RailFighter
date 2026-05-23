using UnityEngine;

public class DiveEagle : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float diveSpeed = 5f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Bounds")]
    [SerializeField] private float bottomBound = -10f; // Y position to destroy at

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        // Move straight down
        transform.position += Vector3.down * diveSpeed * Time.deltaTime;

        // Destroy when off-screen at bottom
        if (transform.position.y < bottomBound)
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Hit by player bullet
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }

        // Hit player
        if (other.CompareTag("Player"))
        {
            PlayerControllerRailFighter player = other.GetComponent<PlayerControllerRailFighter>();
            if (player != null)
            {
                player.TakeDamage(1);
            }
            Die();
        }
    }
}