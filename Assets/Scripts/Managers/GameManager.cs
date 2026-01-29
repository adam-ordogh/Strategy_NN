using System.Threading.Tasks;
using UnityEngine;

public class GameManager
{
    public MapManager mapManager;
    public UnitManager unitManager;
    public BuildingManager buildingManager;
    public ProductionManager productionManager;
    public UnitVisualizer unitVisualizer;
    public BuildingVisualizer buildingVisualizer;

    public int turnNumber = 1;
    public int currentPlayerId = 1;

    public GameManager(MapManager mapManager, UnitManager unitManager, BuildingManager buildingManager, ProductionManager productionManager, UnitVisualizer unitVisualizer, BuildingVisualizer buildingVisualizer)
    {
        this.mapManager = mapManager;
        this.unitManager = unitManager;
        this.buildingManager = buildingManager;
        this.productionManager = productionManager;
        this.unitVisualizer = unitVisualizer;
        this.buildingVisualizer = buildingVisualizer;
    }

    public void Start()
    {

    }

    public void Update()
    {

    }

    public void NextTurn()
    {
        unitManager.ResetUnitsForNewTurn(currentPlayerId);
        currentPlayerId = (currentPlayerId % 2) + 1;
        if (currentPlayerId == 1)
        {
            turnNumber++;
        }
        productionManager.ProcessTurn(currentPlayerId);
    }
}
