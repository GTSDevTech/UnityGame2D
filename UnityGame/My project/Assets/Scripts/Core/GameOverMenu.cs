using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    public string gameSceneName = "Spain_Luis";
    public string startSceneName = "StartScene";

    public void Retry()
    {
        SceneManager.LoadScene(1); // si Spain_Luis es la escena 1 en la lista
    }

    public void Menu()
    {
        SceneManager.LoadScene(startSceneName);
    }
}
