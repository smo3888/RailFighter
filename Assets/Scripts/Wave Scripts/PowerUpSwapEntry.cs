using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One entry in the swap submenu scroll list.
// Attach to your swapEntryPrefab.
public class PowerUpSwapEntry : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Image equippedIndicator;   // Highlight if this is currently equipped
    public Button selectButton;

    private PowerUpData data;
    private PowerUpMenuUI menuUI;

    public void Setup(PowerUpData powerUp, PowerUpMenuUI menu)
    {
        data = powerUp;
        menuUI = menu;

        if (icon != null) icon.sprite = powerUp.icon;
        if (nameText != null) nameText.text = powerUp.displayName;
        if (descText != null) descText.text = powerUp.description;

        // Show indicator if currently equipped in either slot
        if (equippedIndicator != null && PowerUpManager.Instance != null)
        {
            bool isEquipped =
                PowerUpManager.Instance.GetEquippedOffensive()?.powerUpID == powerUp.powerUpID ||
                PowerUpManager.Instance.GetEquippedDefensive()?.powerUpID == powerUp.powerUpID;
            equippedIndicator.gameObject.SetActive(isEquipped);
        }

        if (selectButton != null)
            selectButton.onClick.AddListener(() => menuUI.SelectForEquip(data));
    }
}