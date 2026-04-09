using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneTrigger : MonoBehaviour
{
    [Header("Zone Settings")]
    public string sceneToLoad;
    public int challengeNumber; // 0 = not a challenge room, just a transition
    public bool requiresKey = false;
    public int keysRequired = 0;

    [Header("Prompt UI")]
    public GameObject promptUI;
    public TextMeshProUGUI promptText;

    private bool playerInRange = false;
    private bool alreadyComplete = false;

    void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);

        if (challengeNumber > 0 && GameManager.Instance != null)
            alreadyComplete = GameManager.Instance.IsChallengeComplete(challengeNumber);
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            TryEnterZone();
    }

    void TryEnterZone()
    {
        Debug.Log("TryEnterZone called - GameManager exists: " + (GameManager.Instance != null));
        Debug.Log("Scene to load: " + sceneToLoad);

        if (GameManager.Instance == null) return;

        // Check key requirement
        if (requiresKey && GameManager.Instance.GetKeyCount() < keysRequired)
        {
            if (promptText != null)
                promptText.text = $"Need {keysRequired} keys to enter. You have {GameManager.Instance.GetKeyCount()}.";
            return;
        }

        // Only save dungeon return position when ENTERING a challenge room
        if (challengeNumber > 0)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                PlayerControllerRailFighter player = playerObj.GetComponent<PlayerControllerRailFighter>();
                GameManager.Instance.Data.dungeonReturnX = playerObj.transform.position.x;
                GameManager.Instance.Data.dungeonReturnY = playerObj.transform.position.y;

                // Save the exact rail the player is standing on by name
                string railName = player != null ? player.GetCurrentRailName() : "";
                GameManager.Instance.Data.dungeonReturnRailName = railName;

                Debug.Log($"Saved dungeon return position: {playerObj.transform.position.x}, {playerObj.transform.position.y} on rail: {railName}");
            }
        }

        // Save before transitioning
        GameManager.Instance.SaveGame(false);

        // Load the scene
        if (challengeNumber > 0)
            GameManager.Instance.LoadChallengeRoom(sceneToLoad);
        else
            GameManager.Instance.LoadScene(sceneToLoad);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;

        if (promptUI != null)
            promptUI.SetActive(true);

        if (promptText != null)
        {
            if (alreadyComplete)
                promptText.text = "Challenge complete! Press W to re-enter.";
            else if (requiresKey)
                promptText.text = $"Requires {keysRequired} keys. Press W to enter.";
            else
                promptText.text = "Press W to enter.";
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;

        if (promptUI != null)
            promptUI.SetActive(false);
    }
}