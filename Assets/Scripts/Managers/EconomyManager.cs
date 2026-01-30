using UnityEngine;

public enum ResourceType { Food, Wood, Gold }
public class EconomyManager : MonoBehaviour
{
    public void ProcessTurn(PlayerProfile player)
    {
        // Újraszámolás a kapacitásokra
        // (Ha mondjuk egy épület le lett rombolva, tudjunk róla)
        RecalculateCapacities(player);

        int goldIncome = player.assignedGoldWorkers * 5;
        int woodIncome = player.assignedWoodWorkers * 5;
        int foodIncome = player.assignedFoodWorkers * 5;

        player.gold += goldIncome;
        player.wood += woodIncome;
        player.food += foodIncome;

        // Minden egység és dolgozó elfogyaszt 1 élelmet
        int totalPopulation = player.myUnits.Count + player.assignedGoldWorkers + player.assignedWoodWorkers + player.assignedFoodWorkers;
        player.food -= totalPopulation;

        // Éhezés esetén büntetés (még nem biztos hogy marad)
        if (player.food < 0)
        {
            Debug.LogWarning($"Player {player.playerId} is starving!");
        }

        Debug.Log($"Economy Update P{player.playerId}: +{goldIncome}G +{woodIncome}W. Food Status: {player.food}");
    }

    public void RecalculateCapacities(PlayerProfile player)
    {
        player.maxGoldSlots = 0;
        player.maxWoodSlots = 0;
        player.maxFoodSlots = 0;
        player.maxPopulation = 0; // Base pop

        foreach (var building in player.myBuildings)
        {
            player.maxPopulation += building.data.populationProvided;
            switch(building.data.buildingType)
            {
                case Building.BuildingType.Mine:
                    player.maxGoldSlots += building.data.jobSlotsProvided;
                    break;
                case Building.BuildingType.Lumberyard:
                    player.maxWoodSlots += building.data.jobSlotsProvided;
                    break;
                case Building.BuildingType.Farm:
                    player.maxFoodSlots += building.data.jobSlotsProvided;
                    break;
            }
        }

        player.assignedGoldWorkers = Mathf.Clamp(player.assignedGoldWorkers, 0, player.maxGoldSlots);
        player.assignedWoodWorkers = Mathf.Clamp(player.assignedWoodWorkers, 0, player.maxWoodSlots);
        player.assignedFoodWorkers = Mathf.Clamp(player.assignedFoodWorkers, 0, player.maxFoodSlots);

        // Ha netán max levesz dolgozákat, megnézni hgoy vissza-e kapjuk elérhető populáció ként
    }
    public bool ChangeWorkerAssignment(PlayerProfile player, ResourceType resource, int amount)
    {
        if (amount > 0)
        {
            if (player.availablePopulation < amount) return false;

            switch (resource)
            {
                case ResourceType.Food:
                    if (player.assignedFoodWorkers + amount > player.maxFoodSlots) return false;
                    player.assignedFoodWorkers += amount;
                    break;
                case ResourceType.Wood:
                    if (player.assignedWoodWorkers + amount > player.maxWoodSlots) return false;
                    player.assignedWoodWorkers += amount;
                    break;
                case ResourceType.Gold:
                    if (player.assignedGoldWorkers + amount > player.maxGoldSlots) return false;
                    player.assignedGoldWorkers += amount;
                    break;
            }
        }
        else
        {
            int absoluteAmount = Mathf.Abs(amount);
            switch (resource)
            {
                case ResourceType.Food:
                    if (player.assignedFoodWorkers < absoluteAmount) return false;
                    player.assignedFoodWorkers -= absoluteAmount;
                    break;
                case ResourceType.Wood:
                    if (player.assignedWoodWorkers < absoluteAmount) return false;
                    player.assignedWoodWorkers -= absoluteAmount;
                    break;
                case ResourceType.Gold:
                    if (player.assignedGoldWorkers < absoluteAmount) return false;
                    player.assignedGoldWorkers -= absoluteAmount;
                    break;
            }
        }

        return true;
    }
}
