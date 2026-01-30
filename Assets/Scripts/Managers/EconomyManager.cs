using UnityEngine;

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

    private void RecalculateCapacities(PlayerProfile player)
    {
        player.maxGoldSlots = 0;
        player.maxWoodSlots = 0;
        player.maxFoodSlots = 0;
        player.maxPopulation = 10; // Base pop

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
    }
    // PLACE HOLDEREK TESZTELÉSHEZ
    public void AssignFoodWorkers(PlayerProfile player, bool add)
    {
        if (add)
        {
            if (player.availablePopulation > 0)
                player.assignedFoodWorkers += 1;
        }
        else
        {
            if (player.assignedFoodWorkers > 0)
                player.assignedFoodWorkers -= 1;
        }
    }

    public void AssignWoodWorkers(PlayerProfile player, bool add)
    {        
        if (add)
        {
            if (player.availablePopulation > 0)            
                player.assignedWoodWorkers += 1;
        }
        else
        {
            if(player.assignedWoodWorkers > 0)
            player.assignedWoodWorkers -= 1;
        }        
    }

    public void AssignGoldWorkers(PlayerProfile player, bool add)
    {
        if (add)
        {
            if (player.availablePopulation > 0)
                player.assignedGoldWorkers += 1;
        }
        else
        {
            if (player.assignedGoldWorkers > 0)
                player.assignedGoldWorkers -= 1;
        }
    }
}
