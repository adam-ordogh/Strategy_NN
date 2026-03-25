using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.U2D.Aseprite;

public class SaveManager : MonoBehaviour
{
    public GameInitializer gameInitializer;
    public MinimapController minimapController;

    public void SaveGame(string saveFileName)
    {
        if (gameInitializer == null || gameInitializer.gameManager == null)
        {
            Debug.LogError("Save Failed: GameInitializer or GameManager is missing!");
            return;
        }

        GameManager gameManager = gameInitializer.gameManager;
        GameSaveData data = new GameSaveData();

        // Fő adatok
        data.turnNumber = gameManager.turnNumber;
        data.currentPlayerIndex = gameManager.players.FindIndex(p => p.playerId == gameManager.currentPlayerId);
        data.saveDate = System.DateTime.Now.ToString("g");

        // Térkép adatok
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

        // Játékosok
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

        // Épületek és egységek
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

        // Gyártási sorok
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

        // JSON mentés
        string json = JsonUtility.ToJson(data, true);
        string folderPath = Application.persistentDataPath + "/Saves";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        File.WriteAllText(Path.Combine(folderPath, saveFileName + ".json"), json);

        // Minimap mentése
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

        // Jelenlegi állapot törlése
        gm.unitManager.ClearAllUnits();
        gm.buildingManager.ClearAllBuildings();

        if(gameInitializer.buildingVisualizer != null)
        {
            gameInitializer.buildingVisualizer.ClearAllBuildingTiles();
        }

        gameInitializer.influenceManager.ClearAllInfluenceData();

        // Globális állapot visszaállítása
        gm.turnNumber = data.turnNumber;

        while (gm.players[gm.players.FindIndex(p => p.playerId == gm.currentPlayerId)].playerId != data.players[data.currentPlayerIndex].playerId)
        {
            gm.GetType().GetField("currentPlayerIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(gm, data.currentPlayerIndex);
        }

        gm.mapManager.mapWidth = data.mapWidth;
        gm.mapManager.mapHeight = data.mapHeight;

        foreach (var tData in data.mapTiles)
        {
            gm.mapManager.mapData.SetTileData(tData.x, tData.y, new MapData.TileData
            {
                type = tData.type,
                isPassable = tData.isPassable
            });
        }

        gm.mapManager.mapData.InitializeMoveCostMap();

        gameInitializer.mapVisualizer.DrawMap(gm.mapManager.mapData, gm.mapManager.mapWidth, gm.mapManager.mapHeight);

        // Játékosok viszaállítása
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

        // Egységek visszaállítása
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

        // Épületek visszaállítása
        foreach (var bData in data.buildings)
        {
            BuildingData template = gm.buildingManager.GetTemplateByName(bData.templateName);
            if (template != null)
            {
                Building b = new Building(template, bData.ownerId, bData.position);

                b.LoadState(bData.currentHp, bData.turnsRemaining, bData.assignedWorkers, bData.isConstructed);

                gm.buildingManager.CreateBuilding(b);
            }
        }

        // Gyártási sorok visszaállítása
        foreach (var qData in data.productionQueues)
        {
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

        // Végső frissítések
        gameInitializer.economyManager.RecalculateCapacities(gm.HumanPlayer);
        if (gameInitializer.gameUiController != null)
        {
            gameInitializer.gameUiController.UpdateUI();
            gameInitializer.gameUiController.turnLabel.text = $"<sprite name=\"turn_icon\"> {gm.turnNumber}";
        }
        minimapController.UpdateMinimap();

        Debug.Log("Game Loaded Successfully!");
    }
}