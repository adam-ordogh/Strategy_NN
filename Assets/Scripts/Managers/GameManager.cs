using System.Collections.Generic;
using System.Threading.Tasks;
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

    private Dictionary<int, AiPlayerController> aiControllers = new Dictionary<int, AiPlayerController>();

    public GameManager(MapManager mapManager, UnitManager unitManager, BuildingManager buildingManager, ProductionManager productionManager, EconomyManager economyManager, UnitVisualizer unitVisualizer, BuildingVisualizer buildingVisualizer)
    {
        this.mapManager = mapManager;
        this.unitManager = unitManager;
        this.buildingManager = buildingManager;
        this.productionManager = productionManager;
        this.economyManager = economyManager;
        this.unitVisualizer = unitVisualizer;
        this.buildingVisualizer = buildingVisualizer;

        //InitializePlayers();
    }

    public void Start()
    {

    }

    public void Update()
    {

    }

    public void InitializePlayers()
    {
        var p1 = new PlayerProfile { playerId = 1, isAi = false, food = 50, gold = 200, wood = 200};
        players.Add(p1);

        //var p2 = new PlayerProfile { playerId = 2, isAi = false, food = 50, gold = 200, wood = 200};
        var p2 = new PlayerProfile { playerId = 2, isAi = true, food = 50, gold = 200, wood = 200 };
        players.Add(p2);

        aiControllers.Add(p2.playerId, new AiPlayerController(p2.playerId, this));
    }

    public void InitializeStartingTownCenters() {
        // 1. Place the building through the manager
        Building p1Base = buildingManager.PlaceBuilding(buildingManager.buildingTemplates[0], new Vector2Int(5, 11), players[0].playerId);
        buildingManager.CompleteBuildingInstantly(p1Base);

        // Repeat for AI
        Building p2Base = buildingManager.PlaceBuilding(buildingManager.buildingTemplates[0], new Vector2Int(43, 38), players[1].playerId);
        buildingManager.CompleteBuildingInstantly(p2Base);
    }

    public PlayerProfile GetPlayerProfile(int id)
    {
        return players.Find(p => p.playerId == id);
    }

    public void NextTurn()
    {
        unitManager.ResetUnitsForNewTurn(currentPlayerId);
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        if (currentPlayerIndex == 0)
        {
            turnNumber++;
        }
        PlayerProfile activePlayer = CurrentPlayer;
        Debug.Log($"Starting Turn for Player {activePlayer.playerId}");

        buildingManager.AdvanceConstruction(currentPlayerId);
        productionManager.ProcessTurn(currentPlayerId);
        economyManager.ProcessTurn(activePlayer);

        if (activePlayer.isAi)
        {
            // Tell the AI to think and act
            if (aiControllers.TryGetValue(activePlayer.playerId, out var controller))
            {
                controller.ExecuteTurn();
            }
        }
        else
        {
            // Human turn: Do nothing, just wait for InputController events
            Debug.Log("Waiting for Human Player...");
        }
    }
}
