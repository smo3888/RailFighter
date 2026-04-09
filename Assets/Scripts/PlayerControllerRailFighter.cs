using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class PlayerControllerRailFighter : MonoBehaviour
{
    public float moveSpeed = 8f;

    [Header("Rail Jump - Search Tolerances")]
    public float verticalHorizontalTolerance = 4f;
    public float horizontalVerticalTolerance = 2f;
    public float verticalMaxDistance = 10f;
    public float horizontalMaxDistance = 10f;

    [Header("Rail Jump - Edge Detection")]
    public float edgeCancelBuffer = 0.5f;

    [Header("Rail Jump - Visual")]
    public Color highlightColor = Color.yellow;

    public GameObject PlayerLaser;
    public GameObject GameOver;

    [Header("Spawn")]
    [Tooltip("Drag an empty GameObject here to set the default spawn point in the dungeon.")]
    public Transform defaultSpawnPoint;

    [Header("Auto-Fire Settings")]
    public bool autoFireEnabled = true;
    public float fireRate = 1f;
    private float lastFireTime = 0f;

    [Header("Target Lock System")]
    public TargetLockSystem targetLockSystem;

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

    [Header("QTE Mode (Boss Cutscenes)")]
    public bool qteMode = false;

    private bool canShoot = true;
    private bool isSlowed = false;
    private float slowTimer = 0f;
    private float normalMoveSpeed;
    private Color normalColor;
    private SpriteRenderer playerSpriteRenderer;

    // Rail state
    private Transform currentRail;
    private float railMin;
    private float railMax;
    private bool isOnVerticalRail = false;

    // Two-press jump state
    private Transform pendingRail = null;
    private Vector2 pendingDirection = Vector2.zero;
    private SpriteRenderer pendingRailRenderer = null;
    private Color pendingRailOriginalColor;

    private const string HORIZONTAL_RAIL_TAG = "RailHorizontal";
    private const string VERTICAL_RAIL_TAG = "RailVertical";

    void Start()
    {
        lastFireTime = Time.time;
        currentHealth = maxHealth;

        // Check if we have a dungeon return position to apply
        bool hasReturnData = false;
        if (GameManager.Instance != null)
        {
            string railName = GameManager.Instance.Data.dungeonReturnRailName;
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            hasReturnData = !string.IsNullOrEmpty(railName) && currentScene == GameManager.Instance.dungeonScene;
        }

        if (hasReturnData)
        {
            // Returning from a challenge room — coroutine will handle positioning
            float x = GameManager.Instance.Data.dungeonReturnX;
            float y = GameManager.Instance.Data.dungeonReturnY;
            string railName = GameManager.Instance.Data.dungeonReturnRailName;

            // Still snap to nearest rail initially so the player isn't in limbo
            Transform nearest = FindNearestRailOfAnyType();
            if (nearest != null)
            {
                currentRail = nearest;
                isOnVerticalRail = IsVerticalRail(currentRail);
                UpdateRailBounds();
            }

            Debug.Log($"Applying dungeon return — rail: {railName}, pos: {x}, {y}");
            StartCoroutine(ApplyReturnPosition(x, y, railName));
        }
        else
        {
            // Normal spawn — use defaultSpawnPoint if assigned, otherwise use prefab position
            if (defaultSpawnPoint != null)
                transform.position = new Vector3(defaultSpawnPoint.position.x, defaultSpawnPoint.position.y, 0);

            // Snap to nearest rail from spawn position
            Transform nearest = FindNearestRailOfAnyType();
            if (nearest != null)
            {
                currentRail = nearest;
                isOnVerticalRail = IsVerticalRail(currentRail);
                UpdateRailBounds();
                if (isOnVerticalRail)
                    transform.position = new Vector3(currentRail.position.x, transform.position.y, 0);
                else
                    transform.position = new Vector3(transform.position.x, currentRail.position.y, 0);
            }
        }

        if (healthBar != null)
        {
            healthBar.highValue = maxHealth;
            healthBar.value = currentHealth;
        }

        UpdateHeartDisplay();

        if (targetLockSystem == null)
            targetLockSystem = FindAnyObjectByType<TargetLockSystem>();

        normalMoveSpeed = moveSpeed;
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer)
            normalColor = playerSpriteRenderer.color;
    }

    IEnumerator ApplyReturnPosition(float x, float y, string railName)
    {
        // Wait one frame so all rails are fully initialized
        yield return null;

        GameObject railObj = GameObject.Find(railName);

        if (railObj != null)
        {
            currentRail = railObj.transform;
            isOnVerticalRail = IsVerticalRail(currentRail);
            UpdateRailBounds();

            if (isOnVerticalRail)
                transform.position = new Vector3(currentRail.position.x, Mathf.Clamp(y, railMin, railMax), 0);
            else
                transform.position = new Vector3(Mathf.Clamp(x, railMin, railMax), currentRail.position.y, 0);

            Debug.Log($"Return position applied. Rail: {railName}, final pos: {transform.position}");
        }
        else
        {
            Debug.LogWarning($"Rail '{railName}' not found — falling back to nearest rail.");
            Transform fallback = FindNearestRailOfAnyType();
            if (fallback != null)
            {
                currentRail = fallback;
                isOnVerticalRail = IsVerticalRail(currentRail);
                UpdateRailBounds();
                if (isOnVerticalRail)
                    transform.position = new Vector3(currentRail.position.x, Mathf.Clamp(y, railMin, railMax), 0);
                else
                    transform.position = new Vector3(Mathf.Clamp(x, railMin, railMax), currentRail.position.y, 0);
            }
        }

        // Snap camera instantly to new position so it doesn't drag across the map
        if (Camera.main != null)
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);

        // Clear the return data so it doesn't re-apply on next load
        GameManager.Instance.Data.dungeonReturnX = 0f;
        GameManager.Instance.Data.dungeonReturnY = 0f;
        GameManager.Instance.Data.dungeonReturnRailName = "";
    }

    // Returns the name of the rail the player is currently standing on
    public string GetCurrentRailName()
    {
        return currentRail != null ? currentRail.name : "";
    }

    void Update()
    {
        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
                RemoveSlowDebuff();
        }

        if (!qteMode)
        {
            if (isOnVerticalRail)
            {
                float vertical = Input.GetAxisRaw("Vertical");
                if (MobileControls.topPressed) vertical = 1;
                if (MobileControls.bottomPressed) vertical = -1;

                if (vertical != 0 && pendingRail != null)
                {
                    bool cancelUp = vertical < 0 && pendingDirection == Vector2.up;
                    bool cancelDown = vertical > 0 && pendingDirection == Vector2.down;
                    if (cancelUp || cancelDown) CancelPendingRail();
                }

                float newY = transform.position.y + (vertical * moveSpeed * Time.deltaTime);
                newY = Mathf.Clamp(newY, railMin, railMax);
                transform.position = new Vector3(currentRail.position.x, newY, 0);
            }
            else
            {
                float horizontal = Input.GetAxisRaw("Horizontal");
                if (MobileControls.leftPressed) horizontal = -1;
                if (MobileControls.rightPressed) horizontal = 1;

                if (horizontal != 0 && pendingRail != null)
                {
                    bool cancelLeft = horizontal > 0 && pendingDirection == Vector2.left;
                    bool cancelRight = horizontal < 0 && pendingDirection == Vector2.right;
                    if (cancelLeft || cancelRight) CancelPendingRail();
                }

                float newX = transform.position.x + (horizontal * moveSpeed * Time.deltaTime);
                newX = Mathf.Clamp(newX, railMin, railMax);
                transform.position = new Vector3(newX, currentRail.position.y, 0);
            }
        }

        HandleRailJumpInput();

        if (!qteMode && autoFireEnabled && canShoot)
        {
            if (Time.time >= lastFireTime + fireRate)
            {
                lastFireTime = Time.time;
                AutoFireAtNearestEnemy();
            }
        }
        else if (!qteMode && !autoFireEnabled && canShoot)
        {
            if (Input.GetMouseButtonDown(0))
                ShootLaser();
        }
    }

    // ── Rail Type Detection ──────────────────────────────────────────────────
    bool IsVerticalRail(Transform rail)
    {
        if (rail.CompareTag(VERTICAL_RAIL_TAG)) return true;
        if (rail.CompareTag(HORIZONTAL_RAIL_TAG)) return false;

        BoxCollider2D col = rail.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            float width = col.bounds.size.x;
            float height = col.bounds.size.y;
            return height > width;
        }
        return false;
    }

    // ── Two-Press Rail Jump System ───────────────────────────────────────────
    void HandleRailJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || MobileControls.swipedUp)
            HandleDirectionPress(Vector2.up);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || MobileControls.swipedDown)
            HandleDirectionPress(Vector2.down);
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || MobileControls.doubleTapLeft)
            HandleDirectionPress(Vector2.left);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || MobileControls.doubleTapRight)
            HandleDirectionPress(Vector2.right);
    }

    void HandleDirectionPress(Vector2 direction)
    {
        if (pendingRail != null && pendingDirection == direction)
        {
            ConfirmJump();
            return;
        }

        float edgeThreshold = 0.15f;
        if (isOnVerticalRail)
        {
            if (direction == Vector2.up || direction == Vector2.down)
            {
                bool atTopEdge = transform.position.y >= railMax - edgeThreshold;
                bool atBottomEdge = transform.position.y <= railMin + edgeThreshold;
                if (direction == Vector2.up && !atTopEdge) return;
                if (direction == Vector2.down && !atBottomEdge) return;
            }
        }
        else
        {
            if (direction == Vector2.left || direction == Vector2.right)
            {
                bool atLeftEdge = transform.position.x <= railMin + edgeThreshold;
                bool atRightEdge = transform.position.x >= railMax - edgeThreshold;
                if (direction == Vector2.left && !atLeftEdge) return;
                if (direction == Vector2.right && !atRightEdge) return;
            }
        }

        CancelPendingRail();
        Transform best = FindNearestRailInDirection(direction);
        if (best == null) return;

        pendingRail = best;
        pendingDirection = direction;
        pendingRailRenderer = best.GetComponent<SpriteRenderer>();
        if (pendingRailRenderer != null)
        {
            pendingRailOriginalColor = pendingRailRenderer.color;
            pendingRailRenderer.color = highlightColor;
        }
    }

    void ConfirmJump()
    {
        if (pendingRail == null) return;
        if (pendingRailRenderer != null)
            pendingRailRenderer.color = pendingRailOriginalColor;

        currentRail = pendingRail;
        isOnVerticalRail = IsVerticalRail(currentRail);
        UpdateRailBounds();

        if (isOnVerticalRail)
            transform.position = new Vector3(currentRail.position.x, Mathf.Clamp(transform.position.y, railMin, railMax), 0);
        else
            transform.position = new Vector3(Mathf.Clamp(transform.position.x, railMin, railMax), currentRail.position.y, 0);

        pendingRail = null;
        pendingDirection = Vector2.zero;
        pendingRailRenderer = null;

        if (qteMode) Debug.Log("Player jumped rails during QTE!");
    }

    void CancelPendingRail()
    {
        if (pendingRail == null) return;
        if (pendingRailRenderer != null)
            pendingRailRenderer.color = pendingRailOriginalColor;
        pendingRail = null;
        pendingDirection = Vector2.zero;
        pendingRailRenderer = null;
    }

    Transform FindNearestRailInDirection(Vector2 direction)
    {
        string[] tags = new string[] { HORIZONTAL_RAIL_TAG, VERTICAL_RAIL_TAG };
        Transform best = null;
        float bestDist = Mathf.Infinity;

        foreach (string tag in tags)
        {
            foreach (GameObject railObj in GameObject.FindGameObjectsWithTag(tag))
            {
                Transform rail = railObj.transform;
                if (rail == currentRail) continue;

                float dx = rail.position.x - transform.position.x;
                float dy = rail.position.y - transform.position.y;
                bool targetIsVertical = IsVerticalRail(rail);

                if (direction == Vector2.up || direction == Vector2.down)
                {
                    if (direction == Vector2.up && dy <= 0f) continue;
                    if (direction == Vector2.down && dy >= 0f) continue;
                    if (Mathf.Abs(dy) > verticalMaxDistance) continue;

                    if (targetIsVertical)
                    {
                        if (Mathf.Abs(dx) > verticalHorizontalTolerance) continue;
                    }
                    else
                    {
                        BoxCollider2D railCol = rail.GetComponent<BoxCollider2D>();
                        if (railCol != null)
                        {
                            float jumpTolerance = 1.5f;
                            float railLeft = rail.position.x - (railCol.size.x * rail.localScale.x / 2f) - jumpTolerance;
                            float railRight = rail.position.x + (railCol.size.x * rail.localScale.x / 2f) + jumpTolerance;
                            if (transform.position.x < railLeft || transform.position.x > railRight) continue;
                        }
                    }
                }
                else
                {
                    if (direction == Vector2.left && dx >= 0f) continue;
                    if (direction == Vector2.right && dx <= 0f) continue;
                    if (Mathf.Abs(dy) > horizontalVerticalTolerance) continue;
                    if (Mathf.Abs(dx) > horizontalMaxDistance) continue;
                }

                float dist = Vector2.Distance(transform.position, rail.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = rail;
                }
            }
        }
        return best;
    }

    Transform FindNearestRailOfAnyType()
    {
        Transform nearest = null;
        float nearestDist = Mathf.Infinity;
        string[] tags = new string[] { HORIZONTAL_RAIL_TAG, VERTICAL_RAIL_TAG };

        foreach (string tag in tags)
        {
            foreach (GameObject r in GameObject.FindGameObjectsWithTag(tag))
            {
                float dist = Vector3.Distance(transform.position, r.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = r.transform;
                }
            }
        }
        return nearest;
    }

    // ── Rail Bounds ──────────────────────────────────────────────────────────
    void UpdateRailBounds()
    {
        BoxCollider2D col = currentRail.GetComponent<BoxCollider2D>();
        if (col == null) return;

        float buffer = 0.1f;
        if (isOnVerticalRail)
        {
            railMin = col.bounds.min.y + buffer;
            railMax = col.bounds.max.y - buffer;
        }
        else
        {
            railMin = col.bounds.min.x + buffer;
            railMax = col.bounds.max.x - buffer;
        }
    }

    // ── QTE ─────────────────────────────────────────────────────────────────
    public void EnableQTEMode()
    {
        qteMode = true;
        CancelPendingRail();
        Debug.Log("QTE Mode ENABLED");
    }

    public void DisableQTEMode()
    {
        qteMode = false;
        Debug.Log("QTE Mode DISABLED");
    }

    public void SetCanShoot(bool value) { canShoot = value; }

    // ── Health ───────────────────────────────────────────────────────────────
    public void TakeDamage(int damage)
    {
        if (Time.time < lastHitTime + invincibilityTime) return;
        lastHitTime = Time.time;
        currentHealth -= damage;
        if (healthBar != null) healthBar.value = currentHealth;
        UpdateHeartDisplay();
        if (currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if (healthBar != null) healthBar.value = currentHealth;
        UpdateHeartDisplay();
    }

    // ── Debuffs ──────────────────────────────────────────────────────────────
    public void ApplySlowDebuff(float duration, float slowPercent, Color tintColor)
    {
        if (isSlowed) return;
        isSlowed = true;
        slowTimer = duration;
        moveSpeed = normalMoveSpeed * (1f - slowPercent);
        if (playerSpriteRenderer) playerSpriteRenderer.color = tintColor;
    }

    void RemoveSlowDebuff()
    {
        isSlowed = false;
        slowTimer = 0f;
        moveSpeed = normalMoveSpeed;
        if (playerSpriteRenderer) playerSpriteRenderer.color = normalColor;
    }

    // ── UI ───────────────────────────────────────────────────────────────────
    void UpdateHeartDisplay()
    {
        for (int i = 0; i < heartSprites.Length; i++)
            if (heartSprites[i] != null)
                heartSprites[i].SetActive(i < currentHealth);
    }

    // ── Death ────────────────────────────────────────────────────────────────
    void Die()
    {
        Debug.Log("PLAYER DIED!");
        CancelPendingRail();
        if (GameOver != null) GameOver.SetActive(true);
        Destroy(gameObject);
    }

    // ── Collision ────────────────────────────────────────────────────────────
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
            TakeDamage(1);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
            TakeDamage(1);

        PlayerLaserScript laser = other.GetComponent<PlayerLaserScript>();
        if (laser != null && laser.isEnemyLaser)
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }

    // ── Shooting ─────────────────────────────────────────────────────────────
    void AutoFireAtNearestEnemy()
    {
        GameObject targetEnemy = null;

        if (targetLockSystem != null && targetLockSystem.HasLockedTarget())
        {
            targetEnemy = targetLockSystem.GetLockedTarget();
            if (targetEnemy == null || !targetEnemy.activeInHierarchy)
            {
                targetLockSystem.ClearLock();
                targetEnemy = null;
            }
        }

        if (targetEnemy == null)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Obstacle");
            if (enemies.Length == 0) return;

            GameObject closest = null;
            float closestDist = Mathf.Infinity;
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist) { closestDist = dist; closest = enemy; }
            }

            if (closest == null) return;

            RailFighterEnemy patroller = closest.GetComponent<RailFighterEnemy>();
            if (patroller != null && patroller.HasEscorts())
            {
                EscortEnemy[] allEscorts = FindObjectsOfType<EscortEnemy>();
                GameObject nearestEscort = null;
                float nearestEscortDist = Mathf.Infinity;
                foreach (EscortEnemy escort in allEscorts)
                {
                    float dist = Vector3.Distance(transform.position, escort.transform.position);
                    if (dist < nearestEscortDist) { nearestEscortDist = dist; nearestEscort = escort.gameObject; }
                }
                if (nearestEscort != null) closest = nearestEscort;
            }

            targetEnemy = closest;
        }

        if (targetEnemy != null)
        {
            Vector3 direction = (targetEnemy.transform.position - transform.position).normalized;
            Vector3 spawnPos = transform.position + (direction * 0.5f);
            GameObject laser = Instantiate(PlayerLaser, spawnPos, Quaternion.identity);
            PlayerLaserScript laserScript = laser.GetComponent<PlayerLaserScript>();
            if (laserScript != null) laserScript.SetDirection(direction);
        }
    }

    void ShootLaser()
    {
        Vector3 direction = Vector3.right;
        GameObject laser = Instantiate(PlayerLaser, transform.position, Quaternion.identity);
        PlayerLaserScript laserScript = laser.GetComponent<PlayerLaserScript>();
        if (laserScript != null) laserScript.SetDirection(direction);
    }

    public int GetCurrentHealth() { return currentHealth; }

    public void SetHealth(int amount)
    {
        currentHealth = Mathf.Clamp(amount, 0, maxHealth);
        UpdateHeartDisplay();
    }
}