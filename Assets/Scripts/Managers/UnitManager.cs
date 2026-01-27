using UnityEngine;
using System.Collections.Generic;

public class UnitManager : MonoBehaviour
{
    public MapData mapData;

    public event System.Action<Unit, Vector2Int, Vector2Int> OnUnitMoved;
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

    // Mozgás függvények ----------------------------
    public void TryMoveUnit(Vector2Int fromPos, Vector2Int toPos)
    {
        if (!mapData.units.TryGetValue(fromPos, out Unit unit))
        {
            Debug.Log("Nincs egység a kiinduló pozíción.");
            return;
        }

        var reachable = GetReachableTilesWithCost(unit);

        if (!reachable.ContainsKey(toPos))
        {
            Debug.Log("A célpozíció nem elérhető a megadott mozgáspontokkal.");
            return;
        }

        int moveCost = reachable[toPos];
        unit.remainingMovementPoints -= moveCost;

        unit.Move(toPos);
        mapData.units.Remove(fromPos);
        mapData.units[toPos] = unit;

        OnUnitMoved?.Invoke(unit, fromPos, toPos);
    }

    public Dictionary<Vector2Int, int> GetReachableTilesWithCost(Unit unit)
    {
        var reachable = new Dictionary<Vector2Int, int>();
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int cost)>();

        queue.Enqueue((unit.position, 0));
        visited.Add(unit.position);

        while (queue.Count > 0)
        {
            var (currentPos, currentCost) = queue.Dequeue();

            if (currentPos != unit.position)
                reachable[currentPos] = currentCost;

            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var nextPos = currentPos + dir;
                if (visited.Contains(nextPos)) continue;
                if (!IsTileValidForMovement(nextPos)) continue;

                int nextCost = currentCost + 1; // Replace with tile cost if needed
                if (nextCost > unit.remainingMovementPoints) continue;

                visited.Add(nextPos);
                queue.Enqueue((nextPos, nextCost));
            }
        }

        return reachable;
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

    // Támadás függvények ----------------------------
    public struct AttackCommand
    {
        public Vector2Int TargetPos;
        public Vector2Int StandPos; // The best tile to move to in order to attack
    }

    public List<AttackCommand> GetReachableEnemies(Unit unit)
    {
        List<AttackCommand> validTargets = new List<AttackCommand>();

        // 1. Get all tiles the unit can actually walk to
        var reachableTiles = GetReachableTilesWithCost(unit).Keys;
        List<Vector2Int> allStandableTiles = new List<Vector2Int>(reachableTiles);
        allStandableTiles.Add(unit.position); // Include standing still

        // 2. We only need to check units that are NOT on our team
        foreach (var kvp in mapData.units)
        {
            Vector2Int enemyPos = kvp.Key;
            Unit enemyUnit = kvp.Value;

            if (enemyUnit.ownerId == unit.ownerId) continue;

            // 3. For this specific enemy, is there ANY tile we can stand on to hit them?
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
                    break; // Found a way to hit this enemy, move to the next enemy
                }
            }
        }

        return validTargets;
    }

    public Vector2Int? GetBestAttackPosition(Unit attacker, Vector2Int targetPos)
    {
        var reachableTiles = GetReachableTilesWithCost(attacker);
        // Add the current position too, in case they are already in range
        reachableTiles[attacker.position] = 0;

        Vector2Int? bestTile = null;
        int minCost = int.MaxValue;

        foreach (var tile in reachableTiles.Keys)
        {
            // Chebyshev distance from THIS reachable tile to the ENEMY
            int dist = Mathf.Max(Mathf.Abs(tile.x - targetPos.x), Mathf.Abs(tile.y - targetPos.y));

            if (dist <= attacker.attackRange)
            {
                // We found a tile we can move to and hit the enemy!
                // We pick the one with the lowest movement cost
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

        // Chebyshev Distance for 8-directional range
        int distance = Mathf.Max(Mathf.Abs(attackerPos.x - targetPos.x), Mathf.Abs(attackerPos.y - targetPos.y));

        if (distance <= attacker.attackRange)
        {
            attacker.DealDamageToUnit(target);
        }
    }
}
