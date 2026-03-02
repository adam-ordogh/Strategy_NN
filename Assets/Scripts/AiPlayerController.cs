using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum AIGoal
{
    FocusEconomy,
    FocusMilitary,
    FocusExpansion
}

public class AiPlayerController
{
    public int playerId;
    private GameManager gameManager; 
    private PlayerProfile myProfile;

    private AIGoal currentGoal;

    public AiPlayerController(int id, GameManager init)
    {
        playerId = id;
        gameManager = init;
        myProfile = gameManager.GetPlayerProfile(playerId);


        //myProfile.PrintResourceStatus();
    }

    // Called by your GameManager when it's this player's turn
    public void ExecuteTurn()
    {
        // 1. MACRO: Decide what the goal is for this turn (Later: Neural Network)
        AIGoal currentGoal = DetermineMacroGoal();

        // 2. MICRO: Execute deterministic logic based on that goal
        ExecuteMicroActions(currentGoal);

        // 3. Finish Turn
        gameManager.NextTurn();
    }

    //private AIGoal DetermineMacroGoal()
    //{
    //    // TODO: Replace with Neural Network Inference
    //    // Placeholder Logic:
    //    //if (myProfile.gold < 50 || myProfile.wood < 50)
    //    //    return AIGoal.FocusEconomy;
    //    //else
    //    //    return AIGoal.FocusMilitary;

    //    return AIGoal.FocusEconomy;
    //}

    private AIGoal DetermineMacroGoal()
    {
        IncomeReport income = gameManager.economyManager.GetProjectedIncome(myProfile);

        // 1. EMERGENCY: If we are losing food or have massive unemployment, fix economy first.
        if (income.foodNet < 2 || IsSufferingUnemployment())
        {
            return AIGoal.FocusEconomy;
        }

        // 2. EXPANSION: If we have enough resources to "buy" more land and a stable base.
        // Adjust these thresholds based on your game's cost for Town Halls/Outposts.
        if (myProfile.gold > 50 && myProfile.wood > 50)
        {
            return AIGoal.FocusExpansion;
        }

        // 3. DEFAULT: Grow the base.
        return currentGoal = AIGoal.FocusEconomy;
    }

    private void ExecuteMicroActions(AIGoal goal)
    {
        switch (goal)
        {
            case AIGoal.FocusEconomy:
                ExecuteEconomyMicro();
                break;
            case AIGoal.FocusMilitary:
                ExecuteMilitaryMicro();
                break;
            case AIGoal.FocusExpansion:
                ExecuteExpansionMicro();
                break;
        }
    }

    private void ExecuteEconomyMicro()
    {
        RebalanceWorkers();
        AssignIdleWorkers();

        // Track what we've decided to build THIS TURN 
        // This solves the "Income Blindness" problem
        int farmsPlannedThisTurn = 0;
        int woodPlannedThisTurn = 0;
        int minesPlannedThisTurn = 0;

        bool madeProgress = true;
        int safetyBreak = 0;

        while (madeProgress && safetyBreak < 10)
        {
            madeProgress = false;
            safetyBreak++;

            IncomeReport income = gameManager.economyManager.GetProjectedIncome(myProfile);

            // Calculate "Effective Income" (Projected + what we just placed)
            // Adjust the '5' based on your average building output
            int effectiveFood = income.foodNet + (farmsPlannedThisTurn * 5);
            int effectiveWood = income.woodNet + (woodPlannedThisTurn * 5);
            int effectiveGold = income.goldNet + (minesPlannedThisTurn * 5);

            int foodTarget = 5 + (myProfile.currentPopulation / 5);

            // --- PRIORITY 1: UNEMPLOYMENT ---
            if (IsSufferingUnemployment())
            {
                // If food is low, try to build a farm. 
                // If it succeeds, we 'madeProgress' and the loop continues.
                if (effectiveFood < foodTarget)
                {
                    if (TryBuild(Building.BuildingType.Farm)) { farmsPlannedThisTurn++; madeProgress = true; }
                }

                // If we didn't build a farm (or food was already fine), check Wood/Gold
                if (!madeProgress)
                {
                    // Better ratio: Try to keep Wood and Gold roughly equal (1:1)
                    if (effectiveWood < effectiveGold)
                    {
                        if (TryBuild(Building.BuildingType.Woodcutter)) { woodPlannedThisTurn++; madeProgress = true; }
                        // Fallback to mine if woodcutter failed
                        else if (TryBuild(Building.BuildingType.Mine)) { minesPlannedThisTurn++; madeProgress = true; }
                    }
                    else
                    {
                        if (TryBuild(Building.BuildingType.Mine)) { minesPlannedThisTurn++; madeProgress = true; }
                        // Fallback to woodcutter if mine failed
                        else if (TryBuild(Building.BuildingType.Woodcutter)) { woodPlannedThisTurn++; madeProgress = true; }
                    }
                }
            }
            // --- PRIORITY 2: HOUSING (Only if no unemployment) ---
            else if (effectiveFood > 2) // Simplified check
            {
                int totalJobSlots = myProfile.myBuildings.Sum(b => b.data.jobSlotsProvided);
                int totalWorkers = myProfile.myBuildings.Sum(b => b.assignedWorkers);
                if (totalJobSlots - totalWorkers <= 1)
                {
                    if (TryBuild(Building.BuildingType.House)) madeProgress = true;
                }
            }

            // If we successfully placed a building, we might have new jobs to fill immediately
            if (madeProgress) AssignIdleWorkers();
        }
    }

