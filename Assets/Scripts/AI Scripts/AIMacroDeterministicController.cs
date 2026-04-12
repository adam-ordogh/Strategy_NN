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

    private int stuckCounter = 0;
    private AIGoal lastGoal = AIGoal.FocusEconomy;

    public AIMacroDeterministic(int playerId)
    {
        PlayerId = playerId;
    }

    public string GetAITypeName() => "Deterministic";

    public void Initialize(GameManager gameManager)
    {
        this.gameManager = gameManager;
        this.myProfile = gameManager.GetPlayerProfile(PlayerId);
        this.micro = new AIMicroController(PlayerId, gameManager);
    }


    public void ExecuteTurn()
    {
        //myProfile.PrintResourceStatus();
        micro.RefreshProfile();

        AIGoal currentGoal = DetermineMacroGoal();
        ExecuteMicroActions(currentGoal);

        //Debug.Log($"[AI DETERMINISTIC Completed turn with goal: {currentGoal}");
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

        AIGoal chosenGoal;
        if (expansionScore >= economyScore && expansionScore >= militaryScore)
            chosenGoal = AIGoal.FocusExpansion;
        else if (militaryScore >= economyScore)
            chosenGoal = AIGoal.FocusMilitary;
        else
            chosenGoal = AIGoal.FocusEconomy;

        if (chosenGoal == lastGoal && chosenGoal == AIGoal.FocusEconomy)
        {
            stuckCounter++;
            if (stuckCounter >= 3)
            {
                if (militaryScore > expansionScore)
                    chosenGoal = AIGoal.FocusMilitary;
                else
                    chosenGoal = AIGoal.FocusExpansion;
                stuckCounter = 0;
            }
        }
        else
        {
            stuckCounter = 0;
        }
        lastGoal = chosenGoal;

        return chosenGoal;
    }

    private void ExecuteMicroActions(AIGoal goal)
    {
        switch (goal)
        {
            case AIGoal.FocusEconomy:
                micro.ExecuteEconomyMicro();
                //Debug.Log("---------DETERMINISTIC AI FOCUSING ON ECONOMY THIS TURN!---------");
                break;
            case AIGoal.FocusMilitary:
                micro.ExecuteMilitaryMicro();
                //Debug.Log("---------DETERMINISTIC AI FOCUSING ON MILITARY THIS TURN!---------");
                break;
            case AIGoal.FocusExpansion:
                micro.ExecuteExpansionMicro();
                //Debug.Log("---------DETERMINISTIC AI FOCUSING ON EXPANSION THIS TURN!---------");
                break;
        }

        micro.ExecuteRoadMicro();
        micro.HandleUnitMicro(currentArmyState, ref currentArmyState);
    }

    // --------------------- DESIRE CALCULATIONS ---------------------

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

        if (myProfile.gold > 140 && myProfile.wood > 140)
        {
            score -= 60; 
        }

        return score;
    }

    private float CalculateMilitaryDesire()
    {
        float score = 20;
        float myStrength = micro.CalculateMilitaryStrength();
        float observedEnemyStrength = micro.GetObservedEnemyStrength();

        bool hasBarracks = myProfile.myBuildings.Any(b => b.buildingType == Building.BuildingType.Barracks);
        if (hasBarracks && myStrength == 0)
        {
            score += 60; 
        }

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

        if (myProfile.gold > 70) score += 20;
        if (myProfile.gold > 150) score += 30;

        return score;
    }

    private float CalculateExpansionDesire()
    {
        float score = 0;

        int buildingCount = myProfile.myBuildings.Count(b => b.buildingType != Building.BuildingType.Road);

        if (buildingCount > 15) score += 20;
        if (buildingCount > 20) score += 40; 
        if (micro.CalculateMilitaryStrength() > 100) score += 20;

        if (myProfile.myBuildings.Any(b => b.buildingType == Building.BuildingType.Outpost && !b.isConstructed))
        {
            score = 0;
        }

        return score;
    }
}