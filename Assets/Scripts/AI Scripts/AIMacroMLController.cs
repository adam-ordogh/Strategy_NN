// AIMacroMLController.cs
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;

public class AIMacroMLController : IAIController
{
    public int PlayerId { get; private set; }
    private GameManager gameManager;
    private AIMacroML mlAgent;
    private AIMicroController micro;
    public MilitaryState currentArmyState = MilitaryState.Gathering;

    public void Initialize(GameManager gameManager)
    {
        this.gameManager = gameManager;
        this.micro = new AIMicroController(PlayerId, gameManager);

        // Find or create ML agent
        FindOrCreateMLAgent();
    }

    public AIMacroMLController(int playerId)
    {
        PlayerId = playerId;
    }


    private void FindOrCreateMLAgent()
    {
        var agents = GameObject.FindObjectsByType<AIMacroML>(FindObjectsSortMode.None);
        foreach (var agent in agents)
        {
            if (agent.playerId == PlayerId)
            {
                mlAgent = agent;
                break;
            }
        }

        if (mlAgent == null)
        {
            GameObject agentGO = new GameObject($"ML_Agent_Player{PlayerId}");
            agentGO.SetActive(false);

            // Set up BehaviorParameters BEFORE adding the Agent component
            var behaviorParams = agentGO.AddComponent<BehaviorParameters>();
            behaviorParams.BehaviorName = "AIMacroML";
            behaviorParams.BrainParameters.VectorObservationSize = 24;
            behaviorParams.BrainParameters.NumStackedVectorObservations = 1;
            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(3);
            behaviorParams.BehaviorType = BehaviorType.Default;
            behaviorParams.TeamId = PlayerId;

            // Now add the Agent
            mlAgent = agentGO.AddComponent<AIMacroML>();
            mlAgent.playerId = PlayerId;
            mlAgent.gameManager = gameManager;
            mlAgent.currentArmyState = currentArmyState;
            mlAgent.owner = this;

            agentGO.SetActive(true); // Initialize() fires here, BehaviorParameters already set
        }
    }

    public void ExecuteTurn()
    {
        micro.RefreshProfile();

        if (mlAgent != null)
        {
            mlAgent.ManualUpdate();
            currentArmyState = mlAgent.currentArmyState;
        }
        else
        {
            Debug.LogError($"ML Agent not found for player {PlayerId}!");
            gameManager.NextTurn();
        }
    }
}