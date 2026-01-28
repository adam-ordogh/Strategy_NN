using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameInitializer : MonoBehaviour
{
    public bool isTrainingMode;

    public InputController inputController;

    public Tilemap map;
    public Tilemap highlightMap;
    public Tilemap unitMap;
    public Tilemap buildingMap;

    public TileRegistry tileRegistry;
    private MapGenerator mapGenerator;
    private UnitVisualizer unitVisualizer;
    private BuildingVisualizer buildingVisualizer;

    public MapManager mapManager;
    public BuildingManager buildingManager;
    public UnitManager unitManager;
    public GameManager gameManager;
    public VisualsManager visualsManager;

    // async volt eddig
    void Start()
    {
        isTrainingMode = false;
        CreateCameraControls();
        InstantiateComponents();

        StartGame();
    }

    void Update()
    {
        gameManager.Update();
    }

    public void CreateCameraControls()
    {
        int borderOffset = 10;
        var camController = Camera.main.GetComponent<CameraController2D>();
        camController.mapMinBounds = new Vector2(-borderOffset, -borderOffset);
        camController.mapMaxBounds = new Vector2(mapManager.mapWidth + borderOffset, mapManager.mapHeight + borderOffset);
    }

    public void InstantiateComponents()
    {
        mapGenerator = new MapGenerator(mapManager, map, tileRegistry);
        unitVisualizer = new UnitVisualizer(unitMap, tileRegistry, highlightMap, tileRegistry.GetTile(MapData.TileType.MovementHighlight));
        buildingVisualizer = new BuildingVisualizer(buildingMap, tileRegistry.GetTile(MapData.TileType.Building));
        gameManager = new GameManager(mapManager, unitManager, buildingManager, unitVisualizer, buildingVisualizer);
        unitManager.Initialize(mapManager.mapData);
        buildingManager.Initialize(mapManager.mapData);

        unitManager.IsTileBlockedByBuilding = (pos) => buildingManager.GetBuildingAtTile(pos) != null;

        if (!isTrainingMode)
        {
            unitVisualizer.SetAnimationRunner(visualsManager);

            AddVisualEventListeners();

            inputController.unitVisualizer = unitVisualizer;
            inputController.buildingVisualizer = buildingVisualizer;
            inputController.buildingManager = buildingManager;
            inputController.gameManager = gameManager;
        }
    }

    public void AddVisualEventListeners()
    {
        unitManager.OnUnitMoved += unitVisualizer.HandleUnitMoved;
        unitManager.OnUnitDestroyed += unitVisualizer.HandleUnitDied;
    }

    public void StartGame()
    {
        mapGenerator.Generate();
        
        gameManager.Start();
    }
}
