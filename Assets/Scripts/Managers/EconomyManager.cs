using System.Collections.Generic;
using System;
using UnityEngine;
using static MapData;
using UnityEditor.U2D.Aseprite;

public enum ResourceType { Food, Wood, Gold }
public struct IncomeReport
{
    public int goldNet;
    public int woodNet;
    public int foodNet;
}

public class EconomyManager : MonoBehaviour
{
    private MapData mapData;
    private BuildingManager buildingManager;

    public void Initialize(MapData mapData, BuildingManager buildingManager)
    {
        this.mapData = mapData;
        this.buildingManager = buildingManager;
    }
    private int GetProductionFromWorkers(Building building)
    {
        //return building.data.GetWorkerOutput(building.assignedWorkers); 
        int baseOutput = building.data.GetWorkerOutput(building.assignedWorkers);
        if (baseOutput == 0) return 0;

        int finalOutput = baseOutput;

        // Környezeti bónuszok
        if (building.buildingType == Building.BuildingType.Lumberyard)
        {
            int forestCount = CountAdjacentTiles(building.position, TileType.Forest);
            // Minden 2 erdőszomszéd után +1 bónusz termelés dolgozónként
            int bonusPerWorker = forestCount / 2;
            finalOutput += (bonusPerWorker * building.assignedWorkers);
        }

        // Út csatlakozás bónusz (ha csatlakozik a városházához)
        if (building.isConnectedToCapital)
        {
            finalOutput += 1;
        }

        return finalOutput;
    }


    public void ProcessTurn(PlayerProfile player)
    {
        RecalculateRoadNetwork(player);
        RecalculateCapacities(player);

        IncomeReport report = GetProjectedIncome(player);

        if (report.foodNet > 0)
        {
            if (player.currentPopulation < 5)
            {
                player.currentPopulation += 1;
            }
            else
            {
                int newPeople = report.foodNet / 5;
                player.currentPopulation = Mathf.Min(player.currentPopulation + newPeople, player.housingCapacity);
            }
        }
        else if (report.foodNet < 0)
        {
            int deaths = Mathf.Max(1, Mathf.Abs(report.foodNet) / 5);
            player.currentPopulation = Mathf.Max(0, player.currentPopulation - deaths);

            // Ha a populáció csökken, ellenőrizzük a dolgozókat és egységeket, esetleg meghalhatnak?
        }
        if (player.availablePopulation > (player.currentPopulation * 0.5f))
        {
            report.goldNet -= 5;
            Debug.Log("High unemployment! Losing gold.");
        }

        //player.gold = Mathf.Clamp(player.gold + report.goldNet, 0, player.maxGold);
        //player.wood = Mathf.Clamp(player.wood + report.woodNet, 0, player.maxWood);
        //player.food = Mathf.Clamp(player.food + report.foodNet, 0, player.maxFood);
        bool hasGlobalStorage = player.maxGold > 0; // Or check for a TownHall specifically

        if (hasGlobalStorage)
        {
            player.gold = Mathf.Clamp(player.gold + report.goldNet, 0, player.maxGold);
            player.wood = Mathf.Clamp(player.wood + report.woodNet, 0, player.maxWood);
            player.food = Mathf.Clamp(player.food + report.foodNet, 0, player.maxFood);
        }
        else
        {
            // No storage buildings? Just add the income without capping it.
            player.gold += report.goldNet;
            player.wood += report.woodNet;
            player.food += report.foodNet;
        }

    }

