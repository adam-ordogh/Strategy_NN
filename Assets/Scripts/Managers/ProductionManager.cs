using UnityEngine;
using System.Collections.Generic;

public class ProductionManager : MonoBehaviour
{
    private MapData mapData;
    private UnitManager unitManager;
    private BuildingManager buildingManager;

    // Egy egyszerű osztály a gyártási megrendelésekhez
    public class ProductionOrder
    {
        public Unit.UnitType unitType;
        public int turnsRemaining;
    }

    private Dictionary<Building, Queue<ProductionOrder>> productionQueues = new Dictionary<Building, Queue<ProductionOrder>>();

    public void Initialize(MapData data, UnitManager uManager, BuildingManager bManager)
    {
        this.mapData = data;
        this.unitManager = uManager;
        this.buildingManager = bManager;
    }

    public void QueueUnit(Building factory, Unit.UnitType unitType)
    {
        if (!CanProduceUnit(factory, unitType)) return;

        if (!productionQueues.ContainsKey(factory))
        {
            productionQueues[factory] = new Queue<ProductionOrder>();
        }

        int trainingTime = GetTrainingTime(unitType);

        productionQueues[factory].Enqueue(new ProductionOrder
        {
            unitType = unitType,
            turnsRemaining = trainingTime
        });

        Debug.Log($"Queued {unitType} at {factory.buildingType}. Time: {trainingTime} turns.");
    }

    public void ProcessTurn(int ownerId)
    {
        foreach (var kvp in productionQueues)
        {
            Building building = kvp.Key;

            if (building.ownerId != ownerId) continue;

            Queue<ProductionOrder> queue = kvp.Value;

            if (queue.Count > 0)
            {
                ProductionOrder currentOrder = queue.Peek();
                currentOrder.turnsRemaining--;

                if (currentOrder.turnsRemaining <= 0)
                {
                    TrySpawnFinishedUnit(building, queue);
                }
            }
        }
    }

    private void TrySpawnFinishedUnit(Building building, Queue<ProductionOrder> queue)
    {
        ProductionOrder order = queue.Peek();

        Vector2Int? spawnPos = GetSpawnPosition(building);

        if (spawnPos.HasValue)
        {
            unitManager.SpawnUnit(order.unitType, spawnPos.Value, building.ownerId);

            queue.Dequeue();
            Debug.Log("Unit training complete!");
        }
        else
        {
            Debug.LogWarning("Production halted! No space to spawn unit.");
        }
    }

    public bool CanProduceUnit(Building factory, Unit.UnitType unitType)
    {
        if (factory.buildingType != Building.BuildingType.Barracks) return false;
        return true;
        // Nem nézzük a spawnt itt, mert lehet, hogy lesz hely mire kész a egység
    }

    private int GetTrainingTime(Unit.UnitType type)
    {
        switch (type)
        {
            case Unit.UnitType.Archer: return 2;
            case Unit.UnitType.Cavalry: return 3;
            default: return 1; // Soldier
        }
    }

    private Vector2Int? GetSpawnPosition(Building factory)
    {
        foreach (Vector2Int tile in factory.GetOccupiedTiles())
        {
            Vector2Int[] neighbors = {
                tile + Vector2Int.up, tile + Vector2Int.down,
                tile + Vector2Int.left, tile + Vector2Int.right
            };
            foreach (var n in neighbors)
            {
                if (IsTileEmpty(n)) return n;
            }
        }
        return null;
    }

    private bool IsTileEmpty(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= mapData.mapWidth || pos.y < 0 || pos.y >= mapData.mapHeight) return false;
        if (!mapData.mapTiles[pos.x, pos.y].isPassable) return false;
        if (mapData.units.ContainsKey(pos)) return false;
        if (buildingManager.GetBuildingAtTile(pos) != null) return false;
        return true;
    }
}