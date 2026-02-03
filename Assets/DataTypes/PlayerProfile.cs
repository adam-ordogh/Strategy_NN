using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerProfile
{
    public int playerId;
    public bool isAi;

    public List<Unit> myUnits = new List<Unit>();
    public List<Building> myBuildings = new List<Building>();

    public int gold;
    public int wood;
    public int food;

    public int maxGold = 0;
    public int maxWood = 0;
    public int maxFood = 0;

    public int baseGoldIncome = 2;
    public int baseWoodIncome = 2;
    public int baseFoodIncome = 2;

    public int totalPopulation;
    public int currentGoldWorkers;
    public int currentWoodWorkers;
    public int currentFoodWorkers;
    public int queuedPopulation;

    public int CurrentUsedPopulation
    {
        get
        {
            int unitPop = 0;
            foreach (var u in myUnits) unitPop += u.data.populationCost;

            int workerPop = 0;
            foreach (var b in myBuildings) workerPop += b.assignedWorkers;

            return unitPop + workerPop + queuedPopulation;
        }
    }
    public int availablePopulation => maxPopulation - CurrentUsedPopulation;

    //public int currentGoldSlot;
    //public int maxWoodSlots;
    //public int maxFoodSlots;
    public int maxPopulation;

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
}
