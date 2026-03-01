using UnityEngine;
using UnityEngine.UIElements;
public class CoinUI : MonoBehaviour
{
    
 
    void OnEnable()
    { 
        
       
       
    }
 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
 var root = GetComponent<UIDocument>().rootVisualElement;
        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        Label Coins = root.Q<Label>("Coins");
        Coins.text = "Coins: " + totalCoins;
        
    }
}
