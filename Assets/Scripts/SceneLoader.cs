
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("»м€ сцены не указано.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "—цена не найдена в списке сцен: " + sceneName
            );
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadMainMenu()
    {
        LoadScene("Menu");
    }

    public void LoadCyberMuseum()
    {
        LoadScene("FirstMuseum");
    }

    public void LoadPlanetarium()
    {
        LoadScene("SecondMuseum");
    }

    public void LoadZoo()
    {
        LoadScene("ThirdMuseum");
    }

    public void RestartCurrentScene()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}