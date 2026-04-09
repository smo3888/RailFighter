using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemyWave", menuName = "Rail Fighter/Enemy Wave")]
public class EnemyWave : ScriptableObject
{
    [Header("Wave Identity")]
    public string waveName = "Wave";

    [Header("Enemy Spawn Settings")]
    public List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();

    [Header("Spawn Rate")]
    [Tooltip("Time in seconds between each enemy spawn")]
    public float spawnInterval = 2f;

    [Tooltip("Max enemies allowed on screen at once during this wave")]
    public int maxEnemiesOnScreen = 8;

    [Header("Completion")]
    [Tooltip("All enemies in this wave must die before the next wave starts")]
    public bool mustClearBeforeNext = true;

    [Header("Spawn Positioning")]
    [Tooltip("Default spawn mode for enemies that don't specify their own")]
    public SpawnMode defaultSpawnMode = SpawnMode.CameraRelative;

    [Tooltip("How far from camera enemies can spawn (in world units)")]
    public float spawnRangeX = 12f;
    public float spawnRangeY = 6f;
}

public enum SpawnMode
{
    CameraRelative,     // Random position near camera
    OnRail,             // Snap to a random point within a visible rail's bounds
    OnTopOfRail,        // Snap to the top surface of a random visible horizontal rail
    OnPlayer,           // Spawn directly on the player
    AbovePlayer,        // Spawn above the player
    BehindPlayer,       // Spawn behind player based on movement direction
}

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;

    [Tooltip("How many of this enemy type to spawn in this wave")]
    public int count = 3;

    [Tooltip("Relative weight for random selection when multiple types exist")]
    [Range(1, 10)]
    public int spawnWeight = 1;

    [Tooltip("Override the wave's default spawn mode for this enemy type")]
    public bool overrideSpawnMode = false;
    public SpawnMode spawnMode = SpawnMode.CameraRelative;
}