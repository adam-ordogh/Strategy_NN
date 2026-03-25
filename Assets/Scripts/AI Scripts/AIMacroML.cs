using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Policies;

public class AIMacroML : Agent
{
    [Header("References")]
    public GameManager gameManager;
    public int playerId;

    [Header("Settings")]
    [SerializeField] private bool useHeuristicForTesting = true;
    public bool isTraining = true;
    //[SerializeField] public bool headlessMode = true;

    // Micro controller
    private AIMicroController micro;
    private PlayerProfile myProfile;

    [HideInInspector] public AIMacroMLController owner;

    // Jelenlegi döntés és állapot
    public AIGoal currentGoal { get; private set; } = AIGoal.FocusEconomy;
    public MilitaryState currentArmyState = MilitaryState.Gathering;

    // Jutalom nyomkövetéséhez szükséges változók
    private int previousGold;
    private int previousWood;
    private int previousFood;
    private int previousBuildingCount;
    private int previousUnitCount;
    private int previousTerritorySize;

    // Harci statisztikák
    private int totalEnemiesKilled;
    private int totalEnemyBuildingsDestroyed;
    private int totalUnitsLost;
    private int totalBuildingsLost;
    private int previousEnemiesKilled;
    private int previousEnemyBuildingsDestroyed;
    private int previousUnitsLost;
    private int previousBuildingsLost;

    public override void Initialize()
    {
        //Debug.Log($"[ML Agent {playerId}] Initialize() called");

        if (gameManager != null && playerId >= 0)
        {
            //Debug.Log($"[ML Agent {playerId}] Creating micro controller");
            micro = new AIMicroController(playerId, gameManager);
            myProfile = gameManager.GetPlayerProfile(playerId);
            //Debug.Log($"[ML Agent {playerId}] Micro created: {micro != null}, Profile found: {myProfile != null}");

            HookCombatEvents();

            StoreCurrentValues();
        }
        else
        {
            //Debug.LogError($"[ML Agent {playerId}] Cannot initialize - gameManager: {gameManager != null}, playerId: {playerId}");
        }

        currentGoal = AIGoal.FocusEconomy;
    }

    private void HookCombatEvents()
    {
        gameManager.unitManager.OnUnitDestroyed += (unit) =>
        {
            if (unit.ownerId == playerId)
            {
                totalUnitsLost++;
                //Debug.Log($"[ML Agent {playerId}] Unit lost! Total: {totalUnitsLost}");
            }
            else
            {
                totalEnemiesKilled++;
                //Debug.Log($"[ML Agent {playerId}] Enemy killed! Total: {totalEnemiesKilled}");
            }
        };

        gameManager.buildingManager.OnBuildingRemoved += (building) =>
        {
            if (building.ownerId == playerId)
            {
                totalBuildingsLost++;
                //Debug.Log($"[ML Agent {playerId}] Building lost! Total: {totalBuildingsLost}");
            }
            else
            {
                totalEnemyBuildingsDestroyed++;
                //Debug.Log($"[ML Agent {playerId}] Enemy building destroyed! Total: {totalEnemyBuildingsDestroyed}");
            }
        };
    }

