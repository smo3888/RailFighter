using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    // ============================================
    // WAVE CLASS - Configurable per wave
    // ============================================
    [System.Serializable]
    public class Wave
    {
        [Header("Wave Settings")]
        public int totalEnemies = 10;

        [Header("Enemy Types")]
        public EnemySpawnSettings[] allowedEnemies;

        [Header("Special Enemies - Turrets")]
        public bool spawnTurrets = false;
        public int maxTurrets = 0;
        public float turretSpawnInterval = 10f;

        [Header("Special Enemies - Eagles")]
        public bool spawnEagles = false;
        public int maxEagles = 0;
        public float eagleSpawnInterval = 5f;

        [Header("Spawn Rate")]
        public float enemySpawnInterval = 2f;
    }

    // ============================================
    // ENEMY SPAWN SETTINGS CLASS
    // ============================================
    [System.Serializable]
    public class EnemySpawnSettings
    {
        public GameObject enemyPrefab;
        public int maxSpawnCount;
        public bool canSpawn = true;
    }

    // ============================================
    // WAVES ARRAY
    // ============================================
    [Header("Waves")]
    public Wave[] waves;

    // ============================================
    // SPECIAL ENEMY PREFABS
    // ============================================
    [Header("Special Enemies")]
    public GameObject turretPrefab;
    public TurretSpawnPoint[] turretSpawnPoints;
    public GameObject EnemyLockonPrefab;

    // ============================================
    // SPAWNING SETTINGS
    // ============================================
    [Header("Spawning")]
    public int maxEnemiesOnScreen = 10;
    public float minDistanceFromPlayer = 5f;
    public float minDistanceFromEnemies = 3f;
    public int maxSpawnAttempts = 10;

    // ============================================
    // PLAY AREA BOUNDS
    // ============================================
    [Header("Play Area Bounds")]
    public bool useManualBounds = false;
    public float manualMinX = -10f;
    public float manualMaxX = 13f;
    public float manualMinY = -4.5f;
    public float manualMaxY = 5f;
    public float boundsPadding = 1f;

    private float minX, maxX, minY, maxY;

    // ============================================
    // UI REFERENCES
    // ============================================
    [Header("UI")]
    public UIDocument uiDocument;
    private Label waveLabel;
    private Label enemyLabel;

    // ============================================
    // BORDER REFERENCES
    // ============================================
    [Header("Borders")]
    public GameObject Border_Top;
    public GameObject Border_Bottom;
    public GameObject Border_Left;
    public GameObject Border_Right;

    // ============================================
    // INTERNAL TRACKING
    // ============================================
    private Transform player;
    private int currentWave = 0;
    private int enemiesSpawned = 0;
    private float spawnTimer = 0f;
    public int activeEnemyCount = 0;
    private int[] enemyTypeSpawnedCount;
    private bool allWavesComplete = false;
    private bool waveSpawningComplete = false;

    // Special enemy tracking
    private int turretsSpawned = 0;
    private int eaglesSpawned = 0;
    private float nextTurretSpawnTime;
    private float nextEagleSpawnTime;

    // ============================================
    // START
    // ============================================
    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

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

        if (uiDocument != null)
        {
            waveLabel = uiDocument.rootVisualElement.Q<Label>("WaveLabel");
            enemyLabel = uiDocument.rootVisualElement.Q<Label>("EnemyLabel");
        }

        StartWave(0);
    }

    // ============================================
    // UPDATE
    // ============================================
    void Update()
    {
        if (allWavesComplete)
        {
            return;
        }

        // SAFETY CHECK - prevent array out of bounds
        if (currentWave >= waves.Length)
        {
            allWavesComplete = true;
            return;
        }

        Wave currentWaveData = waves[currentWave];

        // Check if all spawning is complete
        if (!waveSpawningComplete)
        {
            if (enemiesSpawned >= currentWaveData.totalEnemies &&
                turretsSpawned >= currentWaveData.maxTurrets &&
                eaglesSpawned >= currentWaveData.maxEagles)
            {
                waveSpawningComplete = true;
            }
        }

        // Spawn regular enemies
        if (enemiesSpawned < currentWaveData.totalEnemies &&
            activeEnemyCount < maxEnemiesOnScreen)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentWaveData.enemySpawnInterval)
            {
                SpawnEnemy();
                spawnTimer = 0f;
            }
        }

        // Turret Spawning (if enabled and under limit)
        if (currentWaveData.spawnTurrets &&
            turretsSpawned < currentWaveData.maxTurrets &&
            Time.time >= nextTurretSpawnTime)
        {
            SpawnTurret();
            turretsSpawned++;
            nextTurretSpawnTime = Time.time + currentWaveData.turretSpawnInterval;
        }

        // Eagle Spawning (if enabled and under limit)
        if (currentWaveData.spawnEagles &&
            eaglesSpawned < currentWaveData.maxEagles &&
            Time.time >= nextEagleSpawnTime)
        {
            SpawnEagleLockOn();
            eaglesSpawned++;
            nextEagleSpawnTime = Time.time + currentWaveData.eagleSpawnInterval;
        }

        // Only check wave completion if spawning is done AND all enemies dead
        if (waveSpawningComplete && activeEnemyCount <= 0)
        {
            NextWave();
        }

        // Update UI
        if (enemyLabel != null)
        {
            enemyLabel.text = "Enemies " + activeEnemyCount;
        }
    }

    // ============================================
    // START WAVE
    // ============================================
    void StartWave(int waveIndex)
    {
        currentWave = waveIndex;
        enemiesSpawned = 0;
        turretsSpawned = 0;
        eaglesSpawned = 0;
        waveSpawningComplete = false;

        Wave currentWaveData = waves[currentWave];

        // Initialize enemy type counters for this wave
        enemyTypeSpawnedCount = new int[currentWaveData.allowedEnemies.Length];

        if (waveLabel != null)
        {
            waveLabel.text = "Wave " + (currentWave + 1);
        }

        // Reset special enemy timers
        nextTurretSpawnTime = Time.time + currentWaveData.turretSpawnInterval;
        nextEagleSpawnTime = Time.time + currentWaveData.eagleSpawnInterval;
    }

    // ============================================
    // GET SPAWN POSITION
    // ============================================
    Vector3 GetSpawnPosition()
    {
        Vector3 spawnPos = Vector3.zero;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            spawnPos = new Vector3(x, y, 0);

            if (player != null && Vector3.Distance(spawnPos, player.position) < minDistanceFromPlayer)
            {
                continue;
            }

            bool tooCloseToEnemy = false;
            RailFighterEnemy[] existingEnemies = FindObjectsOfType<RailFighterEnemy>();
            foreach (RailFighterEnemy enemy in existingEnemies)
            {
                if (Vector3.Distance(spawnPos, enemy.transform.position) < minDistanceFromEnemies)
                {
                    tooCloseToEnemy = true;
                    break;
                }
            }

            if (!tooCloseToEnemy)
            {
                return spawnPos;
            }
        }

        if (player != null)
        {
            float centerX = (minX + maxX) / 2;

            if (player.position.x < centerX)
            {
                spawnPos = new Vector3(maxX, Random.Range(minY, maxY), 0);
            }
            else
            {
                spawnPos = new Vector3(minX, Random.Range(minY, maxY), 0);
            }
        }

        return spawnPos;
    }

    // ============================================
    // SPAWN ENEMY
    // ============================================
    void SpawnEnemy()
    {
        Wave currentWaveData = waves[currentWave];
        List<int> availableTypes = new List<int>();

        for (int i = 0; i < currentWaveData.allowedEnemies.Length; i++)
        {
            if (currentWaveData.allowedEnemies[i].canSpawn &&
                enemyTypeSpawnedCount[i] < currentWaveData.allowedEnemies[i].maxSpawnCount &&
                currentWaveData.allowedEnemies[i].enemyPrefab != null)
            {
                availableTypes.Add(i);
            }
        }

        if (availableTypes.Count == 0)
        {
            Debug.LogWarning("No available enemy types to spawn!");
            return;
        }

        int randomTypeIndex = availableTypes[Random.Range(0, availableTypes.Count)];
        EnemySpawnSettings chosenEnemy = currentWaveData.allowedEnemies[randomTypeIndex];

        Vector3 spawnPos = GetSpawnPosition();

        GameObject newEnemy = Instantiate(chosenEnemy.enemyPrefab, spawnPos, Quaternion.identity);

        enemiesSpawned++;
        enemyTypeSpawnedCount[randomTypeIndex]++;
    }

    // ============================================
    // NEXT WAVE
    // ============================================
    void NextWave()
    {
        // Heal player 1 heart on wave completion
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerControllerRailFighter playerHealth = playerObj.GetComponent<PlayerControllerRailFighter>();
            if (playerHealth != null)
            {
                playerHealth.Heal(1);
            }
        }

        currentWave++;

        if (currentWave >= waves.Length)
        {
            Debug.Log("All waves complete! Time for boss!");
            allWavesComplete = true;
            return;
        }

        StartWave(currentWave);

        if (currentWave == 3)
        {
            DestroyRightBorder();
            CameraScroll cam = Camera.main.GetComponent<CameraScroll>();
            if (cam != null)
            {
                cam.StartScrolling();
            }
        }
    }

    // ============================================
    // DESTROY RIGHT BORDER
    // ============================================
    void DestroyRightBorder()
    {
        if (Border_Right != null)
        {
            Destroy(Border_Right);
        }
    }

    // ============================================
    // SPAWN TURRET
    // ============================================
    void SpawnTurret()
    {
        List<TurretSpawnPoint> availablePoints = new List<TurretSpawnPoint>();
        foreach (TurretSpawnPoint point in turretSpawnPoints)
        {
            if (!point.isOccupied)
            {
                availablePoints.Add(point);
            }
        }

        if (availablePoints.Count > 0)
        {
            TurretSpawnPoint spawnPoint = availablePoints[Random.Range(0, availablePoints.Count)];
            GameObject turret = Instantiate(turretPrefab, spawnPoint.transform.position, Quaternion.identity);
            spawnPoint.isOccupied = true;
            turret.GetComponent<TurretEnemy>().spawnPoint = spawnPoint;

            // Track turret as enemy
            EnemySpawned();
        }
    }

    // ============================================
    // SPAWN EAGLE LOCK-ON
    // ============================================
    void SpawnEagleLockOn()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Instantiate(EnemyLockonPrefab, playerObj.transform.position, Quaternion.identity);

            // Track eagle as enemy
            EnemySpawned();
        }
    }

    // ============================================
    // ENEMY TRACKING
    // ============================================
    public void EnemySpawned()
    {
        activeEnemyCount++;
    }

    public void EnemyDestroyed()
    {
        activeEnemyCount--;
    }

    // Backward compatibility
    public void EnemyKilled()
    {
        EnemyDestroyed();
    }
}