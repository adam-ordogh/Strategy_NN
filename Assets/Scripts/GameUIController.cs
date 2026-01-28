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

    public void SelectedTownHall()
    {
        initializer.inputController.activeBuildingType = Building.BuildingType.TownHall;
    }
    public void SelectedBarracks()
    {
        initializer.inputController.activeBuildingType = Building.BuildingType.Barracks;
    }
    public void SelectedHouse()
    {
        initializer.inputController.activeBuildingType = Building.BuildingType.House;
    }
    public void SelectedLumberyard()
    {
        initializer.inputController.activeBuildingType = Building.BuildingType.Lumberyard;
    }
    public void SelectedFarm()
    {
        initializer.inputController.activeBuildingType = Building.BuildingType.Farm;
    }
    public void SelectedMine()
    {
        initializer.inputController.activeBuildingType = Building.BuildingType.Mine;
    }

}
