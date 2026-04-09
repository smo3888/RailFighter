using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemEntry : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public Button selectButton;

    private ShopItem item;
    private ShopUI shopUI;

    public void Setup(ShopItem shopItem, ShopUI ui)
    {
        item = shopItem;
        shopUI = ui;

        if (itemIcon != null) itemIcon.sprite = item.itemSprite;
        if (itemNameText != null) itemNameText.text = item.displayName;
        if (itemPriceText != null) itemPriceText.text = item.buyPrice.ToString();

        if (selectButton != null)
            selectButton.onClick.AddListener(() => shopUI.SelectItem(item));
    }
}