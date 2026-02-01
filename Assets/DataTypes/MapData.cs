using UnityEngine;
using System.Collections.Generic;

public class MapData
{
    //-------------------------
    //Lehet at lesz dolgozva
    public enum TileType
    {
        Grass,
        Forest,
        Mountain,
        Gold,     
        MovementHighlight,
        Border
    };

    [System.Serializable]
    public struct TileData
    {
        public TileType type;
        public bool isPassable;
    }
    //-------------------------

    public int mapWidth;
    public int mapHeight;
    public TileData[,] mapTiles;
    public int[,] influenceMap;
    public float[,] moveCostMap;
    public Dictionary<Vector2Int, Unit> units = new Dictionary<Vector2Int, Unit>();
    public Dictionary<Vector2Int, Building> buildings = new Dictionary<Vector2Int, Building>();

    public MapData(int width, int height)
    {
        this.mapWidth = width;
        this.mapHeight = height;
        mapTiles = new TileData[mapWidth, mapHeight];
        influenceMap = new int[mapWidth, mapHeight];
        moveCostMap = new float[mapWidth, mapHeight];
    }

    public TileData GetTileData(int x, int y)
    {
        return mapTiles[x, y];
    }

    public void SetTileData(int x, int y, TileData data)
    {
        mapTiles[x, y] = data;
    }

    public void InitializeMoveCostMap()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (!mapTiles[x, y].isPassable)
                    moveCostMap[x, y] = Mathf.Infinity;
                else if (mapTiles[x, y].type == TileType.Forest)
                    moveCostMap[x, y] = 2.0f;
                else
                    moveCostMap[x, y] = 1.0f;
            }
        }
    }
}
