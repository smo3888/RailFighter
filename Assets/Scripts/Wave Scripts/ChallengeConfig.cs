using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewChallengeConfig", menuName = "Rail Fighter/Challenge Config")]
public class ChallengeConfig : ScriptableObject
{
    [Header("Challenge Identity")]
    public string challengeName = "Challenge";
    public int challengeNumber = 1;

    [Header("Waves")]
    [Tooltip("Waves play in order from top to bottom")]
    public List<EnemyWave> waves = new List<EnemyWave>();

    [Header("Scoring")]
    [Tooltip("Target time in seconds for a perfect run")]
    public float targetTime = 60f;
}