using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewShopConfig", menuName = "Rail Fighter/Shop Config")]
public class ShopConfig : ScriptableObject
{
    [Header("Shop Identity")]
    [Tooltip("Unique ID for this shop — used to track which shop the player is in")]
    public string shopID;
    public string shopName = "Shop";

    [Header("Stock")]
    public List<ShopItem> items = new List<ShopItem>();
}