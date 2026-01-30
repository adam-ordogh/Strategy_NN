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

    public int totalPopulation;
    public int assignedGoldWorkers;
    public int assignedWoodWorkers;
    public int assignedFoodWorkers;
    public int queuedPopulation;

    public int CurrentUsedPopulation
    {
        get
        {
            int unitPop = 0;
            foreach (var u in myUnits) unitPop += u.data.populationCost;

            int workerPop = assignedGoldWorkers + assignedWoodWorkers + assignedFoodWorkers;

            return unitPop + workerPop + queuedPopulation;
        }
    }
    public int availablePopulation => maxPopulation - CurrentUsedPopulation;

    public int maxGoldSlots;
    public int maxWoodSlots;
    public int maxFoodSlots;
    public int maxPopulation;
}
