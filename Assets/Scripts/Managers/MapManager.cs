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
            Debug.Log($"Unit at ({pos.x},{pos.y}): Type={unit.unitType}, Player={unit.ownerId}, Health={unit.health}, Attack={unit.attackPower}, Movement range={unit.movementPoints}");
        }
    }
}
