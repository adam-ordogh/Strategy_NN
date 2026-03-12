// AIMicroController.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static UnitManager;

public class AIMicroController
{
    private int playerId;
    private GameManager gameManager;
    private PlayerProfile myProfile;

    // Constructor
    public AIMicroController(int playerId, GameManager gameManager)
    {
        this.playerId = playerId;
        this.gameManager = gameManager;
        this.myProfile = gameManager.GetPlayerProfile(playerId);
    }

    // Call this at the start of each turn to refresh the profile reference
    public void RefreshProfile()
    {
        myProfile = gameManager.GetPlayerProfile(playerId);
    }

    // ==================== MAIN EXECUTION METHODS ====================

    public void ExecuteEconomyMicro()
    {
        RebalanceWorkers();
        AssignIdleWorkers();

        bool madeProgress = true;
        int safetyBreak = 0;

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

            bool needsFarm = !HasUnfilledBuildings(Building.BuildingType.Farm, farmsPlanned);
            bool needsWood = !HasUnfilledBuildings(Building.BuildingType.Woodcutter, woodPlanned);
            bool needsMine = !HasUnfilledBuildings(Building.BuildingType.Mine, minesPlanned);

            // First warehouse (critical)
            if (ShouldBuildWarehouse(warehousesPlanned + warehousesInConst) && CountBuildings(Building.BuildingType.Warehouse) < 1)
            {
                if (TryBuild(Building.BuildingType.Warehouse)) { warehousesPlanned++; madeProgress = true; }
            }

            int targetIncome = 15 + (myProfile.myBuildings.Count * 2);

            // Basic needs
            if (effectiveFood < 10 && needsFarm && TryBuild(Building.BuildingType.Farm)) { farmsPlanned++; madeProgress = true; }
            else if (effectiveWood < 10 && needsWood && TryBuild(Building.BuildingType.Woodcutter)) { woodPlanned++; madeProgress = true; }
            else if (effectiveGold < 10 && needsMine && TryBuild(Building.BuildingType.Mine)) { minesPlanned++; madeProgress = true; }

            // Population growth
            else if (effectiveFood > 5 && myProfile.availablePopulation < 2)
            {
                if (ShouldBuildHouse() && TryBuild(Building.BuildingType.House))
                {
                    madeProgress = true;
                }
            }

            // Dynamic economy balancing
            else if (myProfile.availablePopulation > 0 || effectiveFood < targetIncome)
            {
                if (effectiveFood < targetIncome)
                {
                    if (needsFarm && TryBuild(Building.BuildingType.Farm)) { farmsPlanned++; madeProgress = true; }
                }
                else if (effectiveGold < effectiveWood)
                {
                    if (needsMine && TryBuild(Building.BuildingType.Mine)) { minesPlanned++; madeProgress = true; }
                }
                else
                {
                    if (needsWood && TryBuild(Building.BuildingType.Woodcutter)) { woodPlanned++; madeProgress = true; }
                }
            }

            // Additional warehouses
            if (ShouldBuildWarehouse(warehousesPlanned + warehousesInConst) && CountBuildings(Building.BuildingType.Warehouse) >= 1)
            {
                if (TryBuild(Building.BuildingType.Warehouse)) { warehousesPlanned++; madeProgress = true; }
            }

            if (madeProgress) AssignIdleWorkers();
        }
    }

    public void ExecuteMilitaryMicro()
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

    public void ExecuteExpansionMicro()
    {
        BuildingData expansionTemplate = GetBuildingTemplate(Building.BuildingType.Outpost);

        if (expansionTemplate != null && myProfile.CanAfford(expansionTemplate.goldCost, expansionTemplate.woodCost, 0))
        {
            Vector2Int? bestSpot = FindBestPlacementTile(expansionTemplate, AIGoal.FocusExpansion);

            if (bestSpot.HasValue)
            {
                Building b = gameManager.buildingManager.PlaceBuilding(expansionTemplate, bestSpot.Value, playerId);
            }
        }
    }

    public void ExecuteRoadMicro()
    {
        if (myProfile.gold <= 35 || myProfile.wood <= 35) return;

        var disconnected = myProfile.myBuildings
            .Where(b => !b.isConnectedToCapital && b.isConstructed)
            .OrderByDescending(b => GetBuildingPriority(b))
            .ToList();

        BuildingData roadTemplate = GetBuildingTemplate(Building.BuildingType.Road);
        int roadsBuilt = 0;

        foreach (var b in disconnected)
        {
            if (b.isConnectedToCapital) continue;

            List<Vector2Int> path = FindRoadPath(b);

            if (path != null && path.Count > 0 && path.Count <= 6)
            {
                if (myProfile.CanAfford(roadTemplate.woodCost * path.Count, roadTemplate.goldCost * path.Count, 0))
                {
                    foreach (var tile in path)
                    {
                        gameManager.buildingManager.PlaceBuilding(roadTemplate, tile, playerId);
                    }

                    gameManager.economyManager.RecalculateRoadNetwork(myProfile);

                    roadsBuilt++;
                }
            }

            if (roadsBuilt >= 2) break;
        }
    }

    public void HandleUnitMicro(MilitaryState currentArmyState, ref MilitaryState armyState)
    {
        Vector2Int enemyBase = GetClosestEnemyBase(myProfile.myBuildings[0].position);
        Vector2Int rallyPoint = GetRallyPoint();

        int combatUnits = CountUnits(Unit.UnitType.Soldier) + CountUnits(Unit.UnitType.Archer) +
                          CountUnits(Unit.UnitType.Cavalry) + CountUnits(Unit.UnitType.Siege);

        // Update army state based on size
        if (armyState == MilitaryState.Gathering && combatUnits >= 15)
        {
            armyState = MilitaryState.Attacking;
            Debug.Log($"[AI {playerId}] Army size reached {combatUnits}. Initiating attack!");
        }
        else if (armyState == MilitaryState.Attacking && combatUnits < 5)
        {
            armyState = MilitaryState.Gathering;
            Debug.Log($"[AI {playerId}] Army decimated. Retreating to gather.");
        }

        foreach (var unit in myProfile.myUnits.ToList())
        {
            // Attack if targets are reachable
            var targets = gameManager.unitManager.GetReachableTargets(unit);

            if (targets.Count > 0)
            {
                AttackCommand bestTarget = SelectBestTarget(unit, targets);
                gameManager.unitManager.TryMoveUnit(unit.position, bestTarget.StandPos);
                gameManager.unitManager.TryAttack(bestTarget.StandPos, bestTarget.TargetPos);
                continue;
            }

            // Move towards goal
            Vector2Int targetGoal = (armyState == MilitaryState.Attacking) ? enemyBase : rallyPoint;
            Vector2Int idealGoal = GetFormationOffset(unit, targetGoal);
            Vector2Int? reachableGoal = FindBestReachableTile(unit, idealGoal);

            if (reachableGoal.HasValue)
            {
                var path = gameManager.unitManager.GetPathToTarget(unit, reachableGoal.Value);
                if (path != null && path.Count > 1)
                {
                    gameManager.unitManager.TryMoveUnit(unit.position, path.Last());
                }
            }
            else if (IsNearBarracks(unit.position))
            {
                MoveToAnyEmptyNeighbor(unit);
            }
        }
    }

    // ==================== WORKER MANAGEMENT ====================

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

    // ==================== BUILDING PLACEMENT ====================

    private Vector2Int? FindBestPlacementTile(BuildingData template, AIGoal goal)
    {
        var influenceManager = gameManager.buildingManager.influenceManager;
        Vector2Int enemyBasePos = GetClosestEnemyBase(myProfile.myBuildings[0].position);

        List<Vector2Int> myTerritory = influenceManager.GetTilesOwnedBy(playerId);

        float highestScore = float.MinValue;
        Vector2Int? bestTile = null;

        // Filter by resource requirements
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
                float score = EvaluateTileScore(checkPos, template, goal, enemyBasePos);

                if (score > highestScore)
                {
                    highestScore = score;
                    bestTile = checkPos;
                }
            }
        }

        return bestTile;
    }

    private float EvaluateTileScore(Vector2Int pos, BuildingData template, AIGoal goal, Vector2Int enemyBase)
    {
        float score = 0f;

        Vector2Int basePos = myProfile.myBuildings[0].position;
        float dist = Vector2Int.Distance(pos, basePos);

        // Check for sufficient free neighbors (except houses which can be packed)
        int freeNeighbors = 0;
        foreach (var dir in Pathfinder.Directions)
        {
            Vector2Int n = pos + dir;
            if (IsInsideMap(n) && gameManager.buildingManager.GetBuildingAtTile(n) == null)
                freeNeighbors++;
        }

        if (freeNeighbors < 1 && template.buildingType != Building.BuildingType.House)
            score -= 200f;

        // Goal-specific scoring
        if (goal == AIGoal.FocusEconomy)
        {
            score -= dist * 2f; // Prefer closer to base

            switch (template.buildingType)
            {
                case Building.BuildingType.Woodcutter:
                    score += CountNearbyTiles(pos, MapData.TileType.Forest, 1) * 10f;
                    break;
                case Building.BuildingType.Mine:
                    score += CountNearbyTiles(pos, MapData.TileType.Mountain, 1) * 50f;
                    break;
                case Building.BuildingType.Farm:
                    // Bonus for farm clusters
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
            score += dist * 1f; // Prefer further from base

            // Anti-clustering
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

            // Resource capture
            int captureRadius = template.influenceRadius;
            score += CountNearbyTiles(pos, MapData.TileType.Mountain, captureRadius) * 5f;
            score += CountNearbyTiles(pos, MapData.TileType.Forest, captureRadius) * 2f;

            // Push towards enemy
            Vector2Int enemyPos = GetClosestEnemyBase(pos);
            if (enemyPos.x != -1)
            {
                float distToEnemy = Vector2Int.Distance(pos, enemyPos);
                score -= distToEnemy * 2f; // Closer to enemy = higher score
            }
        }
        else if (goal == AIGoal.FocusMilitary)
        {
            if (enemyBase.x != -1)
            {
                float distToEnemy = Vector2Int.Distance(pos, enemyBase);
                score -= distToEnemy * 10f; // Closer to enemy = higher score
            }
        }

        return score;
    }

    private bool TryBuild(Building.BuildingType type)
    {
        BuildingData template = GetBuildingTemplate(type);
        if (template == null) return false;

        if (!myProfile.CanAfford(template.goldCost, template.woodCost, 0)) return false;

        Vector2Int? bestSpot = FindBestPlacementTile(template, AIGoal.FocusEconomy);
        if (bestSpot.HasValue)
        {
            Building newBuilding = gameManager.buildingManager.PlaceBuilding(template, bestSpot.Value, playerId);
            return newBuilding != null;
        }

        return false;
    }

    // ==================== MILITARY TACTICAL DECISIONS ====================

    private AttackCommand SelectBestTarget(Unit unit, List<AttackCommand> targets)
    {
        AttackCommand bestTarget = targets[0];

        if (unit.data.unitType == Unit.UnitType.Siege)
        {
            // Siege weapons prefer buildings
            var buildingTarget = targets.FirstOrDefault(t => gameManager.mapManager.mapData.buildings.ContainsKey(t.TargetPos));
            if (buildingTarget.TargetPos != Vector2Int.zero) return buildingTarget;
        }
        else
        {
            // Other units prioritize weak enemies
            Unit targetEnemy = gameManager.mapManager.mapData.units.GetValueOrDefault(bestTarget.TargetPos);

            foreach (var t in targets)
            {
                Unit enemy = gameManager.mapManager.mapData.units.GetValueOrDefault(t.TargetPos);
                if (enemy != null && targetEnemy != null && enemy.currentHealth < targetEnemy.currentHealth)
                {
                    bestTarget = t;
                    targetEnemy = enemy;
                }
            }
        }

        return bestTarget;
    }

    private Unit.UnitType DetermineBestUnitType(Dictionary<Unit.UnitType, int> enemyComp)
    {
        var unitCounts = new Dictionary<Unit.UnitType, int>();
        foreach (var unit in myProfile.myUnits)
        {
            unitCounts[unit.data.unitType] = unitCounts.GetValueOrDefault(unit.data.unitType) + 1;
        }

        var queueCounts = new Dictionary<Unit.UnitType, int>();
        foreach (var b in myProfile.myBuildings)
        {
            if (b.buildingType == Building.BuildingType.Barracks)
            {
                var queue = gameManager.productionManager.GetQueueForBuilding(b);
                if (queue != null)
                {
                    foreach (var order in queue)
                    {
                        queueCounts[order.template.unitType] = queueCounts.GetValueOrDefault(order.template.unitType) + 1;
                    }
                }
            }
        }

        int effSoldiers = unitCounts.GetValueOrDefault(Unit.UnitType.Soldier) + queueCounts.GetValueOrDefault(Unit.UnitType.Soldier);
        int effArchers = unitCounts.GetValueOrDefault(Unit.UnitType.Archer) + queueCounts.GetValueOrDefault(Unit.UnitType.Archer);
        int effCavalry = unitCounts.GetValueOrDefault(Unit.UnitType.Cavalry) + queueCounts.GetValueOrDefault(Unit.UnitType.Cavalry);
        int effSiege = unitCounts.GetValueOrDefault(Unit.UnitType.Siege) + queueCounts.GetValueOrDefault(Unit.UnitType.Siege);

        // Base scores (decrease as we have more)
        float soldierScore = 12f - (effSoldiers * 1.0f);
        float archerScore = 10f - (effArchers * 1.0f);
        float cavalryScore = 8f - (effCavalry * 1.0f);
        float siegeScore = 2f - (effSiege * 2.0f);

        // Counter logic
        if (enemyComp.GetValueOrDefault(Unit.UnitType.Archer) > 0)
        {
            soldierScore += enemyComp[Unit.UnitType.Archer] * 2;
            cavalryScore += enemyComp[Unit.UnitType.Archer] * 6;
        }

        if (enemyComp.GetValueOrDefault(Unit.UnitType.Cavalry) > 0)
        {
            soldierScore += enemyComp[Unit.UnitType.Cavalry] * 8;
        }

        int totalArmySize = effSoldiers + effArchers + effCavalry;
        if (totalArmySize > 5 && effSiege < 2)
        {
            siegeScore += 15f;
        }

        // Economic factor - rich AI can afford expensive units
        if (myProfile.gold > 400)
        {
            cavalryScore += 5f;
            siegeScore += 2f;
        }

        var scores = new Dictionary<Unit.UnitType, float> {
            { Unit.UnitType.Soldier, soldierScore },
            { Unit.UnitType.Archer, archerScore },
            { Unit.UnitType.Cavalry, cavalryScore },
            { Unit.UnitType.Siege, siegeScore }
        };

        return scores.OrderByDescending(x => x.Value).First().Key;
    }

    // ==================== PATHFINDING & MOVEMENT ====================

    private Vector2Int? FindBestReachableTile(Unit unit, Vector2Int target)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(unit.position);
        visited.Add(unit.position);

        Vector2Int bestTile = unit.position;
        float minDist = Vector2Int.Distance(unit.position, target);

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

            foreach (Vector2Int dir in Pathfinder.Directions)
            {
                Vector2Int neighbor = current + dir;
                if (visited.Contains(neighbor)) continue;

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
        if (!IsInsideMap(pos)) return false;
        if (!gameManager.mapManager.mapData.mapTiles[pos.x, pos.y].isPassable) return false;
        if (gameManager.mapManager.mapData.units.ContainsKey(pos)) return false;
        if (gameManager.mapManager.mapData.buildings.ContainsKey(pos)) return false;

        return true;
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

    private Vector2Int GetFormationOffset(Unit unit, Vector2Int baseRally)
    {
        // 3x3 offset pattern around rally point
        int offsetIndex = unit.GetHashCode() % 9;
        int dx = (offsetIndex % 3) - 1;
        int dy = (offsetIndex / 3) - 1;

        // Ranged units stay slightly back
        if (unit.data.unitType == Unit.UnitType.Archer || unit.data.unitType == Unit.UnitType.Siege)
        {
            Vector2Int basePos = myProfile.myBuildings[0].position;
            Vector2Int directionToBase = new Vector2Int(
                Mathf.Clamp(basePos.x - baseRally.x, -1, 1),
                Mathf.Clamp(basePos.y - baseRally.y, -1, 1)
            );
            return baseRally + new Vector2Int(dx, dy) + directionToBase;
        }

        return baseRally + new Vector2Int(dx, dy);
    }

    // ==================== ROAD NETWORK ====================

    private List<Vector2Int> FindRoadPath(Building target)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();

        // Start from the target building's neighboring tiles
        foreach (var startTile in target.GetOccupiedTiles())
        {
            foreach (var dir in Pathfinder.Directions)
            {
                Vector2Int neighbor = startTile + dir;
                if (!IsInsideMap(neighbor) || visited.Contains(neighbor)) continue;

                Building b = gameManager.buildingManager.GetBuildingAtTile(neighbor);

                // Check if already connected
                if (b != null && b.isConnectedToCapital && b.ownerId == playerId &&
                   (b.buildingType == Building.BuildingType.Road || b.buildingType == Building.BuildingType.TownCenter))
                {
                    return new List<Vector2Int>();
                }

                // Valid road placement tile
                if (b == null || (b.buildingType == Building.BuildingType.Road && b.ownerId == playerId))
                {
                    visited.Add(neighbor);
                    parents[neighbor] = startTile;
                    distances[neighbor] = 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        // BFS to find connection to capital
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (var dir in Pathfinder.Directions)
            {
                Vector2Int next = current + dir;
                if (!IsInsideMap(next) || visited.Contains(next)) continue;

                Building b = gameManager.buildingManager.GetBuildingAtTile(next);

                // Found connection!
                if (b != null && b.isConnectedToCapital && b.ownerId == playerId &&
                   (b.buildingType == Building.BuildingType.Road || b.buildingType == Building.BuildingType.TownCenter))
                {
                    return ReconstructPath(parents, current, target);
                }

                // Continue BFS
                if (b == null || (b.buildingType == Building.BuildingType.Road && b.ownerId == playerId))
                {
                    int newDist = distances[current] + 1;
                    if (newDist > 10) continue;

                    visited.Add(next);
                    parents[next] = current;
                    distances[next] = newDist;
                    queue.Enqueue(next);
                }
            }
        }
        return null;
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parents, Vector2Int end, Building startBuilding)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int curr = end;

        int safetyLimit = 1000;

        while (parents.ContainsKey(curr) && safetyLimit > 0)
        {
            safetyLimit--;

            if (gameManager.buildingManager.GetBuildingAtTile(curr) == null)
            {
                path.Add(curr);
            }

            curr = parents[curr];

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

    // ==================== UTILITY & HELPER METHODS ====================

    public bool CanPlaceIndustrialBuilding()
    {
        var woodcutter = GetBuildingTemplate(Building.BuildingType.Woodcutter);
        var mine = GetBuildingTemplate(Building.BuildingType.Mine);
        return FindBestPlacementTile(woodcutter, AIGoal.FocusEconomy).HasValue &&
               FindBestPlacementTile(mine, AIGoal.FocusEconomy).HasValue;
    }

    public bool ShouldBuildHouse()
    {
        return myProfile.availablePopulation < myProfile.housingCapacity * 0.3f &&
               (myProfile.housingCapacity - myProfile.currentPopulation) <= 2;
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

    private bool HasUnfilledBuildings(Building.BuildingType type, int plannedThisTurn)
    {
        int underConstruction = myProfile.myBuildings.Count(b => !b.isConstructed && b.buildingType == type);
        if (underConstruction + plannedThisTurn > 0) return true;

        var constructed = myProfile.myBuildings.Where(b => b.isConstructed && b.buildingType == type);
        foreach (var b in constructed)
        {
            if (b.CanAcceptWorker()) return true;
        }

        return false;
    }

    public float GetObservedEnemyStrength()
    {
        float totalObserved = 0;
        float observationRadius = 15f;

        foreach (var otherPlayer in gameManager.players)
        {
            if (otherPlayer.playerId == playerId) continue;

            foreach (var enemyUnit in otherPlayer.myUnits)
            {
                if (IsPositionVisible(enemyUnit.position, observationRadius))
                {
                    totalObserved += 10f;
                }
            }

            foreach (var enemyBuilding in otherPlayer.myBuildings)
            {
                if (enemyBuilding.buildingType == Building.BuildingType.Barracks && enemyBuilding.isConstructed)
                {
                    bool buildingVisible = false;
                    foreach (var tile in enemyBuilding.GetOccupiedTiles())
                    {
                        if (IsPositionVisible(tile, observationRadius))
                        {
                            buildingVisible = true;
                            break;
                        }
                    }

                    if (buildingVisible)
                    {
                        totalObserved += 20f;
                    }
                }
            }
        }

        return Mathf.Max(totalObserved, 30f);
    }

    private Dictionary<Unit.UnitType, int> GetObservedEnemyComposition()
    {
        var comp = new Dictionary<Unit.UnitType, int> {
            { Unit.UnitType.Soldier, 0 }, { Unit.UnitType.Archer, 0 },
            { Unit.UnitType.Cavalry, 0 }, { Unit.UnitType.Siege, 0 }
        };

        // For now, just look at opponent's profile (will be replaced with fog of war)
        var enemy = gameManager.GetPlayerProfile(1);
        foreach (var u in enemy.myUnits)
        {
            comp[u.data.unitType]++;
        }
        return comp;
    }

    private bool IsPositionVisible(Vector2Int targetPos, float observationRadius)
    {
        foreach (var myUnit in myProfile.myUnits)
        {
            if (Vector2Int.Distance(myUnit.position, targetPos) < observationRadius)
                return true;
        }

        foreach (var myBuilding in myProfile.myBuildings)
        {
            if (Vector2Int.Distance(myBuilding.position, targetPos) < observationRadius)
                return true;
        }

        return false;
    }

    public bool IsEnemyNearBase(float range)
    {
        if (myProfile.myBuildings.Count == 0) return false;
        Vector2Int myBase = myProfile.myBuildings[0].position;

        foreach (var p in gameManager.players)
        {
            if (p.playerId == playerId) continue;

            foreach (var unit in p.myUnits)
            {
                if (Vector2Int.Distance(myBase, unit.position) < range)
                    return true;
            }
        }
        return false;
    }

    public float CalculateMilitaryStrength()
    {
        return myProfile.myUnits.Count * 10f;
    }

    private int CountBuildings(Building.BuildingType type)
    {
        return myProfile.myBuildings.Count(b => b.buildingType == type);
    }

    private int CountUnits(Unit.UnitType type)
    {
        return myProfile.myUnits.Count(u => u.data.unitType == type);
    }

    private int CountNearbyTiles(Vector2Int center, MapData.TileType type, int radius)
    {
        int count = 0;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int checkPos = new Vector2Int(center.x + x, center.y + y);
                if (!IsInsideMap(checkPos)) continue;

                if (gameManager.mapManager.mapData.mapTiles[checkPos.x, checkPos.y].type == type)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private Vector2Int GetRallyPoint()
    {
        if (myProfile.myBuildings.Count == 0) return Vector2Int.zero;

        Vector2Int rally = myProfile.myBuildings[0].position;
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

        return rally + new Vector2Int(2, 2);
    }

    private Vector2Int GetClosestEnemyBase(Vector2Int fromPos)
    {
        Vector2Int closest = new Vector2Int(-1, -1);
        float minDist = float.MaxValue;

        foreach (var p in gameManager.players)
        {
            if (p.playerId != playerId && p.myBuildings.Count > 0)
            {
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

    private BuildingData GetBuildingTemplate(Building.BuildingType type)
    {
        return gameManager.buildingManager.buildingTemplates.Find(t => t.buildingType == type);
    }

    private UnitData GetUnitTemplate(Unit.UnitType type)
    {
        return gameManager.unitManager.unitTemplates.Find(t => t.unitType == type);
    }

    private bool IsInsideMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gameManager.mapManager.mapData.mapWidth &&
               pos.y >= 0 && pos.y < gameManager.mapManager.mapData.mapHeight;
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