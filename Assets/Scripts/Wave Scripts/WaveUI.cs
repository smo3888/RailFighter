using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI enemyText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI completeText;

    void Start()
    {
        if (completeText != null)
            completeText.gameObject.SetActive(false);
    }

    public void UpdateWave(int current, int total)
    {
        if (waveText != null)
            waveText.text = $"Wave {current} / {total}";
    }

    public void UpdateEnemies(int onScreen, int remaining)
    {
        if (enemyText != null)
            enemyText.text = $"Enemies: {onScreen} | Remaining: {remaining}";
    }

    public void UpdateTime(float time)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int ms = Mathf.FloorToInt((time * 100f) % 100f);
            timerText.text = $"{minutes:00}:{seconds:00}.{ms:00}";
        }
    }

    public void ShowComplete(float finalTime)
    {
        if (completeText != null)
        {
            completeText.gameObject.SetActive(true);
            int minutes = Mathf.FloorToInt(finalTime / 60f);
            int seconds = Mathf.FloorToInt(finalTime % 60f);
            int ms = Mathf.FloorToInt((finalTime * 100f) % 100f);
            completeText.text = $"Complete! {minutes:00}:{seconds:00}.{ms:00}";
        }
    }
}