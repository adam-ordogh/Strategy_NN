using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static UnitManager;
using UnityEditor.ShaderGraph.Internal;

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
    }

    public void ExecuteTurn()
    {
        myProfile.PrintResourceStatus();
        AIGoal currentGoal = DetermineMacroGoal();

        ExecuteMicroActions(currentGoal);

        gameManager.NextTurn();
    }

    //private AIGoal DetermineMacroGoal()
    //{
    //    var income = gameManager.economyManager.GetProjectedIncome(myProfile);

    //    // PHASE 1: MINIMUM SURVIVAL (Economy First)
    //    // We need a baseline of food and gold before even thinking about anything else.
    //    if (income.foodNet < 2 || myProfile.gold < 30)
    //        return AIGoal.FocusEconomy;

    //    // PHASE 2: BASELINE INFRASTRUCTURE
    //    // Don't build an army until we have at least a few basic buildings.
    //    // (e.g., 2 Farms, 1 Woodcutter, 1 Mine)
    //    if (myProfile.myBuildings.Count < 5)
    //        return AIGoal.FocusEconomy;

    //    //// PHASE 3: THREAT & OPPORTUNITY (Military Logic)
    //    //float myStrength = CalculateMilitaryStrength(myProfile);
    //    //float enemyStrength = CalculateMilitaryStrength(gameManager.GetPlayerProfile(1));

    //    // Only focus on military if we have "War Chest" resources (e.g. 100+ Gold)
    //    // OR if we are being actively threatened.
    //    //bool underThreat = IsEnemyNearBase();
    //    //bool canAffordWar = myProfile.gold > 150 && myProfile.wood > 100;

    //    //if (underThreat || (canAffordWar && myStrength < 50))
    //    //{
    //    //    return AIGoal.FocusMilitary;
    //    //}
    //    bool economyIsBooming = myProfile.gold > 200 && myProfile.wood > 150;
    //    float myStrength = CalculateMilitaryStrength(myProfile);

    //    // Threshold: Build at least 10 units before considering yourself "Ready"
    //    if (economyIsBooming && myStrength < 100) // 10 units * 10 strength
    //    {
    //        return AIGoal.FocusMilitary;
    //    }

    //    // 4. REACTIVE DEFENSE (Same as before)
    //    float enemyStrength = CalculateMilitaryStrength(gameManager.GetPlayerProfile(1));
    //    if (myStrength < enemyStrength * 1.1f || IsEnemyNearBase())
    //    {
    //        return AIGoal.FocusMilitary;
    //    }

    //    // PHASE 4: TERRITORIAL GROWTH
    //    bool canPlaceIndustrial = CanPlaceIndustrialBuilding();

    //    // If our land is "full" of industrial spots, or we have a huge surplus, EXPAND.
    //    if (!canPlaceIndustrial || (myProfile.gold > 250 && myProfile.wood > 250))
    //    {
    //        return AIGoal.FocusExpansion;
    //    }

    //    return AIGoal.FocusEconomy;
    //}

    private AIGoal DetermineMacroGoal()
    {
        var income = gameManager.economyManager.GetProjectedIncome(myProfile);

        // 1. EMERGENCY OVERRIDES (Hard-coded essentials)
        if (income.foodNet < 2) return AIGoal.FocusEconomy; // Don't starve
        if (IsEnemyNearBase(range:15f)) return AIGoal.FocusMilitary; // Immediate defense

        // 2. CALCULATE DESIRE SCORES
        float economyScore = CalculateEconomyDesire(income);
        float militaryScore = CalculateMilitaryDesire();
        float expansionScore = CalculateExpansionDesire();

        Debug.Log($"[AI {playerId}] Desire Scores - Economy: {economyScore}, Military: {militaryScore}, Expansion: {expansionScore}");

        // 3. PICK THE WINNER
        if (expansionScore >= economyScore && expansionScore >= militaryScore)
            return AIGoal.FocusExpansion;

        if (militaryScore >= economyScore)
            return AIGoal.FocusMilitary;

        return AIGoal.FocusEconomy;
    }

    // --- DESIRE CALCULATIONS ---

    private float CalculateEconomyDesire(IncomeReport income)
    {
        float score = 0;
        // We want economy more if we are broke
        if (myProfile.gold < 100) score += 50;
        if (myProfile.wood < 100) score += 50;
        if(myProfile.availablePopulation < 3) score += 50;
        // We want economy if our income is low compared to our building count
        if (income.goldNet < myProfile.myBuildings.Count * 2) score += 40;

        return score;
    }

    private float CalculateMilitaryDesire()
    {
        float score = 20; // Base desire to have an army

        float myStrength = CalculateMilitaryStrength(myProfile);
        // FUTURE PROOF: Only react to what we have actually SEEN
        float observedEnemyStrength = GetObservedEnemyStrength();

        // If we are weaker than what we've seen, increase desire
        if (myStrength < observedEnemyStrength * 1.2f)
            score += 60;

        // "War Chest" logic: If we are floating 500+ gold, we might as well build units
        if (myProfile.gold > 300) score += 30;

        // Cap the desire so it doesn't "choke" expansion forever
        return Mathf.Min(score, 90f);
    }

    private float CalculateExpansionDesire()
    {
        float score = 0;

        // High resources = High desire to expand
        score += (myProfile.gold + myProfile.wood) / 20f;

        // If we have a solid army, we feel safe to expand
        if (CalculateMilitaryStrength(myProfile) > 100) score += 40;

        // If we can't find good places for industrial buildings locally
        if (!CanPlaceIndustrialBuilding()) score += 100;

        return score;
    }



    /// -------------------------------------------------------------------------------------------------------------------------

    private float GetObservedEnemyStrength()
    {
        float totalObserved = 0;
        float observationRadius = 15f; // Tiles

        foreach (var otherPlayer in gameManager.players)
        {
            if (otherPlayer.playerId == this.playerId) continue;

            foreach (var enemyUnit in otherPlayer.myUnits)
            {
                bool isVisible = false;
                // Can any of my units see this enemy?
                foreach (var myUnit in myProfile.myUnits)
                {
                    if (Vector2Int.Distance(myUnit.position, enemyUnit.position) < observationRadius)
                    {
                        isVisible = true;
                        break;
                    }
                }
                // Can any of my buildings see it? (Optional)
                if (!isVisible)
                {
                    foreach (var myBuilding in myProfile.myBuildings)
                    {
                        if (Vector2Int.Distance(myBuilding.position, enemyUnit.position) < observationRadius)
                        {
                            isVisible = true;
                            break;
                        }
                    }
                }

                if (isVisible) totalObserved += 10f; // Simplified strength per unit
            }
        }

        // If we haven't seen anyone, assume a small baseline threat so we aren't defenseless
        return Mathf.Max(totalObserved, 30f);
    }

    private bool CanPlaceIndustrialBuilding()
    {
        // Quick check: Is there any tile in our territory that has a Forest or Mountain nearby?
        // If FindBestPlacementTile for a Mine/Woodcutter returns null, it means we are "saturated"
        var woodcutter = GetBuildingTemplate(Building.BuildingType.Woodcutter);
        var mine = GetBuildingTemplate(Building.BuildingType.Mine);
        return FindBestPlacementTile(woodcutter, AIGoal.FocusEconomy).HasValue && FindBestPlacementTile(mine, AIGoal.FocusEconomy).HasValue;
    }

    private void ExecuteMicroActions(AIGoal goal)
    {
        switch (goal)
        {
            case AIGoal.FocusEconomy:
                ExecuteEconomyMicro();
                Debug.Log("---------FFOCUSING ON ECONOMY THIS TURN!---------");
                break;
            case AIGoal.FocusMilitary:
                ExecuteMilitaryMicro();
                Debug.Log("---------FFOCUSING ON MILITARY THIS TURN!---------");
                break;
            case AIGoal.FocusExpansion:
                ExecuteExpansionMicro();
                Debug.Log("---------FFOCUSING ON EXPANSION THIS TURN!---------");
                break;
        }

        HandleUnitMicro();
    }

    private void ExecuteEconomyMicro()
    {
        RebalanceWorkers();
        AssignIdleWorkers();

        bool madeProgress = true;
        int safetyBreak = 0;

        // Épülő épületek nyomonkövetése
        int farmsPlanned = 0;
        int woodPlanned = 0;
        int minesPlanned = 0;
        int warehousesPlanned = 0;

        while (madeProgress && safetyBreak < 10)
        {
            madeProgress = false;
            safetyBreak++;

            IncomeReport income = gameManager.economyManager.GetProjectedIncome(myProfile);

            int farmsInConst = myProfile.myBuildings.Count(b => !b.isConstructed && b.buildingType == Building.BuildingType.Farm);
            int woodsInConst = myProfile.myBuildings.Count(b => !b.isConstructed && b.buildingType == Building.BuildingType.Woodcutter);
            int minesInConst = myProfile.myBuildings.Count(b => !b.isConstructed && b.buildingType == Building.BuildingType.Mine);
            int warehousesInConst = myProfile.myBuildings.Count(b => !b.isConstructed && b.buildingType == Building.BuildingType.Warehouse);

            int effectiveFood = income.foodNet + ((farmsPlanned + farmsInConst) * 10);
            int effectiveWood = income.woodNet + ((woodPlanned + woodsInConst) * 6);
            int effectiveGold = income.goldNet + ((minesPlanned + minesInConst) * 5);

            // Dinamikus bejövetel cél: minden épület növeli a szükséges bejövetelt, hogy ösztönözze a folyamatos növekedést
            int targetIncome = 15 + (myProfile.myBuildings.Count * 2);

            // Alap szükségletek
            if (effectiveFood < 10 && TryBuild(Building.BuildingType.Farm)) { farmsPlanned++; madeProgress = true; }
            else if (effectiveWood < 10 && TryBuild(Building.BuildingType.Woodcutter)) { woodPlanned++; madeProgress = true; }
            else if (effectiveGold < 10 && TryBuild(Building.BuildingType.Mine)) { minesPlanned++; madeProgress = true; }
            // Populáció növelése
            else if (effectiveFood > 5 && myProfile.availablePopulation < 2)
            {
                if (ShouldBuildHouse() && TryBuild(Building.BuildingType.House))
                {
                    madeProgress = true;
                }
            }
            // Ekonómia dinamikus kiegyensúlyozása a célbevételhez képest
            else if (myProfile.availablePopulation > 0 || effectiveFood < targetIncome)
            {                
                if (effectiveFood < targetIncome)
                {
                    if (TryBuild(Building.BuildingType.Farm)) { farmsPlanned++; madeProgress = true; }
                }
                else if (effectiveGold < effectiveWood)
                {
                    if (TryBuild(Building.BuildingType.Mine)) { minesPlanned++; madeProgress = true; }
                }
                else
                {
                    if (TryBuild(Building.BuildingType.Woodcutter)) { woodPlanned++; madeProgress = true; }
                }
            }

            // Raktár építése, ha szükséges (pl. ha sok nyersanyag gyűlik össze és nincs elég tárhely)
            if (ShouldBuildWarehouse(warehousesPlanned + warehousesInConst))
            {
                if (TryBuild(Building.BuildingType.Warehouse)) { warehousesPlanned++; madeProgress = true; }
            }

            if (madeProgress) AssignIdleWorkers();
        }
    }


    private void ExecuteExpansionMicro()
    {
        BuildingData expansionTemplate = GetBuildingTemplate(Building.BuildingType.Outpost);

        if (expansionTemplate != null && myProfile.CanAfford(expansionTemplate.goldCost, expansionTemplate.woodCost, 0))
        {
            Vector2Int? bestSpot = FindBestPlacementTile(expansionTemplate, AIGoal.FocusExpansion);

            if (bestSpot.HasValue)
            {
                Building b = gameManager.buildingManager.PlaceBuilding(expansionTemplate, bestSpot.Value, playerId);
                if (b != null)
                {
                    //Debug.Log($"[AI {playerId}] Expanding territory with {expansionTemplate.buildingType} at {bestSpot.Value}");
                }
            }
        }
    }

    //private void ExecuteMilitaryMicro()
    //{
    //    // A. BARRACKS PRODUCTION
    //    if (CountBuildings(Building.BuildingType.Barracks) < 2)
    //    {
    //        BuildingData barracksTemplate = GetBuildingTemplate(Building.BuildingType.Barracks);
    //        if (myProfile.CanAfford(barracksTemplate.woodCost, barracksTemplate.goldCost, 0))
    //        {
    //            // Place Barracks near the "Frontier" (Outposts) to shorten reinforcement lines
    //            Vector2Int? spot = FindBestPlacementTile(barracksTemplate, AIGoal.FocusMilitary);
    //            if (spot.HasValue) gameManager.buildingManager.PlaceBuilding(barracksTemplate, spot.Value, playerId);
    //        }
    //    }

    //    // B. RECRUITMENT
    //    foreach (var b in myProfile.myBuildings)
    //    {
    //        if (b.buildingType == Building.BuildingType.Barracks && b.isConstructed)
    //        {
    //            UnitData soldier = GetUnitTemplate(Unit.UnitType.Soldier);
    //            // Use the ProductionManager you already have!
    //            if (myProfile.CanAfford(soldier.woodCost, soldier.goldCost, soldier.foodCost))
    //                gameManager.productionManager.QueueUnit(b, soldier);
    //        }
    //    }
    //}
    private void ExecuteMilitaryMicro()
    {
        // A. BARRACKS PRODUCTION (Keep your existing logic for building the barracks)
        if (CountBuildings(Building.BuildingType.Barracks) < 2)
        {
            BuildingData barracksTemplate = GetBuildingTemplate(Building.BuildingType.Barracks);
            if (myProfile.CanAfford(barracksTemplate.woodCost, barracksTemplate.goldCost, 0))
            {
                Vector2Int? spot = FindBestPlacementTile(barracksTemplate, AIGoal.FocusMilitary);
                if (spot.HasValue) gameManager.buildingManager.PlaceBuilding(barracksTemplate, spot.Value, playerId);
            }
        }

        var enemyComp = GetObservedEnemyComposition();

        Unit.UnitType bestType = DetermineBestUnitType(enemyComp);
        UnitData template = GetUnitTemplate(bestType);

        foreach (var b in myProfile.myBuildings)
        {
            if (b.buildingType == Building.BuildingType.Barracks && b.isConstructed)
            {
                if (myProfile.CanAfford(template.woodCost, template.goldCost, template.foodCost))
                {
                    gameManager.productionManager.QueueUnit(b, template);

                    bestType = DetermineBestUnitType(enemyComp);
                    template = GetUnitTemplate(bestType);
                }
            }
        }
    }

    // --- MILITARY FUNCTIONS ---
    private float CalculateMilitaryStrength(PlayerProfile profile)
    {
        // Simple sum of unit combat power (adjust based on your UnitData)
        return profile.myUnits.Count * 10f;
    }

    private UnitData GetUnitTemplate(Unit.UnitType type)
    {
        return gameManager.unitManager.unitTemplates.Find(t => t.unitType == type);
    }

    private int CountBuildings(Building.BuildingType type)
    {
        return myProfile.myBuildings.Count(b => b.buildingType == type);
    }

    private bool IsEnemyNearBase(float range)
    {
        Vector2Int myBase = myProfile.myBuildings[0].position;

        // Check all enemy units
        foreach (var p in gameManager.players)
        {
            if (p.playerId == this.playerId) continue;

            foreach (var unit in p.myUnits)
            {
                // If an enemy unit is within 15 tiles of our base, we need an army!
                if (Vector2Int.Distance(myBase, unit.position) < range)
                    return true;
            }
        }
        return false;
    }

    private int CountUnits(Unit.UnitType type)
    {
        return myProfile.myUnits.Count(u => u.data.unitType == type);
    }

    private Unit.UnitType DetermineBestUnitType(Dictionary<Unit.UnitType, int> enemyComp)
    {
        int effSoldiers = CountUnits(Unit.UnitType.Soldier) + GetQueuedCount(Unit.UnitType.Soldier);
        int effArchers = CountUnits(Unit.UnitType.Archer) + GetQueuedCount(Unit.UnitType.Archer);
        int effCavalry = CountUnits(Unit.UnitType.Cavalry) + GetQueuedCount(Unit.UnitType.Cavalry);
        int effSiege = CountUnits(Unit.UnitType.Siege) + GetQueuedCount(Unit.UnitType.Siege);

        float soldierScore = 12f - (effSoldiers * 1.0f);
        float archerScore = 10f - (effArchers * 1.0f);
        float cavalryScore = 8f - (effCavalry * 1.0f);
        float siegeScore = 2f - (effSiege * 2.0f);

        if (enemyComp[Unit.UnitType.Archer] > 0)
        {
            soldierScore += enemyComp[Unit.UnitType.Archer] * 2;
            cavalryScore += enemyComp[Unit.UnitType.Archer] * 6;
        }

        if (enemyComp[Unit.UnitType.Cavalry] > 0)
        {
            soldierScore += enemyComp[Unit.UnitType.Cavalry] * 8;
        }

        int totalArmySize = effSoldiers + effArchers + effCavalry;
        if (totalArmySize > 5 && effSiege < 2)
        {
            siegeScore += 15f;
        }

        if (myProfile.gold > 400)
        {
            cavalryScore += 5f;
            siegeScore += 2f;
        }

        Debug.Log($"[AI {playerId}] Unit Desire Scores - Soldier: {soldierScore}, Archer: {archerScore}, Cavalry: {cavalryScore}, Siege: {siegeScore}");

        // Pick the highest score
        var scores = new Dictionary<Unit.UnitType, float> {
            { Unit.UnitType.Soldier, soldierScore },
            { Unit.UnitType.Archer, archerScore },
            { Unit.UnitType.Cavalry, cavalryScore },
            { Unit.UnitType.Siege, siegeScore }
        };

        return scores.OrderByDescending(x => x.Value).First().Key;
    }

    public int GetQueuedCount(Unit.UnitType type)
    {
        int count = 0;
        foreach(Building b in myProfile.myBuildings)
        {
            if (b.buildingType == Building.BuildingType.Barracks)
            {
                var barracksQueue = gameManager.productionManager.GetQueueForBuilding(b);
                if (barracksQueue != null) { 
                    foreach (var queue in barracksQueue)
                    {
                        if (queue.template.unitType == type)
                            count++;
                    }
                }
            }
        }
        return count;
    }

    private Dictionary<Unit.UnitType, int> GetObservedEnemyComposition()
    {
        var comp = new Dictionary<Unit.UnitType, int> {
        { Unit.UnitType.Soldier, 0 }, { Unit.UnitType.Archer, 0 },
        { Unit.UnitType.Cavalry, 0 }, { Unit.UnitType.Siege, 0 }
    };

        // Use your "Seen Units" logic (from Fog of War)
        // For now, let's just look at the opponent's profile
        var enemy = gameManager.GetPlayerProfile(1); // Assuming player 1 is the enemy
        foreach (var u in enemy.myUnits)
        {
            comp[u.data.unitType]++;
        }
        return comp;
    }

    private void HandleUnitMicro()
    {
        Vector2Int enemyBase = GetClosestEnemyBase(myProfile.myBuildings[0].position);
        Vector2Int rallyPoint = GetRallyPoint();

        foreach (var unit in myProfile.myUnits.ToList())
        {
            // 1. ATTACK IF POSSIBLE
            //var targets = gameManager.unitManager.GetReachableEnemies(unit);
            //if (targets.Count > 0)
            //{
            //    gameManager.unitManager.TryMoveUnit(unit.position, targets[0].StandPos);
            //    gameManager.unitManager.TryAttack(targets[0].StandPos, targets[0].TargetPos);
            //    continue;
            //}
            var targets = gameManager.unitManager.GetReachableTargets(unit);

            if (targets.Count > 0)
            {
                // Simple Prioritization:
                // Catapults pick the first building they see.
                // Everyone else picks the first unit they see.
                AttackCommand bestTarget = targets[0];

                if (unit.data.unitType == Unit.UnitType.Siege) // Placeholder for your future type
                {
                    var buildingTarget = targets.FirstOrDefault(t => gameManager.mapManager.mapData.buildings.ContainsKey(t.TargetPos));
                    if (buildingTarget.TargetPos != Vector2Int.zero) bestTarget = buildingTarget;
                }

                gameManager.unitManager.TryMoveUnit(unit.position, bestTarget.StandPos);
                gameManager.unitManager.TryAttack(bestTarget.StandPos, bestTarget.TargetPos);
                continue;
            }

            // 2. FIND DESTINATION (Respecting Blocking)
            Vector2Int idealGoal = (myProfile.myUnits.Count > 10) ? enemyBase : rallyPoint;

            // Find the closest point to that goal that isn't blocked by a "Soldier Frontline"
            Vector2Int? reachableGoal = FindBestReachableTile(unit, idealGoal);

            if (reachableGoal.HasValue)
            {
                var path = gameManager.unitManager.GetPathToTarget(unit, reachableGoal.Value);
                if (path != null && path.Count > 1)
                {
                    // Move as far as movement points allow
                    gameManager.unitManager.TryMoveUnit(unit.position, path.Last());
                }
            }
            else if (IsNearBarracks(unit.position))
            {
                // EMERGENCY UNCLOG: If stuck ON a barracks, just move to ANY random empty neighbor
                MoveToAnyEmptyNeighbor(unit);
            }
        }
    }

    // Finds the closest empty tile to the 'target' that the unit can currently reach
    private Vector2Int? FindBestReachableTile(Unit unit, Vector2Int target)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(unit.position);
        visited.Add(unit.position);

        Vector2Int bestTile = unit.position;
        float minDist = Vector2Int.Distance(unit.position, target);

        // We only check a limited area to keep performance high
        int iterations = 0;
        while (queue.Count > 0 && iterations < 200)
        {
            iterations++;
            Vector2Int current = queue.Dequeue();

            float d = Vector2Int.Distance(current, target);
            if (d < minDist)
            {
                minDist = d;
                bestTile = current;
            }

            // Check the 4 adjacent tiles
            foreach (Vector2Int neighbor in new Vector2Int[] { current + Vector2Int.up, current + Vector2Int.down, current + Vector2Int.left, current + Vector2Int.right })
            {
                if (visited.Contains(neighbor)) continue;

                // This is the key: we use a custom check that respects your "Tactical Blocking"
                if (IsTileActuallyEmptyAndPassable(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return bestTile == unit.position ? (Vector2Int?)null : bestTile;
    }

    private bool IsTileActuallyEmptyAndPassable(Vector2Int pos)
    {
        // 1. Map Bounds
        if (pos.x < 0 || pos.x >= gameManager.mapManager.mapData.mapWidth || pos.y < 0 || pos.y >= gameManager.mapManager.mapData.mapHeight) return false;
        // 2. Terrain Passability
        if (!gameManager.mapManager.mapData.mapTiles[pos.x, pos.y].isPassable) return false;
        // 3. UNIT BLOCKING (Your tactical requirement)
        if (gameManager.mapManager.mapData.units.ContainsKey(pos)) return false;
        // 4. Building Blocking
        if (gameManager.mapManager.mapData.buildings.ContainsKey(pos)) return false;

        return true;
    }

    private Vector2Int GetRallyPoint()
    {
        // Default to the Town Center
        Vector2Int rally = myProfile.myBuildings[0].position;

        // Find our furthest building (to keep the army at the frontier)
        float maxDist = 0;
        foreach (var b in myProfile.myBuildings)
        {
            float d = Vector2Int.Distance(b.position, myProfile.myBuildings[0].position);
            if (d > maxDist)
            {
                maxDist = d;
                rally = b.position;
            }
        }

        // Offset the rally point slightly so they don't stand ON the building
        return rally + new Vector2Int(2, 2);
    }

    private void MoveToAnyEmptyNeighbor(Unit unit)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int neighbor = unit.position + dir;
            if (IsTileActuallyEmptyAndPassable(neighbor))
            {
                gameManager.unitManager.TryMoveUnit(unit.position, neighbor);
                return;
            }
        }
    }

    private bool IsNearBarracks(Vector2Int pos)
    {
        foreach (var b in myProfile.myBuildings)
        {
            if (b.buildingType == Building.BuildingType.Barracks)
            {
                if (Vector2Int.Distance(pos, b.position) < 3f) return true;
            }
        }
        return false;
    }

    // --- ECONOMY FUNCTIONS ---
    private bool ShouldBuildHouse()
    {
        //// Assuming you have a way to check current population vs max capacity
        //int currentPop = myProfile.currentPopulation;
        //int popCap = myProfile.housingCapacity;

        //// Only build a house if we are within 2 points of the cap
        //// This prevents the "Population Explosion"
        //return (popCap - currentPop) <= 2;

        return myProfile.availablePopulation < myProfile.housingCapacity * 0.3f && (myProfile.housingCapacity - myProfile.currentPopulation) <= 2;
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
        //Debug.Log($"[AI {playerId}] Evaluating {myTerritory.Count} owned tiles for placing {template.buildingType}");

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
        //myProfile.PrintResourceStatus();
        BuildingData template = GetBuildingTemplate(type);
        if (template == null) return false;

        if (!myProfile.CanAfford(template.goldCost, template.woodCost, 0)) return false;

        Vector2Int? bestSpot = FindBestPlacementTile(template, currentGoal);
        if (bestSpot.HasValue)
        {
            Building newBuilding = gameManager.buildingManager.PlaceBuilding(template, bestSpot.Value, playerId);

            if (newBuilding != null)
            {
                //Debug.Log($"[AI {playerId}] Decision: Built {type} at {bestSpot.Value}");
                //myProfile.PrintResourceStatus();
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
            score += CountNearbyTiles(pos, MapData.TileType.Mountain, captureRadius) * 5f; 
            score += CountNearbyTiles(pos, MapData.TileType.Forest, captureRadius) * 2f;   

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

    //private bool ShouldBuildWarehouse()
    //{
    //    float maxStorage = myProfile.maxFood; // Calculate this based on (TownCenters * 75) + (Warehouses * 100)
    //    float bufferThreshold = 0.8f; // Build when 80% full

    //    bool goldFull = myProfile.gold > maxStorage * bufferThreshold;
    //    bool woodFull = myProfile.wood > maxStorage * bufferThreshold;
    //    bool foodFull = myProfile.food > maxStorage * bufferThreshold;

    //    return goldFull || woodFull || foodFull;
    //}

    private bool ShouldBuildWarehouse(int builtThisTurn)
    {
        if (myProfile.myBuildings.Count < 8)
            return false;

        int townCenters = CountBuildings(Building.BuildingType.TownCenter);
        int warehouses = CountBuildings(Building.BuildingType.Warehouse) + builtThisTurn;
        float maxStorage = (townCenters * 75) + (warehouses * 100);

        if (maxStorage == 0) return false;

        float goldPercent = myProfile.gold / maxStorage;
        float woodPercent = myProfile.wood / maxStorage;
        float foodPercent = myProfile.food / maxStorage;

        var income = gameManager.economyManager.GetProjectedIncome(myProfile);
        float totalProduction = income.goldNet + income.woodNet + income.foodNet;

        if (totalProduction < 50) return false;

        return goldPercent > 0.7f || woodPercent > 0.7f || foodPercent > 0.7f;
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