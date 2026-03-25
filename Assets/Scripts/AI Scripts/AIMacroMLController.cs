using Unity.InferenceEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;

public class AIMacroMLController : IAIController
{
    public int PlayerId { get; private set; }
    private GameManager gameManager;
    private AIMacroML mlAgent;
    private AIMicroController micro;
    private bool isTraining;

    private ModelAsset aiModel;

    public MilitaryState currentArmyState = MilitaryState.Gathering;

    public AIMacroMLController(int playerId, bool isTraining = false, ModelAsset model = null)
    {
        PlayerId = playerId;
        this.isTraining = isTraining;
        this.aiModel = model;
    }

    public string GetAITypeName() => isTraining ? "ML (Training)" : "ML (Inference)";

    public void Initialize(GameManager gameManager)
    {
        this.gameManager = gameManager;
        this.micro = new AIMicroController(PlayerId, gameManager);

        FindOrCreateMLAgent();
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

            var behaviorParams = agentGO.AddComponent<BehaviorParameters>();
            behaviorParams.BehaviorName = "AIMacroML";

            behaviorParams.Model = aiModel;

            behaviorParams.BrainParameters.VectorObservationSize = 24;
            behaviorParams.BrainParameters.NumStackedVectorObservations = 1;
            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(3);
            behaviorParams.BehaviorType = BehaviorType.Default;
            behaviorParams.TeamId = PlayerId;

            behaviorParams.BehaviorType = isTraining ? BehaviorType.Default : BehaviorType.InferenceOnly;

            mlAgent = agentGO.AddComponent<AIMacroML>();
            mlAgent.playerId = PlayerId;
            mlAgent.gameManager = gameManager;
            mlAgent.currentArmyState = currentArmyState;
            mlAgent.owner = this;
            mlAgent.isTraining = isTraining; 

            agentGO.SetActive(true);
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