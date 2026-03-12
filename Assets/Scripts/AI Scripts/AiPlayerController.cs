using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum AIGoal
{
    FocusEconomy,
    FocusMilitary,
    FocusExpansion
}

public enum MilitaryState { Defending, Gathering, Attacking }

public class AiPlayerController
{
    public int playerId;
    private GameManager gameManager;
    private PlayerProfile myProfile;

    private AIGoal currentGoal;
    public MilitaryState currentArmyState = MilitaryState.Gathering;

    // Micro controller handles all detailed execution
    private AIMicroController micro;

    public AiPlayerController(int id, GameManager init)
    {
        playerId = id;
        gameManager = init;
        myProfile = gameManager.GetPlayerProfile(playerId);

        // Initialize micro controller
        micro = new AIMicroController(playerId, gameManager);
    }

    public void ExecuteTurn()
    {
        myProfile.PrintResourceStatus();

        // Refresh micro controller's profile reference
        micro.RefreshProfile();

        // 1. MACRO: Decide what to focus on
        AIGoal currentGoal = DetermineMacroGoal();

        // 2. MICRO: Execute based on that goal
        ExecuteMicroActions(currentGoal);

        gameManager.NextTurn();
    }

    private AIGoal DetermineMacroGoal()
    {
        var income = gameManager.economyManager.GetProjectedIncome(myProfile);

        // Emergency overrides
        if (income.foodNet < 2) return AIGoal.FocusEconomy;
        if (micro.IsEnemyNearBase(15f)) return AIGoal.FocusMilitary;

        // Calculate desire scores
        float economyScore = CalculateEconomyDesire(income);
        float militaryScore = CalculateMilitaryDesire();
        float expansionScore = CalculateExpansionDesire();

        Debug.Log($"[AI {playerId}] Desire Scores - Economy: {economyScore}, Military: {militaryScore}, Expansion: {expansionScore}");

        if (expansionScore >= economyScore && expansionScore >= militaryScore)
            return AIGoal.FocusExpansion;

        if (militaryScore >= economyScore)
            return AIGoal.FocusMilitary;

        return AIGoal.FocusEconomy;
    }

    private void ExecuteMicroActions(AIGoal goal)
    {
        // Execute goal-specific micro
        switch (goal)
        {
            case AIGoal.FocusEconomy:
                micro.ExecuteEconomyMicro();
                Debug.Log("---------FOCUSING ON ECONOMY THIS TURN!---------");
                break;
            case AIGoal.FocusMilitary:
                micro.ExecuteMilitaryMicro();
                Debug.Log("---------FOCUSING ON MILITARY THIS TURN!---------");
                break;
            case AIGoal.FocusExpansion:
                micro.ExecuteExpansionMicro();
                Debug.Log("---------FOCUSING ON EXPANSION THIS TURN!---------");
                break;
        }

        // Always execute these
        micro.ExecuteRoadMicro();
        micro.HandleUnitMicro(currentArmyState, ref currentArmyState);
    }

    // ==================== DESIRE CALCULATIONS ====================

    private float CalculateEconomyDesire(IncomeReport income)
    {
        float score = 0;

        if (myProfile.gold < 50) score += 60;
        else if (myProfile.gold < 150) score += 20;

        if (myProfile.wood < 50) score += 60;
        else if (myProfile.wood < 150) score += 20;

        if (myProfile.availablePopulation < 3) score += 40;

        int realBuildingCount = myProfile.myBuildings.Count(b =>
            b.buildingType != Building.BuildingType.Road &&
            b.buildingType != Building.BuildingType.Warehouse);

        if (income.goldNet < realBuildingCount * 2)
        {
            score += 40;
        }

        return score;
    }

    private float CalculateMilitaryDesire()
    {
        float score = 20;
        float myStrength = micro.CalculateMilitaryStrength();
        float observedEnemyStrength = micro.GetObservedEnemyStrength();

        if (myStrength < observedEnemyStrength * 1.2f)
        {
            score += 50;
            score += (observedEnemyStrength * 0.5f);
        }

        float desiredStrength = myProfile.myBuildings.Count * 5f;
        if (myStrength < desiredStrength)
        {
            score += 30;
        }

        if (myProfile.gold > 150) score += 20;
        if (myProfile.gold > 300) score += 30;

        return score;
    }

    private float CalculateExpansionDesire()
    {
        float score = 0;
        score += (myProfile.gold + myProfile.wood) / 20f;

        if (micro.CalculateMilitaryStrength() > 100) score += 20;
        if (!micro.CanPlaceIndustrialBuilding()) score += 100;

        return score;
    }
}