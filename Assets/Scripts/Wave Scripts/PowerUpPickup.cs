using UnityEngine;

// Attach to a world GameObject with a Trigger Collider2D.
// Assign a PowerUpData asset — the icon will auto-populate from it.
public class PowerUpPickup : MonoBehaviour
{
    [Header("Data")]
    public PowerUpData powerUpData;

    [Header("Visual")]
    public SpriteRenderer iconRenderer;

    [Header("Bob Animation")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.2f;

    [Header("Prompt")]
    public GameObject promptUI; // Optional "Press W" label

    private Vector3 startPos;
    private bool playerInRange = false;

    void Start()
    {
        startPos = transform.position;

        if (iconRenderer != null && powerUpData != null)
            iconRenderer.sprite = powerUpData.icon;

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Update()
    {
        // Bob animation
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmount;

        // Keyboard/mobile pickup trigger (same key as zone entry)
        if (playerInRange && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
            Collect();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;

        if (promptUI != null)
            promptUI.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Collect()
    {
        if (powerUpData == null) return;
        PowerUpManager.Instance?.CollectPowerUp(powerUpData);
        Destroy(gameObject);
    }
}