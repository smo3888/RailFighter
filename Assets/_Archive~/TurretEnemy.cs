using UnityEngine;

public class TurretEnemy : MonoBehaviour
{
    public TurretSpawnPoint spawnPoint;
    public int health = 2;
    public GameObject TurretHead;

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0) die();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("turret hit by: " + other.gameObject.name + " with tag: " + other.tag);
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }

    void die()
    {
        if (spawnPoint != null) spawnPoint.isOccupied = false;
        WaveManager wm = FindObjectOfType<WaveManager>();
        if (wm != null) wm.EnemyDestroyed();
        Destroy(gameObject);
    }
}