    public void RecalculateCapacities(PlayerProfile player)
    {
        player.currentGoldWorkers = 0;
        player.currentWoodWorkers = 0;
        player.currentFoodWorkers = 0;
        player.maxGold = 0;
        player.maxWood = 0;
        player.maxFood = 0;
        player.housingCapacity = 0;

        foreach (var building in player.myBuildings)
        {
            if(building.isConstructed == false) continue;

            player.housingCapacity += building.data.populationProvided;

            switch (building.data.buildingType)
            {
                case Building.BuildingType.Mine:
                    player.currentGoldWorkers += building.assignedWorkers;
                    break;
                case Building.BuildingType.Lumberyard:
                    player.currentWoodWorkers += building.assignedWorkers;
                    break;
                case Building.BuildingType.Farm:
                    player.currentFoodWorkers += building.assignedWorkers;
                    break;
                case Building.BuildingType.Warehouse:
                    player.maxGold += building.data.storageProvided;
                    player.maxWood += building.data.storageProvided;
                    player.maxFood += building.data.storageProvided;
                    break;
                case Building.BuildingType.TownHall:
                    player.maxGold += building.data.storageProvided;
                    player.maxWood += building.data.storageProvided;
                    player.maxFood += building.data.storageProvided;
                    break;
            }
        }
    }

    public IncomeReport GetProjectedIncome(PlayerProfile player)
    {
        RecalculateRoadNetwork(player);

        int totalGoldGenerated = 0;
        int totalWoodGenerated = 0;
        int totalFoodGenerated = 0;
        int totalWorkers = 0;

        foreach (Building b in player.myBuildings)
        {
            if (!b.isConstructed) continue;

            int production = GetProductionFromWorkers(b);
            totalWorkers += b.assignedWorkers;

            switch (b.buildingType)
            {
                case Building.BuildingType.Mine:
                    totalGoldGenerated += production;
                    break;
                case Building.BuildingType.Lumberyard:
                    totalWoodGenerated += production;
                    break;
                case Building.BuildingType.Farm:
                    totalFoodGenerated += production;
                    break;
            }
        }

        int unitUpkeep = 0;
        foreach (var unit in player.myUnits)
        {
            unitUpkeep += unit.data.goldUpkeep;
        }

        int totalConsumption = player.myUnits.Count + totalWorkers;

        return new IncomeReport
        {
            goldNet = player.baseGoldIncome + totalGoldGenerated - unitUpkeep,
            woodNet = player.baseWoodIncome + totalWoodGenerated,
            foodNet = player.baseFoodIncome + totalFoodGenerated - totalConsumption
        };
    }

    private void RecalculateRoadNetwork(PlayerProfile player)
    {
        foreach (var b in player.myBuildings) b.isConnectedToCapital = false;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Minden városházát hozzáadunk a kezdőpontokhoz
        foreach (var b in player.myBuildings)
        {
            if (b.buildingType == Building.BuildingType.TownHall && b.isConstructed)
            {
                foreach (var tile in b.GetOccupiedTiles())
                {
                    if (!visited.Contains(tile))
                    {
                        queue.Enqueue(tile);
                        visited.Add(tile);
                    }
                }
                b.isConnectedToCapital = true;
            }
        }

        // Flood Fill
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (var dir in Pathfinder.Directions)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next) || !IsInsideMap(next)) continue;

                Building b = buildingManager.GetBuildingAtTile(next);
                                
                if (b != null && b.isConstructed && b.ownerId == player.playerId)
                {
                    visited.Add(next);
                    b.isConnectedToCapital = true;

                    if (b.buildingType == Building.BuildingType.Road)
                    {
                        queue.Enqueue(next);
                    }
                    else
                    {
                        foreach (var t in b.GetOccupiedTiles()) visited.Add(t);
                    }
                }
            }
        }
    }

    private bool IsInsideMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mapData.mapWidth &&
               pos.y >= 0 && pos.y < mapData.mapHeight;
    }

    private int CountAdjacentTiles(Vector2Int center, TileType type)
    {
        int count = 0;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector2Int checkPos = new Vector2Int(center.x + x, center.y + y);

                if (checkPos.x < 0 || checkPos.x >= mapData.mapWidth ||
                    checkPos.y < 0 || checkPos.y >= mapData.mapHeight) continue;

                if (mapData.mapTiles[checkPos.x, checkPos.y].type == type)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
