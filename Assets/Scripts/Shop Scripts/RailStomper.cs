using UnityEngine;

public class RailStomper : MonoBehaviour
{
    
    private enum State { Floating, Slamming, Drilled }
    private State currentState = State.Floating;

    [Header("Timing")]
    [SerializeField] private float floatDuration = 3f;
    private float floatTimer;
    [SerializeField] private float slamSpeed = 15f;

    [Header("Damage")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float damageInterval = 3f;
    private float damageTimer;

    [Header("References")]
    private GameObject targetRail;
    private SpriteRenderer railSpriteRenderer;
    private Color originalRailColor;
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;

    [Header("Positioning")]
    [SerializeField] private float spawnHeightAboveRail = 3f;
    [SerializeField] private float railEmbedDepth = 0.5f;

    private WaveManager waveManager;

    void Start()
    {
        currentHealth = maxHealth;
        floatTimer = floatDuration;
        damageTimer = damageInterval;

        waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.EnemySpawned();
        }

        FindAndTargetRail();
    }

    void FindAndTargetRail()
    {
        RailStomperSpawn[] spawnableRails = FindObjectsOfType<RailStomperSpawn>();

        if (spawnableRails.Length > 0)
        {
            // Filter to only AVAILABLE rails (not occupied)
            System.Collections.Generic.List<RailStomperSpawn> availableRails = new System.Collections.Generic.List<RailStomperSpawn>();
            foreach (RailStomperSpawn spawnPoint in spawnableRails)
            {
                if (!spawnPoint.IsOccupied)
                {
                    availableRails.Add(spawnPoint);
                }
            }

            if (availableRails.Count > 0)
            {
                // Pick random available rail
                RailStomperSpawn selectedSpawn = availableRails[Random.Range(0, availableRails.Count)];
                targetRail = selectedSpawn.gameObject;

                // Mark this rail as occupied
                selectedSpawn.MarkOccupied();

                // Position stomper
                Vector3 railCenter = targetRail.transform.position;
                transform.position = new Vector3(railCenter.x, railCenter.y + spawnHeightAboveRail, railCenter.z);

                railSpriteRenderer = targetRail.GetComponent<SpriteRenderer>();
                if (railSpriteRenderer != null)
                {
                    originalRailColor = railSpriteRenderer.color;
                }
            }
            else
            {
                Debug.LogWarning("All spawnable rails are occupied!");
            }
        }
        else
        {
            Debug.LogError("No rails with RailStomperSpawn component found!");
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Floating:
                HandleFloating();
                break;
            case State.Slamming:
                HandleSlamming();
                break;
            case State.Drilled:
                HandleDrilled();
                break;
        }
    }

    void HandleFloating()
    {
        floatTimer -= Time.deltaTime;

        if (floatTimer <= 0f)
        {
            currentState = State.Slamming;
        }
    }

    void HandleSlamming()
    {
        transform.position += Vector3.down * slamSpeed * Time.deltaTime;

        if (targetRail != null && transform.position.y <= targetRail.transform.position.y)
        {
            transform.position = new Vector3(transform.position.x, targetRail.transform.position.y + railEmbedDepth, transform.position.z);

            if (railSpriteRenderer != null)
            {
                railSpriteRenderer.color = dangerColor;
            }

            currentState = State.Drilled;
        }
    }

    void HandleDrilled()
    {
        damageTimer -= Time.deltaTime;

        if (damageTimer <= 0f)
        {
            DamagePlayerIfOnRail();
            damageTimer = damageInterval;
        }
    }

    void DamagePlayerIfOnRail()
    {
        PlayerControllerRailFighter player = FindObjectOfType<PlayerControllerRailFighter>();

        if (player != null && targetRail != null)
        {
            if (IsPlayerOnThisRail(player))
            {
                player.TakeDamage(damageAmount);
            }
        }
    }

    bool IsPlayerOnThisRail(PlayerControllerRailFighter player)
    {
        float railY = targetRail.transform.position.y;
        float playerY = player.transform.position.y;

        return Mathf.Abs(playerY - railY) < 0.5f;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (railSpriteRenderer != null)
        {
            railSpriteRenderer.color = originalRailColor;
        }

        // Mark rail as available again
        if (targetRail != null)
        {
            RailStomperSpawn spawn = targetRail.GetComponent<RailStomperSpawn>();
            if (spawn != null)
            {
                spawn.MarkAvailable();
            }
        }

        if (waveManager != null)
        {
            waveManager.EnemyDestroyed();
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (railSpriteRenderer != null)
        {
            railSpriteRenderer.color = originalRailColor;
        }

        // Mark rail as available again (safety)
        if (targetRail != null)
        {
            RailStomperSpawn spawn = targetRail.GetComponent<RailStomperSpawn>();
            if (spawn != null)
            {
                spawn.MarkAvailable();
            }
        }
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