    
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Policies;

public class AIMacroML : Agent
{
    [Header("References")]
    public GameManager gameManager;
    public int playerId;

    [Header("Settings")]
    //[SerializeField] private int decisionInterval = 5;
    [SerializeField] private bool useHeuristicForTesting = true;
    [SerializeField] public bool headlessMode = true; // Set by training manager

    // Micro controller for execution
    private AIMicroController micro;
    private PlayerProfile myProfile;
    private int turnCounter = 0;

    [HideInInspector] public AIMacroMLController owner;

    // Current decision
    public AIGoal currentGoal { get; private set; } = AIGoal.FocusEconomy;
    public MilitaryState currentArmyState = MilitaryState.Gathering;

    public override void Initialize()
    {
        Debug.Log($"[ML Agent {playerId}] Initialize() called");

        if (gameManager != null && playerId >= 0)
        {
            Debug.Log($"[ML Agent {playerId}] Creating micro controller");
            micro = new AIMicroController(playerId, gameManager);
            myProfile = gameManager.GetPlayerProfile(playerId);
            Debug.Log($"[ML Agent {playerId}] Micro created: {micro != null}, Profile found: {myProfile != null}");
        }
        else
        {
            Debug.LogError($"[ML Agent {playerId}] Cannot initialize - gameManager: {gameManager != null}, playerId: {playerId}");
        }

        currentGoal = AIGoal.FocusEconomy;
    }

