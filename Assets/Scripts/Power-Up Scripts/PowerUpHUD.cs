using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to a Canvas GameObject.
// Wire up the two slot panels in the inspector.
// The Powers button should call OpenPowerUpMenu() on PowerUpMenuUI.
public class PowerUpHUD : MonoBehaviour
{
    [Header("Offensive Slot")]
    public Image offensiveIcon;
    public Image offensiveCooldownFill;   // Image with fill type set to Radial or Horizontal
    public TextMeshProUGUI offensiveLabel;
    public Button offensiveButton;
    public GameObject offensiveEmptyIndicator; // "Empty" text/icon shown when no power-up equipped

    [Header("Defensive Slot")]
    public Image defensiveIcon;
    public Image defensiveCooldownFill;
    public TextMeshProUGUI defensiveLabel;
    public Button defensiveButton;
    public GameObject defensiveEmptyIndicator;

    [Header("Powers Menu Button")]
    public Button powersButton;
    public PowerUpMenuUI powerUpMenu;

    [Header("Colors")]
    public Color readyColor = Color.white;
    public Color cooldownColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    void Start()
    {
        // Subscribe to manager events
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnEquipmentChanged += RefreshSlots;
            PowerUpManager.Instance.OnOffensiveCooldownTick += UpdateOffensiveCooldown;
            PowerUpManager.Instance.OnDefensiveCooldownTick += UpdateDefensiveCooldown;
        }

        // Slot buttons activate their power-up
        if (offensiveButton != null)
            offensiveButton.onClick.AddListener(PowerUpManager.Instance.ActivateOffensive);
        if (defensiveButton != null)
            defensiveButton.onClick.AddListener(PowerUpManager.Instance.ActivateDefensive);

        // Powers button opens the menu
        if (powersButton != null && powerUpMenu != null)
            powersButton.onClick.AddListener(powerUpMenu.Open);

        RefreshSlots();
        ResetCooldownFills();
    }

    void OnDestroy()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnEquipmentChanged -= RefreshSlots;
            PowerUpManager.Instance.OnOffensiveCooldownTick -= UpdateOffensiveCooldown;
            PowerUpManager.Instance.OnDefensiveCooldownTick -= UpdateDefensiveCooldown;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // SLOT REFRESH
    // ─────────────────────────────────────────────────────────────

    public void RefreshSlots()
    {
        if (PowerUpManager.Instance == null) return;

        PowerUpData off = PowerUpManager.Instance.GetEquippedOffensive();
        PowerUpData def = PowerUpManager.Instance.GetEquippedDefensive();

        RefreshSlot(off, offensiveIcon, offensiveLabel, offensiveEmptyIndicator, offensiveButton);
        RefreshSlot(def, defensiveIcon, defensiveLabel, defensiveEmptyIndicator, defensiveButton);
    }

    void RefreshSlot(PowerUpData data, Image icon, TextMeshProUGUI label,
        GameObject emptyIndicator, Button button)
    {
        bool hasData = data != null;

        if (icon != null)
        {
            icon.sprite = hasData ? data.icon : null;
            icon.color = hasData ? data.slotColor : Color.clear;
        }

        if (label != null)
            label.text = hasData ? data.displayName : "";

        if (emptyIndicator != null)
            emptyIndicator.SetActive(!hasData);

        if (button != null)
            button.interactable = hasData;
    }

    // ─────────────────────────────────────────────────────────────
    // COOLDOWN DISPLAY
    // ─────────────────────────────────────────────────────────────

    // progress: 0 = just activated (full cooldown), 1 = ready
    void UpdateOffensiveCooldown(float progress)
    {
        ApplyCooldownToFill(offensiveCooldownFill, offensiveIcon, progress);
    }

    void UpdateDefensiveCooldown(float progress)
    {
        ApplyCooldownToFill(defensiveCooldownFill, defensiveIcon, progress);
    }

    void ApplyCooldownToFill(Image fillImage, Image iconImage, float progress)
    {
        if (fillImage != null)
            fillImage.fillAmount = 1f - progress; // 1 = fully greyed, 0 = clear

        if (iconImage != null)
            iconImage.color = Color.Lerp(cooldownColor, readyColor, progress);
    }

    void ResetCooldownFills()
    {
        if (offensiveCooldownFill != null) offensiveCooldownFill.fillAmount = 0f;
        if (defensiveCooldownFill != null) defensiveCooldownFill.fillAmount = 0f;
    }
}