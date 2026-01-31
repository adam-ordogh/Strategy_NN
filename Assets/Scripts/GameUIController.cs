using UnityEngine;

public class GameUIController : MonoBehaviour
{
    public GameInitializer initializer;

    // Turn panel
    public TMPro.TextMeshProUGUI turnLabel;
    public TMPro.TextMeshProUGUI currentPlayerLabel;

    // Resource panel
    public TMPro.TextMeshProUGUI foodLabel;
    public TMPro.TextMeshProUGUI woodLabel;
    public TMPro.TextMeshProUGUI goldLabel;
    public TMPro.TextMeshProUGUI availablePopLabel;
    public TMPro.TextMeshProUGUI foodWorkersLabel;
    public TMPro.TextMeshProUGUI woodWorkersLabel;
    public TMPro.TextMeshProUGUI goldWorkersLabel;

    // Production panel
    public GameObject productionPanel;
    public TMPro.TextMeshProUGUI queueText;

    // Building panel
    public GameObject buildingPanelOpened;
    public GameObject buildingPanelClosed;

    public void EndTurn()
    {
        initializer.gameManager.NextTurn();

        int turnNumber = initializer.gameManager.turnNumber;
        turnLabel.text = $"Turn {turnNumber}";

        int currentPlayer = initializer.gameManager.currentPlayerId;
        currentPlayerLabel.text = $"Player {currentPlayer}";       

        UpdateUI();
    }

    public void SubscribeToPlayerUpdates(PlayerProfile profile)
    {
        profile.OnResourcesChanged += UpdateUI;
    }

    public void UpdateUI()
    {
        PlayerProfile activePlayer = initializer.gameManager.CurrentPlayer;
        foodLabel.text = $"Food: {activePlayer.food}";
        woodLabel.text = $"Wood: {activePlayer.wood}";
        goldLabel.text = $"Gold: {activePlayer.gold}";
        availablePopLabel.text = $"Population: {activePlayer.availablePopulation}/{activePlayer.maxPopulation}";
        foodWorkersLabel.text = $"Food Workers: {activePlayer.assignedFoodWorkers}/{activePlayer.maxFoodSlots}";
        woodWorkersLabel.text = $"Wood Workers: {activePlayer.assignedWoodWorkers}/{activePlayer.maxWoodSlots}";
        goldWorkersLabel.text = $"Gold Workers: {activePlayer.assignedGoldWorkers}/{activePlayer.maxGoldSlots}";
    }

    private void HandleBuildingEvent(Building b) => UpdateUI();
    private void HandleQueueEvent(Building b, Unit.UnitType u) => UpdateUI();
    private void HandleDequeueEvent(Building b) => UpdateUI();

    public void Subscribe(BuildingManager bm, ProductionManager pm)
    {
        bm.OnBuildingPlaced += HandleBuildingEvent;
        bm.OnBuildingRemoved += HandleBuildingEvent;

        pm.OnUnitQueued += HandleQueueEvent;
        pm.OnUnitDequeued += HandleDequeueEvent;

    }

    public void Unsubscribe(BuildingManager bm)
    {
        bm.OnBuildingPlaced -= HandleBuildingEvent;
        bm.OnBuildingRemoved -= HandleBuildingEvent;
        // Kezelni kell a ProductionManager eseményeit is, ha szükséges
    }

    public void AddFoodWorker()
    {
        var player = initializer.gameManager.CurrentPlayer;
        if (initializer.economyManager.ChangeWorkerAssignment(player, ResourceType.Food, 1))
        {
            UpdateUI(); 
        }
    }

    public void RemoveFoodWorker()
    {
        var player = initializer.gameManager.CurrentPlayer;
        initializer.economyManager.ChangeWorkerAssignment(player, ResourceType.Food, -1);
        UpdateUI();
    }

    public void AddWoodWorker()
    {
        var player = initializer.gameManager.CurrentPlayer;
        if (initializer.economyManager.ChangeWorkerAssignment(player, ResourceType.Wood, 1))
        {
            UpdateUI();
        }
    }

    public void RemoveWoodWorker()
    {
        var player = initializer.gameManager.CurrentPlayer;
        initializer.economyManager.ChangeWorkerAssignment(player, ResourceType.Wood, -1);
        UpdateUI();
    }

    public void AddGoldWorker()
    {
        var player = initializer.gameManager.CurrentPlayer;
        if (initializer.economyManager.ChangeWorkerAssignment(player, ResourceType.Gold, 1))
        {
            UpdateUI(); 
        }
    }

    public void RemoveGoldWorker()
    {
        var player = initializer.gameManager.CurrentPlayer;
        initializer.economyManager.ChangeWorkerAssignment(player, ResourceType.Gold, -1);
        UpdateUI();
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
                RefreshSelectedBuildingUI();
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
                RefreshSelectedBuildingUI();
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
                RefreshSelectedBuildingUI();
            }
        }
        else
        {
            Debug.LogWarning("You cannot command a building you do not own!");
        }
    }


    public void OpenBuildingMenu()
    {
        buildingPanelOpened.SetActive(true);
        buildingPanelClosed.SetActive(false);
    }

    public void CloseBuildingMenu()
    {
        buildingPanelOpened.SetActive(false);
        buildingPanelClosed.SetActive(true);
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

    public void RefreshSelectedBuildingUI()
    {
        Building selected = initializer.inputController.selectedBuilding;

        if (selected == null)
        {
            productionPanel.SetActive(false);
            return;
        }

        if (selected.buildingType == Building.BuildingType.Barracks && initializer.gameManager.currentPlayerId == selected.ownerId)
        {
            productionPanel.SetActive(true);

            // Fetch the queue from ProductionManager
            var queue = initializer.productionManager.GetQueueForBuilding(selected);

            if (queue != null && queue.Count > 0)
            {
                string queueStatus = "Next in: " + queue[0].turnsRemaining + " turns\n";
                for (int i = 0; i < queue.Count; i++)
                {
                    queueStatus += $"[{i}] {queue[i].unitType} ";
                    // Add a "Cancel" button logic here later, 
                    // for now just showing text is enough for testing
                }
                queueText.text = queueStatus;
            }
            else
            {
                queueText.text = "Queue Empty";
            }
        }
        else
        {
            productionPanel.SetActive(false);
        }
    }

    public void CancelLastInQueue()
    {
        Building selected = initializer.inputController.selectedBuilding;
        if (selected == null) return;

        var queue = initializer.productionManager.GetQueueForBuilding(selected);
        if (queue != null && queue.Count > 1) // Count > 1 because we don't refund index 0
        {
            initializer.productionManager.CancelSpecificUnit(selected, queue.Count - 1);
            UpdateUI();
            RefreshSelectedBuildingUI();
        }
    }
}
