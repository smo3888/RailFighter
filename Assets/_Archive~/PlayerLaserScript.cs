using UnityEngine;

public class PlayerLaserScript : MonoBehaviour
{
    public float speed = 10f;
    public bool isEnemyLaser = false;
    public bool isPiercing = false;   // Set true by ActivatePiercingShot — passes through enemies

    private Rigidbody2D rb;
    private Transform ignoreRail;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        Destroy(gameObject, 3f);
    }

    public void SetDirection(Vector3 dir)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        Vector3 direction = dir.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        if (rb != null) rb.linearVelocity = direction * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject, collision.transform);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject, other.transform);
    }

    void HandleCollision(GameObject obj, Transform trans)
    {
        if (!isEnemyLaser && obj.CompareTag("Player")) return;
        if (isEnemyLaser && obj.CompareTag("Obstacle")) return;
        if (trans == ignoreRail) return;

        // Piercing — pass through enemies (Obstacle tag), only stop on walls/rails
        if (isPiercing && obj.CompareTag("Obstacle") && !isEnemyLaser) return;

        if (obj.CompareTag("Rail") || obj.CompareTag("Obstacle") || obj.CompareTag("Player"))
            Destroy(gameObject);
    }

    public void SetIgnoreRail(Transform rail)
    {
        ignoreRail = rail;
    }
}