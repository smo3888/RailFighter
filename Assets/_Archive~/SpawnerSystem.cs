using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnerSystem : MonoBehaviour
{
    public GameObject[] obstaclePrefabs;
    public float spawnInterval = 2f;
    public float spawnDistance = 10f;

    // For Rail Fighter
    public Transform[] rails;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnObstacle();
            timer = 0f;
        }
    }

    void SpawnObstacle()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "RailFighter")
        {
            SpawnForRailFighter();
        }
        else
        {
            SpawnForSpriteFlight();
        }
    }

    void SpawnForRailFighter()
    {
        float randomY = Random.Range(-4f, 4f);
        Vector3 spawnPos = new Vector3(spawnDistance, randomY, 0);

        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        Instantiate(obstaclePrefabs[randomIndex], spawnPos, Quaternion.identity);
    }

    void SpawnForSpriteFlight()
    {
        // Pick random side (0=top, 1=bottom, 2=left, 3=right)
        int side = Random.Range(0, 4);
        Vector3 spawnPosition = Vector3.zero;

        switch (side)
        {
            case 0: // Top
                spawnPosition = new Vector3(Random.Range(-5f, 5f), 7f, 0f);
                break;
            case 1: // Bottom
                spawnPosition = new Vector3(Random.Range(-5, 5f), -7f, 0f);
                break;
            case 2: // Left
                spawnPosition = new Vector3(-6f, Random.Range(-6f, 6f), 0f);
                break;
            case 3: // Right
                spawnPosition = new Vector3(6f, Random.Range(-6f, 6f), 0f);
                break;
        }

        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        Instantiate(obstaclePrefabs[randomIndex], spawnPosition, Quaternion.identity);
    }
}