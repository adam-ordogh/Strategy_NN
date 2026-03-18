// TrainingRestart.cs
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingRestart : MonoBehaviour
{
    public GameInitializer gameInitializer;
    public int maxTurnsPerEpisode = 300;

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
        //if (gameManager.turnNumber > 5)
        //{
        //    foreach (var player in gameManager.players)
        //    {
        //        if (player.myBuildings.Count == 0)
        //        {
        //            Debug.Log($"Episode ended by ELIMINATION of {player.playerId} at turn {gameManager.turnNumber}");
        //            return true;
        //        }
        //    }
        //}

        //if (gameManager.turnNumber > 5)
        //{
        //    foreach (var player in gameManager.players)
        //    {
        //        // FIX: Ignore roads here as well
        //        if (player.myBuildings.Count(b => b.buildingType != Building.BuildingType.Road) == 0)
        //        {
        //            Debug.Log($"Episode ended by ELIMINATION of {player.playerId} at turn {gameManager.turnNumber}");
        //            return true;
        //        }
        //    }
        //}


        if (gameManager.turnNumber > 5)
        {
            foreach (var player in gameManager.players)
            {
                bool hasTownCenter = player.myBuildings.Any(b =>
                    b.buildingType == Building.BuildingType.TownCenter);
                // FIX: Ignore roads here as well
                if (!hasTownCenter)
                {
                    Debug.Log($"Episode ended by ELIMINATION of {player.playerId} at turn {gameManager.turnNumber}");
                    return true;
                }
            }
        }

        if (gameManager.turnNumber > maxTurnsPerEpisode)
        {
            Debug.Log($"Episode ended by TIMEOUT at turn {gameManager.turnNumber}");
            return true;
        }

        return false;
    }

    void RestartEpisode()
    {
        //Debug.Log($"Episode ended at turn {gameManager.turnNumber}. Restarting...");

        // Simply reload the scene
        var agents = GameObject.FindObjectsByType<AIMacroML>(FindObjectsSortMode.None);
        foreach (var agent in agents)
        {
            agent.AddReward(-200.0f);
            agent.EndEpisode();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}