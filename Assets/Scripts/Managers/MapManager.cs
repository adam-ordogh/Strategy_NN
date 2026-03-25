using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{    
    public int mapWidth = 50;
    public int mapHeight = 50;
    public MapData mapData { get; private set; }

    void Awake()
    {
        mapData = new MapData(mapWidth, mapHeight);
    }

    public void Initialize()
    {
        mapData = new MapData(mapWidth, mapHeight);
    }

    public void GetMapData()
    {
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                Debug.Log($"Tile ({i},{j}): Type={mapData.GetTileData(i, j).type}, Passable={mapData.GetTileData(i, j).isPassable}");
            }
        }
    }
    
    public void ListUnits()
    {
        foreach (var kvp in mapData.units)
        {
            Vector2Int pos = kvp.Key;
            Unit unit = kvp.Value;
            Debug.Log($"Unit at ({pos.x},{pos.y}): Type={unit.data.unitType}, Player={unit.ownerId}, Health={unit.currentHealth}, Attack={unit.data.attackPower}, Movement range={unit.data.movementRange}");
        }
    }

    public void ListBuildings()
    {
        foreach (var kvp in mapData.buildings)
        {
            Vector2Int pos = kvp.Key;
            Building building = kvp.Value;
            Debug.Log($"Building at ({pos.x},{pos.y}): Type={building.buildingType}, Player={building.ownerId}");
        }
    }

    public void ReplaceMapData(int width, int height, List<TileSaveData> savedTiles)
    {
        mapWidth = width;
        mapHeight = height;
        mapData = new MapData(width, height); 

        foreach (var t in savedTiles)
        {
            mapData.SetTileData(t.x, t.y, new MapData.TileData { type = t.type, isPassable = t.isPassable });
        }
    }
}
