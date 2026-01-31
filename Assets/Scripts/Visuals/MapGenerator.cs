using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator
{
    private MapManager mapManager;

    private Vector2Int player1Start => new Vector2Int(5, mapManager.mapHeight - 6);
    private Vector2Int player2Start => new Vector2Int(mapManager.mapWidth - 6, 5);

    public MapGenerator(MapManager mapManager)
    {
        this.mapManager = mapManager;
    }

    public void Generate()
    {
        for (int x = 0; x < mapManager.mapWidth; x++)
        {
            for (int y = 0; y <= x; y++)
            {
                MapData.TileType type = GenerateTileType(x, y);

                SetData(x, y, type);
                SetData(mapManager.mapWidth - 1 - x, mapManager.mapHeight - 1 - y, type);
            }
        }

        ClearArea(player1Start, 4);
        ClearArea(player2Start, 4);
    }

    private void SetData(int x, int y, MapData.TileType type)
    {
        mapManager.mapData.SetTileData(x, y, new MapData.TileData
        {
            type = type,
            isPassable = type != MapData.TileType.Mountain
        });
    }

    private MapData.TileType GenerateTileType(int x, int y)
    {
        float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.15f);
        if (noise < 0.3f) return MapData.TileType.Forest;
        else if (noise < 0.7f) return MapData.TileType.Grass;
        else return MapData.TileType.Mountain;
    }

    private void ClearArea(Vector2Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
                if (pos.x < 0 || pos.y < 0 || pos.x >= mapManager.mapWidth || pos.y >= mapManager.mapHeight)
                    continue;

                //Kör alakú terület törlése
                if (x * x + y * y > radius * radius)
                    continue;

                var type = MapData.TileType.Grass;
                mapManager.mapData.SetTileData(pos.x, pos.y, new MapData.TileData
                {
                    type = type,
                    isPassable = true
                });
            }
        }
    }

}

