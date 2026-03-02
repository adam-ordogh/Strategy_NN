using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class PlayerProfile
{
    public int playerId;
    public bool isAi;

    public List<Unit> myUnits = new List<Unit>();
    public List<Building> myBuildings = new List<Building>();

    // Nyersanyagok
    public int gold;
    public int wood;
    public int food;

    public int maxGold = 0;
    public int maxWood = 0;
    public int maxFood = 0;

    public int baseGoldIncome = 2;
    public int baseWoodIncome = 2;
    public int baseFoodIncome = 2;

    public int currentGoldWorkers;
    public int currentWoodWorkers;
    public int currentFoodWorkers;

    // Populáció és dolgozók
    public int currentPopulation = 3;
    public int housingCapacity;

    public int queuedPopulation;

    public int CurrentUsedPopulation
    {
        get
        {
            int unitPop = myUnits.Sum(u => u.data.populationCost);
            int workerPop = myBuildings.Sum(b => b.assignedWorkers);
            return unitPop + workerPop + queuedPopulation;
        }
    }
    public int availablePopulation => currentPopulation - CurrentUsedPopulation;


    public event System.Action OnResourcesChanged;

    public bool CanAfford(int goldCost, int woodCost, int foodCost)
    {
        return gold >= goldCost && wood >= woodCost && food >= foodCost;
    }

    public void SpendResources(int goldCost, int woodCost, int foodCost)
    {
        gold -= goldCost;
        wood -= woodCost;
        food -= foodCost;
        OnResourcesChanged?.Invoke();
    }

    public void PrintResourceStatus()
    {
        Debug.Log($"Player {playerId} - Gold: {gold}/{maxGold}, Wood: {wood}/{maxWood}, Food: {food}/{maxFood}, Population: {currentPopulation}/{housingCapacity} (Used: {CurrentUsedPopulation})");
    }
}
