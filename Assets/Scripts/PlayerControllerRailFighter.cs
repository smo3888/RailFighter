using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class PlayerControllerRailFighter : MonoBehaviour
{
    public float moveSpeed = 8f;
    public Transform[] rails;
    public int[] railRows;
    public int[] railColumns;
    public int currentRailIndex = 0;
    public GameObject PlayerLaser;
    public GameObject GameOver;
    public float jumpCooldown = 1f;
    private float lastJumpTime = 0f;

    [Header("Auto-Fire Settings")]
    public bool autoFireEnabled = true;
    public float fireRate = 1f;
    private float lastFireTime = 0f;

    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;
    public float invincibilityTime = 1.5f;
    private float lastHitTime = -999f;

    [Header("Health Display")]
    public GameObject[] heartSprites;

    [Header("UI")]
    public UIDocument cooldownUI;
    private ProgressBar cooldownBar;
    private ProgressBar healthBar;

    private Transform currentRail;
    private float railMinX;
    private float railMaxX;

    void Start()
    {
        lastFireTime = Time.time;
        lastJumpTime = Time.time;
        currentHealth = maxHealth;
        
        currentRail = rails[currentRailIndex];
        UpdateRailBounds();
        transform.position = new Vector3(currentRail.position.x, currentRail.position.y, 0);

        var root = cooldownUI.rootVisualElement;
        cooldownBar = root.Q<ProgressBar>("CooldownBar");
        healthBar = root.Q<ProgressBar>("HealthBar");

        if (healthBar != null)
        {
            healthBar.highValue = maxHealth;
            healthBar.value = currentHealth;
        }

        UpdateHeartDisplay();
    }

    void Update()
    {
        // Movement
        float horizontal = Input.GetAxisRaw("Horizontal");

        if (MobileControls.leftPressed) horizontal = -1;
        if (MobileControls.rightPressed) horizontal = 1;

        float newX = transform.position.x + (horizontal * moveSpeed * Time.deltaTime);
        newX = Mathf.Clamp(newX, railMinX, railMaxX);

        transform.position = new Vector3(newX, currentRail.position.y, 0);

        // Desktop rail click
        if (Input.GetMouseButtonDown(0))
        {
            CheckRailClick();
        }

        // Mobile swipe rail change
        if (MobileControls.swipedUp)
        {
            TryMoveToRailAbove();
        }
        if (MobileControls.swipedDown)
        {
            TryMoveToRailBelow();
        }

        UpdateCooldownBar();

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

    void TryMoveToRailAbove()
    {
        if (Time.time - lastJumpTime < jumpCooldown) return;

        int currentRow = railRows[currentRailIndex];

        for (int i = 0; i < rails.Length; i++)
        {
            if (railRows[i] == currentRow + 1)
            {
                currentRailIndex = i;
                currentRail = rails[currentRailIndex];
                UpdateRailBounds();
                transform.position = new Vector3(transform.position.x, currentRail.position.y, 0);
                lastJumpTime = Time.time;
                return;
            }
        }
    }

    void TryMoveToRailBelow()
    {
        if (Time.time - lastJumpTime < jumpCooldown) return;

        int currentRow = railRows[currentRailIndex];

        for (int i = 0; i < rails.Length; i++)
        {
            if (railRows[i] == currentRow - 1)
            {
                currentRailIndex = i;
                currentRail = rails[currentRailIndex];
                UpdateRailBounds();
                transform.position = new Vector3(transform.position.x, currentRail.position.y, 0);
                lastJumpTime = Time.time;
                return;
            }
        }
    }

    void UpdateCooldownBar()
    {
        float timePassed = Time.time - lastJumpTime;

        if (timePassed < jumpCooldown)
        {
            cooldownBar.value = timePassed / jumpCooldown;
        }
        else
        {
            cooldownBar.value = 1;
        }
    }

    void CheckRailClick()
    {
        if (Time.time - lastJumpTime < jumpCooldown)
        {
            return;
        }

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag("Rail"))
        {
            Transform clickedRail = hit.collider.transform;
            int clickedIndex = GetRailIndex(clickedRail);

            if (clickedIndex != -1 && IsAdjacent(currentRailIndex, clickedIndex))
            {
                currentRailIndex = clickedIndex;
                currentRail = rails[currentRailIndex];
                UpdateRailBounds();

                transform.position = new Vector3(mousePos.x, currentRail.position.y, 0);

                lastJumpTime = Time.time;
            }
        }
    }

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
        float railWidth = railCollider.size.x * currentRail.localScale.x;

        float buffer = 0.1f;
        railMinX = currentRail.position.x - (railWidth / 2) + buffer;
        railMaxX = currentRail.position.x + (railWidth / 2) - buffer;
    }

    void AutoFireAtNearestEnemy()
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

        Vector3 direction = (closest.transform.position - transform.position).normalized;
        Vector3 spawnPos = transform.position + (direction * 0.5f);
        GameObject laser = Instantiate(PlayerLaser, spawnPos, Quaternion.identity);
        laser.GetComponent<PlayerLaserScript>().SetDirection(direction);
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
}