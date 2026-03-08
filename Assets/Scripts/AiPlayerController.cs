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

public enum MilitaryState { Defending, Gathering, Attacking };

public class AiPlayerController
{
    public int playerId;
    private GameManager gameManager;
    private PlayerProfile myProfile;

    private AIGoal currentGoal;
    public MilitaryState currentArmyState = MilitaryState.Gathering;


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
        if (IsEnemyNearBase(range: 15f)) return AIGoal.FocusMilitary; // Immediate defense

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

        if (myProfile.gold < 100) score += 50;
        if (myProfile.wood < 100) score += 50;
        if (myProfile.availablePopulation < 3) score += 50;

        int realBuildingCount = myProfile.myBuildings.Count(b =>
            b.buildingType != Building.BuildingType.Road &&
            b.buildingType != Building.BuildingType.Warehouse);

        if (income.goldNet < realBuildingCount * 2)
        {
            score += 40;
        }

        return score;
    }

    private float CalculateMilitaryDesire()
    {
        float score = 20; 

        float myStrength = CalculateMilitaryStrength(myProfile);
        float observedEnemyStrength = GetObservedEnemyStrength();

        if (myStrength < observedEnemyStrength * 1.2f)
            score += 60;

        if (myProfile.gold > 100) score += 30;

        return score;
    }

    private float CalculateExpansionDesire()
    {
        float score = 0;

        score += (myProfile.gold + myProfile.wood) / 20f;

        if (CalculateMilitaryStrength(myProfile) > 100) score += 20;

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

        AssignIdleWorkers();

        if (myProfile.gold > 35 && myProfile.wood > 35)
        {
            ExecuteRoadMicro();
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

            // Kezdetleges raktárépítés, hogy ne veszítsünk erőforrást túl korán, de ne is áldozzunk rá túl sokat
            if (ShouldBuildWarehouse(warehousesPlanned + warehousesInConst) && CountWarehouses() < 1 )
            {
                if (TryBuild(Building.BuildingType.Warehouse)) { warehousesPlanned++; madeProgress = true; }
            }

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
                if (effectiveGold < effectiveWood)
                {
                    if (TryBuild(Building.BuildingType.Mine)) { minesPlanned++; madeProgress = true; }
                }
                else
                {
                    if (TryBuild(Building.BuildingType.Woodcutter)) { woodPlanned++; madeProgress = true; }
                }
            }

            // Raktár építése, ha szükséges (pl. ha sok nyersanyag gyűlik össze és nincs elég tárhely)
            if (ShouldBuildWarehouse(warehousesPlanned + warehousesInConst) && CountWarehouses() >= 1)
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

    private void ExecuteMilitaryMicro()
    {
        int desiredBarracks = Mathf.Max(2, myProfile.currentPopulation / 20);

        if (CountBuildings(Building.BuildingType.Barracks) < desiredBarracks)
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

    // ------------------------------------ MILITARY FUNCTIONS ------------------------------------
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

        foreach (var p in gameManager.players)
        {
            if (p.playerId == this.playerId) continue;

            foreach (var unit in p.myUnits)
            {
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
        foreach (Building b in myProfile.myBuildings)
        {
            if (b.buildingType == Building.BuildingType.Barracks)
            {
                var barracksQueue = gameManager.productionManager.GetQueueForBuilding(b);
                if (barracksQueue != null)
                {
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
        Vector2Int enemyBase = GetClosestEnemyBase(gameManager.players[0].myBuildings[0].position);
        Vector2Int rallyPoint = GetRallyPoint();

        foreach (var unit in myProfile.myUnits.ToList())
        {
            // 1. ATTACK IF POSSIBLE
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
        if (pos.x < 0 || pos.x >= gameManager.mapManager.mapData.mapWidth || pos.y < 0 || pos.y >= gameManager.mapManager.mapData.mapHeight) return false;
        if (!gameManager.mapManager.mapData.mapTiles[pos.x, pos.y].isPassable) return false;
        if (gameManager.mapManager.mapData.units.ContainsKey(pos)) return false;
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

    // ------------------------------------ ECONOMY FUNCTIONS ------------------------------------
    
    private int CountWarehouses()
    {
        return myProfile.myBuildings.Count(b => b.buildingType == Building.BuildingType.Warehouse);
    }

    private bool CanPlaceIndustrialBuilding()
    {
        var woodcutter = GetBuildingTemplate(Building.BuildingType.Woodcutter);
        var mine = GetBuildingTemplate(Building.BuildingType.Mine);
        return FindBestPlacementTile(woodcutter, AIGoal.FocusEconomy).HasValue && FindBestPlacementTile(mine, AIGoal.FocusEconomy).HasValue;
    }

    private bool ShouldBuildHouse()
    {
        return myProfile.availablePopulation < myProfile.housingCapacity * 0.3f && (myProfile.housingCapacity - myProfile.currentPopulation) <= 2;
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

    // ------------------------------------ EXPANSION FUNCTIONS ------------------------------------
    private Vector2Int GetClosestEnemyBase(Vector2Int fromPos)
    {
        Vector2Int closest = new Vector2Int(-1, -1);
        float minDist = float.MaxValue;

        foreach (var p in gameManager.players)
        {
            if (p.playerId != this.playerId && p.myBuildings.Count > 0)
            {
                // Jelenleg feltételezzük, hogy az első épületük a bázisuk (ez lehet, hogy nem mindig igaz, de jó kiindulási pont)
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

    //  ------------------------------------ UTILITY FUNCTIONS ------------------------------------

    private BuildingData GetBuildingTemplate(Building.BuildingType type)
    {
        return gameManager.buildingManager.buildingTemplates.Find(t => t.buildingType == type);
    }

    private float EvaluateTileScore(Vector2Int pos, BuildingData template, AIGoal goal)
    {
        float score = 0f;

        Vector2Int basePos = myProfile.myBuildings[0].position;
        float dist = Vector2Int.Distance(pos, basePos);

        // Penalize "Suffocation": Check if the building will be totally blocked
        int freeNeighbors = 0;
        foreach (var dir in Pathfinder.Directions)
        {
            Vector2Int n = pos + dir;
            if (IsInsideMap(n) && gameManager.buildingManager.GetBuildingAtTile(n) == null)
                freeNeighbors++;
        }

        if (freeNeighbors < 1 && template.buildingType != Building.BuildingType.House)
            score -= 200f; // Extremely heavy penalty for blocking all road access points

        if (goal == AIGoal.FocusEconomy)
        {
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
                            if (d < 4) score += 5f;
                        }
                    }
                    break;
            }
        }
        else if (goal == AIGoal.FocusExpansion)
        {
            score += dist * 1f;

            foreach (var b in myProfile.myBuildings)
            {
                if (b.buildingType == Building.BuildingType.Outpost || b.buildingType == Building.BuildingType.TownCenter)
                {
                    float d = Vector2Int.Distance(pos, b.position);

                    if (d < template.influenceRadius * 1.5f)
                    {
                        score -= 500f;
                    }
                }
            }

            int captureRadius = template.influenceRadius;
            score += CountNearbyTiles(pos, MapData.TileType.Mountain, captureRadius) * 5f;
            score += CountNearbyTiles(pos, MapData.TileType.Forest, captureRadius) * 2f;

            Vector2Int enemyPos = GetClosestEnemyBase(pos);
            if (enemyPos.x != -1) 
            {
                float distToEnemy = Vector2Int.Distance(pos, enemyPos);
                score -= distToEnemy * 2f; 
            }
        }
        else if (goal == AIGoal.FocusMilitary)
        {
            Vector2Int enemyBase = GetClosestEnemyBase(pos);
            if (enemyBase.x != -1)
            {
                float distToEnemy = Vector2Int.Distance(pos, enemyBase);

                score -= distToEnemy * 10f;
            }
        }

        return score;
    }

    private bool IsInsideMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gameManager.mapManager.mapData.mapWidth && pos.y >= 0 && pos.y < gameManager.mapManager.mapData.mapHeight;
    }

    //private List<Vector2Int> FindRoadPath(Building target)
    //{
    //    Queue<Vector2Int> queue = new Queue<Vector2Int>();
    //    Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
    //    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

    //    // Start the search from all tiles the building occupies
    //    foreach (var tile in target.GetOccupiedTiles())
    //    {
    //        queue.Enqueue(tile);
    //        visited.Add(tile);
    //    }

    //    while (queue.Count > 0)
    //    {
    //        Vector2Int current = queue.Dequeue();

    //        // GOAL: We hit something already connected to the Capital
    //        Building bAtTile = gameManager.buildingManager.GetBuildingAtTile(current);
    //        if (bAtTile != null && bAtTile.isConnectedToCapital && bAtTile != target)
    //        {
    //            return ReconstructPath(parents, current, target);
    //        }

    //        // Search neighbors
    //        foreach (var dir in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
    //        {
    //            Vector2Int next = current + dir;

    //            if (!IsInsideMap(next) || visited.Contains(next)) continue;

    //            Building b = gameManager.buildingManager.GetBuildingAtTile(next);

    //            // We can build roads on empty tiles or step through our own buildings
    //            if (b == null || b.ownerId == playerId)
    //            {
    //                visited.Add(next);
    //                parents[next] = current;
    //                queue.Enqueue(next);
    //            }
    //        }
    //    }
    //    return null; // No connection possible
    //}
    //private List<Vector2Int> FindRoadPath(Building target)
    //{
    //    Queue<Vector2Int> queue = new Queue<Vector2Int>();
    //    Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
    //    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    //    Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();

    //    // Start searching from all tiles around the building (the "exits")
    //    foreach (var startTile in target.GetOccupiedTiles())
    //    {
    //        foreach (var dir in Pathfinder.Directions)
    //        {
    //            Vector2Int neighbor = startTile + dir;
    //            if (!IsInsideMap(neighbor) || visited.Contains(neighbor)) continue;

    //            distances[neighbor] = 1;

    //            // Check what's at the neighbor tile
    //            Building b = gameManager.buildingManager.GetBuildingAtTile(neighbor);

    //            // If the neighbor is ALREADY a connected road/building, we are done!
    //            // (Path length is 0, we are already connected)
    //            if (b != null && b.isConnectedToCapital && b.ownerId == playerId)
    //                return new List<Vector2Int>();

    //            // Otherwise, if it's an empty tile or an existing (but disconnected) road, 
    //            // we can use it to start our path.
    //            if (b == null || (b.buildingType == Building.BuildingType.Road && b.ownerId == playerId))
    //            {
    //                visited.Add(neighbor);
    //                parents[neighbor] = startTile; // Link back to building
    //                queue.Enqueue(neighbor);
    //            }
    //        }
    //    }

    //    while (queue.Count > 0)
    //    {
    //        Vector2Int current = queue.Dequeue();

    //        foreach (var dir in Pathfinder.Directions)
    //        {
    //            Vector2Int next = current + dir;

    //            distances[next] = distances[current] + 1;
    //            if (distances[next] > 10) continue;

    //            if (!IsInsideMap(next) || visited.Contains(next)) continue;

    //            Building b = gameManager.buildingManager.GetBuildingAtTile(next);

    //            // GOAL: We hit an existing part of the network
    //            if (b != null && b.isConnectedToCapital && b.ownerId == playerId)
    //            {
    //                return ReconstructPath(parents, current, target);
    //            }

    //            // TRAVERSAL: Only step on empty tiles or existing roads
    //            if (b == null || (b.buildingType == Building.BuildingType.Road && b.ownerId == playerId))
    //            {
    //                visited.Add(next);
    //                parents[next] = current;
    //                queue.Enqueue(next);
    //            }
    //        }
    //    }
    //    return null;
    //}

    private List<Vector2Int> FindRoadPath(Building target)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>(); // Track distance

        // Start searching from all tiles around the building (the "exits")
        foreach (var startTile in target.GetOccupiedTiles())
        {
            foreach (var dir in Pathfinder.Directions)
            {
                Vector2Int neighbor = startTile + dir;
                if (!IsInsideMap(neighbor) || visited.Contains(neighbor)) continue;

                Building b = gameManager.buildingManager.GetBuildingAtTile(neighbor);

                // STRICT CHECK: The neighbor must be connected AND it must be a Road or Town Center.
                if (b != null && b.isConnectedToCapital && b.ownerId == playerId &&
                   (b.buildingType == Building.BuildingType.Road || b.buildingType == Building.BuildingType.TownCenter))
                {
                    return new List<Vector2Int>(); // We are already touching the network!
                }

                // Otherwise, we can step on empty tiles or disconnected roads
                if (b == null || (b.buildingType == Building.BuildingType.Road && b.ownerId == playerId))
                {
                    visited.Add(neighbor);
                    parents[neighbor] = startTile;
                    distances[neighbor] = 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (var dir in Pathfinder.Directions)
            {
                Vector2Int next = current + dir;
                if (!IsInsideMap(next) || visited.Contains(next)) continue;

                Building b = gameManager.buildingManager.GetBuildingAtTile(next);

                // STRICT GOAL: We hit an existing, connected Road or Town Center
                if (b != null && b.isConnectedToCapital && b.ownerId == playerId &&
                   (b.buildingType == Building.BuildingType.Road || b.buildingType == Building.BuildingType.TownCenter))
                {
                    // We pass 'current' because 'next' already has a road/TC on it. 
                    // We only need to build up to 'current'.
                    return ReconstructPath(parents, current, target);
                }

                // TRAVERSAL
                if (b == null || (b.buildingType == Building.BuildingType.Road && b.ownerId == playerId))
                {
                    int newDist = distances[current] + 1;

                    // PERFORMANCE FIX: Don't search forever. If we can't find a road within 10 tiles, give up.
                    // You only build roads if length <= 6 anyway!
                    if (newDist > 10) continue;

                    visited.Add(next);
                    parents[next] = current;
                    distances[next] = newDist;
                    queue.Enqueue(next);
                }
            }
        }
        return null; // No path found
    }

    //private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parents, Vector2Int end)
    //{
    //    List<Vector2Int> path = new List<Vector2Int>();
    //    Vector2Int curr = end;

    //    while (parents.ContainsKey(curr))
    //    {
    //        // Only add to the 'to-build' list if there isn't already a building/road here
    //        if (gameManager.buildingManager.GetBuildingAtTile(curr) == null)
    //        {
    //            path.Add(curr);
    //        }
    //        curr = parents[curr];
    //    }
    //    return path;
    //}

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parents, Vector2Int end, Building startBuilding)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int curr = end;

        // Safety break to prevent hard crashes if a loop ever happens again
        int safetyLimit = 1000;

        // Loop until we hit a tile that belongs to the building we started from
        while (parents.ContainsKey(curr) && safetyLimit > 0)
        {
            safetyLimit--;

            // Only add to the 'to-build' list if there isn't already a building/road here
            if (gameManager.buildingManager.GetBuildingAtTile(curr) == null)
            {
                path.Add(curr);
            }

            // Move to the next tile in the chain
            curr = parents[curr];

            // EXIT CONDITION: Are we standing on the building we started from?
            if (startBuilding.GetOccupiedTiles().Contains(curr))
            {
                break;
            }
        }

        if (safetyLimit <= 0)
        {
            Debug.LogError("ReconstructPath caught in infinite loop!");
            return null;
        }

        return path;
    }

    private void ExecuteRoadMicro()
    {
        // 1. Get all disconnected buildings
        var disconnected = myProfile.myBuildings
            .Where(b => !b.isConnectedToCapital && b.isConstructed)
            .OrderByDescending(b => GetBuildingPriority(b))
            .ToList();

        BuildingData roadTemplate = GetBuildingTemplate(Building.BuildingType.Road);
        int roadsBuilt = 0;

        foreach (var b in disconnected)
        {
            List<Vector2Int> path = FindRoadPath(b);

            // Only build if the path is short and we can afford it
            if (path != null && path.Count > 0 && path.Count <= 6)
            {
                if (myProfile.CanAfford(roadTemplate.woodCost * path.Count, roadTemplate.goldCost * path.Count, 0))
                {
                    foreach (var tile in path)
                    {
                        gameManager.buildingManager.PlaceBuilding(roadTemplate, tile, playerId);
                    }

                    roadsBuilt++;
                }
            }

            if (roadsBuilt >= 2) break; // Don't bankrupt ourselves on roads in one turn
        }
    }

    private int GetBuildingPriority(Building b)
    {
        return b.buildingType switch
        {
            Building.BuildingType.Mine => 10,
            Building.BuildingType.Woodcutter => 8,
            Building.BuildingType.Barracks => 5,
            Building.BuildingType.Farm => 3,
            _ => 0
        };
    }
}