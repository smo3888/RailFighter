using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Challenge Config")]
    public ChallengeConfig challengeConfig;

    [Header("Rail References")]
    [Tooltip("All rails in this room — auto-populated if left empty")]
    public List<Transform> rails = new List<Transform>();

    [Header("UI Reference")]
    public WaveUI waveUI;

    [Header("Spawn Settings")]
    [Tooltip("Minimum distance from player before an enemy can spawn")]
    public float minDistanceFromPlayer = 4f;

    [Tooltip("Offset above rail surface for OnTopOfRail spawn mode")]
    public float railTopOffset = 0.5f;

    [Tooltip("Offset above player for AbovePlayer spawn mode")]
    public float abovePlayerOffset = 5f;

    // Public state
    public int currentWaveIndex { get; private set; } = 0;
    public int enemiesRemainingInWave { get; private set; } = 0;
    public int enemiesOnScreen { get; private set; } = 0;
    public bool allWavesComplete { get; private set; } = false;
    public float elapsedTime { get; private set; } = 0f;

    private EnemyWave currentWave;
    private bool waveInProgress = false;
    private bool isRunning = false;
    private Transform player;
    private Camera mainCamera;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        mainCamera = Camera.main;

        if (challengeConfig == null)
        {
            Debug.LogWarning("WaveManager: No ChallengeConfig assigned!");
            return;
        }

        if (challengeConfig.waves.Count == 0)
        {
            Debug.LogWarning("WaveManager: ChallengeConfig has no waves!");
            return;
        }

        if (rails.Count == 0)
        {
            foreach (GameObject r in GameObject.FindGameObjectsWithTag("RailHorizontal"))
                rails.Add(r.transform);
            foreach (GameObject r in GameObject.FindGameObjectsWithTag("RailVertical"))
                rails.Add(r.transform);
        }

        StartChallenge();
    }

    void Update()
    {
        if (!isRunning || allWavesComplete) return;
        elapsedTime += Time.deltaTime;
        if (waveUI != null) waveUI.UpdateTime(elapsedTime);
    }

    void StartChallenge()
    {
        isRunning = true;
        elapsedTime = 0f;
        currentWaveIndex = 0;
        allWavesComplete = false;
        StartCoroutine(RunWave(currentWaveIndex));
    }

    IEnumerator RunWave(int index)
    {
        if (index >= challengeConfig.waves.Count)
        {
            CompleteChallenge();
            yield break;
        }

        currentWave = challengeConfig.waves[index];
        if (currentWave == null)
        {
            Debug.LogWarning($"Wave {index} is null in ChallengeConfig!");
            yield break;
        }

        waveInProgress = true;
        enemiesRemainingInWave = GetTotalEnemyCount(currentWave);
        enemiesOnScreen = 0;

        Debug.Log($"Starting wave {index + 1}: {currentWave.waveName}");

        if (waveUI != null)
            waveUI.UpdateWave(index + 1, challengeConfig.waves.Count);

        List<EnemySpawnEntry> spawnQueue = BuildSpawnQueue(currentWave);
        int spawnIndex = 0;

        while (spawnIndex < spawnQueue.Count)
        {
            while (enemiesOnScreen >= currentWave.maxEnemiesOnScreen)
                yield return new WaitForSeconds(0.5f);

            SpawnEnemy(spawnQueue[spawnIndex], currentWave);
            spawnIndex++;

            yield return new WaitForSeconds(currentWave.spawnInterval);
        }

        if (currentWave.mustClearBeforeNext)
        {
            while (enemiesOnScreen > 0)
                yield return new WaitForSeconds(0.5f);
        }

        waveInProgress = false;

        PlayerControllerRailFighter playerController = player?.GetComponent<PlayerControllerRailFighter>();
        if (playerController != null)
            playerController.Heal(1);

        currentWaveIndex++;
        StartCoroutine(RunWave(currentWaveIndex));
    }

    List<EnemySpawnEntry> BuildSpawnQueue(EnemyWave wave)
    {
        List<EnemySpawnEntry> queue = new List<EnemySpawnEntry>();

        foreach (EnemySpawnEntry entry in wave.enemies)
            for (int i = 0; i < entry.count; i++)
                queue.Add(entry);

        for (int i = queue.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            EnemySpawnEntry temp = queue[i];
            queue[i] = queue[j];
            queue[j] = temp;
        }

        return queue;
    }

    void SpawnEnemy(EnemySpawnEntry entry, EnemyWave wave)
    {
        if (entry.enemyPrefab == null) return;

        SpawnMode mode = entry.overrideSpawnMode ? entry.spawnMode : wave.defaultSpawnMode;
        Vector3 spawnPos = GetSpawnPosition(mode, wave);

        Instantiate(entry.enemyPrefab, spawnPos, Quaternion.identity);
        enemiesOnScreen++;
    }

    Vector3 GetSpawnPosition(SpawnMode mode, EnemyWave wave)
    {
        switch (mode)
        {
            case SpawnMode.OnPlayer:
                return player != null ? player.position : Vector3.zero;

            case SpawnMode.AbovePlayer:
                return player != null
                    ? player.position + Vector3.up * abovePlayerOffset
                    : Vector3.zero;

            case SpawnMode.BehindPlayer:
                if (player == null) return GetCameraRelativeSpawnPosition(wave);
                float camCenterX = mainCamera != null ? mainCamera.transform.position.x : 0f;
                float side = player.position.x > camCenterX ? -1f : 1f;
                return new Vector3(
                    player.position.x + side * wave.spawnRangeX,
                    player.position.y, 0);

            case SpawnMode.OnRail:
                return GetRailSpawnPosition(wave);

            case SpawnMode.OnTopOfRail:
                return GetTopOfRailSpawnPosition(wave);

            case SpawnMode.CameraRelative:
            default:
                return GetCameraRelativeSpawnPosition(wave);
        }
    }

    Vector3 GetCameraRelativeSpawnPosition(EnemyWave wave)
    {
        if (mainCamera == null) return Vector3.zero;
        Vector3 camPos = mainCamera.transform.position;

        for (int i = 0; i < 10; i++)
        {
            float x = camPos.x + Random.Range(-wave.spawnRangeX, wave.spawnRangeX);
            float y = camPos.y + Random.Range(-wave.spawnRangeY, wave.spawnRangeY);
            Vector3 candidate = new Vector3(x, y, 0);

            if (player != null && Vector3.Distance(candidate, player.position) < minDistanceFromPlayer)
                continue;

            return candidate;
        }

        return new Vector3(camPos.x + wave.spawnRangeX, camPos.y, 0);
    }

    Vector3 GetRailSpawnPosition(EnemyWave wave)
    {
        List<Transform> visibleRails = GetVisibleRails(wave);
        if (visibleRails.Count == 0) return GetCameraRelativeSpawnPosition(wave);

        Transform chosenRail = visibleRails[Random.Range(0, visibleRails.Count)];
        BoxCollider2D col = chosenRail.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            float x = Random.Range(col.bounds.min.x, col.bounds.max.x);
            float y = Random.Range(col.bounds.min.y, col.bounds.max.y);
            return new Vector3(x, y, 0);
        }

        return chosenRail.position;
    }

    Vector3 GetTopOfRailSpawnPosition(EnemyWave wave)
    {
        // Only use horizontal rails for top-of-rail spawning
        List<Transform> visibleRails = GetVisibleRails(wave, horizontalOnly: true);
        if (visibleRails.Count == 0) return GetCameraRelativeSpawnPosition(wave);

        Transform chosenRail = visibleRails[Random.Range(0, visibleRails.Count)];
        BoxCollider2D col = chosenRail.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            // Random X along the rail, Y at the top surface + offset
            float x = Random.Range(col.bounds.min.x + 1f, col.bounds.max.x - 1f);
            float y = col.bounds.max.y + railTopOffset;
            return new Vector3(x, y, 0);
        }

        return chosenRail.position + Vector3.up * railTopOffset;
    }

    List<Transform> GetVisibleRails(EnemyWave wave, bool horizontalOnly = false)
    {
        Vector3 camPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        List<Transform> visible = new List<Transform>();

        foreach (Transform rail in rails)
        {
            if (rail == null) continue;

            if (horizontalOnly && !rail.CompareTag("RailHorizontal")) continue;

            float dx = Mathf.Abs(rail.position.x - camPos.x);
            float dy = Mathf.Abs(rail.position.y - camPos.y);
            if (dx <= wave.spawnRangeX + 2f && dy <= wave.spawnRangeY + 2f)
                visible.Add(rail);
        }

        return visible;
    }

    int GetTotalEnemyCount(EnemyWave wave)
    {
        int total = 0;
        foreach (EnemySpawnEntry entry in wave.enemies)
            total += entry.count;
        return total;
    }

    public void EnemyDestroyed()
    {
        enemiesOnScreen = Mathf.Max(0, enemiesOnScreen - 1);
        enemiesRemainingInWave = Mathf.Max(0, enemiesRemainingInWave - 1);
        if (waveUI != null)
            waveUI.UpdateEnemies(enemiesOnScreen, enemiesRemainingInWave);
    }

    public void EnemyKilled() => EnemyDestroyed();
    public void EnemySpawned() => enemiesOnScreen++;

    void CompleteChallenge()
    {
        allWavesComplete = true;
        isRunning = false;

        Debug.Log($"Challenge complete! Time: {elapsedTime:F2}s");

        if (GameManager.Instance != null)
            GameManager.Instance.CompleteChallenge(challengeConfig.challengeNumber);

        if (waveUI != null)
            waveUI.ShowComplete(elapsedTime);
    }
}