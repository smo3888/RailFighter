using UnityEngine;

public class HolyBorder : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            PlayerLaserScript laser = other.GetComponent<PlayerLaserScript>();
            if (laser != null && !laser.isEnemyLaser)
            {
                TakeDamage(1);
                Destroy(other.gameObject);
            }
        }
    }

    void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Border hit! Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log("Border destroyed!");
            Destroy(gameObject);
        }
    }
}