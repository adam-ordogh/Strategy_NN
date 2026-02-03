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
    private int GetProductionFromWorkers(Building building)
    {       
       return building.data.GetWorkerOutput(building.assignedWorkers); 
    }


    public void ProcessTurn(PlayerProfile player)
    {
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

        player.gold = Mathf.Clamp(player.gold + report.goldNet, 0, player.maxGold);
        player.wood = Mathf.Clamp(player.wood + report.woodNet, 0, player.maxWood);
        player.food = Mathf.Clamp(player.food + report.foodNet, 0, player.maxFood);

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
}
