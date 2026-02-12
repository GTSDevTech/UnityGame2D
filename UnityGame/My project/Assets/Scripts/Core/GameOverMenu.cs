using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    public string gameSceneName = "scene-luis-3";
    public string startSceneName = "StartScene";

    public void Retry()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void Menu()
    {
        SceneManager.LoadScene(startSceneName);
    }
}
