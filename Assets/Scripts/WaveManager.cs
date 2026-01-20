using UnityEngine;
using UnityEngine.UIElements;

// ============================================
// WAVE MANAGER
// Controls enemy spawning across waves
// Spawns enemies at random positions away from player and other enemies
// ============================================

public class WaveManager : MonoBehaviour
{
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
    // WAVE SETTINGS
    // ============================================
    [Header("Wave Settings")]
    public int currentWave = 0;
    public int[] enemiesPerWave;

    // ============================================
    // ENEMY TYPES
    // ============================================
    [Header("Enemy Types")]
    public EnemySpawnSettings[] enemyTypes;

    // ============================================
    // SPAWNING SETTINGS
    // ============================================
    [Header("Spawning")]
    public float spawnInterval = 2f;
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
    private int enemiesSpawned = 0;
    private int enemiesKilled = 0;
    private float spawnTimer = 0f;
    private int currentEnemiesAlive = 0;
    private int[] enemyTypeSpawnedCount;
    private bool allWavesComplete = false;

    // ============================================
    // START
    // ============================================
    void Start()
    {
        enemyTypeSpawnedCount = new int[enemyTypes.Length];

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

        if (enemiesSpawned < enemiesPerWave[currentWave] &&
            currentEnemiesAlive < maxEnemiesOnScreen)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnEnemy();
                spawnTimer = 0f;
            }
        }

        if (enemiesSpawned >= enemiesPerWave[currentWave] &&
            enemiesKilled >= enemiesPerWave[currentWave])
        {
            NextWave();
        }
    }

    // ============================================
    // START WAVE
    // ============================================
    void StartWave(int waveIndex)
    {
        currentWave = waveIndex;
        enemiesSpawned = 0;
        enemiesKilled = 0;
        currentEnemiesAlive = 0;

        for (int i = 0; i < enemyTypeSpawnedCount.Length; i++)
        {
            enemyTypeSpawnedCount[i] = 0;
        }

        if (waveLabel != null)
        {
            waveLabel.text = "Wave " + (currentWave + 1) + "/" + enemiesPerWave.Length;
        }
    }

    // ============================================
    // GET SPAWN POSITION
    // Finds position away from player AND other enemies
    // ============================================
    Vector3 GetSpawnPosition()
    {
        Vector3 spawnPos = Vector3.zero;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            spawnPos = new Vector3(x, y, 0);

            // Check distance from player
            if (player != null && Vector3.Distance(spawnPos, player.position) < minDistanceFromPlayer)
            {
                continue;
            }

            // Check distance from other enemies
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

        // Fallback - spawn on opposite side from player
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
        System.Collections.Generic.List<int> availableTypes =
            new System.Collections.Generic.List<int>();

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (enemyTypes[i].canSpawn &&
                enemyTypeSpawnedCount[i] < enemyTypes[i].maxSpawnCount &&
                enemyTypes[i].enemyPrefab != null)
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
        EnemySpawnSettings chosenEnemy = enemyTypes[randomTypeIndex];

        Vector3 spawnPos = GetSpawnPosition();

        GameObject newEnemy = Instantiate(chosenEnemy.enemyPrefab,
                                         spawnPos,
                                         Quaternion.identity);

        enemiesSpawned++;
        enemyTypeSpawnedCount[randomTypeIndex]++;
        currentEnemiesAlive++;
    }

    // ============================================
    // NEXT WAVE
    // ============================================
    void NextWave()
    {
        currentWave++;

        if (currentWave >= enemiesPerWave.Length)
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
    // ENEMY KILLED
    // ============================================
    public void EnemyKilled()
    {
        enemiesKilled++;
        currentEnemiesAlive--;
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
}