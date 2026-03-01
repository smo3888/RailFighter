using UnityEngine;
using UnityEngine.UIElements;

public class CoinManager : MonoBehaviour
{
    
    public static CoinManager instance;
    public int totalCoins = 0;
    public GameObject coinPrefab;
    public void AddCoins(int amount)
    {
        totalCoins += amount;
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();
        Debug.Log("Coins: " + totalCoins);
    }

    public int GetTotalCoins()
    { return totalCoins; }


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // load saved coins
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);


    }


    // Update is called once per frame
    void Update()
    {
       
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            {
            int currentCoins = PlayerPrefs.GetInt("TotalCoins", 0);
            PlayerPrefs.SetInt("TotalCoins", currentCoins + 1);
            PlayerPrefs.Save();
            Destroy(gameObject);

            Debug.Log("Coin collected! Total coins: " + currentCoins);
        }

       
    }

    
        
   
   
}
