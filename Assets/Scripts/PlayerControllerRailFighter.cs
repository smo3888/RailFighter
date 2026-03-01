using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class PlayerControllerRailFighter : MonoBehaviour
{
    public enum RailType { Horizontal, Vertical }

    public float moveSpeed = 8f;

    [Header("Rail Configuration")]
    public Transform[] rails;
    public int[] railRows;
    public int[] railColumns;
    public RailType[] railTypes;
    public int currentRailIndex = 0;

    public GameObject PlayerLaser;
    public GameObject GameOver;

    [Header("Auto-Fire Settings")]
    public bool autoFireEnabled = true;
    public float fireRate = 1f;
    private float lastFireTime = 0f;

    [Header("Target Lock System")]
    public TargetLockSystem targetLockSystem; // Drag the TargetLockSystem component here

    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;
    public float invincibilityTime = 1.5f;
    private float lastHitTime = -999f;

    [Header("Health Display")]
    public GameObject[] heartSprites;

    [Header("UI")]
    public UIDocument cooldownUI;
    private ProgressBar healthBar;

    [Header("Keyboard Double Tap")]
    public float keyDoubleTapTime = 0.3f;
    private float lastATapTime = -999f;
    private float lastDTapTime = -999f;
    private float lastLeftArrowTapTime = -999f;
    private float lastRightArrowTapTime = -999f;

    private Transform currentRail;
    private float railMinX;
    private float railMaxX;

    void Start()
    {
        lastFireTime = Time.time;
        currentHealth = maxHealth;

        currentRail = rails[currentRailIndex];
        UpdateRailBounds();
        transform.position = new Vector3(currentRail.position.x, currentRail.position.y, 0);

        if (healthBar != null)
        {
            healthBar.highValue = maxHealth;
            healthBar.value = currentHealth;
        }

        UpdateHeartDisplay();

        // Find TargetLockSystem if not assigned
        if (targetLockSystem == null)
        {
            targetLockSystem = FindObjectOfType<TargetLockSystem>();
        }
    }

    void Update()
    {
        // Movement - changes based on rail type
        if (railTypes[currentRailIndex] == RailType.Horizontal)
        {
            // Horizontal rail - move left/right
            float horizontal = Input.GetAxisRaw("Horizontal");

            if (MobileControls.leftPressed) horizontal = -1;
            if (MobileControls.rightPressed) horizontal = 1;

            float newX = transform.position.x + (horizontal * moveSpeed * Time.deltaTime);
            newX = Mathf.Clamp(newX, railMinX, railMaxX);

            transform.position = new Vector3(newX, currentRail.position.y, 0);
        }
        else // Vertical rail
        {
            // Vertical rail - move up/down
            float vertical = Input.GetAxisRaw("Vertical");

            if (MobileControls.bottomPressed) vertical = -1;  // Bottom of screen = down
            if (MobileControls.topPressed) vertical = 1;      // Top of screen = up

            float newY = transform.position.y + (vertical * moveSpeed * Time.deltaTime);
            newY = Mathf.Clamp(newY, railMinX, railMaxX);

            transform.position = new Vector3(currentRail.position.x, newY, 0);
        }

        // Mobile swipe rail change (vertical)
        if (MobileControls.swipedUp)
        {
            TryMoveToRailAbove();
        }
        if (MobileControls.swipedDown)
        {
            TryMoveToRailBelow();
        }

        // Double tap horizontal rail change
        if (MobileControls.doubleTapLeft)
        {
            TryMoveToRailLeft();
        }
        if (MobileControls.doubleTapRight)
        {
            TryMoveToRailRight();
        }

        // Keyboard rail change - Vertical (single press) - ONLY on horizontal rails
        if (railTypes[currentRailIndex] == RailType.Horizontal)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                TryMoveToRailAbove();
            }
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                TryMoveToRailBelow();
            }
        }

        // Keyboard rail change - Horizontal (double tap)
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastATapTime < keyDoubleTapTime)
            {
                TryMoveToRailLeft();
            }
            lastATapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (Time.time - lastLeftArrowTapTime < keyDoubleTapTime)
            {
                TryMoveToRailLeft();
            }
            lastLeftArrowTapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastDTapTime < keyDoubleTapTime)
            {
                TryMoveToRailRight();
            }
            lastDTapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (Time.time - lastRightArrowTapTime < keyDoubleTapTime)
            {
                TryMoveToRailRight();
            }
            lastRightArrowTapTime = Time.time;
        }

        // Auto-fire
        if (autoFireEnabled)
        {
            if (Time.time >= lastFireTime + fireRate)
            {
                lastFireTime = Time.time;
                AutoFireAtNearestEnemy();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                ShootLaser();
            }
        }
    }

    // ============================================
    // HEALTH SYSTEM
    // ============================================
    public void TakeDamage(int damage)
    {
        if (Time.time < lastHitTime + invincibilityTime)
        {
            return;
        }

        lastHitTime = Time.time;
        currentHealth -= damage;

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        UpdateHeartDisplay();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHeartDisplay()
    {
        for (int i = 0; i < heartSprites.Length; i++)
        {
            if (heartSprites[i] != null)
            {
                heartSprites[i].SetActive(i < currentHealth);
            }
        }
    }

    void Die()
    {
        GameOver.SetActive(true);
        Destroy(gameObject);
    }

    // ============================================
    // COLLISION
    // ============================================
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            TakeDamage(1);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            PlayerLaserScript laser = other.GetComponent<PlayerLaserScript>();
            if (laser != null && laser.isEnemyLaser)
            {
                TakeDamage(1);
                Destroy(other.gameObject);
            }
        }
    }

    // ============================================
    // RAIL JUMPING - VERTICAL
    // ============================================
    void TryMoveToRailAbove()
    {
        int currentRow = railRows[currentRailIndex];
        int targetRow = currentRow + 1;

        int bestRail = -1;
        float bestDistance = Mathf.Infinity;

        for (int i = 0; i < rails.Length; i++)
        {
            if (railRows[i] == targetRow)
            {
                float dist = Mathf.Abs(rails[i].position.x - transform.position.x);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestRail = i;
                }
            }
        }

        if (bestRail != -1)
        {
            currentRailIndex = bestRail;
            currentRail = rails[currentRailIndex];
            UpdateRailBounds();
            transform.position = new Vector3(transform.position.x, currentRail.position.y, 0);
        }
    }

    void TryMoveToRailBelow()
    {
        int currentRow = railRows[currentRailIndex];
        int targetRow = currentRow - 1;

        int bestRail = -1;
        float bestDistance = Mathf.Infinity;

        for (int i = 0; i < rails.Length; i++)
        {
            if (railRows[i] == targetRow)
            {
                float dist = Mathf.Abs(rails[i].position.x - transform.position.x);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestRail = i;
                }
            }
        }

        if (bestRail != -1)
        {
            currentRailIndex = bestRail;
            currentRail = rails[currentRailIndex];
            UpdateRailBounds();
            transform.position = new Vector3(transform.position.x, currentRail.position.y, 0);
        }
    }

    // ============================================
    // RAIL JUMPING - HORIZONTAL
    // ============================================
    void TryMoveToRailLeft()
    {
        int currentRow = railRows[currentRailIndex];
        int currentCol = railColumns[currentRailIndex];

        int bestRail = -1;
        int bestCol = -1;

        for (int i = 0; i < rails.Length; i++)
        {
            if (railRows[i] == currentRow && railColumns[i] < currentCol)
            {
                if (bestRail == -1 || railColumns[i] > bestCol)
                {
                    bestCol = railColumns[i];
                    bestRail = i;
                }
            }
        }

        if (bestRail != -1)
        {
            currentRailIndex = bestRail;
            currentRail = rails[currentRailIndex];
            UpdateRailBounds();
            transform.position = new Vector3(currentRail.position.x, currentRail.position.y, 0);
        }
    }

    void TryMoveToRailRight()
    {
        int currentRow = railRows[currentRailIndex];
        int currentCol = railColumns[currentRailIndex];

        int bestRail = -1;
        int bestCol = int.MaxValue;

        for (int i = 0; i < rails.Length; i++)
        {
            if (railRows[i] == currentRow && railColumns[i] > currentCol)
            {
                if (bestRail == -1 || railColumns[i] < bestCol)
                {
                    bestCol = railColumns[i];
                    bestRail = i;
                }
            }
        }

        if (bestRail != -1)
        {
            currentRailIndex = bestRail;
            currentRail = rails[currentRailIndex];
            UpdateRailBounds();
            transform.position = new Vector3(currentRail.position.x, currentRail.position.y, 0);
        }
    }

    // ============================================
    // HELPERS
    // ============================================
    bool IsAdjacent(int fromIndex, int toIndex)
    {
        int rowDiff = Mathf.Abs(railRows[fromIndex] - railRows[toIndex]);
        int colDiff = Mathf.Abs(railColumns[fromIndex] - railColumns[toIndex]);

        if (rowDiff == 0 && colDiff == 1) return true;
        if (rowDiff == 1) return true;

        return false;
    }

    int GetRailIndex(Transform rail)
    {
        for (int i = 0; i < rails.Length; i++)
        {
            if (rails[i] == rail)
            {
                return i;
            }
        }
        return -1;
    }

    void UpdateRailBounds()
    {
        BoxCollider2D railCollider = currentRail.GetComponent<BoxCollider2D>();

        if (railTypes[currentRailIndex] == RailType.Horizontal)
        {
            // Horizontal rail - clamp X position
            float railWidth = railCollider.size.x * currentRail.localScale.x;
            float buffer = 0.1f;
            railMinX = currentRail.position.x - (railWidth / 2) + buffer;
            railMaxX = currentRail.position.x + (railWidth / 2) - buffer;
        }
        else
        {
            // Vertical rail - clamp Y position
            float railHeight = railCollider.size.y * currentRail.localScale.y;
            float buffer = 0.1f;
            railMinX = currentRail.position.y - (railHeight / 2) + buffer;
            railMaxX = currentRail.position.y + (railHeight / 2) - buffer;
        }
    }

    // ============================================
    // SHOOTING
    // ============================================
    void AutoFireAtNearestEnemy()
    {
        GameObject targetEnemy = null;

        // PRIORITY 1: Check if there's a locked target
        if (targetLockSystem != null && targetLockSystem.HasLockedTarget())
        {
            targetEnemy = targetLockSystem.GetLockedTarget();

            // If locked target is dead/destroyed, clear the lock and find nearest
            if (targetEnemy == null || !targetEnemy.activeInHierarchy)
            {
                targetLockSystem.ClearLock();
                targetEnemy = null;
            }
        }

        // PRIORITY 2: If no locked target, find nearest enemy
        if (targetEnemy == null)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Obstacle");

            if (enemies.Length == 0) return;

            GameObject closest = null;
            float closestDist = Mathf.Infinity;

            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy;
                }
            }

            if (closest == null) return;

            // Check for escorts
            RailFighterEnemy patroller = closest.GetComponent<RailFighterEnemy>();
            if (patroller != null && patroller.HasEscorts())
            {
                EscortEnemy[] allEscorts = FindObjectsOfType<EscortEnemy>();
                GameObject nearestEscort = null;
                float nearestEscortDist = Mathf.Infinity;

                foreach (EscortEnemy escort in allEscorts)
                {
                    float dist = Vector3.Distance(transform.position, escort.transform.position);
                    if (dist < nearestEscortDist)
                    {
                        nearestEscortDist = dist;
                        nearestEscort = escort.gameObject;
                    }
                }

                if (nearestEscort != null)
                {
                    closest = nearestEscort;
                }
            }

            targetEnemy = closest;
        }

        // Fire at the target (either locked or nearest)
        if (targetEnemy != null)
        {
            Vector3 direction = (targetEnemy.transform.position - transform.position).normalized;
            Vector3 spawnPos = transform.position + (direction * 0.5f);
            GameObject laser = Instantiate(PlayerLaser, spawnPos, Quaternion.identity);
            laser.GetComponent<PlayerLaserScript>().SetDirection(direction);
        }
    }

    void ShootLaser()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3 direction = (mouseWorldPos - transform.position).normalized;
        Vector3 spawnPos = transform.position + (direction * 0.5f);
        GameObject laser = Instantiate(PlayerLaser, spawnPos, Quaternion.identity);
        laser.GetComponent<PlayerLaserScript>().SetDirection(direction);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth); // Don't exceed max

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        UpdateHeartDisplay(); // Reactivates the heart GameObject
    }
}