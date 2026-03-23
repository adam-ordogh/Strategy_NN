using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.U2D.Aseprite;

public class SaveManager : MonoBehaviour
{
    // CHANGE 1: Reference the Initializer instead of GameManager directly
    public GameInitializer gameInitializer;
    public MinimapController minimapController;

    public void SaveGame(string saveFileName)
    {
        // Safety check
        if (gameInitializer == null || gameInitializer.gameManager == null)
        {
            Debug.LogError("Save Failed: GameInitializer or GameManager is missing!");
            return;
        }

        // Grab the active game manager
        GameManager gameManager = gameInitializer.gameManager;
        GameSaveData data = new GameSaveData();

        // 1. Core Data
        data.turnNumber = gameManager.turnNumber;
        data.currentPlayerIndex = gameManager.players.FindIndex(p => p.playerId == gameManager.currentPlayerId);
        data.saveDate = System.DateTime.Now.ToString("g");

        // 2. MAP TILES (The Flattening Logic)
        data.mapWidth = gameManager.mapManager.mapWidth;
        data.mapHeight = gameManager.mapManager.mapHeight;
        data.mapTiles = new List<TileSaveData>();

        for (int y = 0; y < data.mapHeight; y++)
        {
            for (int x = 0; x < data.mapWidth; x++)
            {
                var tile = gameManager.mapManager.mapData.GetTileData(x, y);
                data.mapTiles.Add(new TileSaveData
                {
                    x = x,
                    y = y,
                    type = tile.type,
                    isPassable = tile.isPassable
                });
            }
        }

        // 3. Players
        data.players = new List<PlayerSaveData>();
        foreach (var p in gameManager.players)
        {
            data.players.Add(new PlayerSaveData
            {
                playerId = p.playerId,
                isAi = p.isAi,
                gold = p.gold,
                wood = p.wood,
                food = p.food,
                currentPopulation = p.currentPopulation
            });
        }

        // 4. Buildings & Units
        data.buildings = new List<BuildingSaveData>();
        foreach (var b in gameManager.mapManager.mapData.buildings.Values)
        {
            data.buildings.Add(new BuildingSaveData
            {
                templateName = b.data.name.Replace("(Instance)", "").Trim(),
                position = b.position,
                ownerId = b.ownerId,
                currentHp = b.currentHp,
                assignedWorkers = b.assignedWorkers,
                isConstructed = b.isConstructed,
                turnsRemaining = b.turnsRemaining
            });
        }

        data.units = new List<UnitSaveData>();
        foreach (var u in gameManager.mapManager.mapData.units.Values)
        {
            data.units.Add(new UnitSaveData
            {
                templateName = u.data.name.Replace("(Instance)", "").Trim(),
                position = u.position,
                ownerId = u.ownerId,
                currentHealth = u.currentHealth
            });
        }

        // 5. Production Queues
        data.productionQueues = new List<ProductionQueueSaveData>();
        foreach (var building in gameManager.mapManager.mapData.buildings.Values)
        {
            var queue = gameManager.productionManager.GetQueueForBuilding(building);
            if (queue != null && queue.Count > 0)
            {
                var queueData = new ProductionQueueSaveData
                {
                    buildingPosition = building.position,
                    orders = new List<ProductionOrderSaveData>()
                };
                foreach (var order in queue)
                {
                    queueData.orders.Add(new ProductionOrderSaveData
                    {
                        unitTemplateName = order.template.name.Replace("(Instance)", "").Trim(),
                        turnsRemaining = order.turnsRemaining
                    });
                }
                data.productionQueues.Add(queueData);
            }
        }

        // 6. JSON Serialization
        string json = JsonUtility.ToJson(data, true);
        string folderPath = Application.persistentDataPath + "/Saves";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        File.WriteAllText(Path.Combine(folderPath, saveFileName + ".json"), json);

        // 7. Minimap Snapshot
        byte[] imgBytes = minimapController.GetMinimapPngBytes();
        if (imgBytes != null)
        {
            File.WriteAllBytes(Path.Combine(folderPath, saveFileName + ".png"), imgBytes);
        }

        Debug.Log($"Game saved successfully to: {folderPath}/{saveFileName}.json");
    }

