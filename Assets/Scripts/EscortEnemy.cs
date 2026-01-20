using UnityEngine;

public class EscortEnemy : MonoBehaviour
{
    // ============================================
    // REFERENCES
    // ============================================
    private RailFighterEnemy patroller;

    // ============================================
    // BASIC STATS
    // ============================================
    [Header("Stats")]
    public float health = 1f;
    public float orbitSpeed = 2f;
    public float orbitRadius = 1.5f;

    // ============================================
    // ORBIT
    // ============================================
    private float orbitAngle;

    // ============================================
    // SET PATROLLER
    // ============================================
    public void SetPatroller(RailFighterEnemy target, float startAngle)
    {
        patroller = target;
        orbitAngle = startAngle;

        // Snap to orbit position immediately
        if (patroller != null)
        {
            float x = Mathf.Cos(orbitAngle) * orbitRadius;
            float y = Mathf.Sin(orbitAngle) * orbitRadius;
            transform.position = patroller.transform.position + new Vector3(x, y, 0);
        }
    }

    // ============================================
    // UPDATE
    // ============================================
    void Update()
    {
        if (patroller == null)
        {
            Destroy(gameObject);
            return;
        }

        orbitAngle += orbitSpeed * Time.deltaTime;

        float x = Mathf.Cos(orbitAngle) * orbitRadius;
        float y = Mathf.Sin(orbitAngle) * orbitRadius;

        transform.position = patroller.transform.position + new Vector3(x, y, 0);
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
        if (patroller != null)
        {
            patroller.RemoveEscort(this);
        }

        Destroy(gameObject);
    }
}