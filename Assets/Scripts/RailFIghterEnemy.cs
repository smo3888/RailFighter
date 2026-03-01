using UnityEngine;
using UnityEngine.UIElements;

public class RailFighterEnemy : MonoBehaviour
{
    // ============================================
    // REFERENCES
    // ============================================
    

    // ============================================
    // BASIC STATS
    // ============================================
    public float moveSpeed = 3f;
    public Transform player;
    public float health = 5f;
    private float maxHealth = 5f;

    // ============================================
    // FLYER AI BEHAVIOR
    // ============================================
    [Header("Flyer AI")]
    public float fleeDistance = 3f;
    public float dashSpeed = 8f;
    public float patrolSpeed = 2f;
    public float shootCooldown = 2f;
    public GameObject enemyBulletPrefab;
    public float bulletSpeed = 5f;

    // ============================================
    // ESCORTS
    // ============================================
    [Header("Escorts")]
    public bool spawnWithEscorts = false;
    public GameObject escortPrefab;
    public int escortCount = 3;
    public float escortOrbitRadius = 1.5f;

    private System.Collections.Generic.List<EscortEnemy> escorts = new System.Collections.Generic.List<EscortEnemy>();

    // ============================================
    // STATE MACHINE
    // ============================================
    private enum FlightState { PatrolCenter, DashToEdge, AtEdge, DashToFleePoint, AtFleePoint, ReturnToCenter }
    private FlightState currentState = FlightState.PatrolCenter;
    private int patrolDirection = 1;
    private Vector3 dashTarget;
    private float centerY;

    // ============================================
    // WOBBLE EFFECT
    // ============================================
    [Header("Wobble")]
    public float wobbleSpeed = 5f;
    public float wobbleAmount = 0.3f;
    public float wobbleAmountMoving = 0.6f;
    private float wobbleOffset;

    // ============================================
    // PLAY AREA BOUNDS
    // ============================================
    [Header("Play Area Bounds")]
    public bool useManualBounds = false;
    public float manualMinX = -10f;
    public float manualMaxX = 13f;
    public float manualMinY = -4.5f;
    public float manualMaxY = 5f;
    public float boundsPadding = 0.5f;

    private float minX;
    private float maxX;
    private float minY;
    private float maxY;

    // ============================================
    // DEBUG
    // ============================================
    [Header("Debug")]
    public bool showDebugGizmos = true;

    // ============================================
    // INTERNAL STATE
    // ============================================
    private float lastShotTime = 0f;

