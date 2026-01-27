using UnityEngine;

public class GameUIController : MonoBehaviour
{
    public GameInitializer initializer;

    public TMPro.TextMeshProUGUI turnLabel;

    public void EndTurn()
    {
        initializer.gameManager.NextTurn();

        int turnNumber = initializer.gameManager.turnNumber;
        turnLabel.text = $"Turn {turnNumber}";
    }
}
