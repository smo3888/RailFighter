using UnityEngine;

public class TurretEnemy : MonoBehaviour
{
    public TurretSpawnPoint spawnPoint;
    public int health = 2;
    public GameObject TurretHead;

    void Start()
    {
        
    }

    void Update()
    {

    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0) // Changed from < to <=
        {
            die();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("turret hit by: " + other.gameObject.name + " with tag: " + other.tag);
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(1);
            Destroy(other.gameObject); // Destroy the BULLET, not the turret
        }
    }

    void die()
    {
        if (spawnPoint != null) // Fixed: was == null, should be != null
        {
            spawnPoint.isOccupied = false;
        }
        FindObjectOfType<WaveManager>().EnemyDestroyed(); // Changed from EnemyKilled
        Destroy(gameObject);
    }
}