using UnityEngine;

public class ChallengeExit : MonoBehaviour
{
    [Header("Exit Settings")]
    public GameObject exitTrigger;

    [Header("Wave Settings")]
    public bool waitForAllWaves = true;

    [Tooltip("If waitForAllWaves is false, exit spawns at this specific wave number")]
    public int specificWaveToExitOn = 0;

    [Header("Boss Settings")]
    [Tooltip("Enable this if the exit should wait for a boss to die instead of waves")]
    public bool waitForBossDeath = false;
    public MonoBehaviour bossScript;

    [Header("Challenge Settings")]
    [Tooltip("Which challenge number is this room (1-4). 0 = no challenge completion")]
    public int challengeNumber = 0;

    private WaveManager waveManager;
    private bool exitSpawned = false;

    void Start()
    {
        waveManager = FindObjectOfType<WaveManager>();
        if (exitTrigger != null)
            exitTrigger.SetActive(false);
    }

    void Update()
    {
        if (exitSpawned) return;

        if (waitForBossDeath)
            CheckBossDeath();
        else
            CheckWaveCompletion();

        if (exitSpawned)
        {
            if (exitTrigger != null && !exitTrigger.activeSelf)
                exitTrigger.SetActive(true);
        }
    }

    void CheckWaveCompletion()
    {
        if (waveManager == null) return;

        bool shouldExit = false;

        if (waitForAllWaves)
        {
            shouldExit = waveManager.allWavesComplete && waveManager.enemiesOnScreen <= 0;
        }
        else
        {
            shouldExit = waveManager.currentWaveIndex >= specificWaveToExitOn
                         && waveManager.enemiesOnScreen <= 0;
        }

        if (shouldExit)
            ActivateExit();
    }

    void CheckBossDeath()
    {
        if (bossScript == null)
            ActivateExit();
    }

    void ActivateExit()
    {
        exitSpawned = true;

        if (challengeNumber > 0 && GameManager.Instance != null)
            GameManager.Instance.CompleteChallenge(challengeNumber);

        if (exitTrigger != null)
            exitTrigger.SetActive(true);

        Debug.Log("Challenge exit activated!");
    }
}