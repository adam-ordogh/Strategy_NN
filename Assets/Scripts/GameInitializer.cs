using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.MLAgents.Policies;
//using Unity.InferenceEngine;


public class GameInitializer : MonoBehaviour
{
    public bool isTrainingMode;
    public bool turnOffVisualsInTraining = true; // Ez a flag kikapcsolja a vizuális elemeket, ha edzés módban vagyunk, hogy gyorsítsa a tanulást
    public bool isAiVsAiMode;
    public ModelAsset trainedAiModel;

    private MapGenerator mapGenerator;

    public InputController inputController;
    public GameUIController gameUiController;
    public MinimapController minimapController;

    public Tilemap map;
    public Tilemap featureMap;
    public Tilemap influenceMap;
    public Tilemap highlightMap;
    public Tilemap unitMap;
    public Tilemap buildingMap;

    public GameObject healthBarPrefab;
    public GameObject workerBarPrefab;
    public GameObject buildingBasePrefab;
    public GameObject unitBasePrefab;

    public Transform terrainFeaturesContainer;
    public TileRegistry tileRegistry;
    public MapVisualizer mapVisualizer;
    private UnitVisualizer unitVisualizer;
    public BuildingVisualizer buildingVisualizer;
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
        //isTrainingMode = false;
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
        unitVisualizer = new UnitVisualizer(unitBasePrefab, highlightMap, tileRegistry.GetTile(MapData.TileType.MovementHighlight), healthBarPrefab);
        buildingVisualizer = new BuildingVisualizer(buildingMap, buildingBasePrefab, healthBarPrefab, workerBarPrefab);
        influenceVisualizer = new InfluenceVisualizer(influenceMap, tileRegistry.GetTile(MapData.TileType.Border));

        gameManager = new GameManager(mapManager, unitManager, buildingManager, productionManager, economyManager, unitVisualizer, buildingVisualizer);

        InitializeComponents();

        unitManager.IsTileBlockedByBuilding = (pos) => buildingManager.GetBuildingAtTile(pos) != null && buildingManager.GetBuildingAtTile(pos).buildingType != Building.BuildingType.Road;

        AddEconomyListeners();

