using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

public class MainMenuController : MonoBehaviour
{
    public string gameSceneName = "GameScene";

    public GameObject menuPanel;
    public GameObject newGamePanel;

    // --- CONTINUE GAME ---
    public void OnContinueClicked()
    {
        string path = Path.Combine(Application.persistentDataPath, "Saves");
        if (Directory.Exists(path))
        {
            var directory = new DirectoryInfo(path);
            var newestFile = directory.GetFiles("*.json").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

            if (newestFile != null)
            {
                LevelLoadBridge.SaveFileToLoad = Path.GetFileNameWithoutExtension(newestFile.Name);
                SceneManager.LoadScene(gameSceneName);
                return;
            }
        }
        Debug.Log("No saves found to continue.");
    }
    // --- NEW GAME PANEL---
    public void ToggleNewGamePanel()
    {
        bool isOpening = newGamePanel.activeSelf;

        newGamePanel.SetActive(!isOpening);
        menuPanel.SetActive(isOpening);
    }



    // --- NEW GAME ---
    public void OnStartNewGameClicked(int mapSeed)
    {
        LevelLoadBridge.SaveFileToLoad = ""; 
        LevelLoadBridge.MapSeed = mapSeed;
        SceneManager.LoadScene(gameSceneName);
    }

    // --- EXIT GAME ---
    public void OnExitClicked()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }
}