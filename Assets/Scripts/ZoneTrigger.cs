using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZoneTrigger : MonoBehaviour
{
    [Header("Transition Target")]
    public string targetSceneName;
    public Vector2 spawnPositionInTarget;
    public string spawnRailNameInTarget;

    [Header("Optional Settings")]
    public bool requiresKey = false;
    public string requiredKeyType;

    void Start()
    {
        // Make sure collider is set as trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Optional gate check
        if (requiresKey && !PlayerHasRequiredKey()) return;

        // Save where the player should spawn in the target scene
        GameManager.Instance.Data.dungeonReturnX = spawnPositionInTarget.x;
        GameManager.Instance.Data.dungeonReturnY = spawnPositionInTarget.y;
        GameManager.Instance.Data.dungeonReturnRailName = spawnRailNameInTarget;

        // Trigger the scene transition (fade canvas handles the visual)
        GameManager.Instance.LoadScene(targetSceneName);
    }

    bool PlayerHasRequiredKey()
    {
        // Hook this into your existing key system
        // Example: return GameManager.Instance.Data.purpleKeys >= 1;
        return true;
    }
}