    private void ExecuteExpansionMicro()
    {
        // Find the building template that expands territory best
        BuildingData expansionTemplate = GetBuildingTemplate(Building.BuildingType.Outpost);

        if (expansionTemplate != null && myProfile.CanAfford(expansionTemplate.goldCost, expansionTemplate.woodCost, 0))
        {
            // When searching for a spot, we will pass the Expansion goal to the scorer
            Vector2Int? bestSpot = FindBestPlacementTile(expansionTemplate, AIGoal.FocusExpansion);

            if (bestSpot.HasValue)
            {
                Building b = gameManager.buildingManager.PlaceBuilding(expansionTemplate, bestSpot.Value, playerId);
                if (b != null)
                {
                    Debug.Log($"[AI {playerId}] Expanding territory with {expansionTemplate.buildingType} at {bestSpot.Value}");
                }
            }
        }
    }

    private void ExecuteMilitaryMicro()
    {
        // 1. Can we build a Barracks?
        // 2. Can we train a unit?
        // 3. Move units toward enemies.
        Debug.Log($"AI Player {playerId} is executing Military Micro.");
    }

    private bool IsSufferingUnemployment()
    {
        return myProfile.availablePopulation > (myProfile.currentPopulation * 0.5f);
    }

    private void RebalanceWorkers()
    {
        IncomeReport income = gameManager.economyManager.GetProjectedIncome(myProfile);

        if (income.foodNet < 2)
        {
            var resourceBuildings = myProfile.myBuildings
                .Where(b => b.buildingType == Building.BuildingType.Woodcutter ||
                            b.buildingType == Building.BuildingType.Mine)
                .ToList();

            foreach (var b in resourceBuildings)
            {
                while (b.assignedWorkers > 0 && income.foodNet < 5)
                {
                    b.TryRemoveWorker(myProfile);
                    income = gameManager.economyManager.GetProjectedIncome(myProfile);
                }
            }
        }
    }

    private void AssignIdleWorkers()
    {
        FillBuildingsOfType(Building.BuildingType.Farm);

        FillBuildingsOfType(Building.BuildingType.Woodcutter);
        FillBuildingsOfType(Building.BuildingType.Mine);
    }

    private void FillBuildingsOfType(Building.BuildingType type)
    {
        foreach (var b in myProfile.myBuildings)
        {
            if (b.buildingType == type && b.isConstructed)
            {
                while (b.CanAcceptWorker() && myProfile.availablePopulation > 0)
                {
                    b.TryAssignWorker(myProfile);
                }
            }
        }
    }

 
    private Vector2Int? FindBestPlacementTile(BuildingData template, AIGoal goal)
    {
        var influenceManager = gameManager.buildingManager.influenceManager;

        List<Vector2Int> myTerritory = influenceManager.GetTilesOwnedBy(playerId);
        Debug.Log($"[AI {playerId}] Evaluating {myTerritory.Count} owned tiles for placing {template.buildingType}");

        float highestScore = float.MinValue;
        Vector2Int? bestTile = null;

        if (template.buildingType == Building.BuildingType.Woodcutter)
        {
            myTerritory = myTerritory.Where(t => CountNearbyTiles(t, MapData.TileType.Forest, 1) > 0).ToList();
        }
        else if (template.buildingType == Building.BuildingType.Mine)
        {
            myTerritory = myTerritory.Where(t => CountNearbyTiles(t, MapData.TileType.Mountain, 1) > 0).ToList();
        }

        foreach (Vector2Int checkPos in myTerritory)
        {
            if (gameManager.buildingManager.CanPlaceBuilding(template, checkPos, playerId))
            {
                float score = EvaluateTileScore(checkPos, template, goal);

                if (score > highestScore)
                {
                    highestScore = score;
                    bestTile = checkPos;
                }
            }
        }

        return bestTile;
    }

