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

    // Micro controller for execution
    private AIMicroController micro;
    private PlayerProfile myProfile;

    [HideInInspector] public AIMacroMLController owner;

    // Current decision
    public AIGoal currentGoal { get; private set; } = AIGoal.FocusEconomy;
    public MilitaryState currentArmyState = MilitaryState.Gathering;

    // Tracking for reward calculation (only what micro doesn't track)
    private int previousGold;
    private int previousWood;
    private int previousFood;
    private int previousBuildingCount;
    private int previousUnitCount;
    private int previousTerritorySize;

    // Combat stats (micro doesn't track these)
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

            // HOOK UP COMBAT EVENTS HERE
            HookCombatEvents();

            // Initialize previous values
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

        // === MILITARY OBSERVATIONS === 5 (using micro)
        float myStrength = micro.CalculateMilitaryStrength();
        float enemyStrength = micro.GetObservedEnemyStrength();

        sensor.AddObservation(Normalize(myStrength, 0, 200));
        sensor.AddObservation(Normalize(enemyStrength, 0, 200));
        sensor.AddObservation(micro.IsEnemyNearBase(15f) ? 1f : 0f);
        sensor.AddObservation(Normalize(myStrength / Mathf.Max(enemyStrength, 1f), 0, 3));
        sensor.AddObservation((float)currentArmyState);

        // === EXPANSION OBSERVATIONS === 4 (using micro)
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

    //private float CalculateReward()
    //{
    //    float reward = 0f;

    //    // Track changes since last turn
    //    int goldDelta = myProfile.gold - previousGold;
    //    int woodDelta = myProfile.wood - previousWood;
    //    int foodDelta = myProfile.food - previousFood;
    //    int buildingsDelta = myProfile.myBuildings.Count - previousBuildingCount;
    //    int unitsDelta = myProfile.myUnits.Count - previousUnitCount;
    //    int territoryDelta = GetTerritorySize() - previousTerritorySize;

    //    // ========== POSITIVE REWARDS ==========
    //    reward += goldDelta * 0.01f;
    //    reward += woodDelta * 0.01f;
    //    reward += foodDelta * 0.02f;

    //    if (buildingsDelta > 0) reward += buildingsDelta * 2.0f;
    //    if (territoryDelta > 0) reward += territoryDelta * 1.5f;
    //    if (unitsDelta > 0) reward += unitsDelta * 1.0f;

    //    // Combat rewards
    //    int enemiesKilledDelta = totalEnemiesKilled - previousEnemiesKilled;
    //    if (enemiesKilledDelta > 0) reward += enemiesKilledDelta * 5.0f;

    //    int buildingsDestroyedDelta = totalEnemyBuildingsDestroyed - previousEnemyBuildingsDestroyed;
    //    if (buildingsDestroyedDelta > 0) reward += buildingsDestroyedDelta * 15.0f;

    //    // ========== NEGATIVE REWARDS ==========
    //    if (myProfile.food <= 0) reward -= 5.0f;

    //    int unitsLostDelta = totalUnitsLost - previousUnitsLost;
    //    if (unitsLostDelta > 0) reward -= unitsLostDelta * 3.0f;

    //    int buildingsLostDelta = totalBuildingsLost - previousBuildingsLost;
    //    if (buildingsLostDelta > 0) reward -= buildingsLostDelta * 10.0f;

    //    //if (myProfile.availablePopulation > 5)
    //    //    reward -= myProfile.availablePopulation * 0.1f;

    //    //if (myProfile.gold < 50) reward -= 0.5f;
    //    //if (myProfile.wood < 50) reward -= 0.5f;

    //    if (myProfile.currentPopulation > myProfile.housingCapacity * 0.9f)
    //        reward -= 1.0f;

    //    // ========== STRATEGIC BONUSES ==========
    //    var income = gameManager.economyManager.GetProjectedIncome(myProfile);
    //    if (income.goldNet + income.woodNet + income.foodNet > 20)
    //        reward += 0.5f;

    //    // Territory resource bonus (using micro's helper)
    //    int forestsInTerritory = CountResourcesInTerritory(MapData.TileType.Forest);
    //    int mountainsInTerritory = CountResourcesInTerritory(MapData.TileType.Mountain);
    //    reward += forestsInTerritory * 0.1f;
    //    reward += mountainsInTerritory * 0.2f;

    //    // Military dominance (using micro)
    //    float myStrength = micro.CalculateMilitaryStrength();
    //    float enemyStrength = micro.GetObservedEnemyStrength();
    //    if (myStrength > enemyStrength * 2f)
    //        reward += 2.0f;

    //    // ========== END GAME REWARDS ==========
    //    foreach (var player in gameManager.players)
    //    {
    //        if (player.playerId != playerId && player.myBuildings.Count == 0)
    //        {
    //            reward += 100.0f;
    //            EndEpisode();
    //            break;
    //        }
    //        else if (player.playerId == playerId && player.myBuildings.Count == 0)
    //        {
    //            reward -= 100.0f;
    //            EndEpisode();
    //            break;
    //        }
    //    }

    //    // Store current values for next turn
    //    StoreCurrentValues();

    //    // Cap reward
    //    reward = Mathf.Clamp(reward, -50f, 50f);

    //    Debug.Log($"[ML Agent {playerId}] Reward: {reward:F2}");
    //    return reward;
    //}

    //private float CalculateReward()
    //{
    //    float reward = 0f;

    //    reward -= 0.05f;

    //    var income = gameManager.economyManager.GetProjectedIncome(myProfile);

    //    // 1. REWARD ECONOMIC ENGINE (Income is better than raw stash)
    //    // Reward having a positive net income, punish starvation.
    //    reward += (income.goldNet + income.woodNet) * 0.002f;

    //    if (income.foodNet < 0) reward -= 1.0f; // Stronger penalty for starving
    //    else reward += income.foodNet * 0.002f;

    //    // 2. REWARD MILESTONES (Deltas for persistent assets)
    //    int buildingsDelta = myProfile.myBuildings.Count - previousBuildingCount;
    //    int territoryDelta = GetTerritorySize() - previousTerritorySize;
    //    int unitsDelta = myProfile.myUnits.Count - previousUnitCount;

    //    if (buildingsDelta > 0) reward += buildingsDelta * 2.0f;
    //    if (territoryDelta > 0) reward += territoryDelta * 1.0f;
    //    if (unitsDelta > 0) reward += unitsDelta * 0.5f;

    //    // 3. COMBAT REWARDS (Keep these, they are good)
    //    int enemiesKilledDelta = totalEnemiesKilled - previousEnemiesKilled;
    //    if (enemiesKilledDelta > 0) reward += enemiesKilledDelta * 3.0f;

    //    int buildingsDestroyedDelta = totalEnemyBuildingsDestroyed - previousEnemyBuildingsDestroyed;
    //    if (buildingsDestroyedDelta > 0) reward += buildingsDestroyedDelta * 8.0f;

    //    // COMBAT PENALTIES
    //    int unitsLostDelta = totalUnitsLost - previousUnitsLost;
    //    if (unitsLostDelta > 0) reward -= unitsLostDelta * 1.0f;

    //    int buildingsLostDelta = totalBuildingsLost - previousBuildingsLost;
    //    if (buildingsLostDelta > 0) reward -= buildingsLostDelta * 3.0f;

    //    // 4. STRATEGIC GOALS
    //    float myStrength = micro.CalculateMilitaryStrength();
    //    float enemyStrength = micro.GetObservedEnemyStrength();

    //    // Small continuous reward for maintaining military superiority
    //    if (myStrength > enemyStrength + 10f) reward += 0.2f;

    //    // 5. WIN/LOSS CONDITIONS
    //    StoreCurrentValues();

    //    // 1. Clamp the regular turn-by-turn rewards FIRST
    //    reward = Mathf.Clamp(reward, -15f, 15f);

    //    foreach (var player in gameManager.players)
    //    {
    //        // FIX: Ensure roads don't block the win/loss reward
    //        //bool isEliminated = player.myBuildings.Count(b => b.buildingType != Building.BuildingType.Road) == 0;


    //        //if (player.playerId != playerId && isEliminated && gameManager.turnNumber > 5)
    //        //{
    //        //    reward += 100.0f; // Unclamped massive bonus for winning
    //        //    AddReward(reward);
    //        //    EndEpisode();
    //        //    return 0f;
    //        //}
    //        //else if (player.playerId == playerId && isEliminated && gameManager.turnNumber > 5)
    //        //{
    //        //    reward -= 100.0f; // Unclamped massive penalty for dying
    //        //    AddReward(reward);
    //        //    EndEpisode();
    //        //    return 0f;
    //        //}

    //        bool hasTownCenter = player.myBuildings.Any(b =>
    //                b.buildingType == Building.BuildingType.TownCenter);

    //        if (player.playerId != playerId && !hasTownCenter && gameManager.turnNumber > 5)
    //        {
    //            reward += 100.0f; // Unclamped massive bonus for winning
    //            AddReward(reward);
    //            EndEpisode();
    //            return 0f;
    //        }
    //        else if (player.playerId == playerId && !hasTownCenter && gameManager.turnNumber > 5)
    //        {
    //            reward -= 100.0f; // Unclamped massive penalty for dying
    //            AddReward(reward);
    //            EndEpisode();
    //            return 0f;
    //        }
    //    }

    //    // If no win/loss condition, just return the clamped reward
    //    return reward;

    //    //foreach (var player in gameManager.players)
    //    //{
    //    //    if (player.playerId != playerId && player.myBuildings.Count == 0 && gameManager.turnNumber > 5)
    //    //    {
    //    //        Debug.Log($"[ML Agent {playerId}] Episode ended by ELIMINATION of Player {player.playerId} at turn {gameManager.turnNumber}");
    //    //        reward = Mathf.Clamp(reward, -15f, 15f);
    //    //        StoreCurrentValues();
    //    //        AddReward(reward + 100.0f);
    //    //        EndEpisode();
    //    //        return 0f;
    //    //    }
    //    //    else if (player.playerId == playerId && player.myBuildings.Count == 0 && gameManager.turnNumber > 5)
    //    //    {
    //    //        reward = Mathf.Clamp(reward, -15f, 15f);
    //    //        StoreCurrentValues();
    //    //        AddReward(reward - 100.0f);
    //    //        EndEpisode();
    //    //        return 0f;
    //    //    }
    //    //}

    //    //StoreCurrentValues();

    //    //// Cap reward to prevent wild gradient spikes
    //    //reward = Mathf.Clamp(reward, -15f, 15f);

    //    //return reward;
    //}

    private float CalculateReward()
    {
        float stepReward = 0f;

        // 1. Time Penalty (Always negative to force action)
        stepReward -= 0.05f;

        // 2. ONE-TIME Milestone Rewards (Deltas) - Scaled down!
        int buildingsDelta = myProfile.myBuildings.Count - previousBuildingCount;
        int territoryDelta = GetTerritorySize() - previousTerritorySize;
        int unitsDelta = myProfile.myUnits.Count - previousUnitCount;

        if (buildingsDelta > 0) stepReward += buildingsDelta * 0.1f;
        if (territoryDelta > 0) stepReward += territoryDelta * 0.05f;
        if (unitsDelta > 0) stepReward += unitsDelta * 0.1f;

        // 3. COMBAT REWARDS (Kept strong to encourage fighting)
        int enemiesKilledDelta = totalEnemiesKilled - previousEnemiesKilled;
        if (enemiesKilledDelta > 0) stepReward += enemiesKilledDelta * 3.0f;

        int buildingsDestroyedDelta = totalEnemyBuildingsDestroyed - previousEnemyBuildingsDestroyed;
        if (buildingsDestroyedDelta > 0) stepReward += buildingsDestroyedDelta * 8.0f;

        int unitsLostDelta = totalUnitsLost - previousUnitsLost;
        if (unitsLostDelta > 0) stepReward -= unitsLostDelta * 1.0f;

        int buildingsLostDelta = totalBuildingsLost - previousBuildingsLost;
        if (buildingsLostDelta > 0) stepReward -= buildingsLostDelta * 3.0f;

        // Clamp the turn-by-turn reward to prevent wild spikes BEFORE win/loss
        stepReward = Mathf.Clamp(stepReward, -10f, 10f);

        // Store values BEFORE checking win conditions
        StoreCurrentValues();

        // 4. WIN/LOSS CONDITIONS
        foreach (var player in gameManager.players)
        {
            bool hasTownCenter = player.myBuildings.Any(b =>
                    b.buildingType == Building.BuildingType.TownCenter);

            if (player.playerId != playerId && !hasTownCenter && gameManager.turnNumber > 5)
            {
                AddReward(stepReward + 200.0f); // Step + Massive Win Bonus
                EndEpisode();
                return 0f;
            }
            else if (player.playerId == playerId && !hasTownCenter && gameManager.turnNumber > 5)
            {
                AddReward(stepReward - 200.0f); // Step + Massive Loss Penalty
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

    // Public methods to be called by combat system
    public void OnEnemyKilled() { totalEnemiesKilled++; }
    public void OnEnemyBuildingDestroyed() { totalEnemyBuildingsDestroyed++; }
    public void OnUnitLost() { totalUnitsLost++; }
    public void OnBuildingLost() { totalBuildingsLost++; }

    // ==================== HELPER METHODS (using micro where possible) ====================

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

        // Use micro's method to find closest enemy base
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