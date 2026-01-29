using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameInitializer : MonoBehaviour
{
    public bool isTrainingMode;

    public InputController inputController;

    public Tilemap map;
    public Tilemap influenceMap;
    public Tilemap highlightMap;
    public Tilemap unitMap;
    public Tilemap buildingMap;

    public TileRegistry tileRegistry;
    private MapGenerator mapGenerator;
    private UnitVisualizer unitVisualizer;
    private BuildingVisualizer buildingVisualizer;
    private InfluenceVisualizer influenceVisualizer;

    public MapManager mapManager;
    public BuildingManager buildingManager;
    public UnitManager unitManager;
    public GameManager gameManager;
    public VisualsManager visualsManager;
    public InfluenceManager influenceManager;
    public ProductionManager productionManager;

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
        unitManager.Initialize(mapManager.mapData);
        influenceManager.Initialize(mapManager.mapData);
        buildingManager.Initialize(mapManager.mapData, influenceManager);
        productionManager.Initialize(mapManager.mapData, unitManager, buildingManager);

        unitManager.IsTileBlockedByBuilding = (pos) => buildingManager.GetBuildingAtTile(pos) != null;

        mapGenerator = new MapGenerator(mapManager, map, tileRegistry);
        unitVisualizer = new UnitVisualizer(unitMap, tileRegistry, highlightMap, tileRegistry.GetTile(MapData.TileType.MovementHighlight));
        buildingVisualizer = new BuildingVisualizer(buildingMap, tileRegistry.GetTile(MapData.TileType.Building));
        influenceVisualizer = new InfluenceVisualizer(influenceMap, tileRegistry.GetTile(MapData.TileType.Border));

        gameManager = new GameManager(mapManager, unitManager, buildingManager, productionManager, unitVisualizer, buildingVisualizer);


        if (!isTrainingMode)
        {
            unitVisualizer.SetAnimationRunner(visualsManager);

            AddVisualEventListeners();

            inputController.mapData = mapManager.mapData;
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
        influenceManager.OnInfluenceChanged += influenceVisualizer.DrawBorders;
        unitManager.OnUnitCreated += unitVisualizer.ShowUnitAt; // You'll need to create this method in Visualizer
    }

    public void StartGame()
    {
        mapGenerator.Generate();
        
        gameManager.Start();
    }
}
