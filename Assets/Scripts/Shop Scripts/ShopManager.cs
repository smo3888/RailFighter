using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Config")]
    public ShopConfig shopConfig;

    [Header("UI Reference")]
    public ShopUI shopUI;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.W;

    private bool playerInRange = false;
    private bool shopOpen = false;
    private List<ShopItem> availableItems = new List<ShopItem>();

    void Start()
    {
        if (shopConfig == null)
        {
            Debug.LogWarning("ShopManager: No ShopConfig assigned!");
            return;
        }

        RefreshAvailableItems();
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey) && !shopOpen)
            OpenShop();
        else if (shopOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseShop();
    }

    void RefreshAvailableItems()
    {
        availableItems.Clear();

        foreach (ShopItem item in shopConfig.items)
        {
            if (item == null) continue;

            // Skip unique items the player already owns
            if (item.isUnique && GameManager.Instance != null &&
                GameManager.Instance.HasPurchasedItem(item.itemID))
                continue;

            availableItems.Add(item);
        }
    }

    public void OpenShop()
    {
        shopOpen = true;
        Time.timeScale = 0f;
        RefreshAvailableItems();

        if (shopUI != null)
            shopUI.OpenShop(availableItems, this);

        Debug.Log($"Opened shop: {shopConfig.shopName}");
    }

    public void CloseShop()
    {
        shopOpen = false;
        Time.timeScale = 1f;

        if (shopUI != null)
            shopUI.CloseShop();
    }

    public bool TryBuyItem(ShopItem item)
    {
        if (GameManager.Instance == null) return false;

        if (GameManager.Instance.Data.currency < item.buyPrice)
        {
            Debug.Log("Not enough currency!");
            shopUI?.ShowMessage("Not enough currency!");
            return false;
        }

        if (item.isUnique && GameManager.Instance.HasPurchasedItem(item.itemID))
        {
            Debug.Log("Already purchased!");
            shopUI?.ShowMessage("Already owned!");
            return false;
        }

        // Deduct currency
        GameManager.Instance.Data.currency -= item.buyPrice;

        // Record purchase
        GameManager.Instance.AddPurchasedItem(item.itemID);

        // Save
        GameManager.Instance.SaveGame(false);

        Debug.Log($"Bought: {item.displayName} for {item.buyPrice}");

        RefreshAvailableItems();
        shopUI?.RefreshDisplay(availableItems);
        shopUI?.ShowMessage($"Purchased {item.displayName}!");

        return true;
    }

    public bool TrySellItem(ShopItem item)
    {
        if (GameManager.Instance == null) return false;

        if (!GameManager.Instance.HasPurchasedItem(item.itemID))
        {
            Debug.Log("Player doesn't own this item!");
            return false;
        }

        // Add currency
        GameManager.Instance.Data.currency += item.sellValue;

        // Remove from purchased
        GameManager.Instance.RemovePurchasedItem(item.itemID);

        // Save
        GameManager.Instance.SaveGame(false);

        Debug.Log($"Sold: {item.displayName} for {item.sellValue}");

        RefreshAvailableItems();
        shopUI?.RefreshDisplay(availableItems);
        shopUI?.ShowMessage($"Sold {item.displayName} for {item.sellValue}!");

        return true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        shopUI?.ShowPrompt(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        shopUI?.ShowPrompt(false);
        if (shopOpen) CloseShop();
    }
}