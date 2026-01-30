using UnityEngine;

public class GameUIController : MonoBehaviour
{
    public GameInitializer initializer;

    public TMPro.TextMeshProUGUI turnLabel;
    public TMPro.TextMeshProUGUI currentPlayerLabel;

    public TMPro.TextMeshProUGUI foodLabel;
    public TMPro.TextMeshProUGUI woodLabel;
    public TMPro.TextMeshProUGUI goldLabel;
    public TMPro.TextMeshProUGUI availablePopLabel;

    public TMPro.TextMeshProUGUI foodWorkersLabel;
    public TMPro.TextMeshProUGUI woodWorkersLabel;
    public TMPro.TextMeshProUGUI goldWorkersLabel;

    public void EndTurn()
    {
        initializer.gameManager.NextTurn();

        int turnNumber = initializer.gameManager.turnNumber;
        turnLabel.text = $"Turn {turnNumber}";

        int currentPlayer = initializer.gameManager.currentPlayerId;
        currentPlayerLabel.text = $"Player {currentPlayer}";

        PlayerProfile activePlayer = initializer.gameManager.CurrentPlayer;
        foodLabel.text = $"Food: {activePlayer.food}";
        woodLabel.text = $"Wood: {activePlayer.wood}";
        goldLabel.text = $"Gold: {activePlayer.gold}";
        availablePopLabel.text = $"Population: {activePlayer.availablePopulation}/{activePlayer.maxPopulation}";
        foodWorkersLabel.text = $"Food Workers: {activePlayer.assignedFoodWorkers}/{activePlayer.maxFoodSlots}";
        woodWorkersLabel.text = $"Wood Workers: {activePlayer.assignedWoodWorkers}/{activePlayer.maxWoodSlots}";
        goldWorkersLabel.text = $"Gold Workers: {activePlayer.assignedGoldWorkers}/{activePlayer.maxGoldSlots}";
    }

    public void AddFoodWorker()
    {
        PlayerProfile activePlayer = initializer.gameManager.CurrentPlayer;
        initializer.economyManager.AssignFoodWorkers(activePlayer, true);
        foodWorkersLabel.text = $"Food Workers: {activePlayer.assignedFoodWorkers}/{activePlayer.maxFoodSlots}";
    }

    public void RemoveFoodWorker()
    {
        PlayerProfile activePlayer = initializer.gameManager.CurrentPlayer;
        initializer.economyManager.AssignFoodWorkers(activePlayer, false);
        foodWorkersLabel.text = $"Food Workers: {activePlayer.assignedFoodWorkers}/{activePlayer.maxFoodSlots}";
    }

    public void AddWoodWorker()
    {
        PlayerProfile activePlayer = initializer.gameManager.CurrentPlayer;
        initializer.economyManager.AssignWoodWorkers(activePlayer, true);
        woodWorkersLabel.text = $"Wood Workers: {activePlayer.assignedWoodWorkers}/{activePlayer.maxWoodSlots}";
    }

    public void RemoveWoodWorker()
    {
        PlayerProfile activePlayer = initializer.gameManager.CurrentPlayer;
        initializer.economyManager.AssignWoodWorkers(activePlayer, false);
        woodWorkersLabel.text = $"Wood Workers: {activePlayer.assignedWoodWorkers}/{activePlayer.maxWoodSlots}";
    }

    public void AddGoldWorker()
    {
        PlayerProfile activePlayer = initializer.gameManager.CurrentPlayer;
        initializer.economyManager.AssignGoldWorkers(activePlayer, true);
        goldWorkersLabel.text = $"Gold Workers: {activePlayer.assignedGoldWorkers}/{activePlayer.maxGoldSlots}";
    }

    public void RemoveGoldWorker()
    {
        PlayerProfile activePlayer = initializer.gameManager.CurrentPlayer;
        initializer.economyManager.AssignGoldWorkers(activePlayer, false);
        goldWorkersLabel.text = $"Gold Workers: {activePlayer.assignedGoldWorkers}/{activePlayer.maxGoldSlots}";
    }

    public void TrainSoldier()
    {
        Building selected = initializer.inputController.selectedBuilding;
        int activePlayer = initializer.gameManager.currentPlayerId;

        if (selected != null && selected.ownerId == activePlayer)
        {
            if(selected.buildingType == Building.BuildingType.Barracks)
            {
                initializer.productionManager.QueueUnit(selected, Unit.UnitType.Soldier);
            }
        }
        else
        {
            Debug.LogWarning("You cannot command a building you do not own!");
        }
    }
    public void TrainArcher()
    {
        Building selected = initializer.inputController.selectedBuilding;
        int activePlayer = initializer.gameManager.currentPlayerId;

        if (selected != null && selected.ownerId == activePlayer)
        {
            if (selected.buildingType == Building.BuildingType.Barracks)
            {
                initializer.productionManager.QueueUnit(selected, Unit.UnitType.Archer);
            }
        }
        else
        {
            Debug.LogWarning("You cannot command a building you do not own!");
        }
    }
    public void TrainCavalry()
    {
        Building selected = initializer.inputController.selectedBuilding;
        int activePlayer = initializer.gameManager.currentPlayerId;

        if (selected != null && selected.ownerId == activePlayer)
        {
            if (selected.buildingType == Building.BuildingType.Barracks)
            {
                initializer.productionManager.QueueUnit(selected, Unit.UnitType.Cavalry);
            }
        }
        else
        {
            Debug.LogWarning("You cannot command a building you do not own!");
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
