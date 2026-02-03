using System.Collections.Generic;
using System;
using UnityEngine;

public enum ResourceType { Food, Wood, Gold }
public struct IncomeReport
{
    public int goldNet;
    public int woodNet;
    public int foodNet;
}

public class EconomyManager : MonoBehaviour
{
    // Könyvtár a különböző épülettípusok termelési görbéihez
    private Dictionary<Building.BuildingType, Func<int, int>> productionCurves =
        new Dictionary<Building.BuildingType, Func<int, int>>();

    void Start()
    {
        productionCurves[Building.BuildingType.Mine] = (workers) =>
        {
            // Mine: Erős kezdet, gyors csökkenés
            if (workers <= 0) return 0;
            if (workers == 1) return 4;  // Erős kezdés
            if (workers == 2) return 6;  // +2
            if (workers == 3) return 7;  // +1
            return 7;  // Cap at 7
        };

        productionCurves[Building.BuildingType.Lumberyard] = (workers) =>
        {
            // Lumberyard: Lineáris de jobb skálázódás
            if (workers <= 0) return 0;
            if (workers == 1) return 3;
            if (workers == 2) return 6;  // +3 
            if (workers == 3) return 9;  // +3 
            return 9 + (workers - 3);  // Lineáris 3 után
        };

        productionCurves[Building.BuildingType.Farm] = (workers) =>
        {
            // Farm: Exponenciális növekedés (öntözést/együttműködést reprezentálja)
            if (workers <= 0) return 0;
            if (workers == 1) return 2;  // Alacsony kezdet
            if (workers == 2) return 5;  // +3
            if (workers == 3) return 9;  // +4
            if (workers == 4) return 14; // +5
            return 14 + (workers - 4) * 2; // Lineáris 4 után
        };
    }

    private int GetProductionFromWorkers(Building building)
    {
        if (productionCurves.TryGetValue(building.buildingType, out var curve))
        {
            return curve(building.assignedWorkers);
        }

        // Alap visszatérés, ha nincs definiált görbe
        return CalculateDefaultProduction(building.assignedWorkers);
    }

    private int CalculateDefaultProduction(int workerCount)
    {
        if (workerCount <= 0) return 0;
        if (workerCount == 1) return 3;
        if (workerCount == 2) return 5;
        return 6;
    }

    public void ProcessTurn(PlayerProfile player)
    {
        RecalculateCapacities(player);

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

        int totalConsumption = player.myUnits.Count + totalWorkers;

        player.gold = Mathf.Clamp(player.gold + player.baseGoldIncome + totalGoldGenerated, 0, player.maxGold);
        player.wood = Mathf.Clamp(player.wood + player.baseWoodIncome + totalWoodGenerated, 0, player.maxWood);
        player.food = Mathf.Clamp(player.food + (player.baseFoodIncome + totalFoodGenerated - totalConsumption), 0, player.maxFood);
    }

    public void RecalculateCapacities(PlayerProfile player)
    {
        player.currentGoldWorkers = 0;
        player.currentWoodWorkers = 0;
        player.currentFoodWorkers = 0;
        player.maxGold = 0;
        player.maxWood = 0;
        player.maxFood = 0;
        player.maxPopulation = 0; // Base pop

        foreach (var building in player.myBuildings)
        {
            if(building.isConstructed == false) continue;

            player.maxPopulation += building.data.populationProvided;
            switch(building.data.buildingType)
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

        int totalConsumption = player.myUnits.Count + totalWorkers;

        return new IncomeReport
        {
            goldNet = player.baseGoldIncome + totalGoldGenerated,
            woodNet = player.baseWoodIncome + totalWoodGenerated,
            foodNet = player.baseFoodIncome + totalFoodGenerated - totalConsumption
        };
    }
}
