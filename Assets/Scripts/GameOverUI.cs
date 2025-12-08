using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameOverUI : MonoBehaviour
{
    
    private UIDocument uiDocument;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        // initialize UI
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
            restartButton.clicked += LoadRailFighter1;
        }
        
        //Level Select Buttons


        if (SceneManager.GetActiveScene().name == "RailFighter2")
        {
            restartButton.clicked += LoadRailFighter2;
        }

        if (SceneManager.GetActiveScene().name == "RailFighter3")
        {
            restartButton.clicked += LoadRailFighter3;
        }

        if (SceneManager.GetActiveScene().name == "RailFighter4")
        {
            restartButton.clicked += LoadRailFighter4;
        }

        if (SceneManager.GetActiveScene().name == "RailFighter5")
        {
            restartButton.clicked += LoadRailFighter5;
        }

        if (SceneManager.GetActiveScene().name == "RailFighter6")
        {
            restartButton.clicked += LoadRailFighter6;
        }

        if (SceneManager.GetActiveScene().name == "RailFighter7")
        {
            restartButton.clicked += LoadRailFighter7;
        }

        if (SceneManager.GetActiveScene().name == "RailFighter8")
        {
            restartButton.clicked += LoadRailFighter8;
        }

        if (SceneManager.GetActiveScene().name == "RailFighter9")
        {
            restartButton.clicked += LoadRailFighter9;
        }

        if (SceneManager.GetActiveScene().name == "RailFighter10")
        {
            restartButton.clicked += LoadRailFighter10;
        }

        // Load Scenes


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

        void LoadRailFighter2()
        {
            SceneManager.LoadScene("RailFighter2");
            Time.timeScale = 1f;
        }

        void LoadRailFighter3()
        {
            SceneManager.LoadScene("RailFighter3");
            Time.timeScale = 1f;
        }

        void LoadRailFighter4()
        {
            SceneManager.LoadScene("RailFighter4");
            Time.timeScale = 1f;
        }

        void LoadRailFighter5()
        {
            SceneManager.LoadScene("RailFighter5");
            Time.timeScale = 1f;
        }

        void LoadRailFighter6()
        {
            SceneManager.LoadScene("RailFighter6");
            Time.timeScale = 1f;
        }

        void LoadRailFighter7()
        {
            SceneManager.LoadScene("RailFighter7");
            Time.timeScale = 1f;
        }

        void LoadRailFighter8()
        {
            SceneManager.LoadScene("RailFighter8");
            Time.timeScale = 1f;
        }

        void LoadRailFighter9()
        {
            SceneManager.LoadScene("RailFighter9");
            Time.timeScale = 1f;
        }

        void LoadRailFighter10()
        {
            SceneManager.LoadScene("RailFighter10");
            Time.timeScale = 1f;
        }

    }

  
}
