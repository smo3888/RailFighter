using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One entry in the Powers menu scroll list.
// Attach to your powerUpEntryPrefab.
public class PowerUpEntryUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;         // "Offensive" or "Defensive"
    public TextMeshProUGUI cooldownText;     // e.g. "15s cooldown"
    public Image equippedIndicator;          // Highlight if currently equipped
    public Button selectButton;

    private PowerUpData data;
    private PowerUpMenuUI menuUI;

    public void Setup(PowerUpData powerUp, PowerUpMenuUI menu)
    {
        data = powerUp;
        menuUI = menu;

        if (icon != null) icon.sprite = powerUp.icon;
        if (nameText != null) nameText.text = powerUp.displayName;
        if (typeText != null) typeText.text = powerUp.powerUpType.ToString();
        if (cooldownText != null) cooldownText.text = $"{powerUp.cooldownDuration}s cooldown";

        // Show equipped indicator if this is currently slotted
        if (equippedIndicator != null && PowerUpManager.Instance != null)
        {
            bool isEquipped =
                PowerUpManager.Instance.GetEquippedOffensive()?.powerUpID == powerUp.powerUpID ||
                PowerUpManager.Instance.GetEquippedDefensive()?.powerUpID == powerUp.powerUpID;
            equippedIndicator.gameObject.SetActive(isEquipped);
        }

        if (selectButton != null)
            selectButton.onClick.AddListener(() => menuUI.SelectPowerUp(data));
    }
}