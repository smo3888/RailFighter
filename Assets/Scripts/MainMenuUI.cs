using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// ============================================
// MAIN MENU UI
// Handles the title screen start button
// ============================================
public class MainMenuUI : MonoBehaviour
{
    // ============================================
    // REFERENCES
    // ============================================
    private UIDocument MainMenu;

    // ============================================
    // START
    // ============================================
    void Start()
    {
        MainMenu = GetComponent<UIDocument>();
        var root = MainMenu.rootVisualElement;

        // Start Button
        var startButton = root.Q<Button>("StartButton");
        if (startButton != null)
        {
            startButton.clicked += StartGame;
        }
    }

    // ============================================
    // START GAME
    // Randomly selects one of the endless layouts or boss fight
    // ============================================
    void StartGame()
    {
        string[] layouts = {
            "RailFighterEndless1",
            "RailFighterEndless2",
            "RailFighterEndless3",
            "RailFighterBattleArena1"
        };

        int randomIndex = Random.Range(0, layouts.Length);
        SceneManager.LoadScene(layouts[randomIndex]);
    }
}