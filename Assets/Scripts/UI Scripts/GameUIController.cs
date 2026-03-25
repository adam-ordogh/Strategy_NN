using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    public GameInitializer initializer;

    // Turn panel
    public TMPro.TextMeshProUGUI turnLabel;

    // Resource panel
    public TMPro.TextMeshProUGUI foodLabel;
    public TMPro.TextMeshProUGUI woodLabel;
    public TMPro.TextMeshProUGUI goldLabel;
    public TMPro.TextMeshProUGUI availablePopLabel;

    // Building panel
    public GameObject buildingPanelOpened;
    public GameObject buildingPanelClosed;

    // -------------
    [Header("Selection Info Panel")]
    public GameObject infoPanel;
    public UnityEngine.UI.Image portraitImage;
    public TMPro.TextMeshProUGUI nameLabel;
    public TMPro.TextMeshProUGUI healthLabel;
    public Slider healthBar;

    [Header("Unit Stats Panel")]
    public GameObject statsPanel;
    public TMPro.TextMeshProUGUI damageText;
    public TMPro.TextMeshProUGUI rangeText;
    public TMPro.TextMeshProUGUI movementText;
    public TMPro.TextMeshProUGUI damageTypeText;
    public TMPro.TextMeshProUGUI armorTypeText;

    [Header("Worker Panel")]
    public GameObject workerPanel;
    public TMPro.TextMeshProUGUI workerCountText;
    public TMPro.TextMeshProUGUI productionCountText;

    [Header("Training System")]
    [Tooltip("Drag your UnitData assets here. Index 0 = Warrior, 1 = Archer, etc.")]
    public List<UnitData> trainingTemplates;

    [Header("Production Queue UI")]
    public GameObject productionPanel;
    public Transform queueContainer;    
    public GameObject queueButtonPrefab;

    [Header("Action Panel")]
    public GameObject actionPanel;

    [Header("Menu Panels")]
    public GameObject pauseMenuPanel;
    public GameObject saveLoadMenuPanel;
    public SaveLoadUI saveLoadUI;

    private bool buildingPanelIsOpen = false;
    public static bool IsAnyMenuOpen { get; private set; }

    public void SubscribeToPlayerUpdates(PlayerProfile profile)
    {
        profile.OnResourcesChanged += UpdateUI;
    }
    public void Subscribe(BuildingManager bm, ProductionManager pm)
    {
        bm.OnBuildingPlaced += HandleBuildingEvent;
        bm.OnBuildingRemoved += HandleBuildingEvent;

        pm.OnUnitQueued += HandleQueueEvent;
        pm.OnUnitDequeued += HandleDequeueEvent;
        pm.OnUnitSpawned += RefreshSelectionUI;
    }

    public void Unsubscribe(BuildingManager bm)
    {
        bm.OnBuildingPlaced -= HandleBuildingEvent;
        bm.OnBuildingRemoved -= HandleBuildingEvent;
        // Kezelni kell a ProductionManager eseményeit is, ha szükséges
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

    public void EndTurn()
    {
        initializer.inputController.DeselectUnit();
        if(buildingPanelIsOpen) CloseBuildingMenu();

        initializer.gameManager.NextTurn();

        int turnNumber = initializer.gameManager.turnNumber;
        turnLabel.text = $"<sprite name=\"turn_icon\"> {turnNumber}";

        RefreshSelectionUI();
        UpdateUI();
    }

    public void UpdateUI()
    {
        PlayerProfile activePlayer = initializer.gameManager.HumanPlayer;
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

        string housingColor = activePlayer.currentPopulation >= activePlayer.housingCapacity ? "red" : "white";

        availablePopLabel.text = $"{activePlayer.availablePopulation} / {activePlayer.currentPopulation} <sprite name=\"population_resource_icon\"> <color={housingColor}>[{activePlayer.housingCapacity}]</color> <sprite name=\"housing_resource_icon\">";

        foodLabel.text = $"<sprite name=\"food_resource_icon_02\"> {activePlayer.food}/{activePlayer.maxFood} {FormatIncome(report.foodNet)} ({totalFoodWorkers} <sprite name=\"population_resource_icon\">)";
        woodLabel.text = $"<sprite name=\"wood_resource_icon\"> {activePlayer.wood}/{activePlayer.maxWood} {FormatIncome(report.woodNet)} ({totalWoodWorkers} <sprite name=\"population_resource_icon\">)";
        goldLabel.text = $"<sprite name=\"gold_resource_icon\"> {activePlayer.gold}/{activePlayer.maxGold} {FormatIncome(report.goldNet)} ({totalGoldWorkers} <sprite name=\"population_resource_icon\">)";
    }

  
    public void TrainUnitByIndex(int index)
    {
        if (trainingTemplates == null || index < 0 || index >= trainingTemplates.Count)
        {
            Debug.LogError($"UI Error: Index {index} is out of bounds for trainingTemplates list.");
            return;
        }

        Building selected = initializer.inputController.selectedBuilding;

        if (selected != null && selected.ownerId == initializer.gameManager.currentPlayerId)
        {
            UnitData templateToQueue = trainingTemplates[index];

            initializer.productionManager.QueueUnit(selected, templateToQueue);

            RefreshSelectionUI();
            UpdateUI();
        }
        else
        {
            Debug.LogWarning("Cannot train unit: No valid player building selected.");
        }
    }

    public void OpenBuildingMenu()
    {
        buildingPanelIsOpen = true;

        buildingPanelOpened.SetActive(true);
        buildingPanelClosed.SetActive(false);
    }

    public void CloseBuildingMenu()
    {
        buildingPanelIsOpen = false;

        buildingPanelOpened.SetActive(false);
        buildingPanelClosed.SetActive(true);
    }

    public void SelectBuildingToBuild(BuildingData buildingData)
    {
        initializer.inputController.activeBuildingType = buildingData;
    }

    public void RefreshSelectionUI()
    {
        var selectedBuilding = initializer.inputController.selectedBuilding;
        var selectedUnit = initializer.inputController.selectedUnit;

        if (selectedBuilding != null || selectedUnit != null) { 
        
        }
        
        infoPanel.SetActive(false);
        statsPanel.SetActive(false);
        workerPanel.SetActive(false);
        productionPanel.SetActive(false);
        actionPanel.SetActive(false);

        if (selectedBuilding != null)
        {
            ShowBuildingInfo(selectedBuilding);
        }
        else if (selectedUnit != null)
        {
            ShowUnitInfo(selectedUnit);
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
                RefreshSelectionUI();

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
                RefreshSelectionUI();

                UpdateUI();
            }
        }
    }

    public void OnActionButtonClicked()
    {
        var selectedBuilding = initializer.inputController.selectedBuilding;
        var selectedUnit = initializer.inputController.selectedUnit;

        if (selectedBuilding != null && selectedBuilding.ownerId == initializer.gameManager.currentPlayerId)
        {
            initializer.buildingManager.RemoveBuilding(selectedBuilding);
            initializer.inputController.DeselectBuilding(); 
        }
        else if (selectedUnit != null && selectedUnit.ownerId == initializer.gameManager.currentPlayerId)
        {
            initializer.unitManager.DestroyUnit(selectedUnit); 
            initializer.inputController.DeselectUnit(); 
        }
    }

    private void ShowBuildingInfo(Building b)
    {
        infoPanel.SetActive(true);
        if (b.data.buildingIcon != null)
        {
            portraitImage.sprite = b.data.buildingIcon;
        }

        nameLabel.text = b.data.buildingName;
        healthLabel.text = $"HP: {b.currentHp}/{b.data.maxHealth}";
        UpdateHealthBar(b.currentHp, b.data.maxHealth);

        if (b.data.jobSlotsProvided > 0)
        {
            workerPanel.SetActive(true);

            workerCountText.text = $"{b.assignedWorkers}/{b.data.jobSlotsProvided}";

            var economy = initializer.gameManager.economyManager;
            int totalProduction = economy.GetProductionFromWorkers(b);

            string resourceIcon = GetResourceIconForBuilding(b.buildingType);

            int baseOutput = b.data.GetWorkerOutput(b.assignedWorkers);
            int bonus = totalProduction - baseOutput;

            if (bonus > 0)
            {
                productionCountText.text = $"{totalProduction} {resourceIcon} <color=green>(+{bonus})</color>";
            }
            else
            {
                productionCountText.text = $"{totalProduction} {resourceIcon}";
            }
        }

        if (b.buildingType == Building.BuildingType.Barracks)
        {
            productionPanel.SetActive(true);
            UpdateProductionQueueUI(b);
        }

        bool isMine = b.ownerId == initializer.gameManager.HumanPlayer.playerId;
        actionPanel.SetActive(isMine);

    }

    private string GetResourceIconForBuilding(Building.BuildingType type)
    {
        switch (type)
        {
            case Building.BuildingType.Woodcutter: return "<sprite name=\"wood_resource_icon\">";
            case Building.BuildingType.Farm: return "<sprite name=\"food_resource_icon_02\">";
            case Building.BuildingType.Mine: return "<sprite name=\"gold_resource_icon\">";
            default: return "";
        }
    }

    private void UpdateHealthBar(int current, int max)
    {
        if (healthBar == null) return;

        healthBar.maxValue = max;
        healthBar.value = current;

        float healthPercent = (float)current / max;

        if (healthBar.fillRect != null)
        {
            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(Color.red, Color.green, healthPercent);
            }
        }
    }

    private void ShowUnitInfo(Unit unit)
    {
        infoPanel.SetActive(true);
        if (unit.data.unitIcon != null)
        {
            portraitImage.sprite = unit.data.unitIcon;
        }

        nameLabel.text = unit.data.unitName;
        healthLabel.text = $"HP: {unit.currentHealth}/{unit.data.maxHealth}";
        UpdateHealthBar(unit.currentHealth, unit.data.maxHealth);

        statsPanel.SetActive(true);

        damageText.text = $"Sebzés: {unit.data.attackPower}";
        rangeText.text = $"Távolság: {unit.data.attackRange}";
        movementText.text = $"Mozgás: {unit.data.movementRange} pont";
        damageTypeText.text = $"Sebzés típus: {unit.data.attackType}";
        armorTypeText.text = $"Páncél típus: {unit.data.armorType}";

        //bool isMine = unit.ownerId == initializer.gameManager.currentPlayerId;
        bool isMine = unit.ownerId == initializer.gameManager.HumanPlayer.playerId;
        actionPanel.SetActive(isMine);
    }

    private void UpdateProductionQueueUI(Building b)
    {
        foreach (Transform child in queueContainer)
        {
            Destroy(child.gameObject);
        }

        var queue = initializer.productionManager.GetQueueForBuilding(b);
        if (queue == null || queue.Count == 0) return;

        int totalTurns = 0;

        for (int i = 0; i < queue.Count; i++)
        {
            int localIndex = i;
            var order = queue[i];
            totalTurns += order.turnsRemaining;

            GameObject buttonObj = Instantiate(queueButtonPrefab, queueContainer);

            var iconImage = buttonObj.GetComponentInChildren<UnityEngine.UI.Image>();
            if (iconImage != null && order.template.unitIcon != null)
            {
                iconImage.sprite = order.template.unitIcon;
                iconImage.preserveAspect = true; 
            }

            var textComp = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = totalTurns.ToString();
                textComp.color = Color.yellow;
                textComp.fontStyle = TMPro.FontStyles.Bold;

                
                textComp.outlineWidth = 0.25f;
                textComp.outlineColor = new Color32(0, 0, 0, 255);
            }

            var buttonComp = buttonObj.GetComponent<UnityEngine.UI.Button>();
            buttonComp.onClick.AddListener(() =>
            {
                initializer.productionManager.CancelSpecificUnit(b, localIndex);
                RefreshSelectionUI();
                UpdateUI();
            });
        }
    }

    // MENU PANEL FUNCTIONS

    public void TogglePauseMenu()
    {
        bool isOpening = pauseMenuPanel.activeSelf;

        pauseMenuPanel.SetActive(!isOpening);

        UpdateMenuState();
    }

    public void OnResumeClicked()
    {
        TogglePauseMenu();

    }

    public void OpenSaveMenu() => ToggleSaveLoadMenu(SaveLoadUI.UIMode.Save);
    public void OpenLoadMenu() => ToggleSaveLoadMenu(SaveLoadUI.UIMode.Load);

    public void ToggleSaveLoadMenu(SaveLoadUI.UIMode mode)
    {
        bool isOpening = !saveLoadMenuPanel.activeSelf;

        TogglePauseMenu();
        saveLoadMenuPanel.SetActive(isOpening);

        if (isOpening)
        {
            if(mode == SaveLoadUI.UIMode.Save)
                saveLoadUI.OpenPanel(SaveLoadUI.UIMode.Save);
            else
                saveLoadUI.OpenPanel(SaveLoadUI.UIMode.Load);
            
        }

        UpdateMenuState();
    }

    public void OnExitToMainMenuClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene"); 

        UpdateMenuState();
    }

    public void OnExitGameClicked()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

    public void CloseAllMenus()
    {
        pauseMenuPanel.SetActive(false);
        saveLoadMenuPanel.SetActive(false);
        //loadMenuPanel.SetActive(false);
        UpdateMenuState();
    }

    private void UpdateMenuState()
    {
        IsAnyMenuOpen = pauseMenuPanel.activeSelf || saveLoadMenuPanel.activeSelf;

        //Cursor.visible = IsAnyMenuOpen;
    }
}
