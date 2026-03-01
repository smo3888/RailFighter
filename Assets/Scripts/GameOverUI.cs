using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// ============================================
// GAME OVER UI
// Handles button interactions on the game over screen
// Supports returning to menu and restarting the current mode
// ============================================

public class GameOverUI : MonoBehaviour
{
    // ============================================
    // REFERENCES
    // ============================================
    private UIDocument uiDocument;

    // ============================================
    // LAYOUTS
    // ============================================
    private string[] endlessLayouts = {
        "RailFighterEndless1",
        "RailFighterEndless2",
        "RailFighterEndless3"
    };

    // ============================================
    // START
    // ============================================
    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Back To Menu Button
        var backToMenuButton = root.Q<Button>("BackToMenuButton");
        if (backToMenuButton != null)
        {
            backToMenuButton.clicked += LoadMenu;
        }

        // Restart Button - rerolls random layout
        var restartButton = root.Q<Button>("RestartButton");
        if (restartButton != null)
        {
            restartButton.clicked += RestartRandom;
        }
    }

    // ============================================
    // SCENE LOADERS
    // ============================================

    // Returns to main menu
    void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
        Time.timeScale = 1f;
    }

    // Loads a random endless layout
    void RestartRandom()
    {
        int randomIndex = Random.Range(0, endlessLayouts.Length);
        SceneManager.LoadScene(endlessLayouts[randomIndex]);
        Time.timeScale = 1f;
    }
}