    private BuildingData GetBuildingTemplate(Building.BuildingType type)
    {
        return gameManager.buildingManager.buildingTemplates.Find(t => t.buildingType == type);
    }

    private bool TryBuild(Building.BuildingType type)
    {
        myProfile.PrintResourceStatus();
        BuildingData template = GetBuildingTemplate(type);
        if (template == null) return false;

        if (!myProfile.CanAfford(template.goldCost, template.woodCost, 0)) return false;

        Vector2Int? bestSpot = FindBestPlacementTile(template, currentGoal);
        if (bestSpot.HasValue)
        {
            Building newBuilding = gameManager.buildingManager.PlaceBuilding(template, bestSpot.Value, playerId);

            if (newBuilding != null)
            {
                Debug.Log($"[AI {playerId}] Decision: Built {type} at {bestSpot.Value}");
                myProfile.PrintResourceStatus();
                return true;
            }
        }

        return false;
    }

    private int CountNearbyTiles(Vector2Int center, MapData.TileType type, int radius)
    {
        int count = 0;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int checkPos = new Vector2Int(center.x + x, center.y + y);

                // Boundary check
                if (checkPos.x < 0 || checkPos.x >= gameManager.mapManager.mapData.mapWidth ||
                    checkPos.y < 0 || checkPos.y >= gameManager.mapManager.mapData.mapHeight) continue;

                if (gameManager.mapManager.mapData.mapTiles[checkPos.x, checkPos.y].type == type)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private float EvaluateTileScore(Vector2Int pos, BuildingData template, AIGoal goal)
    {
        float score = 0f;

        Vector2Int basePos = myProfile.myBuildings[0].position; 
        float dist = Vector2Int.Distance(pos, basePos);
        //score -= dist * 2f;
       if(goal == AIGoal.FocusEconomy)
        {
            // ECONOMY: Keep buildings close to the base for a compact, defensible town.
            score -= dist * 2f;
        }
        else if (goal == AIGoal.FocusExpansion)
        {
            // 1. BORDER PUSH: Gentle reward for moving outward
            score += dist * 1f;

            // 2. ANTI-CLUSTERING: Prevent Outpost spam
            foreach (var b in myProfile.myBuildings)
            {
                // Check against other buildings that provide influence
                if (b.buildingType == Building.BuildingType.Outpost || b.buildingType == Building.BuildingType.TownCenter)
                {
                    float d = Vector2Int.Distance(pos, b.position);
                    // If the new outpost is closer than 1.5x the influence radius, nuke the score
                    if (d < template.influenceRadius * 1.5f)
                    {
                        score -= 500f;
                    }
                }
            }

            // 3. RESOURCE GRABBING: Will this new influence radius capture resources?
            int captureRadius = template.influenceRadius;
            score += CountNearbyTiles(pos, MapData.TileType.Mountain, captureRadius) * 5f; // Mountains are heavily prized
            score += CountNearbyTiles(pos, MapData.TileType.Forest, captureRadius) * 2f;   // Forests are nice to have

            // 4. AGGRESSION: Push towards the enemy
            Vector2Int enemyPos = GetClosestEnemyBase(pos);
            if (enemyPos.x != -1) // If we found an enemy
            {
                float distToEnemy = Vector2Int.Distance(pos, enemyPos);
                score -= distToEnemy * 2f; // Closer to enemy = lower distance = higher score
            }
        }

        switch (template.buildingType)
        {
            case Building.BuildingType.Woodcutter:
                score += CountNearbyTiles(pos, MapData.TileType.Forest, 1) * 10f;
                break;

            case Building.BuildingType.Mine:
                score += CountNearbyTiles(pos, MapData.TileType.Mountain, 1) * 50f;
                break;

            case Building.BuildingType.Farm:
                foreach (var b in myProfile.myBuildings)
                {
                    if (b.buildingType == Building.BuildingType.Farm)
                    {
                        float d = Vector2Int.Distance(pos, b.position);
                        if (d < 4) score += 5f; // Small bonus for clustering
                    }
                }
                break;
        }

        return score;
    }

    // --- EXPANSION FUNCTIONS ---
    private Vector2Int GetClosestEnemyBase(Vector2Int fromPos)
    {
        Vector2Int closest = new Vector2Int(-1, -1);
        float minDist = float.MaxValue;

        // Search through all players to find enemies
        foreach (var p in gameManager.players)
        {
            if (p.playerId != this.playerId && p.myBuildings.Count > 0)
            {
                // Assume their first building is their Town Center
                Vector2Int enemyBase = p.myBuildings[0].position;
                float d = Vector2Int.Distance(fromPos, enemyBase);
                if (d < minDist)
                {
                    minDist = d;
                    closest = enemyBase;
                }
            }
        }
        return closest;
    }
}