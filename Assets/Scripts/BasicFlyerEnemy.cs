using UnityEngine;

public class BasicFlyerEnemy : MonoBehaviour
{
    // ============================================
    // REFERENCES
    // ============================================
    private Transform player;

    // ============================================
    // BASIC STATS
    // ============================================
    [Header("Stats")]
    public float health = 1f;
    public float attackSpeed = 5f;

    // ============================================
    // START
    // ============================================
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // ============================================
    // UPDATE
    // ============================================
    void Update()
    {
        if (player == null)
        {
            return;
        }

        // Always fly at player
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * attackSpeed * Time.deltaTime;
    }

    // ============================================
    // COLLISION
    // ============================================
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            PlayerLaserScript laser = collision.gameObject.GetComponent<PlayerLaserScript>();
            if (laser != null && laser.isEnemyLaser)
            {
                return;
            }

            Destroy(collision.gameObject);
            Die();
        }
    }
    
    // ============================================
    // DIE
    // ============================================
    void Die()
    {
        FindObjectOfType<WaveManager>().EnemyDestroyed();
        Destroy(gameObject);
    }

   
}