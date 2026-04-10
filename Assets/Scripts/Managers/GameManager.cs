using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.InferenceEngine;
using UnityEngine;

public class GameManager
{
    public MapManager mapManager;
    public UnitManager unitManager;
    public BuildingManager buildingManager;
    public ProductionManager productionManager;
    public EconomyManager economyManager;
    public UnitVisualizer unitVisualizer;
    public BuildingVisualizer buildingVisualizer;

    public bool isTrainingMode;
    public int turnNumber = 1;

    public PlayerProfile HumanPlayer
    {
        get
        {
            return players.FirstOrDefault(p => !aiControllers.ContainsKey(p.playerId));
        }
    }

    public List<PlayerProfile> players = new List<PlayerProfile>();
    private int currentPlayerIndex = 0;
    public int currentPlayerId => players[currentPlayerIndex].playerId;
    public PlayerProfile CurrentPlayer => players[currentPlayerIndex];

    private Dictionary<int, IAIController> aiControllers = new Dictionary<int, IAIController>();

    public event System.Action<bool> OnGameOver;

    public GameManager(MapManager mapManager, UnitManager unitManager, BuildingManager buildingManager,
                      ProductionManager productionManager, EconomyManager economyManager,
                      UnitVisualizer unitVisualizer, BuildingVisualizer buildingVisualizer)
    {
        this.mapManager = mapManager;
        this.unitManager = unitManager;
        this.buildingManager = buildingManager;
        this.productionManager = productionManager;
        this.economyManager = economyManager;
        this.unitVisualizer = unitVisualizer;
        this.buildingVisualizer = buildingVisualizer;
    }

    public void Start() 
    {
        //Debug.Log("Game started - beginning with Player 1's turn");
        currentPlayerIndex = UnityEngine.Random.Range(0, players.Count);
        ProcessCurrentPlayerTurn();
    }

    public void Update() { }

    public void InitializePlayers()
    {
        // Player 1 - Ember
        var p1 = new PlayerProfile(playerId: 5, isAi: false, playerColor: GetPlayerColor(2),
                                   starterGold: 200, starterWood: 200, starterFood: 50);
        players.Add(p1);
    }

    // Alternative method for more control
    public void InitializePlayersWithCustomAI(List<AIFactory.AIType> aiTypes, bool isTraining, ModelAsset model = null)
    {
        this.isTrainingMode = isTraining;
        for (int i = 0; i < aiTypes.Count; i++)
        {
            int playerId = i + 1;

            var player = new PlayerProfile(playerId: playerId, isAi: true,
                                          playerColor: GetPlayerColor(i),
                                          starterGold: 200, starterWood: 200, starterFood: 50);
            players.Add(player);

            var ai = AIFactory.CreateAI(aiTypes[i], playerId, isTraining, model);
            ai.Initialize(this);
            aiControllers.Add(playerId, ai);
            
        }
    }

    private Color GetPlayerColor(int index)
    {
        Color[] colors = { Color.cyan, new Color(1f, 0.3f, 0.3f), Color.green, Color.yellow, Color.magenta };
        return colors[index % colors.Length];
    }

    public void InitializeStartingTownCenters()
    {
        var startPositions = new List<Vector2Int>
        {
            new Vector2Int(5, 11),
            new Vector2Int(42, 36)
        };

        for (int i = startPositions.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (startPositions[i], startPositions[j]) = (startPositions[j], startPositions[i]);
        }

        for (int i = 0; i < players.Count; i++)
        {
            Building townCenter = buildingManager.PlaceBuilding(
                buildingManager.buildingTemplates[0],
                startPositions[i],
                players[i].playerId);
            buildingManager.CompleteBuildingInstantly(townCenter);            
        }
    }

    public PlayerProfile GetPlayerProfile(int id)
    {
        return players.Find(p => p.playerId == id);
    }


    public IAIController GetPlayerController(int playerId)
    {
        if (aiControllers.TryGetValue(playerId, out var controller))
        {
            return controller;
        }
        return null;
    }

    private void ProcessCurrentPlayerTurn()
    {
        PlayerProfile activePlayer = CurrentPlayer;
        //Debug.Log($"Processing turn for Player {activePlayer.playerId}");

        buildingManager.AdvanceConstruction(currentPlayerId);
        productionManager.ProcessTurn(currentPlayerId);
        economyManager.ProcessTurn(activePlayer);

        if (activePlayer.isAi)
        {
            if (aiControllers.TryGetValue(activePlayer.playerId, out var controller))
            {
                controller.ExecuteTurn();
            }
        }
        else
        {
            //Debug.Log("Waiting for Human Player...");
        }
    }

    public void NextTurn()
    {
        unitManager.ResetUnitsForNewTurn(currentPlayerId);
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        if (currentPlayerIndex == 0)
        {
            turnNumber++;
        }

        foreach (var player in players)
        {
            bool hasTownCenter = player.myBuildings.Any(b =>
                   b.buildingType == Building.BuildingType.TownCenter);
            if(!hasTownCenter && turnNumber > 5)
            {
                if (isTrainingMode)
                {
                    return;
                }
                else
                {
                    var losingController = GetPlayerController(player.playerId);
                    string loserType = losingController?.GetAITypeName() ?? "Unknown";

                    // Find the winner
                    var winner = players.FirstOrDefault(p => p.playerId != player.playerId);
                    var winningController = winner != null ? GetPlayerController(winner.playerId) : null;
                    string winnerType = winningController?.GetAITypeName() ?? "Unknown";

                    Debug.Log($"Game Over at turn {turnNumber}! {winnerType} (Player {winner?.playerId}) defeated {loserType} (Player {player.playerId})");

                    bool humanWon = (winner != null && !winner.isAi);
                    OnGameOver?.Invoke(humanWon);

                    return;
                }
            }
        }

        ProcessCurrentPlayerTurn();
        //buildingManager.StartCoroutine(ProcessTurnWithDelay());
    }

    private IEnumerator ProcessTurnWithDelay()
    {
        yield return null; // Wait 1 frame
        if (buildingManager == null || players.Count == 0) yield break;
        ProcessCurrentPlayerTurn();
    }
}
