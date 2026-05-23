using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    private UIDocument uiDocument;

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        var startButton = root.Q<Button>("StartButton");
        var exitButton = root.Q<Button>("ExitButton");

        if (startButton != null)
        {
            startButton.clicked += OnStartClicked;
        }

        if (exitButton != null)
        {
            exitButton.clicked += OnExitClicked;
        }
    }

    void OnStartClicked()
    {
        SceneManager.LoadScene("GameSelect");
        Time.timeScale = 1f;
    }

    void OnExitClicked()
    {
        Application.Quit();
    }
}