using UnityEngine;
using UnityEngine.SceneManagement;

public class CoinSpawnerSystem : MonoBehaviour
{

    public GameObject coinPrefab;
    public float spawnInterval = 2f;
    public Transform[] rails;

    public float minY = -4f;
    public float maxY = 4f;
    public float spawnX = 10f;

    private float timer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer > spawnInterval)
        {
            SpawnCoin();
            timer = 0f;
        }
    }

    void SpawnCoin()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "RailFighter")
        {
            SpawnCoinOnRail();
        }
        else
        {
            SpawnCoinNormal();
        }
    }

    void SpawnCoinOnRail()
    {
        int randomRailIndex = Random.Range(0, rails.Length);
        Transform selectedRail = rails[randomRailIndex];

        BoxCollider2D railCollider = selectedRail.GetComponent<BoxCollider2D>();
        float railWidth = railCollider.size.x * selectedRail.localScale.x;
        float randomX = Random.Range(
            selectedRail.position.x - (railWidth / 2),
            selectedRail.position.x + (railWidth / 2)
            );

        Vector3 spawnPos = new Vector3(randomX, selectedRail.position.y, 0);
        Instantiate(coinPrefab, spawnPos, Quaternion.identity);

    }

    void SpawnCoinNormal()
    {
        float RandomY = Random.Range(minY, maxY);
        Vector3 spawnPos = new Vector3(spawnX, RandomY, 0);
        Instantiate(coinPrefab, spawnPos, Quaternion.identity);
    }
}