    public void ManualUpdate()
    {
        if (gameManager == null || micro == null)
        {
            Debug.LogError($"[ML Agent {playerId}] Missing components");
            return;
        }

        myProfile = gameManager.GetPlayerProfile(playerId);
        micro.RefreshProfile();

        // Ensure Academy is ready
        if (!Academy.IsInitialized)
        {
            Debug.Log($"[ML Agent {playerId}] Academy not ready, skipping turn");
            gameManager.NextTurn();
            return;
        }

        Debug.Log($"[ML Agent {playerId}] Requesting decision");
        RequestDecision();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log($"[ML Agent {playerId}] Collecting observations at turn {turnCounter}");
        if (myProfile == null) return;

        var income = gameManager.economyManager.GetProjectedIncome(myProfile);

        // Same observations as before...
        // (keep all the observation code from previous version)

        // === ECONOMIC OBSERVATIONS === 15
        sensor.AddObservation(Normalize(myProfile.gold, 0, 500));
        sensor.AddObservation(Normalize(myProfile.wood, 0, 500));
        sensor.AddObservation(Normalize(myProfile.food, 0, 500));

        sensor.AddObservation(Normalize(income.goldNet, -10, 30));
        sensor.AddObservation(Normalize(income.woodNet, -10, 30));
        sensor.AddObservation(Normalize(income.foodNet, -10, 30));

        sensor.AddObservation(Normalize(myProfile.currentPopulation, 0, 50));
        sensor.AddObservation(Normalize(myProfile.availablePopulation, 0, 20));
        sensor.AddObservation(Normalize(myProfile.housingCapacity, 0, 50));

        sensor.AddObservation(Normalize(myProfile.myBuildings.Count, 0, 30));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Farm));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Woodcutter));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Mine));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Barracks));
        sensor.AddObservation(GetBuildingRatio(Building.BuildingType.Outpost));

        // === MILITARY OBSERVATIONS === 5
        float myStrength = micro.CalculateMilitaryStrength();
        float enemyStrength = micro.GetObservedEnemyStrength();

        sensor.AddObservation(Normalize(myStrength, 0, 200));
        sensor.AddObservation(Normalize(enemyStrength, 0, 200));
        sensor.AddObservation(micro.IsEnemyNearBase(15f) ? 1f : 0f);
        sensor.AddObservation(Normalize(myStrength / Mathf.Max(enemyStrength, 1f), 0, 3));
        sensor.AddObservation((float)currentArmyState);

        // === EXPANSION OBSERVATIONS === 4
        sensor.AddObservation(micro.CanPlaceIndustrialBuilding() ? 1f : 0f);
        sensor.AddObservation(GetTerritorySizeNormalized());
        sensor.AddObservation(GetDistanceToEnemyNormalized());
        sensor.AddObservation(GetResourceScarcityNormalized());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log($"[ML Agent {playerId}] Received action: {actions.DiscreteActions[0]}");
        int action = actions.DiscreteActions[0];

        switch (action)
        {
            case 0: currentGoal = AIGoal.FocusEconomy; break;
            case 1: currentGoal = AIGoal.FocusMilitary; break;
            case 2: currentGoal = AIGoal.FocusExpansion; break;
        }

        ExecuteCurrentGoal();

        float reward = CalculateReward();
        AddReward(reward);

        if (owner != null)
        {
            owner.currentArmyState = this.currentArmyState;
        }

        // Tell GameManager to move to next player
        gameManager.NextTurn();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (!useHeuristicForTesting)
        {
            base.Heuristic(actionsOut);
            return;
        }

        var discreteActions = actionsOut.DiscreteActions;
        var income = gameManager.economyManager.GetProjectedIncome(myProfile);

        if (income.foodNet < 2)
            discreteActions[0] = 0;
        else if (micro.IsEnemyNearBase(15f))
            discreteActions[0] = 1;
        else if (!micro.CanPlaceIndustrialBuilding() && myProfile.gold + myProfile.wood > 300)
            discreteActions[0] = 2;
        else if (myProfile.gold < 100 || myProfile.wood < 100)
            discreteActions[0] = 0;
        else if (micro.GetObservedEnemyStrength() > micro.CalculateMilitaryStrength())
            discreteActions[0] = 1;
        else
            discreteActions[0] = 0;
    }

    private void ExecuteCurrentGoal()
    {
        switch (currentGoal)
        {
            case AIGoal.FocusEconomy: micro.ExecuteEconomyMicro(); break;
            case AIGoal.FocusMilitary: micro.ExecuteMilitaryMicro(); break;
            case AIGoal.FocusExpansion: micro.ExecuteExpansionMicro(); break;
        }

        micro.ExecuteRoadMicro();
        micro.HandleUnitMicro(currentArmyState, ref currentArmyState);
    }

    private float CalculateReward()
    {
        float reward = 0f;

        // Resource accumulation
        reward += myProfile.gold / 1000f;
        reward += myProfile.wood / 1000f;
        reward += myProfile.food / 1000f;

        // Military strength
        reward += micro.CalculateMilitaryStrength() / 200f;

        // Territory size
        var territory = gameManager.buildingManager.influenceManager.GetTilesOwnedBy(playerId);
        reward += territory.Count / 500f;

        // Building count
        reward += myProfile.myBuildings.Count / 50f;

        // Penalties
        if (myProfile.food <= 0) reward -= 1f;
        if (myProfile.myBuildings.Count == 0) reward -= 10f;

        return reward;
    }

    // Helper methods (same as before)
    private float Normalize(float value, float min, float max)
    {
        return Mathf.Clamp01((value - min) / (max - min));
    }

    private float GetBuildingRatio(Building.BuildingType type)
    {
        int count = myProfile.myBuildings.Count(b => b.buildingType == type);
        return Normalize(count, 0, 10);
    }

    private float GetTerritorySizeNormalized()
    {
        var territory = gameManager.buildingManager.influenceManager.GetTilesOwnedBy(playerId);
        return Normalize(territory.Count, 0, 200);
    }

    private float GetDistanceToEnemyNormalized()
    {
        if (myProfile.myBuildings.Count == 0) return 1f;

        Vector2Int enemyBase = GetClosestEnemyBase(myProfile.myBuildings[0].position);
        if (enemyBase.x == -1) return 1f;

        float dist = Vector2Int.Distance(myProfile.myBuildings[0].position, enemyBase);
        return Normalize(dist, 0, 100);
    }

    private float GetResourceScarcityNormalized()
    {
        return micro.CanPlaceIndustrialBuilding() ? 0f : 1f;
    }

    private Vector2Int GetClosestEnemyBase(Vector2Int fromPos)
    {
        Vector2Int closest = new Vector2Int(-1, -1);
        float minDist = float.MaxValue;

        foreach (var p in gameManager.players)
        {
            if (p.playerId != playerId && p.myBuildings.Count > 0)
            {
                Vector2Int enemyBase = p.myBuildings[0].position;
                float d = Vector2Int.Distance(fromPos, enemyBase);
                if (d < minDist)
                {
                    minDist = d;
                    closest = enemyBase;
                }
            }
        }
        return closest;
    }
}