using UnityEngine;

public class BossGate : MonoBehaviour
{
    [Header("Keyhole Sprites")]
    public SpriteRenderer keyhole1;
    public SpriteRenderer keyhole2;
    public SpriteRenderer keyhole3;
    public SpriteRenderer keyhole4;

    [Header("Filled Keyhole Sprites")]
    public Sprite keyhole1Filled; // Purple
    public Sprite keyhole2Filled; // Blue
    public Sprite keyhole3Filled; // Green
    public Sprite keyhole4Filled; // Red

    [Header("Empty Keyhole Sprites")]
    public Sprite keyhole1Empty;
    public Sprite keyhole2Empty;
    public Sprite keyhole3Empty;
    public Sprite keyhole4Empty;

    [Header("Gate")]
    public ZoneTrigger bossDoorTrigger;
    public SpriteRenderer gateRenderer;
    public Sprite gateLockedSprite;
    public Sprite gateUnlockedSprite;

    private int lastKeyCount = -1;

    void Start()
    {
        UpdateGate();

        
        {
            Debug.Log("BossGate Start - GameManager exists: " + (GameManager.Instance != null));
            Debug.Log("Keys: " + (GameManager.Instance != null ? GameManager.Instance.GetKeyCount().ToString() : "NO GAME MANAGER"));
            UpdateGate();
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        int currentKeys = GameManager.Instance.GetKeyCount();

        // Only update when key count changes
        if (currentKeys != lastKeyCount)
        {
            lastKeyCount = currentKeys;
            UpdateGate();
        }
    }

    void UpdateGate()
    {
        if (GameManager.Instance == null) return;

        int keys = GameManager.Instance.GetKeyCount();

        // Update keyhole sprites
        if (keyhole1 != null)
            keyhole1.sprite = keys >= 1 ? keyhole1Filled : keyhole1Empty;
        if (keyhole2 != null)
            keyhole2.sprite = keys >= 2 ? keyhole2Filled : keyhole2Empty;
        if (keyhole3 != null)
            keyhole3.sprite = keys >= 3 ? keyhole3Filled : keyhole3Empty;
        if (keyhole4 != null)
            keyhole4.sprite = keys >= 4 ? keyhole4Filled : keyhole4Empty;

        // Unlock gate when all 4 keys collected
        bool unlocked = keys >= 4;

        if (bossDoorTrigger != null)
            bossDoorTrigger.enabled = unlocked;

        if (gateRenderer != null)
        {
            if (unlocked && gateUnlockedSprite != null)
                gateRenderer.sprite = gateUnlockedSprite;
            else if (!unlocked && gateLockedSprite != null)
                gateRenderer.sprite = gateLockedSprite;
        }

        if (unlocked)
            Debug.Log("Boss gate unlocked!");
    }
}