    // ============================================
    // START
    // ============================================
    void Start()
    {

        FindObjectOfType<WaveManager>().EnemySpawned();


    

        player = GameObject.FindGameObjectWithTag("Player").transform;

        wobbleOffset = Random.Range(0f, Mathf.PI * 2f);
        patrolDirection = Random.value > 0.5f ? 1 : -1;

        if (useManualBounds)
        {
            minX = manualMinX;
            maxX = manualMaxX;
            minY = manualMinY;
            maxY = manualMaxY;
        }
        else
        {
            GameObject left = GameObject.FindWithTag("LeftBorder");
            GameObject right = GameObject.FindWithTag("RightBorder");
            GameObject top = GameObject.FindWithTag("TopBorder");
            GameObject bottom = GameObject.FindWithTag("BottomBorder");

            if (left != null)
            {
                Collider2D col = left.GetComponent<Collider2D>();
                minX = col != null ? col.bounds.max.x + boundsPadding : left.transform.position.x + boundsPadding;
            }
            if (right != null)
            {
                Collider2D col = right.GetComponent<Collider2D>();
                maxX = col != null ? col.bounds.min.x - boundsPadding : right.transform.position.x - boundsPadding;
            }
            if (bottom != null)
            {
                Collider2D col = bottom.GetComponent<Collider2D>();
                minY = col != null ? col.bounds.max.y + boundsPadding : bottom.transform.position.y + boundsPadding;
            }
            if (top != null)
            {
                Collider2D col = top.GetComponent<Collider2D>();
                maxY = col != null ? col.bounds.min.y - boundsPadding : top.transform.position.y - boundsPadding;
            }
        }

        centerY = (minY + maxY) / 2f;
        lastShotTime = Time.time;

        // Spawn escorts if enabled
        if (spawnWithEscorts && escortPrefab != null)
        {
            for (int i = 0; i < escortCount; i++)
            {
                float angle = (360f / escortCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * escortOrbitRadius;

                GameObject escort = Instantiate(escortPrefab, transform.position + offset, Quaternion.identity);

                EscortEnemy escortScript = escort.GetComponent<EscortEnemy>();
                if (escortScript != null)
                {
                    escortScript.SetPatroller(this, angle);
                    escorts.Add(escortScript);
                }
            }
        }
    }

    // ============================================
    // ESCORT MANAGEMENT
    // ============================================
    public void AddEscort(EscortEnemy escort)
    {
        escorts.Add(escort);
    }

    public void RemoveEscort(EscortEnemy escort)
    {
        escorts.Remove(escort);
    }

    public bool HasEscorts()
    {
        escorts.RemoveAll(e => e == null);
        return escorts.Count > 0;
    }

    // ============================================
    // GET FLEE POINTS
    // Returns all valid flee points (corners + edge midpoints)
    // ============================================
    Vector3[] GetFleePoints()
    {
        return new Vector3[]
        {
            // Corners
            new Vector3(minX + 1f, minY + 1f, 0),
            new Vector3(minX + 1f, maxY - 1f, 0),
            new Vector3(maxX - 1f, minY + 1f, 0),
            new Vector3(maxX - 1f, maxY - 1f, 0),

            // Edge midpoints
            new Vector3(minX + 1f, centerY, 0),
            new Vector3(maxX - 1f, centerY, 0),
            new Vector3((minX + maxX) / 2f, maxY - 1f, 0),
            new Vector3((minX + maxX) / 2f, minY + 1f, 0),
        };
    }

    // ============================================
    // UPDATE
    // ============================================
    void Update()
    {
        if (showDebugGizmos)
        {
            Debug.DrawLine(new Vector3(minX, minY, 0), new Vector3(maxX, minY, 0), Color.yellow);
            Debug.DrawLine(new Vector3(maxX, minY, 0), new Vector3(maxX, maxY, 0), Color.yellow);
            Debug.DrawLine(new Vector3(maxX, maxY, 0), new Vector3(minX, maxY, 0), Color.yellow);
            Debug.DrawLine(new Vector3(minX, maxY, 0), new Vector3(minX, minY, 0), Color.yellow);
            Debug.DrawLine(new Vector3(minX, centerY, 0), new Vector3(maxX, centerY, 0), Color.green);

            // Draw flee points
            foreach (Vector3 point in GetFleePoints())
            {
                Debug.DrawLine(point + Vector3.up * 0.3f, point - Vector3.up * 0.3f, Color.red);
                Debug.DrawLine(point + Vector3.right * 0.3f, point - Vector3.right * 0.3f, Color.red);
            }
        }

        if (player == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case FlightState.PatrolCenter:
                PatrolCenter();
                ApplyWobble(wobbleAmount);
                if (distanceToPlayer < fleeDistance)
                {
                    StartDashToFleePoint();
                }
                break;

            case FlightState.DashToFleePoint:
                DashToTarget();
                if (Vector3.Distance(transform.position, dashTarget) < 0.1f)
                {
                    currentState = FlightState.AtFleePoint;
                }
                break;

            case FlightState.AtFleePoint:
                ApplyWobble(wobbleAmount);
                if (distanceToPlayer < fleeDistance)
                {
                    StartDashToFleePoint();
                }
                break;
        }

        if (Time.time >= lastShotTime + shootCooldown)
        {
            ShootAtPlayer();
            lastShotTime = Time.time;
        }

        ClampToPlayArea();
        
    }

    // ============================================
    // PATROL CENTER
    // ============================================
    void PatrolCenter()
    {
        transform.position += new Vector3(patrolDirection * patrolSpeed * Time.deltaTime, 0, 0);
        transform.position = new Vector3(transform.position.x, centerY, 0);

        if (transform.position.x >= maxX - 0.5f)
        {
            patrolDirection = -1;
        }
        else if (transform.position.x <= minX + 0.5f)
        {
            patrolDirection = 1;
        }
    }

    // ============================================
    // DASH TO TARGET
    // ============================================
    void DashToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, dashTarget, dashSpeed * Time.deltaTime);
    }

    // ============================================
    // START DASH TO FLEE POINT
    // ============================================
    void StartDashToFleePoint()
    {
        currentState = FlightState.DashToFleePoint;

        Vector3[] fleePoints = GetFleePoints();

        RailFighterEnemy[] allEnemies = FindObjectsOfType<RailFighterEnemy>();

        float bestScore = -999f;
        dashTarget = fleePoints[0];

        foreach (Vector3 point in fleePoints)
        {
            float distFromPlayer = Vector3.Distance(point, player.position);
            float score = distFromPlayer;

            foreach (RailFighterEnemy enemy in allEnemies)
            {
                if (enemy != this)
                {
                    // Penalize points near other enemies
                    float distFromEnemy = Vector3.Distance(point, enemy.transform.position);
                    if (distFromEnemy < 2f)
                    {
                        score -= 5f;
                    }

                    // Penalize points other enemies are heading to
                    float distFromEnemyTarget = Vector3.Distance(point, enemy.dashTarget);
                    if (distFromEnemyTarget < 2f)
                    {
                        score -= 5f;
                    }
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                dashTarget = point;
            }
        }
    }

    // ============================================
    // APPLY WOBBLE
    // ============================================
    void ApplyWobble(float amount)
    {
        float wobbleX = Mathf.Sin((Time.time + wobbleOffset) * wobbleSpeed) * amount * Time.deltaTime;
        float wobbleY = Mathf.Cos((Time.time + wobbleOffset) * wobbleSpeed * 0.7f) * amount * Time.deltaTime;
        transform.position += new Vector3(wobbleX, wobbleY, 0);
    }

    // ============================================
    // SHOOT AT PLAYER
    // ============================================
    void ShootAtPlayer()
    {
        if (enemyBulletPrefab == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        GameObject bullet = Instantiate(enemyBulletPrefab, transform.position, Quaternion.identity);

        PlayerLaserScript laser = bullet.GetComponent<PlayerLaserScript>();
        if (laser != null)
        {
            laser.SetDirection(direction);
        }
    }

    // ============================================
    // CLAMP TO PLAY AREA
    // ============================================
    void ClampToPlayArea()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    // ============================================
    // COLLISION DETECTION
    // ============================================
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            PlayerLaserScript laser = collision.gameObject.GetComponent<PlayerLaserScript>();
            if (laser != null && laser.isEnemyLaser)
            {
                return;
            }

            if (HasEscorts())
            {
                Destroy(collision.gameObject);
                return;
            }

            health -= 1;
           

            Destroy(collision.gameObject);

            if (health <= 0)
            {
                Die();
            }
        }
    }

    // ============================================
    // UPDATE HEALTH BAR POSITION
    // ============================================
    

    // ============================================
    // DIE
    // ============================================
    void Die()
    {
        FindObjectOfType<WaveManager>().EnemyDestroyed();
        Destroy(gameObject);
    }


}