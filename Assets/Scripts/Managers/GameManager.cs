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

    public GameManager(MapManager mapManager, UnitManager unitManager, BuildingManager buildingManager, ProductionManager productionManager, EconomyManager economyManager, UnitVisualizer unitVisualizer, BuildingVisualizer buildingVisualizer)
    {
        this.mapManager = mapManager;
        this.unitManager = unitManager;
        this.buildingManager = buildingManager;
        this.productionManager = productionManager;
        this.economyManager = economyManager;
        this.unitVisualizer = unitVisualizer;
        this.buildingVisualizer = buildingVisualizer;

        InitializePlayers();
    }

    public void Start()
    {

    }

    public void Update()
    {

    }

    private void InitializePlayers()
    {
        var p1 = new PlayerProfile { playerId = 1, isAi = false, food = 50, gold = 200, wood = 200};
        players.Add(p1);

        var p2 = new PlayerProfile { playerId = 2, isAi = false, food = 50, gold = 200, wood = 200};
        players.Add(p2);
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
    }
}
