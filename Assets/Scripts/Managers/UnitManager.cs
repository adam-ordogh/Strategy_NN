using UnityEngine;
using System.Collections.Generic;
using System;

public class UnitManager : MonoBehaviour
{
    public MapData mapData;
    public List<UnitData> unitTemplates;

    public GameManager gameManager;

    public event Action<Unit, Vector2Int> OnUnitCreated;
    public event Action<Unit, List<Vector2Int>> OnUnitMoved;
    public event Action<Unit> OnUnitDestroyed;

    public System.Func<Vector2Int, bool> IsTileBlockedByBuilding;
    public System.Func<Vector2Int, float> GetTileMovementCost;

    public void Initialize(MapData mapData, GameManager gameManager)
    {
        this.mapData = mapData;
        this.gameManager = gameManager;
    }

    public Unit SpawnUnit(Unit.UnitType type, Vector2Int pos, int ownerId)
    {
        UnitData template = unitTemplates.Find(t => t.unitType == type);

        if (template == null)
        {
            Debug.LogError($"No template found for {type}!");
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
    //public Dictionary<Vector2Int, int> GetReachableTilesWithCost(Unit unit)
    //{
    //    var reachable = new Dictionary<Vector2Int, int>();
    //    // Könyvtárban tárolt minimális költség egy mező eléréséhez
    //    var minCostToReach = new Dictionary<Vector2Int, int>();

    //    var queue = new Queue<(Vector2Int pos, int cost)>();

    //    queue.Enqueue((unit.position, 0));
    //    minCostToReach[unit.position] = 0;

    //    while (queue.Count > 0)
    //    {
    //        var (currentPos, currentCost) = queue.Dequeue();

    //        // Ha már találtunk olcsóbb utat ide, kihagyjuk
    //        if (currentCost > minCostToReach[currentPos]) continue;

    //        if (currentPos != unit.position)
    //            reachable[currentPos] = currentCost;

    //        foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
    //        {
    //            var nextPos = currentPos + dir;

    //            if (!IsTileValidForMovement(nextPos)) continue;

    //            int moveCost = 1;
    //            if (IsTileThreatened(nextPos, unit.ownerId))
    //            {
    //                moveCost = 3;
    //            }

    //            int nextCost = currentCost + moveCost;
    //            if (nextCost > unit.remainingMovementPoints) continue;

    //            if (!minCostToReach.ContainsKey(nextPos) || nextCost < minCostToReach[nextPos])
    //            {
    //                minCostToReach[nextPos] = nextCost;
    //                queue.Enqueue((nextPos, nextCost));
    //            }
    //        }
    //    }

    //    return reachable;
    //}
    public Dictionary<Vector2Int, float> GetReachableTilesWithCost(Unit unit)
    {
        // Changed Dictionary value to float
        Dictionary<Vector2Int, float> cost = new Dictionary<Vector2Int, float>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        cost[unit.position] = 0;
        queue.Enqueue(unit.position);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // --- Ha 8-irányú mozgást akarunk:
            //Vector2Int[] neighbors = {
            //current + Vector2Int.up, current + Vector2Int.down,
            //current + Vector2Int.left, current + Vector2Int.right,
            //current + new Vector2Int(1,1), current + new Vector2Int(1,-1),
            //current + new Vector2Int(-1,1), current + new Vector2Int(-1,-1)
            //};

            // Ha 4-irányú mozgást akarunk:
            Vector2Int[] neighbors = {
            current + Vector2Int.up, current + Vector2Int.down,
            current + Vector2Int.left, current + Vector2Int.right
            };

            foreach (var neighbor in neighbors)
            {
                if (IsTileValidForMovement(neighbor))
                {
                    // Alap mozgás ár (Út vs Fű)
                    float moveCost = GetTileMovementCost != null ? GetTileMovementCost(neighbor) : 1.0f;
                    if (IsTileThreatened(neighbor, unit.ownerId))
                    {
                        moveCost += 2.0f;
                    }
                    float newCost = cost[current] + moveCost;

                    if (newCost <= unit.remainingMovementPoints && (!cost.ContainsKey(neighbor) || newCost < cost[neighbor]))
                    {
                        cost[neighbor] = newCost;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
        return cost;
    }

    //public void TryMoveUnit(Vector2Int fromPos, Vector2Int toPos)
    //{
    //    if (!mapData.units.TryGetValue(fromPos, out Unit unit)) return;

    //    List<Vector2Int> path = GetPathToTarget(unit, toPos);
    //    if (path == null) return; 

    //    int moveCost = path.Count - 1;
    //    unit.remainingMovementPoints -= moveCost;
    //    unit.Move(toPos);

    //    mapData.units.Remove(fromPos);
    //    mapData.units[toPos] = unit;

    //    OnUnitMoved?.Invoke(unit, path);
    //}
    public void TryMoveUnit(Vector2Int fromPos, Vector2Int toPos)
    {
        if (!mapData.units.TryGetValue(fromPos, out Unit unit)) return;

        List<Vector2Int> path = GetPathToTarget(unit, toPos);
        if (path == null) return;

        float totalPathCost = 0;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int step = path[i];

            // Alap mozgás ár (Út vs Fű)
            float stepCost = GetTileMovementCost != null ? GetTileMovementCost(step) : 1.0f;

            if (IsTileThreatened(step, unit.ownerId))
            {
                stepCost += 2.0f;
            }

            totalPathCost += stepCost;
        }

        unit.remainingMovementPoints = Mathf.Max(0, unit.remainingMovementPoints - totalPathCost);

        unit.Move(toPos);

        mapData.units.Remove(fromPos);
        mapData.units[toPos] = unit;

        OnUnitMoved?.Invoke(unit, path);
    }

    // Segédfüggvény útvonal visszaállításhoz
    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse(); // Útvonal megfordítása a kezdőponttól a célpontig
        return path;
    }
    //public List<Vector2Int> GetPathToTarget(Unit unit, Vector2Int targetPos)
    //{
    //    // Lista z egyszerű prioritásos sor helyett
    //    // Mindig a legolcsóbb csomópontot választjuk ki először
    //    var openList = new List<(Vector2Int pos, int cost)>();
    //    var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
    //    var costSoFar = new Dictionary<Vector2Int, int>();

    //    openList.Add((unit.position, 0));
    //    costSoFar[unit.position] = 0;

    //    while (openList.Count > 0)
    //    {
    //        // DIJKSTRA LÉPS
    //        openList.Sort((a, b) => a.cost.CompareTo(b.cost));
    //        var current = openList[0].pos;
    //        openList.RemoveAt(0);

    //        if (current == targetPos)
    //            return ReconstructPath(cameFrom, current);

    //        foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
    //        {
    //            var next = current + dir;
    //            if (!IsTileValidForMovement(next)) continue;

    //            // Van-e ellenség az útban?
    //            // Ignoráljuk az ellenséges egységet a targetPos-nál ha ő maga a célpont
    //            int moveCost = 1;
    //            if (IsTileThreatened(next, unit.ownerId))
    //            {
    //                moveCost = 3;
    //            }

    //            int newCost = costSoFar[current] + moveCost;
    //            if (newCost > unit.remainingMovementPoints) continue;

    //            if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
    //            {
    //                costSoFar[next] = newCost;
    //                cameFrom[next] = current;
    //                openList.Add((next, newCost));
    //            }
    //        }
    //    }
    //    return null;
    //}

    public List<Vector2Int> GetPathToTarget(Unit unit, Vector2Int targetPos)
    {
        var openList = new List<(Vector2Int pos, float cost)>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var costSoFar = new Dictionary<Vector2Int, float>(); 

        openList.Add((unit.position, 0));
        costSoFar[unit.position] = 0;

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => a.cost.CompareTo(b.cost));
            var current = openList[0].pos;
            openList.RemoveAt(0);

            if (current == targetPos)
                return ReconstructPath(cameFrom, current);

            // --- Ha 8-irányú mozgást akarunk: 
            //foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            //                        new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)})

            // Ha 4-irányú mozgást akarunk:
            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var next = current + dir;
                if (!IsTileValidForMovement(next)) continue;

                float moveCost = GetTileMovementCost != null ? GetTileMovementCost(next) : 1.0f;

                if (IsTileThreatened(next, unit.ownerId))
                {
                    moveCost += 2.0f;
                }

                float newCost = costSoFar[current] + moveCost;

                if (newCost > unit.remainingMovementPoints) continue;

                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    openList.Add((next, newCost));
                }
            }
        }
        return null;
    }

    private bool IsTileValidForMovement(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= mapData.mapWidth || pos.y < 0 || pos.y >= mapData.mapHeight)
            return false;
        if (!mapData.mapTiles[pos.x, pos.y].isPassable)
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
                    if (unit.ownerId != movingUnitOwnerId && unit.unitType != Unit.UnitType.Archer)
                        return true;
                }
            }
        }
        return false;
    }

    // ---------------------------- TÁMADÁS FÜGGVÉNYEK ----------------------------
    public struct AttackCommand
    {
        public Vector2Int TargetPos;
        public Vector2Int StandPos; // A legjobb pozíció ahonnan elérjük a célt
    }

    public List<AttackCommand> GetReachableEnemies(Unit unit)
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

            // Van-e olyan mező ahol állva elérjük az ellenséget?
            foreach (var standTile in allStandableTiles)
            {
                int dist = Mathf.Max(
                    Mathf.Abs(standTile.x - enemyPos.x),
                    Mathf.Abs(standTile.y - enemyPos.y)
                );

                if (dist <= unit.attackRange)
                {
                    validTargets.Add(new AttackCommand
                    {
                        TargetPos = enemyPos,
                        StandPos = standTile
                    });
                    break; // Ha van ilyen mező, nem kell tovább keresni
                }
            }
        }

        return validTargets;
    }

    //public Vector2Int? GetBestAttackPosition(Unit attacker, Vector2Int targetPos)
    //{
    //    var reachableTiles = GetReachableTilesWithCost(attacker);
    //    reachableTiles[attacker.position] = 0;

    //    Vector2Int? bestTile = null;
    //    float minCost = float.MaxValue;

    //    foreach (var tile in reachableTiles.Keys)
    //    {
    //        // Chebyshev távolság számítása (8-irányú) ettől az ellenféltől
    //        int dist = Mathf.Max(Mathf.Abs(tile.x - targetPos.x), Mathf.Abs(tile.y - targetPos.y));

    //        if (dist <= attacker.attackRange)
    //        {
    //            // Találtunk egy elérhető mezőt ahonnan támadhatunk
    //            // A legjobb mező kiválasztása a legkisebb mozgási költség alapján
    //            if (reachableTiles[tile] < minCost)
    //            {
    //                minCost = reachableTiles[tile];
    //                bestTile = tile;
    //            }
    //        }
    //    }
    //    return bestTile;
    //}
    public Vector2Int? GetBestAttackPosition(Unit attacker, Vector2Int targetPos)
    {
        var reachableTiles = GetReachableTilesWithCost(attacker);

        if (!reachableTiles.ContainsKey(attacker.position))
            reachableTiles[attacker.position] = 0;

        Vector2Int? bestTile = null;
        float minCost = float.MaxValue; 

        foreach (var tile in reachableTiles.Keys)
        {
            int dist = Mathf.Max(Mathf.Abs(tile.x - targetPos.x), Mathf.Abs(tile.y - targetPos.y));

            if (dist <= attacker.attackRange)
            {
                float cost = reachableTiles[tile];

                if (cost < minCost)
                {
                    minCost = cost;
                    bestTile = tile;
                }
            }
        }
        return bestTile;
    }

    public void TryAttackUnit(Vector2Int attackerPos, Vector2Int targetPos)
    {
        if (!mapData.units.TryGetValue(attackerPos, out Unit attacker) ||
            !mapData.units.TryGetValue(targetPos, out Unit target))
            return;

        if (attacker.ownerId == target.ownerId) return;

        if (!attacker.canAttack) return;

        // Chebyshev távság számítása (8-irányú)
        int distance = Mathf.Max(Mathf.Abs(attackerPos.x - targetPos.x), Mathf.Abs(attackerPos.y - targetPos.y));

        if (distance <= attacker.attackRange)
        {
            attacker.DealDamageToUnit(target);
        }
    }
}
