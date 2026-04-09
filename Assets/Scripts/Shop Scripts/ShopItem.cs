using UnityEngine;

public enum ItemType
{
    Ability,
    Consumable,
    Upgrade
}

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Rail Fighter/Shop Item")]
public class ShopItem : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique ID used to track purchases — never change this after setting it")]
    public string itemID;
    public string displayName;
    [TextArea(2, 4)]
    public string description;

    [Header("Visual")]
    public Sprite itemSprite;

    [Header("Pricing")]
    public int buyPrice;
    [Tooltip("How much the player gets for selling this back")]
    public int sellValue;

    [Header("Type")]
    public ItemType itemType;

    [Header("Availability")]
    [Tooltip("Player can only buy this once ever")]
    public bool isUnique = true;

    [Tooltip("Reserved for future stock refresh system — not used yet")]
    public bool canRestock = false;
}