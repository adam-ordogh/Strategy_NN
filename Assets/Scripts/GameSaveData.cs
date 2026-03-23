using System.Collections.Generic;
using UnityEngine;
using static MapData;

[System.Serializable]
public class GameSaveData
{
    // Global State
    public int turnNumber;
    public int currentPlayerIndex;
    public string saveDate;

    // Map State
    public int mapWidth;
    public int mapHeight;
    public List<TileSaveData> mapTiles; // Flattened 2D array

    // Entities
    public List<PlayerSaveData> players;
    public List<BuildingSaveData> buildings;
    public List<UnitSaveData> units;

    // Production Queues
    public List<ProductionQueueSaveData> productionQueues;
}

[System.Serializable]
public struct TileSaveData
{
    public int x, y;
    public TileType type;
    public bool isPassable;
}

[System.Serializable]
public struct PlayerSaveData
{
    public int playerId;
    public bool isAi;
    public int gold, wood, food;
    public int currentPopulation;
}

[System.Serializable]
public struct BuildingSaveData
{
    public string templateName; // Important: The name of the BuildingData asset
    public Vector2Int position;
    public int ownerId;
    public int currentHp;
    public int assignedWorkers;
    public bool isConstructed;
    public int turnsRemaining;
}

[System.Serializable]
public struct UnitSaveData
{
    public string templateName;
    public Vector2Int position;
    public int ownerId;
    public int currentHealth;
}

[System.Serializable]
public struct ProductionQueueSaveData
{
    public Vector2Int buildingPosition; // We use the building's position to link the queue back to it!
    public List<ProductionOrderSaveData> orders;
}

[System.Serializable]
public struct ProductionOrderSaveData
{
    public string unitTemplateName;
    public int turnsRemaining;
}