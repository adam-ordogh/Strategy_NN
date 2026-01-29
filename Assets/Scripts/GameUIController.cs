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

    public void TrainSoldier()
    {
        Building selected = initializer.inputController.selectedBuilding;

        if (selected != null && selected.buildingType == Building.BuildingType.Barracks)
        {
            initializer.productionManager.QueueUnit(selected, Unit.UnitType.Soldier);
        }
        else
        {
            Debug.LogWarning("Cannot train unit: No Barracks selected!");
        }
    }
    public void TrainArcher()
    {
        Building selected = initializer.inputController.selectedBuilding;

        if (selected != null && selected.buildingType == Building.BuildingType.Barracks)
        {
            initializer.productionManager.QueueUnit(selected, Unit.UnitType.Archer);
        }
        else
        {
            Debug.LogWarning("Cannot train unit: No Barracks selected!");
        }
    }
    public void TrainCavalry()
    {
        Building selected = initializer.inputController.selectedBuilding;

        if (selected != null && selected.buildingType == Building.BuildingType.Barracks)
        {
            initializer.productionManager.QueueUnit(selected, Unit.UnitType.Cavalry);
        }
        else
        {
            Debug.LogWarning("Cannot train unit: No Barracks selected!");
        }
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
