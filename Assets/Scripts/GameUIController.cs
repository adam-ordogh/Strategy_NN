using UnityEngine;

public class GameUIController : MonoBehaviour
{
    //public static class UIFormat
    //{
    //    public static string Gold(int val) => $"<sprite name=gold_resource_icon> {val}";
    //    public static string Wood(int val) => $"<sprite name=wood_resource_icon> {val}";
    //    public static string Food(int val) => $"<sprite name=food_resource_icon> {val}";
    //    public static string Pop(int current, int max) => $"<sprite name=population_resource_icon> {current}/{max}";
    //    public static string Housing(int val) => $"<sprite name=housing_resource_icon> {val}";
    //    public static string Worker(int val) => $"<sprite name=population_resource_icon> {val}";
    //}

    public GameInitializer initializer;

    // Turn panel
    public TMPro.TextMeshProUGUI turnLabel;
    public TMPro.TextMeshProUGUI currentPlayerLabel;

    // Resource panel
    public TMPro.TextMeshProUGUI foodLabel;
    public TMPro.TextMeshProUGUI woodLabel;
    public TMPro.TextMeshProUGUI goldLabel;
    public TMPro.TextMeshProUGUI availablePopLabel;

    // Production panel
    public GameObject productionPanel;
    public TMPro.TextMeshProUGUI queueText;

    public GameObject workerPanel; // Assign in Inspector
    public TMPro.TextMeshProUGUI workerCountText;

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
        var report = initializer.economyManager.GetProjectedIncome(activePlayer);

        int totalGoldWorkers = 0;
        int totalWoodWorkers = 0;
        int totalFoodWorkers = 0;

        foreach (var b in activePlayer.myBuildings)
        {
            if (b.buildingType == Building.BuildingType.Mine) totalGoldWorkers += b.assignedWorkers;
            if (b.buildingType == Building.BuildingType.Woodcutter) totalWoodWorkers += b.assignedWorkers;
            if (b.buildingType == Building.BuildingType.Farm) totalFoodWorkers += b.assignedWorkers;
        }

        // Housing/Population line
        string housingColor = activePlayer.currentPopulation >= activePlayer.housingCapacity ? "red" : "white";

        availablePopLabel.text = $"{activePlayer.availablePopulation} / {activePlayer.currentPopulation} <sprite name=\"population_resource_icon\"> <color={housingColor}>[{activePlayer.housingCapacity}]</color> <sprite name=\"housing_resource_icon\">";

        foodLabel.text = $"<sprite name=\"food_resource_icon_02\"> {activePlayer.food}/{activePlayer.maxFood} {FormatIncome(report.foodNet)} ({totalFoodWorkers} <sprite name=\"population_resource_icon\">)";
        woodLabel.text = $"<sprite name=\"wood_resource_icon\"> {activePlayer.wood}/{activePlayer.maxWood} {FormatIncome(report.woodNet)} ({totalWoodWorkers} <sprite name=\"population_resource_icon\">)";
        goldLabel.text = $"<sprite name=\"gold_resource_icon\"> {activePlayer.gold}/{activePlayer.maxGold} {FormatIncome(report.goldNet)} ({totalGoldWorkers} <sprite name=\"population_resource_icon\">)";
    }

    private string FormatIncome(int income)
    {
        string color = income >= 0 ? "green" : "red";
        string sign = income >= 0 ? "+" : "";
        return $"<color={color}>({sign}{income})</color>";
    }

    private void HandleBuildingEvent(Building b) => UpdateUI();
    private void HandleQueueEvent(Building b, UnitData u) => UpdateUI();
    private void HandleDequeueEvent(Building b) => UpdateUI();

    public void Subscribe(BuildingManager bm, ProductionManager pm)
    {
        bm.OnBuildingPlaced += HandleBuildingEvent;
        bm.OnBuildingRemoved += HandleBuildingEvent;

        pm.OnUnitQueued += HandleQueueEvent;
        pm.OnUnitDequeued += HandleDequeueEvent;
        pm.OnUnitSpawned += RefreshSelectedBuildingUI;
    }

    public void Unsubscribe(BuildingManager bm)
    {
        bm.OnBuildingPlaced -= HandleBuildingEvent;
        bm.OnBuildingRemoved -= HandleBuildingEvent;
        // Kezelni kell a ProductionManager eseményeit is, ha szükséges
    }

    public void TrainUnit(UnitData unitData)
    {
        Building selected = initializer.inputController.selectedBuilding;

        if (selected != null && selected.ownerId == initializer.gameManager.currentPlayerId)
        {
            initializer.productionManager.QueueUnit(selected, unitData);
            RefreshSelectedBuildingUI();
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

    public void SelectBuildingToBuild(BuildingData buildingData)
    {
        initializer.inputController.activeBuildingType = buildingData;
    }

    public void RefreshSelectedBuildingUI()
    {
        Building selected = initializer.inputController.selectedBuilding;

        if (selected == null)
        {
            workerPanel.SetActive(false);
            productionPanel.SetActive(false);
            return;
        }

        if (selected.data.jobSlotsProvided > 0 && selected.isConstructed)
        {
            workerPanel.SetActive(true);
            workerCountText.text = $"{selected.assignedWorkers} / {selected.data.jobSlotsProvided}";
        }
        else
        {
            workerPanel.SetActive(false);
        }

        if (selected.buildingType == Building.BuildingType.Barracks && initializer.gameManager.currentPlayerId == selected.ownerId)
        {
            productionPanel.SetActive(true);

            // Queue lekérése a ProductionManagerből
            var queue = initializer.productionManager.GetQueueForBuilding(selected);

            if (queue != null && queue.Count > 0)
            {
                string queueStatus = "Next in: " + queue[0].turnsRemaining + " turns\n";
                for (int i = 0; i < queue.Count; i++)
                {
                    queueStatus += $" {queue[i].template.unitType} ";
                    // Cancel gomb logika ittl esz később
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
        if (queue != null && queue.Count > 1)
        {
            initializer.productionManager.CancelSpecificUnit(selected, queue.Count - 1);
            UpdateUI();
            RefreshSelectedBuildingUI();
        }
    }

    // Munkás hozzárendelés kezelése UI gombokkal
    public void OnAddWorkerClicked()
    {
        Building selected = initializer.inputController.selectedBuilding;
        PlayerProfile player = initializer.gameManager.GetPlayerProfile(selected.ownerId);

        if (selected != null && selected.ownerId == initializer.gameManager.currentPlayerId)
        {
            if (selected.TryAssignWorker(player))
            {
                RefreshSelectedBuildingUI();
                UpdateUI();
            }
        }
    }

    public void OnRemoveWorkerClicked()
    {
        Building selected = initializer.inputController.selectedBuilding;
        PlayerProfile player = initializer.gameManager.GetPlayerProfile(selected.ownerId);

        if (selected != null && selected.ownerId == initializer.gameManager.currentPlayerId)
        {
            if (selected.TryRemoveWorker(player))
            {
                RefreshSelectedBuildingUI();
                UpdateUI();
            }
        }
    }
}
