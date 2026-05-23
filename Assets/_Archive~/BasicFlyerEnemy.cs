using UnityEngine;

public class BasicFlyerEnemy : MonoBehaviour
{
    private Transform player;

    [Header("Stats")]
    public float health = 1f;
    public float attackSpeed = 5f;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * attackSpeed * Time.deltaTime;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            PlayerLaserScript laser = collision.gameObject.GetComponent<PlayerLaserScript>();
            if (laser != null && laser.isEnemyLaser) return;
            Destroy(collision.gameObject);
            Die();
        }
    }

    void Die()
    {
        WaveManager wm = FindObjectOfType<WaveManager>();
        if (wm != null) wm.EnemyDestroyed();
        Destroy(gameObject);
    }
}