using UnityEngine;

public class EagleLordTurret : MonoBehaviour
{
    [Header("Turret Settings")]
    [SerializeField] private string turretSide;
    [SerializeField] private int maxHealth = 15;
    private int currentHealth;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 1.5f;
    private float lastFireTime;

    private EagleLord boss;
    private bool canShoot = true;

    void Start()
    {
        currentHealth = maxHealth;
        lastFireTime = Time.time;
        boss = GetComponentInParent<EagleLord>();
    }

    void Update()
    {
        if (canShoot && Time.time >= lastFireTime + fireRate)
        {
            lastFireTime = Time.time;
            ShootAtPlayer();
        }
    }

    void ShootAtPlayer()
    {
        PlayerControllerRailFighter player = FindAnyObjectByType<PlayerControllerRailFighter>();
        if (player == null) return;

        Vector3 direction = (player.transform.position - transform.position).normalized;
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        PlayerLaserScript laser = projectile.GetComponent<PlayerLaserScript>();
        if (laser != null)
        {
            laser.isEnemyLaser = true;
            laser.SetDirection(direction);
        }
    }

    public void EnableShooting()
    {
        canShoot = true;
    }

    public void DisableShooting()
    {
        canShoot = false;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Tell boss about the damage so health bar updates immediately
        if (boss != null)
        {
            boss.UpdateEncounterHealth(damage);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (boss != null && (turretSide == "Left" || turretSide == "Right"))
        {
            boss.TurretDestroyed(turretSide);
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }
}