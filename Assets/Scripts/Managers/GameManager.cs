//using System.Collections.Generic;
//using System.Threading.Tasks;
//using UnityEngine;

//public class GameManager
//{
//    public MapManager mapManager;
//    public UnitManager unitManager;
//    public BuildingManager buildingManager;
//    public ProductionManager productionManager;
//    public EconomyManager economyManager;
//    public UnitVisualizer unitVisualizer;
//    public BuildingVisualizer buildingVisualizer;

//    public int turnNumber = 1;

//    public List<PlayerProfile> players = new List<PlayerProfile>();
//    private int currentPlayerIndex = 0;
//    public int currentPlayerId => players[currentPlayerIndex].playerId;
//    public PlayerProfile CurrentPlayer => players[currentPlayerIndex];

//    private Dictionary<int, AiPlayerController> aiControllers = new Dictionary<int, AiPlayerController>();

//    public GameManager(MapManager mapManager, UnitManager unitManager, BuildingManager buildingManager, ProductionManager productionManager, EconomyManager economyManager, UnitVisualizer unitVisualizer, BuildingVisualizer buildingVisualizer)
//    {
//        this.mapManager = mapManager;
//        this.unitManager = unitManager;
//        this.buildingManager = buildingManager;
//        this.productionManager = productionManager;
//        this.economyManager = economyManager;
//        this.unitVisualizer = unitVisualizer;
//        this.buildingVisualizer = buildingVisualizer;

//        //InitializePlayers();
//    }

//    public void Start()
//    {

//    }

//    public void Update()
//    {

//    }

//    public void InitializePlayers()
//    {
//        //var p1 = new PlayerProfile { playerId = 1, isAi = false, food = 50, gold = 200, wood = 200};
//        var p1 = new PlayerProfile ( playerId:1, isAi:false, playerColor: Color.cyan, starterGold:200, starterWood:200, starterFood:50);
//        players.Add(p1);

//        //var p2 = new PlayerProfile { playerId = 2, isAi = false, food = 50, gold = 200, wood = 200};
//        var p2 = new PlayerProfile(playerId: 2, isAi: true, playerColor: new Color(1f, 0.3f, 0.3f), starterGold: 200, starterWood: 200, starterFood: 50);
//        players.Add(p2);

//        aiControllers.Add(p2.playerId, new AiPlayerController(p2.playerId, this));
//    }

//    public void InitializeStartingTownCenters() {
//        // 1. Place the building through the manager
//        Building p1Base = buildingManager.PlaceBuilding(buildingManager.buildingTemplates[0], new Vector2Int(5, 11), players[0].playerId);
//        buildingManager.CompleteBuildingInstantly(p1Base);

//        // Repeat for AI
//        Building p2Base = buildingManager.PlaceBuilding(buildingManager.buildingTemplates[0], new Vector2Int(43, 38), players[1].playerId);
//        buildingManager.CompleteBuildingInstantly(p2Base);

//    }

//    public PlayerProfile GetPlayerProfile(int id)
//    {
//        return players.Find(p => p.playerId == id);
//    }

//    public void NextTurn()
//    {
//        unitManager.ResetUnitsForNewTurn(currentPlayerId);
//        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
//        if (currentPlayerIndex == 0)
//        {
//            turnNumber++;
//        }
//        PlayerProfile activePlayer = CurrentPlayer;
//        Debug.Log($"Starting Turn for Player {activePlayer.playerId}");

//        buildingManager.AdvanceConstruction(currentPlayerId);
//        productionManager.ProcessTurn(currentPlayerId);
//        economyManager.ProcessTurn(activePlayer);

//        if (activePlayer.isAi)
//        {
//            // Tell the AI to think and act
//            if (aiControllers.TryGetValue(activePlayer.playerId, out var controller))
//            {
//                controller.ExecuteTurn();
//            }
//        }
//        else
//        {
//            // Human turn: Do nothing, just wait for InputController events
//            Debug.Log("Waiting for Human Player...");
//        }
//    }
//}
// GameManager.cs (updated version)
using System.Collections.Generic;
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

    public int turnNumber = 1;

    public List<PlayerProfile> players = new List<PlayerProfile>();
    private int currentPlayerIndex = 0;
    public int currentPlayerId => players[currentPlayerIndex].playerId;
    public PlayerProfile CurrentPlayer => players[currentPlayerIndex];

    // Use the interface instead of concrete type
    private Dictionary<int, IAIController> aiControllers = new Dictionary<int, IAIController>();

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
        currentPlayerIndex = Random.Range(0, players.Count);
        ProcessCurrentPlayerTurn();
    }

    public void Update() { }

    public void InitializePlayers()
    {
        // Player 1 - Human
        var p1 = new PlayerProfile(playerId: 1, isAi: false, playerColor: Color.cyan,
                                   starterGold: 200, starterWood: 200, starterFood: 50);
        players.Add(p1);

        // Player 2 - AI (Deterministic for testing)
        var p2 = new PlayerProfile(playerId: 2, isAi: true, playerColor: new Color(1f, 0.3f, 0.3f),
                                   starterGold: 200, starterWood: 200, starterFood: 50);
        players.Add(p2);

        // Create deterministic AI for player 2
        var ai2 = AIFactory.CreateAI(AIFactory.AIType.Deterministic, p2.playerId);
        ai2.Initialize(this);
        aiControllers.Add(p2.playerId, ai2);
    }

    // Alternative method for more control
    public void InitializePlayersWithCustomAI(List<AIFactory.AIType> aiTypes)
    {
        for (int i = 0; i < aiTypes.Count; i++)
        {
            //int playerId = i + 1;
            int playerId = i;
            //bool isAi = aiTypes[i] != AIFactory.AIType.Deterministic; // or any logic you want

            var player = new PlayerProfile(playerId: playerId, isAi: true,
                                          playerColor: GetPlayerColor(i),
                                          starterGold: 200, starterWood: 200, starterFood: 50);
            players.Add(player);

            //if (isAi)
            //{
                var ai = AIFactory.CreateAI(aiTypes[i], playerId);
                ai.Initialize(this);
                aiControllers.Add(playerId, ai);
            //}
        }
    }

    private Color GetPlayerColor(int index)
    {
        Color[] colors = { Color.cyan, new Color(1f, 0.3f, 0.3f), Color.green, Color.yellow, Color.magenta };
        return colors[index % colors.Length];
    }

    //public void InitializeStartingTownCenters()
    //{
    //    Building p1Base = buildingManager.PlaceBuilding(buildingManager.buildingTemplates[0],
    //                                                   new Vector2Int(5, 11), players[0].playerId);
    //    buildingManager.CompleteBuildingInstantly(p1Base);

    //    Building p2Base = buildingManager.PlaceBuilding(buildingManager.buildingTemplates[0],
    //                                                   new Vector2Int(43, 38), players[1].playerId);
    //    buildingManager.CompleteBuildingInstantly(p2Base);
    //}

    public void InitializeStartingTownCenters()
    {
        var startPositions = new List<Vector2Int>
        {
            new Vector2Int(5, 11),
            new Vector2Int(43, 38)
        };

        // Fisher-Yates shuffle - works for any number of positions/players
        for (int i = startPositions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
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
                // The AI controller will call NextTurn() when done
            }
        }
        else
        {
            //Debug.Log("Waiting for Human Player...");
            // Human player will trigger NextTurn() through UI
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
            if (player.myBuildings.Count == 0 && turnNumber > 5)
            {
                return; // Stop here, let TrainingRestart handle the reload
            }
        }

        ProcessCurrentPlayerTurn();
    }
}
