using UnityEngine;

// Dummy enemy for testing — infinite health, does nothing.
// Add to a GameObject with a SpriteRenderer and a Collider2D tagged "Obstacle".
public class DummyEnemy : MonoBehaviour
{
    [Header("Visual Feedback")]
    public SpriteRenderer spriteRenderer;
    public Color hitColor = Color.red;
    public float hitFlashDuration = 0.1f;

    private Color originalColor;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Register with WaveManager if one exists (so it doesn't break wave tracking)
        WaveManager wm = FindObjectOfType<WaveManager>();
        if (wm != null) wm.EnemySpawned();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerLaserScript laser = other.GetComponent<PlayerLaserScript>();
        if (laser != null && !laser.isEnemyLaser)
        {
            Debug.Log("Dummy hit!");
            StartCoroutine(FlashHit());
            if (!laser.isPiercing)
                Destroy(other.gameObject);
        }
    }

    System.Collections.IEnumerator FlashHit()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}