using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// The Powers menu. Opens when player taps "Powers" button on HUD.
// Shows all collected power-ups. Clicking one opens an equip prompt.
public class PowerUpMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject equipPromptPanel;

    [Header("Power Up List")]
    public Transform listParent;             // Scroll view content
    public GameObject powerUpEntryPrefab;    // One row per power-up

    [Header("Equip Prompt")]
    public TextMeshProUGUI promptTitle;
    public TextMeshProUGUI promptDescription;
    public Image promptIcon;
    public Button equipOffensiveButton;
    public Button equipDefensiveButton;
    public Button unequipButton;
    public Button cancelPromptButton;

    [Header("Selected Info Panel (optional)")]
    public TextMeshProUGUI selectedNameText;
    public TextMeshProUGUI selectedDescText;
    public Image selectedIconImage;

    [Header("Close")]
    public Button closeButton;

    private PowerUpData selectedPowerUp;

    void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (equipPromptPanel != null) equipPromptPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (cancelPromptButton != null)
            cancelPromptButton.onClick.AddListener(ClosePrompt);

        if (equipOffensiveButton != null)
            equipOffensiveButton.onClick.AddListener(() => EquipSelected(PowerUpType.Offensive));

        if (equipDefensiveButton != null)
            equipDefensiveButton.onClick.AddListener(() => EquipSelected(PowerUpType.Defensive));

        if (unequipButton != null)
            unequipButton.onClick.AddListener(UnequipSelected);

        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.OnInventoryChanged += RefreshList;
    }

    void OnDestroy()
    {
        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.OnInventoryChanged -= RefreshList;
    }

    void Update()
    {
        if (menuPanel != null && menuPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // ─────────────────────────────────────────────────────────────
    // OPEN / CLOSE
    // ─────────────────────────────────────────────────────────────

    public void Open()
    {
        if (menuPanel == null) return;
        menuPanel.SetActive(true);
        Time.timeScale = 0f;
        RefreshList();
    }

    public void Close()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        ClosePrompt();
        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────────────────────────
    // LIST
    // ─────────────────────────────────────────────────────────────

    public void RefreshList()
    {
        if (listParent == null || powerUpEntryPrefab == null) return;

        // Clear old entries
        foreach (Transform child in listParent)
            Destroy(child.gameObject);

        if (PowerUpManager.Instance == null) return;

        List<PowerUpData> collected = PowerUpManager.Instance.GetCollectedPowerUps();

        foreach (PowerUpData data in collected)
        {
            GameObject entry = Instantiate(powerUpEntryPrefab, listParent);
            PowerUpEntryUI entryUI = entry.GetComponent<PowerUpEntryUI>();
            if (entryUI != null)
                entryUI.Setup(data, this);
        }

        if (collected.Count == 0)
            Debug.Log("[PowerUpMenuUI] No power-ups collected yet.");
    }

    // ─────────────────────────────────────────────────────────────
    // SELECTION & PROMPT
    // ─────────────────────────────────────────────────────────────

    public void SelectPowerUp(PowerUpData data)
    {
        selectedPowerUp = data;
        ShowPrompt(data);
    }

    void ShowPrompt(PowerUpData data)
    {
        if (equipPromptPanel == null || data == null) return;
        equipPromptPanel.SetActive(true);

        if (promptTitle != null) promptTitle.text = data.displayName;
        if (promptDescription != null) promptDescription.text = data.description;
        if (promptIcon != null) promptIcon.sprite = data.icon;

        // Show the correct equip button based on type
        bool isOffensive = data.powerUpType == PowerUpType.Offensive;
        if (equipOffensiveButton != null)
            equipOffensiveButton.gameObject.SetActive(isOffensive);
        if (equipDefensiveButton != null)
            equipDefensiveButton.gameObject.SetActive(!isOffensive);

        // Show unequip button if this power-up is currently equipped
        bool isEquipped = PowerUpManager.Instance != null &&
            (PowerUpManager.Instance.GetEquippedOffensive()?.powerUpID == data.powerUpID ||
             PowerUpManager.Instance.GetEquippedDefensive()?.powerUpID == data.powerUpID);

        if (unequipButton != null)
            unequipButton.gameObject.SetActive(isEquipped);
    }

    void ClosePrompt()
    {
        if (equipPromptPanel != null) equipPromptPanel.SetActive(false);
        selectedPowerUp = null;
    }

    // ─────────────────────────────────────────────────────────────
    // EQUIP / UNEQUIP
    // ─────────────────────────────────────────────────────────────

    void EquipSelected(PowerUpType forceType)
    {
        if (selectedPowerUp == null || PowerUpManager.Instance == null) return;

        // If the type matches, equip normally
        // If it doesn't match, we still let them equip to the correct slot
        // (type is enforced by the button shown in the prompt)
        PowerUpManager.Instance.EquipPowerUp(selectedPowerUp);
        ClosePrompt();
        RefreshList();
    }

    void UnequipSelected()
    {
        if (selectedPowerUp == null || PowerUpManager.Instance == null) return;
        PowerUpManager.Instance.UnequipSlot(selectedPowerUp.powerUpType);
        ClosePrompt();
        RefreshList();
    }
}