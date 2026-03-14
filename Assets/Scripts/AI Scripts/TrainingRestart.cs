// TrainingRestart.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingRestart : MonoBehaviour
{
    public GameInitializer gameInitializer;
    public int maxTurnsPerEpisode = 500;

    private GameManager gameManager;

    void Start()
    {
        // Get reference after initialization
        if (gameInitializer != null && gameInitializer.gameManager != null)
        {
            gameManager = gameInitializer.gameManager;
        }
    }

    void Update()
    {
        if (gameManager == null || !gameInitializer.isTrainingMode) return;

        //Debug.Log($"TrainingRestart Update - Turn: {gameManager.turnNumber}");

        // Check if episode should end
        if (ShouldEndEpisode())
        {
            RestartEpisode();
        }
    }

    bool ShouldEndEpisode()
    {
        // End if any player has no buildings
        //foreach (var player in gameManager.players)
        //{
        //    if (player.myBuildings.Count == 0)
        //        return true;
        //}

        if (gameManager.turnNumber > 5)
        {
            foreach (var player in gameManager.players) 
            {
                if (player.myBuildings.Count == 0)
                    return true;
            }
        }

        // End if too many turns
        return gameManager.turnNumber > maxTurnsPerEpisode;
    }

    void RestartEpisode()
    {
        Debug.Log($"Episode ended at turn {gameManager.turnNumber}. Restarting...");

        // Simply reload the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}