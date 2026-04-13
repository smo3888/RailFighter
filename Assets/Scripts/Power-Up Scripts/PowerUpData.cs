using UnityEngine;

public enum PowerUpType { Offensive, Defensive }

public enum PowerUpEffectType
{
    // Offensive
    RapidFire,       // Increases fire rate for duration
    TripleShot,      // Fires 3 projectiles in spread for duration
    PiercingShot,    // Projectiles pass through enemies for duration
    Overdrive,       // Combined speed + fire rate boost

    // Defensive
    Heal,            // Instantly restores effectValue HP
    Shield,          // Extends invincibility window for duration
    SpeedBurst,      // Multiplies move speed for duration
    Invincibility    // Full invincibility for duration
}

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "Rail Fighter/Power Up")]
public class PowerUpData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique ID — never change this after setting it")]
    public string powerUpID;
    public string displayName;
    [TextArea(2, 4)]
    public string description;

    [Header("Visual")]
    public Sprite icon;
    public Color slotColor = Color.white;

    [Header("Classification")]
    public PowerUpType powerUpType;       // Offensive or Defensive
    public PowerUpEffectType effectType;

    [Header("Effect Values")]
    [Tooltip("Duration in seconds. 0 = instant effect (e.g. Heal)")]
    public float effectDuration = 5f;
    [Tooltip("Heal amount, speed multiplier, or fire rate divisor depending on effect")]
    public float effectValue = 2f;

    [Header("Cooldown")]
    [Tooltip("Seconds before this power-up can be activated again")]
    public float cooldownDuration = 15f;
}