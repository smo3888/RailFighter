using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PowerUpCollectionUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject collectionPanel;
    public Button closeButton;

    [Header("Grid")]
    public Transform gridContent;
    public GameObject gridEntryPrefab;

    [Header("Unequip Button")]
    public Button unequipButton;

    [Header("References")]
    public PowerUpMenuUI menuUI;

    private PowerUpType currentSlotType;

    void Start()
    {
        if (collectionPanel != null) collectionPanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (unequipButton != null) unequipButton.onClick.AddListener(Unequip);
    }

    public void Open(PowerUpType slotType, PowerUpMenuUI menu)
    {
        currentSlotType = slotType;
        menuUI = menu;
        if (collectionPanel != null) collectionPanel.SetActive(true);

        // Hide the main menu panel
        menuUI?.menuPanel?.SetActive(false);

        BuildGrid();
    }

    public void Close()
    {
        if (collectionPanel != null) collectionPanel.SetActive(false);

        // Restore the main menu panel
        menuUI?.menuPanel?.SetActive(true);
    }

    void BuildGrid()
    {
        if (gridContent == null || gridEntryPrefab == null) return;
        foreach (Transform child in gridContent) Destroy(child.gameObject);
        if (PowerUpManager.Instance == null) return;

        foreach (PowerUpData data in PowerUpManager.Instance.GetCollectedPowerUps())
        {
            if (data.powerUpType != currentSlotType) continue;
            GameObject entry = Instantiate(gridEntryPrefab, gridContent);
            PowerUpGridEntry entryScript = entry.GetComponent<PowerUpGridEntry>();
            if (entryScript != null) entryScript.Setup(data, this);
        }
    }

    public void SelectPowerUp(PowerUpData data)
    {
        if (menuUI == null || data == null) return;
        menuUI.SelectForEquip(data);
        Close();
    }

    void Unequip()
    {
        PowerUpManager.Instance?.UnequipSlot(currentSlotType);
        menuUI?.RefreshSlots();
        Close();
    }
}