    public void ManualUpdate()
    {
        if (gameManager == null || micro == null)
        {
            Debug.LogError($"[ML Agent {playerId}] Missing components");
            return;
        }

        myProfile = gameManager.GetPlayerProfile(playerId);
        micro.RefreshProfile();

        if (!Academy.IsInitialized)
        {
            Debug.Log($"[ML Agent {playerId}] Academy not ready, skipping turn");
            gameManager.NextTurn();
            return;
        }

        //Debug.Log($"[ML Agent {playerId}] Requesting decision");
        RequestDecision();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Debug.Log($"[ML Agent {playerId}] Collecting observations at turn {gameManager.turnNumber}");
        if (myProfile == null) return;

        var income = gameManager.economyManager.GetProjectedIncome(myProfile);

        // === ECONOMIC OBSERVATIONS === 15
        sensor.AddObservation(Normalize(myProfile.gold, 0, 500));
        sensor.AddObservation(Normalize(myProfile.wood, 0, 500));
        sensor.AddObservation(Normalize(myProfile.food, 0, 500));

        sensor.AddObservation(Normalize(income.goldNet, -10, 30));
        sensor.AddObservation(Normalize(income.woodNet, -10, 30));
        sensor.AddObservation(Normalize(income.foodNet, -10, 30));

        sensor.AddObservation(Normalize(myProfile.currentPopulation, 0, 50));
        sensor.AddObservation(Normalize(myProfile.availablePopulation, 0, 20));
        sensor.AddObservation(Normalize(myProfile.housingCapacity, 0, 50));

        sensor.AddObservation(Normalize(myProfile.myBuildings.Count, 0, 30));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Farm));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Woodcutter));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Mine));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Barracks));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Outpost));

        // === MILITARY OBSERVATIONS === 5 
        float myStrength = micro.CalculateMilitaryStrength();
        float enemyStrength = micro.GetObservedEnemyStrength();

        sensor.AddObservation(Normalize(myStrength, 0, 200));
        sensor.AddObservation(Normalize(enemyStrength, 0, 200));
        sensor.AddObservation(micro.IsEnemyNearBase(15f) ? 1f : 0f);
        sensor.AddObservation(Normalize(myStrength / Mathf.Max(enemyStrength, 1f), 0, 3));
        sensor.AddObservation((float)currentArmyState);

        // === EXPANSION OBSERVATIONS === 4 
        sensor.AddObservation(micro.CanPlaceIndustrialBuilding() ? 1f : 0f);
        sensor.AddObservation(GetTerritorySizeNormalized());
        sensor.AddObservation(GetDistanceToEnemyNormalized());
        sensor.AddObservation(GetResourceScarcityNormalized());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        //Debug.Log($"[ML Agent {playerId}] Received action: {actions.DiscreteActions[0]}");
        int action = actions.DiscreteActions[0];

        switch (action)
        {
            case 0: currentGoal = AIGoal.FocusEconomy; break;
            case 1: currentGoal = AIGoal.FocusMilitary; break;
            case 2: currentGoal = AIGoal.FocusExpansion; break;
        }

        ExecuteCurrentGoal();

        if (isTraining)
        {
            float reward = CalculateReward();
            AddReward(reward);
        }

        if (owner != null)
        {
            owner.currentArmyState = this.currentArmyState;
        }

        gameManager.NextTurn();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (!useHeuristicForTesting)
        {
            base.Heuristic(actionsOut);
            return;
        }

        var discreteActions = actionsOut.DiscreteActions;
        var income = gameManager.economyManager.GetProjectedIncome(myProfile);

        if (income.foodNet < 2)
            discreteActions[0] = 0;
        else if (micro.IsEnemyNearBase(15f))
            discreteActions[0] = 1;
        else if (!micro.CanPlaceIndustrialBuilding() && myProfile.gold + myProfile.wood > 300)
            discreteActions[0] = 2;
        else if (myProfile.gold < 100 || myProfile.wood < 100)
            discreteActions[0] = 0;
        else if (micro.GetObservedEnemyStrength() > micro.CalculateMilitaryStrength())
            discreteActions[0] = 1;
        else
            discreteActions[0] = 0;
    }

    private void ExecuteCurrentGoal()
    {
        switch (currentGoal)
        {
            case AIGoal.FocusEconomy: micro.ExecuteEconomyMicro(); break;
            case AIGoal.FocusMilitary: micro.ExecuteMilitaryMicro(); break;
            case AIGoal.FocusExpansion: micro.ExecuteExpansionMicro(); break;
        }

        micro.ExecuteRoadMicro();
        micro.HandleUnitMicro(currentArmyState, ref currentArmyState);
    }
    private float CalculateReward()
    {
        float stepReward = 0f;

        // Time Penalty 
        stepReward -= 0.05f;

        // ONE-TIME mérföldkő jutalmak (Delták)
        int buildingsDelta = myProfile.myBuildings.Count - previousBuildingCount;
        int territoryDelta = GetTerritorySize() - previousTerritorySize;
        int unitsDelta = myProfile.myUnits.Count - previousUnitCount;

        if (buildingsDelta > 0) stepReward += buildingsDelta * 0.1f;
        if (territoryDelta > 0) stepReward += territoryDelta * 0.05f;
        if (unitsDelta > 0) stepReward += unitsDelta * 0.1f;

        // Harci jutalmak
        int enemiesKilledDelta = totalEnemiesKilled - previousEnemiesKilled;
        if (enemiesKilledDelta > 0) stepReward += enemiesKilledDelta * 3.0f;

        int buildingsDestroyedDelta = totalEnemyBuildingsDestroyed - previousEnemyBuildingsDestroyed;
        if (buildingsDestroyedDelta > 0) stepReward += buildingsDestroyedDelta * 8.0f;

        int unitsLostDelta = totalUnitsLost - previousUnitsLost;
        if (unitsLostDelta > 0) stepReward -= unitsLostDelta * 1.0f;

        int buildingsLostDelta = totalBuildingsLost - previousBuildingsLost;
        if (buildingsLostDelta > 0) stepReward -= buildingsLostDelta * 3.0f;

        stepReward = Mathf.Clamp(stepReward, -10f, 10f);

        StoreCurrentValues();

        // Győzelem / vereség jutalmak
        foreach (var player in gameManager.players)
        {
            bool hasTownCenter = player.myBuildings.Any(b =>
                    b.buildingType == Building.BuildingType.TownCenter);

            if (player.playerId != playerId && !hasTownCenter && gameManager.turnNumber > 5)
            {
                AddReward(stepReward + 200.0f);
                EndEpisode();
                return 0f;
            }
            else if (player.playerId == playerId && !hasTownCenter && gameManager.turnNumber > 5)
            {
                AddReward(stepReward - 200.0f); 
                EndEpisode();
                return 0f;
            }
        }

        return stepReward;
    }

    // ==================== TRACKING METHODS ====================

    private void StoreCurrentValues()
    {
        previousGold = myProfile.gold;
        previousWood = myProfile.wood;
        previousFood = myProfile.food;
        previousBuildingCount = myProfile.myBuildings.Count;
        previousUnitCount = myProfile.myUnits.Count;
        previousTerritorySize = GetTerritorySize();
        previousEnemiesKilled = totalEnemiesKilled;
        previousEnemyBuildingsDestroyed = totalEnemyBuildingsDestroyed;
        previousUnitsLost = totalUnitsLost;
        previousBuildingsLost = totalBuildingsLost;
    }

    public void OnEnemyKilled() { totalEnemiesKilled++; }
    public void OnEnemyBuildingDestroyed() { totalEnemyBuildingsDestroyed++; }
    public void OnUnitLost() { totalUnitsLost++; }
    public void OnBuildingLost() { totalBuildingsLost++; }

    // ==================== HELPER METHODS ====================

    private float Normalize(float value, float min, float max)
    {
        return Mathf.Clamp01((value - min) / (max - min));
    }

    private float GetBuildingRatio(Building.BuildingType type)
    {
        int count = myProfile.myBuildings.Count(b => b.buildingType == type);
        return Normalize(count, 0, 10);
    }

    private int GetTerritorySize()
    {
        return gameManager.buildingManager.influenceManager.GetTilesOwnedBy(playerId).Count;
    }

    private float GetTerritorySizeNormalized()
    {
        return Normalize(GetTerritorySize(), 0, 200);
    }

    private float GetDistanceToEnemyNormalized()
    {
        if (myProfile.myBuildings.Count == 0) return 1f;

        Vector2Int enemyBase = micro.GetClosestEnemyBase(myProfile.myBuildings[0].position);
        if (enemyBase.x == -1) return 1f;

        float dist = Vector2Int.Distance(myProfile.myBuildings[0].position, enemyBase);
        return Normalize(dist, 0, 100);
    }

    private float GetResourceScarcityNormalized()
    {
        return micro.CanPlaceIndustrialBuilding() ? 0f : 1f;
    }

    private int CountResourcesInTerritory(MapData.TileType resourceType)
    {
        int count = 0;
        var territory = gameManager.buildingManager.influenceManager.GetTilesOwnedBy(playerId);
        foreach (var tile in territory)
        {
            if (gameManager.mapManager.mapData.mapTiles[tile.x, tile.y].type == resourceType)
            {
                count++;
            }
        }
        return count;
    }
}