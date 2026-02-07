using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameInitializer : MonoBehaviour
{
    public bool isTrainingMode;

    private MapGenerator mapGenerator;

    public InputController inputController;
    public GameUIController gameUiController;

    public Tilemap map;
    public Tilemap featureMap;
    public Tilemap influenceMap;
    public Tilemap highlightMap;
    public Tilemap unitMap;
    public Tilemap buildingMap;

    public GameObject buildingBasePrefab;
    public GameObject unitBasePrefab;

    public Transform terrainFeaturesContainer;
    public TileRegistry tileRegistry;
    public MapVisualizer mapVisualizer;
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
        mapGenerator = new MapGenerator(mapManager);
        mapVisualizer = new MapVisualizer(map, featureMap, tileRegistry, terrainFeaturesContainer);
        unitVisualizer = new UnitVisualizer(unitBasePrefab, highlightMap, tileRegistry.GetTile(MapData.TileType.MovementHighlight));
        buildingVisualizer = new BuildingVisualizer(buildingMap, buildingBasePrefab);
        influenceVisualizer = new InfluenceVisualizer(influenceMap, tileRegistry.GetTile(MapData.TileType.Border));

        gameManager = new GameManager(mapManager, unitManager, buildingManager, productionManager, economyManager, unitVisualizer, buildingVisualizer);

        InitializeComponents();

        unitManager.IsTileBlockedByBuilding = (pos) => buildingManager.GetBuildingAtTile(pos) != null && buildingManager.GetBuildingAtTile(pos).buildingType != Building.BuildingType.Road;

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
        economyManager.Initialize(mapManager.mapData, buildingManager);
    }

    public void AddVisualEventListeners()
    {
        // Egység események
        unitManager.OnUnitMoved += unitVisualizer.HandleUnitMoved;
        unitManager.OnUnitDestroyed += unitVisualizer.HandleUnitDied;
        unitManager.OnUnitCreated += unitVisualizer.ShowUnitAt;
        unitManager.OnUnitCreated += (unit, pos) =>
        {
            gameUiController.UpdateUI();
        };
        unitManager.OnUnitDestroyed += (unit) =>
        {
            gameUiController.UpdateUI();
        };

        // Épület események
        influenceManager.OnInfluenceChanged += influenceVisualizer.DrawBorders;
        buildingManager.OnBuildingPlaced += buildingVisualizer.ShowBuilding;
        buildingManager.OnBuildingRemoved += buildingVisualizer.RemoveBuilding;
        buildingManager.OnConstructionCompleted += buildingVisualizer.UpdateVisualsToFinished;

        // Felhasználói felület események
        inputController.OnSelectionChanged += gameUiController.RefreshSelectedBuildingUI;

        gameUiController.Subscribe(buildingManager, productionManager);

        // Ez csak teszt jelleggel van itt, nem fog minden játékosra feliratkozni
        gameUiController.SubscribeToPlayerUpdates(gameManager.players[0]);
        gameUiController.SubscribeToPlayerUpdates(gameManager.players[1]);
    }

    public void AddEconomyListeners()
    {
        buildingManager.OnBuildingPlaced += (building) =>
        {
            PlayerProfile owner = gameManager.GetPlayerProfile(building.ownerId);
            economyManager.RecalculateCapacities(owner);
        };

        buildingManager.OnBuildingRemoved += (building) =>
        {
            PlayerProfile owner = gameManager.GetPlayerProfile(building.ownerId);
            economyManager.RecalculateCapacities(owner);
        };

        buildingManager.OnBuildingRemoved += (building) =>
        {
            // Gyártási sor törlése, ha gyártási épületről van szó
            if (building.buildingType == Building.BuildingType.Barracks)
            {
                productionManager.CancelProductionForBuilding(building);
            }
        };

        //buildingManager.OnConstructionCompleted += (building) => economyManager.InvalidateTileCache(gameManager.GetPlayerProfile(building.ownerId));
        //buildingManager.OnBuildingRemoved += (building) => economyManager.InvalidateTileCache(gameManager.GetPlayerProfile(building.ownerId));
    }

    private void BuildingManager_OnConstructionCompleted(Building obj)
    {
        throw new System.NotImplementedException();
    }

    public void StartGame()
    {
        mapGenerator.Generate();

        if (!isTrainingMode)
        {
            mapVisualizer.DrawMap(mapManager.mapData, mapManager.mapWidth, mapManager.mapHeight);
        }

        gameManager.Start();
    }
}
