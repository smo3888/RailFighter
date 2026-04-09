using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject shopPanel;
    public GameObject promptObject;

    [Header("Item Display")]
    public Transform itemListParent;
    public GameObject itemEntryPrefab;

    [Header("Selected Item Info")]
    public Image selectedItemSprite;
    public TextMeshProUGUI selectedItemName;
    public TextMeshProUGUI selectedItemDescription;
    public TextMeshProUGUI selectedItemPrice;
    public TextMeshProUGUI selectedItemSellValue;

    [Header("Currency")]
    public TextMeshProUGUI currencyText;

    [Header("Buttons")]
    public Button buyButton;
    public Button sellButton;
    public Button closeButton;

    [Header("Message")]
    public TextMeshProUGUI messageText;

    private ShopManager shopManager;
    private ShopItem selectedItem;

    void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (promptObject != null) promptObject.SetActive(false);
        if (messageText != null) messageText.text = "";

        if (closeButton != null)
            closeButton.onClick.AddListener(() => shopManager?.CloseShop());
    }

    public void OpenShop(List<ShopItem> items, ShopManager manager)
    {
        shopManager = manager;
        if (shopPanel != null) shopPanel.SetActive(true);
        if (promptObject != null) promptObject.SetActive(false);
        RefreshDisplay(items);
        UpdateCurrencyDisplay();
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        selectedItem = null;
        ClearSelection();
    }

    public void RefreshDisplay(List<ShopItem> items)
    {
        // Clear existing entries
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        // Spawn item entries
        foreach (ShopItem item in items)
        {
            if (itemEntryPrefab == null) break;
            GameObject entry = Instantiate(itemEntryPrefab, itemListParent);
            ShopItemEntry entryScript = entry.GetComponent<ShopItemEntry>();
            if (entryScript != null)
                entryScript.Setup(item, this);
        }

        UpdateCurrencyDisplay();
    }

    public void SelectItem(ShopItem item)
    {
        selectedItem = item;

        if (selectedItemSprite != null)
            selectedItemSprite.sprite = item.itemSprite;
        if (selectedItemName != null)
            selectedItemName.text = item.displayName;
        if (selectedItemDescription != null)
            selectedItemDescription.text = item.description;
        if (selectedItemPrice != null)
            selectedItemPrice.text = $"Buy: {item.buyPrice}";
        if (selectedItemSellValue != null)
            selectedItemSellValue.text = $"Sell: {item.sellValue}";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                shopManager?.TryBuyItem(selectedItem);
                UpdateCurrencyDisplay();
            });
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() =>
            {
                shopManager?.TrySellItem(selectedItem);
                UpdateCurrencyDisplay();
            });
        }
    }

    void ClearSelection()
    {
        if (selectedItemName != null) selectedItemName.text = "";
        if (selectedItemDescription != null) selectedItemDescription.text = "";
        if (selectedItemPrice != null) selectedItemPrice.text = "";
        if (selectedItemSellValue != null) selectedItemSellValue.text = "";
        if (selectedItemSprite != null) selectedItemSprite.sprite = null;
    }

    public void ShowPrompt(bool show)
    {
        if (promptObject != null)
            promptObject.SetActive(show);
    }

    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 2f);
        }
    }

    void ClearMessage()
    {
        if (messageText != null) messageText.text = "";
    }

    void UpdateCurrencyDisplay()
    {
        if (currencyText != null && GameManager.Instance != null)
            currencyText.text = $"Currency: {GameManager.Instance.Data.currency}";
    }
}