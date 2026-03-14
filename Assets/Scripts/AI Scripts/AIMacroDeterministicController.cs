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

public class AIMacroDeterministic : IAIController
{
    public int PlayerId { get; private set; }
    private GameManager gameManager;
    private PlayerProfile myProfile;
    private AIMicroController micro;
    public MilitaryState currentArmyState = MilitaryState.Gathering;

    public void Initialize(GameManager gameManager)
    {
        this.gameManager = gameManager;
        this.myProfile = gameManager.GetPlayerProfile(PlayerId);
        this.micro = new AIMicroController(PlayerId, gameManager);
    }

    public AIMacroDeterministic(int playerId)
    {
        PlayerId = playerId;
    }

    public void ExecuteTurn()
    {
        myProfile.PrintResourceStatus();
        micro.RefreshProfile();

        AIGoal currentGoal = DetermineMacroGoal();
        ExecuteMicroActions(currentGoal);

        // GameManager will call NextTurn() after this
        gameManager.NextTurn();
    }

    private AIGoal DetermineMacroGoal()
    {
        var income = gameManager.economyManager.GetProjectedIncome(myProfile);

        if (income.foodNet < 2) return AIGoal.FocusEconomy;
        if (micro.IsEnemyNearBase(15f)) return AIGoal.FocusMilitary;

        float economyScore = CalculateEconomyDesire(income);
        float militaryScore = CalculateMilitaryDesire();
        float expansionScore = CalculateExpansionDesire();

        //Debug.Log($"[AI {PlayerId}] Desire Scores - Economy: {economyScore}, Military: {militaryScore}, Expansion: {expansionScore}");

        if (expansionScore >= economyScore && expansionScore >= militaryScore)
            return AIGoal.FocusExpansion;

        if (militaryScore >= economyScore)
            return AIGoal.FocusMilitary;

        return AIGoal.FocusEconomy;
    }

    private void ExecuteMicroActions(AIGoal goal)
    {
        switch (goal)
        {
            case AIGoal.FocusEconomy:
                micro.ExecuteEconomyMicro();
                Debug.Log("---------DETERMINISTIC AI FOCUSING ON ECONOMY THIS TURN!---------");
                break;
            case AIGoal.FocusMilitary:
                micro.ExecuteMilitaryMicro();
                Debug.Log("---------DETERMINISTIC AI FOCUSING ON MILITARY THIS TURN!---------");
                break;
            case AIGoal.FocusExpansion:
                micro.ExecuteExpansionMicro();
                Debug.Log("---------DETERMINISTIC AI FOCUSING ON EXPANSION THIS TURN!---------");
                break;
        }

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