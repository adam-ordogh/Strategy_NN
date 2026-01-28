using UnityEngine;
using System.Collections.Generic;

public class UnitManager : MonoBehaviour
{
    public MapData mapData;

    public event System.Action<Unit, List<Vector2Int>> OnUnitMoved;
    public event System.Action<Vector2Int> OnUnitDestroyed;

    public void Initialize(MapData mapData)
    {
        this.mapData = mapData;
    }

    public void CreateUnit(Unit unit)
    {
        // Jelenleg nem nezi meg hogy a mezo foglalt-e
        unit.OnUnitDeath += HandleUnitDeath;
        mapData.units[unit.position] = unit;
    }

    public void HandleUnitDeath(Unit unit) 
    { 
        mapData.units.Remove(unit.position);
        OnUnitDestroyed?.Invoke(unit.position);
        Debug.Log($"Unit at {unit.position} has died and was removed.");
    }
    

    public void ResetUnitsForNewTurn(int currentPlayerId)
    {
        foreach (var unit in mapData.units)
        { 
            if(unit.Value.ownerId != currentPlayerId)
                continue;

            unit.Value.ResetActions();
        }
    }

    // ---------------------------- MOZGÁS FÜGGVÉNYEK ----------------------------
    public Dictionary<Vector2Int, int> GetReachableTilesWithCost(Unit unit)
    {
        var reachable = new Dictionary<Vector2Int, int>();
        // Könyvtárban tárolt minimális költség egy mező eléréséhez
        var minCostToReach = new Dictionary<Vector2Int, int>();

        var queue = new Queue<(Vector2Int pos, int cost)>();

        queue.Enqueue((unit.position, 0));
        minCostToReach[unit.position] = 0;

        while (queue.Count > 0)
        {
            var (currentPos, currentCost) = queue.Dequeue();

            // Ha már találtunk olcsóbb utat ide, kihagyjuk
            if (currentCost > minCostToReach[currentPos]) continue;

            if (currentPos != unit.position)
                reachable[currentPos] = currentCost;

            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var nextPos = currentPos + dir;

                if (!IsTileValidForMovement(nextPos)) continue;

                int moveCost = 1;
                if (IsTileThreatened(nextPos, unit.ownerId))
                {
                    moveCost = 3;
                }

                int nextCost = currentCost + moveCost;
                if (nextCost > unit.remainingMovementPoints) continue;

                if (!minCostToReach.ContainsKey(nextPos) || nextCost < minCostToReach[nextPos])
                {
                    minCostToReach[nextPos] = nextCost;
                    queue.Enqueue((nextPos, nextCost));
                }
            }
        }

        return reachable;
    }

    public void TryMoveUnit(Vector2Int fromPos, Vector2Int toPos)
    {
        if (!mapData.units.TryGetValue(fromPos, out Unit unit)) return;

        List<Vector2Int> path = GetPathToTarget(unit, toPos);
        if (path == null) return; 

        int moveCost = path.Count - 1;
        unit.remainingMovementPoints -= moveCost;
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
    public List<Vector2Int> GetPathToTarget(Unit unit, Vector2Int targetPos)
    {
        // Lista z egyszerű prioritásos sor helyett
        // Mindig a legolcsóbb csomópontot választjuk ki először
        var openList = new List<(Vector2Int pos, int cost)>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var costSoFar = new Dictionary<Vector2Int, int>();

        openList.Add((unit.position, 0));
        costSoFar[unit.position] = 0;

        while (openList.Count > 0)
        {
            // DIJKSTRA LÉPS
            openList.Sort((a, b) => a.cost.CompareTo(b.cost));
            var current = openList[0].pos;
            openList.RemoveAt(0);

            if (current == targetPos)
                return ReconstructPath(cameFrom, current);

            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var next = current + dir;
                if (!IsTileValidForMovement(next)) continue;

                // Van-e ellenség az útban?
                // Ignoráljuk az ellenséges egységet a targetPos-nál ha ő maga a célpont
                int moveCost = 1;
                if (IsTileThreatened(next, unit.ownerId))
                {
                    moveCost = 3;
                }

                int newCost = costSoFar[current] + moveCost;
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

    public Vector2Int? GetBestAttackPosition(Unit attacker, Vector2Int targetPos)
    {
        var reachableTiles = GetReachableTilesWithCost(attacker);
        reachableTiles[attacker.position] = 0;

        Vector2Int? bestTile = null;
        int minCost = int.MaxValue;

        foreach (var tile in reachableTiles.Keys)
        {
            // Chebyshev távolság számítása (8-irányú) ettől az ellenféltől
            int dist = Mathf.Max(Mathf.Abs(tile.x - targetPos.x), Mathf.Abs(tile.y - targetPos.y));

            if (dist <= attacker.attackRange)
            {
                // Találtunk egy elérhető mezőt ahonnan támadhatunk
                // A legjobb mező kiválasztása a legkisebb mozgási költség alapján
                if (reachableTiles[tile] < minCost)
                {
                    minCost = reachableTiles[tile];
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
