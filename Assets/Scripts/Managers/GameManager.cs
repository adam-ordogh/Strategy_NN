using System.Threading.Tasks;
using UnityEngine;

public class GameManager
{
    public MapManager mapManager;
    public UnitManager unitManager;
    public BuildingManager buildingManager;
    public UnitVisualizer unitVisualizer;
    public BuildingVisualizer buildingVisualizer;

    public int turnNumber = 1;
    public int currentPlayerId = 1;

    public GameManager(MapManager mapManager, UnitManager unitManager, BuildingManager buidlingManager, UnitVisualizer unitVisualizer, BuildingVisualizer buildingVisualizer)
    {
        this.mapManager = mapManager;
        this.unitManager = unitManager;
        this.buildingManager = buidlingManager;
        this.unitVisualizer = unitVisualizer;
        this.buildingVisualizer = buildingVisualizer;
    }

    public void Start()
    {
        Unit unit;
        unitManager.CreateUnit(unit = new Unit(Unit.UnitType.Soldier, 1, 8, 2, 1, 5, new Vector2Int(2, 2)));
        unitVisualizer.ShowUnit(unit);

        unitManager.CreateUnit(unit = new Unit(Unit.UnitType.Archer, 1, 6, 2, 4, 3, new Vector2Int(3, 3)));
        unitVisualizer.ShowUnit(unit);

        unitManager.CreateUnit(unit = new Unit(Unit.UnitType.Cavalry, 1, 12, 2, 1, 8, new Vector2Int(4, 4)));
        unitVisualizer.ShowUnit(unit);


        unitManager.CreateUnit(unit = new Unit(Unit.UnitType.Soldier, 2, 8, 2, 1, 5, new Vector2Int(2, 1)));
        unitVisualizer.ShowUnit(unit);

        unitManager.CreateUnit(unit = new Unit(Unit.UnitType.Archer, 2, 6, 2, 4, 3, new Vector2Int(3, 2)));
        unitVisualizer.ShowUnit(unit);

        unitManager.CreateUnit(unit = new Unit(Unit.UnitType.Cavalry, 2, 12, 2, 1, 8, new Vector2Int(4, 3)));
        unitVisualizer.ShowUnit(unit);

        mapManager.ListUnits();


        Building building;
        buildingManager.CreateBuilding(building = new Building(Building.BuildingType.Barracks, 1, new Vector2Int(25, 25)));
        buildingVisualizer.ShowBuilding(building);
    }

    public void Update()
    {

    }

    public void NextTurn()
    {
        unitManager.ResetUnitsForNewTurn(currentPlayerId);
        currentPlayerId = (currentPlayerId % 2) + 1;
        if(currentPlayerId == 1)
            turnNumber++;
    }
}
