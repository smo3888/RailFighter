using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Save Data ────────────────────────────────────────────────────────────
    [System.Serializable]
    public class SaveData
    {
        // Player state
        public int currentHealth = 3;
        public int maxHealth = 3;

        // Currency
        public int currency = 0;

        // Purchased items
        public List<string> purchasedItemIDs = new List<string>();

        // Keys
        public int keys = 0;

        // Dungeon progress
        public bool challenge1Complete = false;
        public bool challenge2Complete = false;
        public bool challenge3Complete = false;
        public bool challenge4Complete = false;

        // World alterations
        public bool shopUnlocked = false;
        public bool shortcutUnlocked = false;
        public bool secretAreaUnlocked = false;
        public bool bossRoomUnlocked = false;

        // Scene state
        public string lastSavedScene = "RailFighterEndless2";
        public float lastSavedPositionX = 0f;
        public float lastSavedPositionY = 0f;

        // Dungeon tracking
        public int currentDungeon = 1;
        public bool[] visitedRooms = new bool[20];

        // Return position after challenge room
        public float dungeonReturnX = 0f;
        public float dungeonReturnY = 0f;
        public string dungeonReturnRailName = "";

        public float challengeRoomEntryX = 0f;
        public float challengeRoomEntryY = 0f;
    }

    public SaveData Data = new SaveData();

    [Header("Autosave")]
    public float autosaveInterval = 300f;
    private float autosaveTimer = 0f;

    [Header("Scene Names")]
    public string dungeonScene = "RailFighterEndless2";
    public string bossScene1 = "RailFighter1";
    public string bossScene2 = "RailFighterBattleArena1";

    [Header("Transition")]
    public float fadeDuration = 0.5f;

    private string savePath;
    private bool isTransitioning = false;

    // ── Singleton Setup ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        savePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "RailFighterSaves", "save.json");
        Directory.CreateDirectory(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "RailFighterSaves"));
        LoadGame();
    }

    void Update()
    {
        autosaveTimer += Time.deltaTime;
        if (autosaveTimer >= autosaveInterval)
        {
            autosaveTimer = 0f;
            SaveGame();
            Debug.Log("Autosaved!");
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Data.keys++;
            Debug.Log("Debug key added. Total keys: " + Data.keys);
        }
    }

    // ── Save / Load ──────────────────────────────────────────────────────────
    public void SaveGame(bool savePosition = true)
    {
        if (savePosition)
        {
            PlayerControllerRailFighter player = FindObjectOfType<PlayerControllerRailFighter>();
            if (player != null)
            {
                Data.lastSavedPositionX = player.transform.position.x;
                Data.lastSavedPositionY = player.transform.position.y;
            }
        }
        Data.lastSavedScene = SceneManager.GetActiveScene().name;
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Game saved to: " + savePath);
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            Data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Game loaded.");
        }
        else
        {
            Data = new SaveData();
            Debug.Log("No save found — starting fresh.");
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(savePath))
            File.Delete(savePath);
        Data = new SaveData();
        Debug.Log("Save deleted.");
    }

    // ── Currency ─────────────────────────────────────────────────────────────
    public void AddCurrency(int amount)
    {
        Data.currency += amount;
        Debug.Log($"Currency: +{amount} = {Data.currency}");
    }

    public bool SpendCurrency(int amount)
    {
        if (Data.currency < amount) return false;
        Data.currency -= amount;
        return true;
    }

    // ── Purchased Items ──────────────────────────────────────────────────────
    public bool HasPurchasedItem(string itemID)
    {
        return Data.purchasedItemIDs.Contains(itemID);
    }

    public void AddPurchasedItem(string itemID)
    {
        if (!Data.purchasedItemIDs.Contains(itemID))
            Data.purchasedItemIDs.Add(itemID);
    }

    public void RemovePurchasedItem(string itemID)
    {
        Data.purchasedItemIDs.Remove(itemID);
    }

    // ── Challenge Room Completion ─────────────────────────────────────────────
    public void CompleteChallenge(int challengeNumber)
    {
        switch (challengeNumber)
        {
            case 1:
                Data.challenge1Complete = true;
                Data.shopUnlocked = true;
                Data.keys++;
                Debug.Log("Challenge 1 complete! Shop unlocked.");
                break;
            case 2:
                Data.challenge2Complete = true;
                Data.shortcutUnlocked = true;
                Data.keys++;
                Debug.Log("Challenge 2 complete! Shortcut unlocked.");
                break;
            case 3:
                Data.challenge3Complete = true;
                Data.secretAreaUnlocked = true;
                Data.keys++;
                Debug.Log("Challenge 3 complete! Secret area unlocked.");
                break;
            case 4:
                Data.challenge4Complete = true;
                Data.bossRoomUnlocked = true;
                Data.keys++;
                Debug.Log("Challenge 4 complete! Boss room unlocked.");
                break;
        }
        SaveGame();
    }

    public bool IsChallengeComplete(int challengeNumber)
    {
        switch (challengeNumber)
        {
            case 1: return Data.challenge1Complete;
            case 2: return Data.challenge2Complete;
            case 3: return Data.challenge3Complete;
            case 4: return Data.challenge4Complete;
            default: return false;
        }
    }

    public int GetKeyCount() => Data.keys;
    public bool IsBossRoomUnlocked() => Data.bossRoomUnlocked;
    public bool IsShopUnlocked() => Data.shopUnlocked;

    // ── Player Health Sync ───────────────────────────────────────────────────
    public void SyncPlayerHealth()
    {
        PlayerControllerRailFighter player = FindObjectOfType<PlayerControllerRailFighter>();
        if (player != null)
            Data.currentHealth = player.GetCurrentHealth();
    }

    public int GetSavedHealth() => Data.currentHealth;

    // ── Scene Transitions ────────────────────────────────────────────────────
    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToScene(sceneName));
    }

    public void LoadDungeon()
    {
        SaveGame();
        LoadScene(dungeonScene);
    }

    public void LoadChallengeRoom(string sceneName)
    {
        SyncPlayerHealth();
        SaveGame();
        LoadScene(sceneName);
    }

    public void ExitChallengeRoom()
    {
        SaveGame(false);
        LoadScene(dungeonScene);
    }

    public void ReturnToLastSave()
    {
        LoadGame();
        LoadScene(Data.lastSavedScene);
    }

    IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;
        yield return StartCoroutine(Fade(0f, 1f));
        SceneManager.LoadScene(sceneName);
        yield return null;

        PlayerControllerRailFighter player = FindObjectOfType<PlayerControllerRailFighter>();
        if (player != null)
            player.SetHealth(Data.currentHealth);

        yield return StartCoroutine(Fade(1f, 0f));
        isTransitioning = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        FadeCanvas fadeCanvas = FindObjectOfType<FadeCanvas>();
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            if (fadeCanvas != null)
                fadeCanvas.SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
    }

    // ── Room Visit Tracking ──────────────────────────────────────────────────
    public void MarkRoomVisited(int roomIndex)
    {
        if (roomIndex >= 0 && roomIndex < Data.visitedRooms.Length)
            Data.visitedRooms[roomIndex] = true;
    }

    public bool IsRoomVisited(int roomIndex)
    {
        if (roomIndex >= 0 && roomIndex < Data.visitedRooms.Length)
            return Data.visitedRooms[roomIndex];
        return false;
    }

    // ── New Game ─────────────────────────────────────────────────────────────
    public void StartNewGame()
    {
        DeleteSave();
        LoadScene(dungeonScene);
    }
}