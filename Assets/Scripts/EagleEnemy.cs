using UnityEngine;

public class EagleEnemy : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;
    private Vector3 targetPosition;
    private Vector3 direction;

    public void SetTarget(Vector3 target)
    {
        targetPosition = target;
        direction = (targetPosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerControllerRailFighter>().TakeDamage(damage);
            WaveManager wm = FindObjectOfType<WaveManager>();
            if (wm != null) wm.EnemyDestroyed();
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        WaveManager wm = FindObjectOfType<WaveManager>();
        if (wm != null) wm.EnemyDestroyed();
        Destroy(gameObject);
    }
}