using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameInitializer : MonoBehaviour
{
    public bool isTrainingMode;

    public InputController inputController;
    public GameUIController gameUiController;

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
    public EconomyManager economyManager;

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
        unitVisualizer = new UnitVisualizer(unitMap, highlightMap, tileRegistry.GetTile(MapData.TileType.MovementHighlight));
        buildingVisualizer = new BuildingVisualizer(buildingMap);
        influenceVisualizer = new InfluenceVisualizer(influenceMap, tileRegistry.GetTile(MapData.TileType.Border));

        gameManager = new GameManager(mapManager, unitManager, buildingManager, productionManager, economyManager, unitVisualizer, buildingVisualizer);

        InitializeComponents();

        unitManager.IsTileBlockedByBuilding = (pos) => buildingManager.GetBuildingAtTile(pos) != null;

        AddEconomyListeners();

        if (!isTrainingMode)
        {
            unitVisualizer.SetAnimationRunner(visualsManager);

            AddVisualEventListeners();

            inputController.mapData = mapManager.mapData;
            inputController.unitVisualizer = unitVisualizer;
            inputController.buildingVisualizer = buildingVisualizer;
            inputController.buildingManager = buildingManager;
            inputController.gameManager = gameManager;   
            
            gameUiController.UpdateUI();
        }
    }

    public void InitializeComponents()
    {
        mapManager.Initialize();
        unitManager.Initialize(mapManager.mapData, gameManager);
        buildingManager.Initialize(mapManager.mapData, influenceManager, gameManager);
        influenceManager.Initialize(mapManager.mapData);
        productionManager.Initialize(mapManager.mapData, unitManager, buildingManager, gameManager);
    }

    public void AddVisualEventListeners()
    {
        unitManager.OnUnitMoved += unitVisualizer.HandleUnitMoved;
        unitManager.OnUnitDestroyed += unitVisualizer.HandleUnitDied;
        influenceManager.OnInfluenceChanged += influenceVisualizer.DrawBorders;
        unitManager.OnUnitCreated += unitVisualizer.ShowUnitAt;

        inputController.OnSelectionChanged += gameUiController.RefreshSelectedBuildingUI;

        gameUiController.Subscribe(buildingManager, productionManager);

        unitManager.OnUnitCreated += (unit, pos) => {
            gameUiController.UpdateUI();
        };

        unitManager.OnUnitDestroyed += (unit) => {
            gameUiController.UpdateUI();
        };

        // Ez csak teszt jelleggel van itt, nem fog minden játékosra feliratkozni
        gameUiController.SubscribeToPlayerUpdates(gameManager.players[0]);
        gameUiController.SubscribeToPlayerUpdates(gameManager.players[1]);
    }

    public void AddEconomyListeners()
    {
        buildingManager.OnBuildingPlaced += (building) => {
            PlayerProfile owner = gameManager.GetPlayerProfile(building.ownerId);
            economyManager.RecalculateCapacities(owner);
        };

        buildingManager.OnBuildingRemoved += (building) => {
            PlayerProfile owner = gameManager.GetPlayerProfile(building.ownerId);
            economyManager.RecalculateCapacities(owner);
        };

        buildingManager.OnBuildingRemoved += (building) => {
            // Gyártási sor törlése, ha gyártási épületről van szó
            if (building.buildingType == Building.BuildingType.Barracks)
            {
                productionManager.CancelProductionForBuilding(building);
            }
        };
    }

    public void StartGame()
    {
        mapGenerator.Generate();
        
        gameManager.Start();
    }
}
