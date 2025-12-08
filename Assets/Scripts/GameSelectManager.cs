using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameSelectManager : MonoBehaviour
{

    private UIDocument uiDocument;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        // initialize UI
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

       

        // Back Button
        var backButton = root.Q<Button>("BackButton");
        if (backButton != null)
        {
            backButton.clicked += LoadMenu;
        }

    

        // RailFighter Button

        var RailFighterButton = root.Q<Button>("RailFighterButton");
        if (RailFighterButton != null)
        {
            RailFighterButton.clicked += LoadRailFighterSelect;
        }

        //Rail Fighter 1 Buton

        var RailFighter1 = root.Q<Button>("RailFighter1");
        if (RailFighter1 != null)
        {
            RailFighter1.clicked += LoadRailFighter1; ;
        }

        //Rail Fighter 2 Button

        var RailFighter2 = root.Q<Button>("RailFighter2");
        if (RailFighter2 != null)
        {
            RailFighter2.clicked += LoadRailFighter2;
        }

        //Rail Fighter 3 Button

        var RailFighter3 = root.Q<Button>("RailFighter3");
        if (RailFighter3 != null)
        {
            RailFighter3.clicked += LoadRailFighter3;
        }

        //Rail Fighter 4 Button

        var RailFighter4 = root.Q<Button>("RailFighter4");
        if (RailFighter4 != null)
        {
            RailFighter4.clicked += LoadRailFighter4;
        }

        //Rail Fighter 5 Button

        var RailFighter5 = root.Q<Button>("RailFighter5");
        if (RailFighter5 != null)
        {
            RailFighter5.clicked += LoadRailFighter5;
        }

        // Load Scenes

    

        void LoadMenu()
        {
            SceneManager.LoadScene("Menu");
            Time.timeScale = 1f;
        }

        

        // Update is called once per frame
        void Update()
        {

        }

        void LoadRailFighter()
        {
            SceneManager.LoadScene("RailFighter");
            Time.timeScale = 1f;
        }

        void LoadRailFighterSelect()
        {
            SceneManager.LoadScene("RailFighterSelect");
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
    }
}
