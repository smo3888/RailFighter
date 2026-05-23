using UnityEngine;
using UnityEngine.UI;

// One square entry in the power-up collection grid.
// Attach to your gridEntryPrefab — a square UI GameObject.
public class PowerUpGridEntry : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;               // The power-up icon fills this
    public Image equippedBorder;          // Highlight border shown when this is equipped
    public Button selectButton;

    private PowerUpData data;
    private PowerUpCollectionUI collectionUI;

    public void Setup(PowerUpData powerUp, PowerUpCollectionUI ui)

    {
        Debug.Log($"[GridEntry] Setup called for {powerUp?.displayName}, selectButton={selectButton != null}");
        data = powerUp;
        collectionUI = ui;

        if (iconImage != null)
        {
            iconImage.sprite = powerUp.icon;
            iconImage.color = Color.white;
        }

        // Show equipped indicator if this is currently slotted
        if (equippedBorder != null && PowerUpManager.Instance != null)
        {
            bool isEquipped =
                PowerUpManager.Instance.GetEquippedOffensive()?.powerUpID == powerUp.powerUpID ||
                PowerUpManager.Instance.GetEquippedDefensive()?.powerUpID == powerUp.powerUpID;
            equippedBorder.gameObject.SetActive(isEquipped);
        }

        if (selectButton != null)
            selectButton.onClick.AddListener(() => {
                Debug.Log($"Click fired! data={data?.displayName}, collectionUI={collectionUI != null}");
                collectionUI?.SelectPowerUp(data);
            });
    }
}