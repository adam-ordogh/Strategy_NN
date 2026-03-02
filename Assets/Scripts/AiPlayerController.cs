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

    private AIGoal DetermineMacroGoal()
    {
        // TODO: Replace with Neural Network Inference
        // Placeholder Logic:
        //if (myProfile.gold < 50 || myProfile.wood < 50)
        //    return AIGoal.FocusEconomy;
        //else
        //    return AIGoal.FocusMilitary;

        return AIGoal.FocusEconomy;
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
                // Expand logic
                break;
        }
    }

    private void ExecuteEconomyMicro()
    {
        // 1. First, move workers from low-priority jobs to high-priority needs
        RebalanceWorkers();

        // 2. Assign any remaining idle workers (from new births)
        AssignIdleWorkers();

        // 3. Proceed with building logic...
        IncomeReport income = gameManager.economyManager.GetProjectedIncome(myProfile);

        int safetyBreak = 0;
        bool builtSomething = true;

        while (builtSomething && safetyBreak < 10)
        {
            Debug.Log("AI trying to build.");

            builtSomething = false;
            safetyBreak++;

            //IncomeReport income = gameManager.economyManager.GetProjectedIncome(myProfile);

            // Count how many buildings of each type are ALREADY being built
            // This prevents the AI from spamming 10 farms while waiting for the first one to finish
            int farmsInConstruction = myProfile.myBuildings.Count(b => !b.isConstructed && b.buildingType == Building.BuildingType.Farm);
            int woodsInConstruction = myProfile.myBuildings.Count(b => !b.isConstructed && b.buildingType == Building.BuildingType.Woodcutter);
            int minesInConstruction = myProfile.myBuildings.Count(b => !b.isConstructed && b.buildingType == Building.BuildingType.Mine);

            // PRIORITY 1: HOUSING
            // If we have 1 or fewer free people, OR we are at our max capacity, build a house.
            if (myProfile.availablePopulation <= 1 || myProfile.currentPopulation >= myProfile.housingCapacity - 1)
            {
                if (TryBuild(Building.BuildingType.House)) { builtSomething = true; continue; }
            }

            // PRIORITY 2: FOOD
            // We assume a farm will eventually provide ~5 food. 
            // If (current income + projected income from construction) is low, build one.
            if (income.foodNet + (farmsInConstruction * 5) < 5)
            {
                if (TryBuild(Building.BuildingType.Farm)) { builtSomething = true; continue; }
            }

            // PRIORITY 3: WOOD
            if (income.woodNet + (woodsInConstruction * 5) < 5)
            {
                Debug.Log("AI building woodcutter.");
                if (TryBuild(Building.BuildingType.Woodcutter)) { builtSomething = true; continue; }
            }

            // PRIORITY 4: GOLD
            if (income.goldNet + (minesInConstruction * 5) < 5)
            {
                if (TryBuild(Building.BuildingType.Mine)) { builtSomething = true; continue; }
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

    private void RebalanceWorkers()
    {
        // We check the projected income. If food is negative, we have an emergency.
        IncomeReport income = gameManager.economyManager.GetProjectedIncome(myProfile);

        // EMERGENCY: Starvation/Zero Growth Prevention
        // If food is low, take workers from Wood and Gold to fill Farms.
        if (income.foodNet < 2)
        {
            // Get all workers currently in non-food buildings
            var resourceBuildings = myProfile.myBuildings
                .Where(b => b.buildingType == Building.BuildingType.Woodcutter ||
                            b.buildingType == Building.BuildingType.Mine)
                .ToList();

            foreach (var b in resourceBuildings)
            {
                // Pull workers out until food is stable or building is empty
                while (b.assignedWorkers > 0 && income.foodNet < 5)
                {
                    b.TryRemoveWorker(myProfile);
                    // Recalculate income after each removal to see if we've pulled enough
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

    private Vector2Int? FindBestPlacementTile(BuildingData template)
    {
        if (myProfile.myBuildings.Count == 0) return null;

        Vector2Int center = myProfile.myBuildings[0].position;
        Vector2Int bestTile = new Vector2Int(-1, -1);
        float highestScore = float.MinValue;
        bool foundAny = false;

        int searchRadius = 10; // Itt az infulence bezőket kell néznie inkább

        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                Vector2Int checkPos = new Vector2Int(center.x + x, center.y + y);

                if (gameManager.buildingManager.CanPlaceBuilding(template, checkPos, playerId))
                {
                    float currentScore = EvaluateTileScore(checkPos, template);
                    if (currentScore > highestScore)
                    {
                        highestScore = currentScore;
                        bestTile = checkPos;
                        foundAny = true;
                    }
                }
            }
        }

        return foundAny ? bestTile : (Vector2Int?)null;
    }

    private BuildingData GetBuildingTemplate(Building.BuildingType type)
    {
        // Finds the template from the manager's list
        return gameManager.buildingManager.buildingTemplates.Find(t => t.buildingType == type);
    }

  
    // Updated TryBuild to return a bool so the loop knows to continue
    private bool TryBuild(Building.BuildingType type)
    {
        myProfile.PrintResourceStatus();
        BuildingData template = GetBuildingTemplate(type);
        if (template == null) return false;

        if (!myProfile.CanAfford(template.goldCost, template.woodCost, 0)) return false;

        Vector2Int? bestSpot = FindBestPlacementTile(template);
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

    private float EvaluateTileScore(Vector2Int pos, BuildingData template)
    {
        float score = 0f;

        Vector2Int basePos = myProfile.myBuildings[0].position; 
        float dist = Vector2Int.Distance(pos, basePos);
        score -= dist * 2f;

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
}