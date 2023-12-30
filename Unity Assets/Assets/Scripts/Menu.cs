using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void StartGameMode(bool single)
    {
        PlayerManager.singlePlayer = single;
        SceneManager.LoadScene("Game", LoadSceneMode.Single);     
    }

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
