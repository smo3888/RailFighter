using UnityEngine;
using UnityEngine.SceneManagement;

public class TestMenuLoader : MonoBehaviour
{
    [Header("Scene Names")]
    public string mimicScene = "RailFighterBattleArena1";
    public string ssEagleScene = "RailFighter1";
    public string dungeonScene = "RailFighterDungeon";
    public string basicLevelScene = "RailFighter";

    public void LoadMimic()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mimicScene);
    }

    public void LoadSSEagle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(ssEagleScene);
    }

    public void LoadDungeon()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(dungeonScene);
    }

    public void LoadBasicLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(basicLevelScene);
    }
}