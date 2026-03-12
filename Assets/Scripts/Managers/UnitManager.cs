using UnityEngine;
using System.Collections.Generic;
using System;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class UnitManager : MonoBehaviour
{
    public MapData mapData;
    public List<UnitData> unitTemplates;

    public GameManager gameManager;

    public event Action<Unit, Vector2Int> OnUnitCreated;
    public event Action<Unit, List<Vector2Int>> OnUnitMoved;
    public event Action<Unit> OnUnitDestroyed;

    public System.Func<Vector2Int, bool> IsTileBlockedByBuilding;

    public void Initialize(MapData mapData, GameManager gameManager)
    {
        this.mapData = mapData;
        this.gameManager = gameManager;
    }

    public Unit SpawnUnit(UnitData template, Vector2Int pos, int ownerId)
    {
        if (template == null)
        {
            Debug.LogError($"No template found for template!");
            return null;
        }

        Unit newUnit = new Unit(
            template,
            ownerId,
            pos
        );

        CreateUnit(newUnit);

        return newUnit;
    }

    public void CreateUnit(Unit unit)
    {
        unit.OnUnitDeath += HandleUnitDeath;
        mapData.units[unit.position] = unit;

        PlayerProfile owner = gameManager.GetPlayerProfile(unit.ownerId);
        if (owner != null)
        {
            owner.myUnits.Add(unit);
        }

        OnUnitCreated?.Invoke(unit, unit.position);
    }

    public void DestroyUnit(Unit unit)
    {
        unit.OnUnitDeath -= HandleUnitDeath;
        mapData.units.Remove(unit.position);
        PlayerProfile owner = gameManager.GetPlayerProfile(unit.ownerId);
        if (owner != null)
        {
            owner.myUnits.Remove(unit);
        }
        OnUnitDestroyed?.Invoke(unit);
    }

    public void HandleUnitDeath(Unit unit)
    {
        if (mapData.units.ContainsKey(unit.position))
        {
            mapData.units.Remove(unit.position);
            PlayerProfile owner = gameManager.GetPlayerProfile(unit.ownerId);
            if (owner != null)
            {
                owner.myUnits.Remove(unit);
            }

            OnUnitDestroyed?.Invoke(unit);
        }
    }

    public void ResetUnitsForNewTurn(int currentPlayerId)
    {
        foreach (var unit in mapData.units.Values)
        {
            if (unit.ownerId == currentPlayerId)
                unit.ResetActions();
        }
    }

    // ---------------------------- MOZGÁS FÜGGVÉNYEK ----------------------------
    static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };
    public Dictionary<Vector2Int, float> GetReachableTilesWithCost(Unit unit)
    {
        var frontier = new PriorityQueue<Vector2Int, float>();
        var cost = new Dictionary<Vector2Int, float>();

        frontier.Enqueue(unit.position, 0);
        cost[unit.position] = 0;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();

            foreach (var dir in Directions)
            {
                var next = current + dir;
                if (!IsTileValidForMovement(next)) continue;

                // Alap mozgás ár (Út vs Fű)
                float moveCost = GetStepCost(unit, next);

                float newCost = cost[current] + moveCost;

                if (newCost > unit.remainingMovementPoints) continue;

                if (!cost.TryGetValue(next, out float oldCost) || newCost < oldCost)
                {
                    cost[next] = newCost;
                    frontier.Enqueue(next, newCost);
                }
            }
        }

        return cost;
    }

    // Részleges mozgás kezelése, ha nincs elég mozgáspont a teljes úthoz
    public void TryMoveUnit(Vector2Int fromPos, Vector2Int toPos)
    {
        if (!mapData.units.TryGetValue(fromPos, out Unit unit)) return;
        if (unit.remainingMovementPoints <= 0) return;

        List<Vector2Int> fullPath = GetPathToTarget(unit, toPos);
        if (fullPath == null || fullPath.Count <= 1) return;

        List<Vector2Int> actualPath = new List<Vector2Int> { fromPos };
        float totalCost = 0;
        Vector2Int lastReachableTile = fromPos;

       
        for (int i = 1; i < fullPath.Count; i++)
        {
            float stepCost = GetStepCost(unit, fullPath[i]);

            if (totalCost + stepCost <= unit.remainingMovementPoints)
            {
                totalCost += stepCost;
                lastReachableTile = fullPath[i];
                actualPath.Add(lastReachableTile);
            }
            else
            {
                break;
            }
        }

        // Csak akkor mozogjon, ha legalább egy lépést meg tud tenni
        if (actualPath.Count > 1)
        {
            unit.remainingMovementPoints -= totalCost;

            unit.Move(lastReachableTile);
            mapData.units.Remove(fromPos);
            mapData.units[lastReachableTile] = unit;

            OnUnitMoved?.Invoke(unit, actualPath);
        }
        else
        {
            Debug.Log("Unit cannot even take one step toward that target!");
        }
    }

    public List<Vector2Int> GetPathToTarget(Unit unit, Vector2Int targetPos)
    {
        // Cost Function meghatározása 
        // Lamda függvényként adjuk meg az egyes mezők mozgási költségét
        Func<Vector2Int, float> unitCostFunc = (pos) =>
        {
            float baseCost = mapData.moveCostMap[pos.x, pos.y];
            return baseCost + (IsTileThreatened(pos, unit.ownerId) ? 2.0f : 0f);
        };

        // Validation Function meghatározása
        Func<Vector2Int, bool> unitValidFunc = (pos) =>
        {
            return IsTileValidForMovement(pos);
        };

        return Pathfinder.FindPath(unit.position, targetPos, unitCostFunc, unitValidFunc);
    }

    private bool IsTileValidForMovement(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= mapData.mapWidth || pos.y < 0 || pos.y >= mapData.mapHeight)
            return false;
        if (float.IsInfinity(mapData.moveCostMap[pos.x, pos.y]))
            return false;
        if (mapData.units.ContainsKey(pos))
            return false;
        if (IsTileBlockedByBuilding != null && IsTileBlockedByBuilding(pos))
            return false;
        return true;
    }

    private bool IsTileThreatened(Vector2Int pos, int movingUnitOwnerId)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector2Int neighbor = new Vector2Int(pos.x + x, pos.y + y);

                if (mapData.units.TryGetValue(neighbor, out Unit unit))
                {
                    if (unit.ownerId != movingUnitOwnerId && unit.data.unitType != Unit.UnitType.Archer)
                        return true;
                }
            }
        }
        return false;
    }

    public float GetStepCost(Unit unit, Vector2Int tile)
    {
        float cost = mapData.moveCostMap[tile.x, tile.y];

        if (IsTileThreatened(tile, unit.ownerId))
        {
            cost += 2.0f;
        }

        return cost;
    }

    // ---------------------------- TÁMADÁS FÜGGVÉNYEK ----------------------------
    public struct AttackCommand
    {
        public Vector2Int TargetPos;
        public Vector2Int StandPos; // A legjobb pozíció ahonnan elérjük a célt
    }

    public List<AttackCommand> GetReachableTargets(Unit unit)
    {
        List<AttackCommand> validTargets = new List<AttackCommand>();

        var reachableTiles = GetReachableTilesWithCost(unit).Keys;
        List<Vector2Int> allStandableTiles = new List<Vector2Int>(reachableTiles);
        allStandableTiles.Add(unit.position);

        foreach (var kvp in mapData.units)
        {
            Vector2Int enemyPos = kvp.Key;
            Unit enemyUnit = kvp.Value;

            if (enemyUnit.ownerId == unit.ownerId) continue;

            foreach (var standTile in allStandableTiles)
            {
                int dist = Mathf.Max(Mathf.Abs(standTile.x - enemyPos.x), Mathf.Abs(standTile.y - enemyPos.y));
                if (dist <= unit.data.attackRange)
                {
                    validTargets.Add(new AttackCommand { TargetPos = enemyPos, StandPos = standTile });
                    break;
                }
            }
        }

        foreach (var kvp in mapData.buildings)
        {
            Building building = kvp.Value;
            if (building.ownerId == unit.ownerId || !building.data.isSelectable) continue;

            foreach (var standTile in allStandableTiles)
            {
                bool buildingReached = false;

                foreach (Vector2Int occupiedTile in building.GetOccupiedTiles())
                {
                    int dist = Mathf.Max(Mathf.Abs(standTile.x - occupiedTile.x), Mathf.Abs(standTile.y - occupiedTile.y));
                    if (dist <= unit.data.attackRange)
                    {
                        validTargets.Add(new AttackCommand { TargetPos = occupiedTile, StandPos = standTile });
                        buildingReached = true;
                        break;
                    }
                }
                if (buildingReached) break;
            }
        }

        return validTargets;
    }

    public Vector2Int? GetBestAttackPosition(Unit attacker, Vector2Int targetPos)
    {
        var reachableTiles = GetReachableTilesWithCost(attacker);

        if (!reachableTiles.ContainsKey(attacker.position))
            reachableTiles[attacker.position] = 0;

        Vector2Int? bestTile = null;
        float minCost = float.MaxValue; 

        foreach (var tile in reachableTiles.Keys)
        {
            // Chebyshev távolság számítása (8-irányú) ettől az ellenféltől
            int dist = Mathf.Max(Mathf.Abs(tile.x - targetPos.x), Mathf.Abs(tile.y - targetPos.y));

            if (dist <= attacker.data.attackRange)
            {
                float cost = reachableTiles[tile];

                // Találtunk egy elérhető mezőt ahonnan támadhatunk
                // A legjobb mező kiválasztása a legkisebb mozgási költség alapján
                if (cost < minCost)
                {
                    minCost = cost;
                    bestTile = tile;
                }
            }
        }
        return bestTile;
    }

    public void TryAttack(Vector2Int attackerPos, Vector2Int targetPos)
    {
        if (!mapData.units.TryGetValue(attackerPos, out Unit attacker)) return;
        if (!attacker.canAttack) return;

        // ha a célpont egy egység
        if (mapData.units.TryGetValue(targetPos, out Unit targetUnit))
        {
            if (attacker.ownerId != targetUnit.ownerId)
            {
                attacker.DealDamageToUnit(targetUnit);
            }
        }
        // Ha a célpont nem egység, akkor lehet épület
        else
        {
            // A BuildingManager referenciát lehet át kell dolgozni, esetleg deleagate-t használva?
            Building targetBuilding = FindFirstObjectByType<BuildingManager>().GetBuildingAtTile(targetPos);

            if (targetBuilding != null && targetBuilding.data.isSelectable && targetBuilding.ownerId != attacker.ownerId)
            {
                // Range check
                int dist = Mathf.Max(Mathf.Abs(attackerPos.x - targetPos.x), Mathf.Abs(attackerPos.y - targetPos.y));
                if (dist <= attacker.data.attackRange)
                {
                    attacker.DealDamageToBuilding(targetBuilding);

                    FindFirstObjectByType<BuildingManager>().CheckBuildingHealth(targetBuilding);
                }
            }
        }
    }
}
