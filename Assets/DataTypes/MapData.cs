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
    public Dictionary<Vector2Int, Unit> units = new Dictionary<Vector2Int, Unit>();
    public Dictionary<Vector2Int, Building> buildings = new Dictionary<Vector2Int, Building>();

    public MapData(int width, int height)
    {
        this.mapWidth = width;
        this.mapHeight = height;
        mapTiles = new TileData[mapWidth, mapHeight];
        influenceMap = new int[mapWidth, mapHeight];
    }

    public TileData GetTileData(int x, int y)
    {
        return mapTiles[x, y];
    }

    public void SetTileData(int x, int y, TileData data)
    {
        mapTiles[x, y] = data;
    }    
}
