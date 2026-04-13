using UnityEditor;
using UnityEngine;

public class EagleLockOn : MonoBehaviour
{
    private GameObject EnemyLockonPrefab;
    public GameObject EaglePrefab;
    public float lockonDelay = 2f;
    private Vector3 targetPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetPosition = transform.position;

        Invoke("SpawnEagle", lockonDelay);

        Destroy(gameObject, lockonDelay );
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnEagle()
    {
        float screenHeight = Camera.main.orthographicSize;
        float screenWidth = Camera.main.orthographicSize + Camera.main.aspect;

        float randomY = Random.Range(-screenWidth, screenWidth);

        Vector3 spawnPos = Vector3.zero;

        bool spawnTop = Random.value > 0.5f;
        

        if(spawnTop)
        {
            spawnPos = new Vector3(randomY, screenHeight + 2f, 0);
        }
        else
        {
            spawnPos = new Vector3(randomY, -screenHeight - 2f, 0);
        }

        GameObject eagle = Instantiate(EaglePrefab, spawnPos, Quaternion.identity);
        eagle.GetComponent<EagleEnemy>().SetTarget(targetPosition);


    }

   
    
}
