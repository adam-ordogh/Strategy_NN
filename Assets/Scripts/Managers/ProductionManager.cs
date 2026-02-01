using UnityEngine;
using System.Collections.Generic;
using static Unit;

public class ProductionManager : MonoBehaviour
{
    // Egy egyszerű osztály a gyártási megrendelésekhez
    public class ProductionOrder
    {
        public UnitData template;
        public int turnsRemaining;
    }

    private Dictionary<Building, List<ProductionOrder>> productionQueues = new Dictionary<Building, List<ProductionOrder>>();
    private MapData mapData;
    private UnitManager unitManager;
    private BuildingManager buildingManager;
    private GameManager gameManager;

    public int maxQueueSize = 5;

    public event System.Action<Building, UnitData> OnUnitQueued;
    public event System.Action<Building> OnUnitDequeued;
    public event System.Action OnUnitSpawned;

    public void Initialize(MapData data, UnitManager uManager, BuildingManager bManager, GameManager gameManager)
    {
        this.mapData = data;
        this.unitManager = uManager;
        this.buildingManager = bManager;
        this.gameManager = gameManager;
    }

    public void QueueUnit(Building factory, UnitData unitType)
    {
        PlayerProfile owner = gameManager.GetPlayerProfile(factory.ownerId);

        if (productionQueues.ContainsKey(factory) && productionQueues[factory].Count >= maxQueueSize)
        {
            Debug.LogWarning("Queue is full!");
            return;
        }

        if (owner.availablePopulation < unitType.populationCost)
        {
            Debug.LogWarning("Not enough population capacity!");
            return;
        }

        if (!owner.CanAfford(unitType.goldCost, unitType.woodCost, unitType.foodCost))
        {
            Debug.LogWarning("Not enough resources!");
            return;
        }

        owner.SpendResources(unitType.goldCost, unitType.woodCost, unitType.foodCost);
        owner.queuedPopulation += unitType.populationCost;

        if (!CanProduceUnit(factory)) return;

        if (!productionQueues.ContainsKey(factory))
        {
            productionQueues[factory] = new List<ProductionOrder>();
        }

        int trainingTime = unitType.trainingTime;

        productionQueues[factory].Add(new ProductionOrder
        {
            template = unitType,
            turnsRemaining = trainingTime
        });

        Debug.Log($"Queued {unitType} at {factory.buildingType}. Time: {trainingTime} turns.");

        OnUnitQueued?.Invoke(factory, unitType);
    }

    public void CancelProductionForBuilding(Building building)
    {
        if (!productionQueues.TryGetValue(building, out List<ProductionOrder> queue))
            return;

        PlayerProfile owner = gameManager.GetPlayerProfile(building.ownerId);

        if (queue.Count > 0)
        {
            queue.RemoveAt(0); 
        }

        while (queue.Count > 0)
        {
            UnitData order = queue[0].template;

            owner.gold += order.goldCost;
            owner.wood += order.woodCost;
            owner.food += order.foodCost;
            owner.queuedPopulation -= order.populationCost;
        }

        productionQueues.Remove(building);
        Debug.Log($"Production queue for {building.buildingType} at {building.position} cleared.");

        OnUnitDequeued?.Invoke(building);
    }

    public void CancelSpecificUnit(Building factory, int index)
{
        if (!productionQueues.TryGetValue(factory, out List<ProductionOrder> queue) || index >= queue.Count)
            return;

        // Nem lehet törölni az első elemet, ez lehet ki lesz véve mert így sosem lehet teljesen leállítani a gyártást
        if (index == 0)
        {
            Debug.LogWarning("Cannot cancel unit already in production!");
            return;
        }

        UnitData orderToCancel = queue[index].template;
        PlayerProfile owner = gameManager.GetPlayerProfile(factory.ownerId);
        
        owner.gold += orderToCancel.goldCost;
        owner.wood += orderToCancel.woodCost;
        owner.food += orderToCancel.foodCost;

        owner.queuedPopulation -= orderToCancel.populationCost;

        queue.RemoveAt(index);
    
        OnUnitDequeued?.Invoke(factory);
    }

    public List<ProductionOrder> GetQueueForBuilding(Building b)
    {
        if (productionQueues.ContainsKey(b)) return productionQueues[b];
        return null;
    }

    public void ProcessTurn(int ownerId)
    {
        foreach (var kvp in productionQueues)
        {
            Building building = kvp.Key;

            if (building.ownerId != ownerId) continue;

            List<ProductionOrder> queue = kvp.Value;

            if (queue.Count > 0)
            {
                ProductionOrder currentOrder = queue[0];
                currentOrder.turnsRemaining--;

                if (currentOrder.turnsRemaining <= 0)
                {
                    TrySpawnFinishedUnit(building, queue);
                }
            }
        }
    }

    private void TrySpawnFinishedUnit(Building building, List<ProductionOrder> queue)
    {
        UnitData order = queue[0].template;

        Vector2Int? spawnPos = GetSpawnPosition(building);

        if (spawnPos.HasValue)
        {
            PlayerProfile owner = gameManager.GetPlayerProfile(building.ownerId);

            owner.queuedPopulation -= order.populationCost;
            unitManager.SpawnUnit(order, spawnPos.Value, owner.playerId);

            queue.RemoveAt(0);
            Debug.Log("Unit training complete!");

            OnUnitSpawned?.Invoke();
        }
        else
        {
            Debug.LogWarning("Production halted! No space to spawn unit.");
        }
    }

    public bool CanProduceUnit(Building factory)
    {
        if (factory.buildingType != Building.BuildingType.Barracks) return false;
        return true;
        // Nem nézzük a spawnt itt, mert lehet, hogy lesz hely mire kész a egység
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
        if (buildingManager.GetBuildingAtTile(pos) != null && buildingManager.GetBuildingAtTile(pos).buildingType != Building.BuildingType.Road) return false;
        return true;
    }
}