        if (!turnOffVisualsInTraining)
        {
            minimapController.Initialize(mapManager.mapData, gameManager);
            unitVisualizer.SetAnimationRunner(visualsManager);

            unitVisualizer.SetGameManager(gameManager);
            buildingVisualizer.SetGameManager(gameManager);
            influenceVisualizer.SetGameManager(gameManager);

            AddVisualEventListeners();

            inputController.mapData = mapManager.mapData;
            inputController.unitVisualizer = unitVisualizer;
            inputController.buildingVisualizer = buildingVisualizer;
            inputController.buildingManager = buildingManager;
            inputController.gameManager = gameManager;

            gameUiController.UpdateUI();
            gameUiController.turnLabel.text = $"<sprite name=\"turn_icon\"> 1";
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

        // Use different player initialization based on mode
        // Against deterministic
        //if (isTrainingMode)
        //{
        //    var aiTypes = new List<AIFactory.AIType>
        //    {
        //        AIFactory.AIType.MLBasic,      // Player 1 - ML agent
        //        AIFactory.AIType.Deterministic  // Player 2 - Opponent
        //    };
        //    gameManager.InitializePlayersWithCustomAI(aiTypes);
        //    inputController.enabled = false;
        //    //gameManager.NextTurn(); // Start the first turn immediately for training mode
        //}
        // Self play
        //if (isTrainingMode)
        //{
        //    var aiTypes = new List<AIFactory.AIType>
        //    {
        //        AIFactory.AIType.MLBasic,  // Player 1 - ML agent
        //        AIFactory.AIType.MLBasic   // Player 2 - also ML agent, same behavior
        //    };
        //    gameManager.InitializePlayersWithCustomAI(aiTypes);
        //    inputController.enabled = false;
        //}
        //else if(isAiVsAiMode)
        //{
        //    var aiTypes = new List<AIFactory.AIType>
        //    {
        //        AIFactory.AIType.MLBasic,  // Player 1 - Deterministic AI
        //        AIFactory.AIType.Deterministic          // Player 2 - Random AI
        //    };
        //    gameManager.InitializePlayersWithCustomAI(aiTypes);
        //    inputController.enabled = false;
        //}
        if (isTrainingMode)
        {
            var aiTypes = new List<AIFactory.AIType> { AIFactory.AIType.MLBasic, AIFactory.AIType.MLBasic };
            //var aiTypes = new List<AIFactory.AIType> { AIFactory.AIType.MLBasic, AIFactory.AIType.Deterministic };

            gameManager.InitializePlayersWithCustomAI(aiTypes, isTraining: true, trainedAiModel);
            if(turnOffVisualsInTraining)
                inputController.enabled = false;
        }
        else if (isAiVsAiMode)
        {
            //var aiTypes = new List<AIFactory.AIType> { AIFactory.AIType.MLBasic, AIFactory.AIType.MLBasic };
            var aiTypes = new List<AIFactory.AIType> { AIFactory.AIType.MLBasic, AIFactory.AIType.Deterministic };
            //var aiTypes = new List<AIFactory.AIType> { AIFactory.AIType.Deterministic, AIFactory.AIType.Deterministic };
            
            gameManager.InitializePlayersWithCustomAI(aiTypes, isTraining: false, trainedAiModel);
            if(turnOffVisualsInTraining)
                inputController.enabled = false;
        }
        else
        {
            gameManager.InitializePlayers();

            AIFactory.AIType selectedAI = LevelLoadBridge.OpponentType;
            var aiTypes = new List<AIFactory.AIType> { selectedAI };

            Debug.Log($"Selected opponent AI type: {selectedAI}");

            gameManager.InitializePlayersWithCustomAI(aiTypes, isTraining: false, trainedAiModel);
            inputController.enabled = true;
        }
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
        buildingManager.OnEnvironmentChanged += mapVisualizer.HandleEnvironmentChange;

        // Felhasználói felület események
        //inputController.OnSelectionChanged += gameUiController.RefreshSelectedBuildingUI;
        inputController.OnSelectionChanged += gameUiController.RefreshSelectionUI;

        gameUiController.Subscribe(buildingManager, productionManager);
        gameManager.OnGameOver += gameUiController.ShowGameOverScreen;

        buildingManager.OnBuildingPlaced += (b) => minimapController.UpdateMinimap();
        buildingManager.OnBuildingRemoved += (b) => minimapController.UpdateMinimap();
        unitManager.OnUnitMoved += (u, pos) => minimapController.UpdateMinimap();
        influenceManager.OnInfluenceChanged += (data) => minimapController.UpdateMinimap();

        // Ez csak teszt jelleggel van itt, nem fog minden játékosra feliratkozni
        gameUiController.SubscribeToPlayerUpdates(gameManager.players[0]);
        //gameUiController.SubscribeToPlayerUpdates(gameManager.players[1]);
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


    public void StartGame()
    {
        // Betöltés
        if (!string.IsNullOrEmpty(LevelLoadBridge.SaveFileToLoad))
        {
            string fileToLoad = LevelLoadBridge.SaveFileToLoad;
            LevelLoadBridge.SaveFileToLoad = ""; 

            FindAnyObjectByType<SaveManager>().LoadGame(fileToLoad);
            return;
        }

        // Új játék
        int seedToUse = LevelLoadBridge.MapSeed != 0 ? LevelLoadBridge.MapSeed : UnityEngine.Random.Range(1, 999999);
        mapGenerator.SetSeed(seedToUse);

        
        mapGenerator.Generate();

        if (!turnOffVisualsInTraining)
        {
            mapVisualizer.DrawMap(mapManager.mapData, mapManager.mapWidth, mapManager.mapHeight);
        }

        gameManager.InitializeStartingTownCenters();
        gameManager.Start();
    }
}
