using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameOverUI : MonoBehaviour
{
    private UIDocument uiDocument;

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

        // Restart Button
        var restartButton = root.Q<Button>("RestartButton");
        if (restartButton != null)
        {
            if (SceneManager.GetActiveScene().name == "RailFighterEndless1")
            {
                restartButton.clicked += LoadRailFighterEndless1;
            }
            else
            {
                restartButton.clicked += LoadRailFighter1;
            }
        }

        // Start Button
        var startButton = root.Q<Button>("StartGame");
        Debug.Log("Start button found: " + (startButton != null));
        if (startButton != null)
        {
            startButton.clicked += LoadRailFighterEndless1;
        }
    }

    void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
        Time.timeScale = 1f;
    }

    void LoadRailFighter1()
    {
        SceneManager.LoadScene("RailFighter1");
        Time.timeScale = 1f;
    }

    void LoadRailFighterEndless1()
    {
        SceneManager.LoadScene("RailFighterEndless1");
        Time.timeScale = 1f;
    }
}