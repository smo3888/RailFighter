using UnityEngine;

public class EagleSpawner : MonoBehaviour
{
    [Header("Eagle Settings")]
    [SerializeField] private GameObject eagleLockOnPrefab;

    [Header("Spawn Points - Full Grid")]
    [SerializeField] private Transform[] allSpawnPoints; // All possible spawn points

    [Header("Wave Patterns")]
    [SerializeField] private WavePattern[] wavePatterns; // Define which spawns to skip per wave

    [System.Serializable]
    public class WavePattern
    {
        public string patternName; // e.g. "Gap on Left"
        public int[] spawnIndicesToSkip; // Which spawn point indices NOT to use
    }

    // Spawn specific wave pattern
    public void SpawnWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= wavePatterns.Length)
        {
            Debug.LogError("Invalid wave index!");
            return;
        }

        WavePattern pattern = wavePatterns[waveIndex];

        // Spawn at all points EXCEPT the ones marked to skip
        for (int i = 0; i < allSpawnPoints.Length; i++)
        {
            // Check if this index should be skipped
            bool shouldSkip = System.Array.Exists(pattern.spawnIndicesToSkip, index => index == i);

            if (!shouldSkip)
            {
                Instantiate(eagleLockOnPrefab, allSpawnPoints[i].position, Quaternion.identity);
            }
        }
    }

    // Spawn all 5 waves in sequence
    public void SpawnFullBarrage()
    {
        StartCoroutine(BarrageSequence());
    }

    System.Collections.IEnumerator BarrageSequence()
    {
        float timeBetweenWaves = 3f; // Adjustable

        for (int i = 0; i < wavePatterns.Length; i++)
        {
            SpawnWave(i);
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }
}