    public void LoadGame(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, "Saves", fileName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogError("Save file not found!");
            return;
        }

        string json = File.ReadAllText(path);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        GameManager gm = gameInitializer.gameManager;

        Debug.Log("Starting Load Sequence...");

        // 1. CLEAR CURRENT STATE
        gm.unitManager.ClearAllUnits();
        gm.buildingManager.ClearAllBuildings();

        if(gameInitializer.buildingVisualizer != null)
        {
            gameInitializer.buildingVisualizer.ClearAllBuildingTiles();
        }

        gameInitializer.influenceManager.ClearAllInfluenceData();

        // 2. RESTORE GLOBAL STATE
        gm.turnNumber = data.turnNumber;
        // To restore current player index safely:
        while (gm.players[gm.players.FindIndex(p => p.playerId == gm.currentPlayerId)].playerId != data.players[data.currentPlayerIndex].playerId)
        {
            // Cycle the turn silently to align the index, without triggering logic
            gm.GetType().GetField("currentPlayerIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(gm, data.currentPlayerIndex);
        }

        // 3. RESTORE PLAYERS
        foreach (var pData in data.players)
        {
            var profile = gm.GetPlayerProfile(pData.playerId);
            if (profile != null)
            {
                profile.gold = pData.gold;
                profile.wood = pData.wood;
                profile.food = pData.food;
                profile.currentPopulation = pData.currentPopulation;
            }
        }

        // 4. RESTORE UNITS
        foreach (var uData in data.units)
        {
            UnitData template = gm.unitManager.GetTemplateByName(uData.templateName);
            if (template != null)
            {
                Unit u = gm.unitManager.SpawnUnit(template, uData.position, uData.ownerId);
                if (u != null)
                {
                    u.LoadState(uData.currentHealth);
                }
            }
            else
            {
                Debug.LogWarning($"Could not find Unit Template: {uData.templateName}");
            }
        }

        // 4. RESTORE BUILDINGS
        foreach (var bData in data.buildings)
        {
            BuildingData template = gm.buildingManager.GetTemplateByName(bData.templateName);
            if (template != null)
            {
                // bypass "PlaceBuilding" to avoid resource costs
                Building b = new Building(template, bData.ownerId, bData.position);

                // Use our new LoadState method to bypass private sets
                b.LoadState(bData.currentHp, bData.turnsRemaining, bData.assignedWorkers, bData.isConstructed);

                // This registers it in the grid and triggers the visualizer
                gm.buildingManager.CreateBuilding(b);
            }
        }

        // 5. RESTORE PRODUCTION QUEUES
        foreach (var qData in data.productionQueues)
        {
            // Find the building we just created at this position
            Building b = gm.buildingManager.GetBuildingAtTile(qData.buildingPosition);
            if (b != null)
            {
                foreach (var orderData in qData.orders)
                {
                    UnitData uTemplate = gm.unitManager.GetTemplateByName(orderData.unitTemplateName);
                    if (uTemplate != null)
                    {
                        gm.productionManager.LoadOrderIntoQueue(b, uTemplate, orderData.turnsRemaining);
                    }
                }
            }
        }

        // 7. FINAL REFRESH
        gameInitializer.economyManager.RecalculateCapacities(gm.HumanPlayer);
        //gameInitializer.gameUiController.UpdateUI();
        if (gameInitializer.gameUiController != null)
        {
            gameInitializer.gameUiController.UpdateUI();
            // If turnLabel is public, set it directly or call a specific refresh
            gameInitializer.gameUiController.turnLabel.text = $"<sprite name=\"turn_icon\"> {gm.turnNumber}";
        }
        minimapController.UpdateMinimap();

        Debug.Log("Game Loaded Successfully!");
    }
}