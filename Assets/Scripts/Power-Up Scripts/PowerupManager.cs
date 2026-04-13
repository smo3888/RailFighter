using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Power Up Registry")]
    [Tooltip("Drag every PowerUpData asset here. The manager looks up IDs from this list.")]
    public List<PowerUpData> allPowerUps = new List<PowerUpData>();

    private PowerUpData equippedOffensive;
    private PowerUpData equippedDefensive;

    private float offensiveCooldownRemaining = 0f;
    private float defensiveCooldownRemaining = 0f;
    private bool offensiveOnCooldown = false;
    private bool defensiveOnCooldown = false;

    public float OffensiveCooldownRemaining => offensiveCooldownRemaining;
    public float DefensiveCooldownRemaining => defensiveCooldownRemaining;

    public System.Action OnInventoryChanged;
    public System.Action OnEquipmentChanged;
    public System.Action<float> OnOffensiveCooldownTick;
    public System.Action<float> OnDefensiveCooldownTick;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadEquippedFromSave();

        // TEMP: auto-equip first power-up for testing — remove once menu is built
        if (allPowerUps.Count > 0)
            EquipPowerUp(allPowerUps[0]);
    }

    void Update()
    {
        if (offensiveOnCooldown)
        {
            offensiveCooldownRemaining -= Time.deltaTime;
            float dur = equippedOffensive != null ? equippedOffensive.cooldownDuration : 1f;
            OnOffensiveCooldownTick?.Invoke(Mathf.Clamp01(1f - (offensiveCooldownRemaining / dur)));
            if (offensiveCooldownRemaining <= 0f)
            {
                offensiveCooldownRemaining = 0f;
                offensiveOnCooldown = false;
                OnOffensiveCooldownTick?.Invoke(1f);
            }
        }

        if (defensiveOnCooldown)
        {
            defensiveCooldownRemaining -= Time.deltaTime;
            float dur = equippedDefensive != null ? equippedDefensive.cooldownDuration : 1f;
            OnDefensiveCooldownTick?.Invoke(Mathf.Clamp01(1f - (defensiveCooldownRemaining / dur)));
            if (defensiveCooldownRemaining <= 0f)
            {
                defensiveCooldownRemaining = 0f;
                defensiveOnCooldown = false;
                OnDefensiveCooldownTick?.Invoke(1f);
            }
        }

        if (Input.GetKeyDown(KeyCode.Q)) ActivateOffensive();
        if (Input.GetKeyDown(KeyCode.E)) ActivateDefensive();
    }

    public void CollectPowerUp(PowerUpData data)
    {
        if (data == null || GameManager.Instance == null) return;
        if (!GameManager.Instance.Data.collectedPowerUpIDs.Contains(data.powerUpID))
        {
            GameManager.Instance.Data.collectedPowerUpIDs.Add(data.powerUpID);
            GameManager.Instance.SaveGame(false);
            Debug.Log($"[PowerUpManager] Collected: {data.displayName}");
        }
        OnInventoryChanged?.Invoke();
    }

    public void EquipPowerUp(PowerUpData data)
    {
        if (data == null || GameManager.Instance == null) return;
        if (data.powerUpType == PowerUpType.Offensive)
        {
            equippedOffensive = data;
            GameManager.Instance.Data.equippedOffensiveID = data.powerUpID;
            offensiveOnCooldown = false;
            offensiveCooldownRemaining = 0f;
            OnOffensiveCooldownTick?.Invoke(1f);
        }
        else
        {
            equippedDefensive = data;
            GameManager.Instance.Data.equippedDefensiveID = data.powerUpID;
            defensiveOnCooldown = false;
            defensiveCooldownRemaining = 0f;
            OnDefensiveCooldownTick?.Invoke(1f);
        }
        GameManager.Instance.SaveGame(false);
        OnEquipmentChanged?.Invoke();
        Debug.Log($"[PowerUpManager] Equipped {data.displayName} ({data.powerUpType})");
    }

    public void UnequipSlot(PowerUpType slotType)
    {
        if (GameManager.Instance == null) return;
        if (slotType == PowerUpType.Offensive)
        {
            equippedOffensive = null;
            GameManager.Instance.Data.equippedOffensiveID = "";
            offensiveOnCooldown = false;
            offensiveCooldownRemaining = 0f;
        }
        else
        {
            equippedDefensive = null;
            GameManager.Instance.Data.equippedDefensiveID = "";
            defensiveOnCooldown = false;
            defensiveCooldownRemaining = 0f;
        }
        GameManager.Instance.SaveGame(false);
        OnEquipmentChanged?.Invoke();
    }

    public void ActivateOffensive()
    {
        if (equippedOffensive == null || offensiveOnCooldown) return;
        ApplyEffect(equippedOffensive);
        offensiveOnCooldown = true;
        offensiveCooldownRemaining = equippedOffensive.cooldownDuration;
        Debug.Log($"[PowerUpManager] Activated offensive: {equippedOffensive.displayName}");
    }

    public void ActivateDefensive()
    {
        if (equippedDefensive == null || defensiveOnCooldown) return;
        ApplyEffect(equippedDefensive);
        defensiveOnCooldown = true;
        defensiveCooldownRemaining = equippedDefensive.cooldownDuration;
        Debug.Log($"[PowerUpManager] Activated defensive: {equippedDefensive.displayName}");
    }

    void ApplyEffect(PowerUpData data)
    {
        PlayerControllerRailFighter player = FindObjectOfType<PlayerControllerRailFighter>();
        if (player == null) { Debug.LogWarning("[PowerUpManager] No player found."); return; }

        switch (data.effectType)
        {
            case PowerUpEffectType.Heal:         player.Heal(Mathf.RoundToInt(data.effectValue)); break;
            case PowerUpEffectType.Shield:        player.ActivateShield(data.effectDuration); break;
            case PowerUpEffectType.SpeedBurst:    player.ActivateSpeedBurst(data.effectValue, data.effectDuration); break;
            case PowerUpEffectType.Invincibility: player.ActivateInvincibility(data.effectDuration); break;
            case PowerUpEffectType.RapidFire:     player.ActivateRapidFire(data.effectValue, data.effectDuration); break;
            case PowerUpEffectType.TripleShot:    player.ActivateTripleShot(data.effectDuration); break;
            case PowerUpEffectType.PiercingShot:  player.ActivatePiercingShot(data.effectDuration); break;
            case PowerUpEffectType.Overdrive:     player.ActivateOverdrive(data.effectValue, data.effectDuration); break;
        }
    }

    public void LoadEquippedFromSave()
    {
        if (GameManager.Instance == null) return;
        equippedOffensive = GetByID(GameManager.Instance.Data.equippedOffensiveID);
        equippedDefensive = GetByID(GameManager.Instance.Data.equippedDefensiveID);
        OnEquipmentChanged?.Invoke();
        Debug.Log($"[PowerUpManager] Loaded — Off: {equippedOffensive?.displayName ?? "none"}, Def: {equippedDefensive?.displayName ?? "none"}");
    }

    public PowerUpData GetEquippedOffensive() => equippedOffensive;
    public PowerUpData GetEquippedDefensive() => equippedDefensive;
    public bool IsOffensiveReady() => equippedOffensive != null && !offensiveOnCooldown;
    public bool IsDefensiveReady() => equippedDefensive != null && !defensiveOnCooldown;

    public List<PowerUpData> GetCollectedPowerUps()
    {
        List<PowerUpData> result = new List<PowerUpData>();
        if (GameManager.Instance == null) return result;
        foreach (string id in GameManager.Instance.Data.collectedPowerUpIDs)
        {
            PowerUpData d = GetByID(id);
            if (d != null) result.Add(d);
        }
        return result;
    }

    public bool HasCollected(string id) =>
        GameManager.Instance?.Data.collectedPowerUpIDs.Contains(id) ?? false;

    public PowerUpData GetByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (PowerUpData d in allPowerUps)
            if (d != null && d.powerUpID == id) return d;
        return null;
    }
}