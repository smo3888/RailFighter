using UnityEngine;

public class DiveEagleLockOn : MonoBehaviour
{
    [Header("Settings")]
    public GameObject diveEaglePrefab; // Drag DiveEagle prefab here
    public float lockonDelay = 2f;

    [Header("Spawn Position")]
    public float spawnYOffset = 10f; // How far above screen to spawn eagle

    private float lockedX; // X position where warning appears and eagle will dive through

    void Start()
    {
        // Lock the X position where this warning circle is
        lockedX = transform.position.x;

        // Spawn dive eagle after delay
        Invoke("SpawnDiveEagle", lockonDelay);

        // Destroy warning circle after delay
        Destroy(gameObject, lockonDelay);
    }

    void SpawnDiveEagle()
    {
        // Get camera bounds to spawn above screen
        float screenTop = Camera.main.orthographicSize;

        // Spawn eagle at top of screen at the same X as the warning
        Vector3 spawnPos = new Vector3(lockedX, screenTop + spawnYOffset, 0);

        Instantiate(diveEaglePrefab, spawnPos, Quaternion.identity);
    }
}