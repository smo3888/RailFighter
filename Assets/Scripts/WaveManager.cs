using UnityEngine;
using UnityEngine.UIElements;

// Manages enemy waves and spawning for levels
// Supports multiple enemy types with per-type spawn limits and screen cap
public class WaveManager : MonoBehaviour
{
    // Holds settings for each enemy type (prefab, max count per wave, enable/disable)
    [System.Serializable]
    public class EnemySpawnSettings
    {
        public GameObject enemyPrefab;
        public int maxSpawnCount;
        public bool canSpawn = true;
    }

    [Header("Wave Settings")]
    public int currentWave = 0;
    public int[] enemiesPerWave;              // Total enemies per wave [5, 10, 15]

    [Header("Enemy Types")]
    public EnemySpawnSettings[] enemyTypes;   // Array of different enemy types

    [Header("Spawning")]
    public Transform[] spawnPoints;
    public float spawnInterval = 2f;
    public int maxEnemiesOnScreen = 10;       // Spawn cap - prevents screen flooding

    [Header ("UI")]
    public UIDocument uiDocument;

    private Label waveLabel;

    [Header("Borders")]
    public GameObject Border_Top;
    public GameObject Border_Bottom;
    public GameObject Border_Left;
    public GameObject Border_Right;
    
    private int enemiesSpawned = 0;
    private int enemiesKilled = 0;
    private float spawnTimer = 0f;
    private int currentEnemiesAlive = 0;

    // Tracks how many of each enemy type spawned this wave
    // Resets every wave, used to enforce per-type spawn limits
    private int[] enemyTypeSpawnedCount;

    void Start()
    {
        // Initialize tracking array to match number of enemy types
        enemyTypeSpawnedCount = new int[enemyTypes.Length];

        //UI 

        if (uiDocument != null)
        {
            waveLabel = uiDocument.rootVisualElement.Q<Label > ("WaveLabel");
        }
        

        StartWave(0);

       
    }

    void Update()
    {
        // Only spawn if we haven't hit wave total AND not at screen cap
        // This prevents overwhelming the player
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

        // Wave complete when all enemies spawned AND all killed
        if (enemiesSpawned >= enemiesPerWave[currentWave] &&
            enemiesKilled >= enemiesPerWave[currentWave])
        {
            NextWave();
        }
    }

    void StartWave(int waveIndex)
    {
        currentWave = waveIndex;
        enemiesSpawned = 0;
        enemiesKilled = 0;
        currentEnemiesAlive = 0;

        // Reset spawn counters for each enemy type
        for (int i = 0; i < enemyTypeSpawnedCount.Length; i++)
        {
            enemyTypeSpawnedCount[i] = 0;
        }

        //UI

        if (waveLabel != null)
        {
            waveLabel.text = "Wave " + (currentWave + 1) + "/" + enemiesPerWave.Length;
        }
    }

    void SpawnEnemy()
    {
        // Build list of enemy types that can still spawn
        // (toggle enabled, not at max count, prefab exists)
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

        // If no available types, skip spawning this frame
        if (availableTypes.Count == 0)
        {
            Debug.LogWarning("No available enemy types to spawn!");
            return;
        }

        // Pick random enemy type from available list
        int randomTypeIndex = availableTypes[Random.Range(0, availableTypes.Count)];
        EnemySpawnSettings chosenEnemy = enemyTypes[randomTypeIndex];

        // Pick random spawn point from array
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Create the enemy at chosen spawn point
        GameObject newEnemy = Instantiate(chosenEnemy.enemyPrefab,
                                         spawn.position,
                                         Quaternion.identity);

        // Update tracking counters
        enemiesSpawned++;
        enemyTypeSpawnedCount[randomTypeIndex]++;
        currentEnemiesAlive++;
    }

    void NextWave()
    {
        currentWave++;

        // Check if all waves complete
        if (currentWave >= enemiesPerWave.Length)
        {
            

            Debug.Log("All waves complete! Time for boss!");
            // Add boss spawn logic here later
            return;
        }

        // Start next wave
        StartWave(currentWave);

        if (currentWave == 3)
        {
            DestroyRightBorder();
            Camera.main.GetComponent<CameraScroll>().StartScrolling();

        }

        
   
    }

    // Called by enemy when it dies
    // Decrements alive count and increments kill count
    public void EnemyKilled()
    {
        enemiesKilled++;
        currentEnemiesAlive--;
    }

    void DestroyRightBorder()
    {
        Destroy(Border_Right);
    }


}