using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator
{
    private MapManager mapManager;

    private float offsetX;
    private float offsetY;

    private Vector2Int player1Start => new Vector2Int(5, mapManager.mapHeight - 39);

    private Vector2Int player2Start => new Vector2Int(mapManager.mapWidth - 8, 36);

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

        ClearArea(player1Start, 6);
        ClearArea(player2Start, 6);

        mapManager.mapData.InitializeMoveCostMap();
    }

    public void SetSeed(int seed)
    {
        Random.InitState(seed);

        offsetX = Random.Range(-100000f, 100000f);
        offsetY = Random.Range(-100000f, 100000f);
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
        // Add the offsets to the coordinates!
        float noise = Mathf.PerlinNoise((x + offsetX) * 0.1f, (y + offsetY) * 0.15f);
        if (noise < 0.3f) return MapData.TileType.Forest;
        else if (noise < 0.7f) return MapData.TileType.Grass;
        else return MapData.TileType.Mountain;
    }

    private void ClearArea(Vector2Int center, int radius)
    {
        Vector2Int visualCenter = new Vector2Int(center.x + 1, center.y + 1);

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

        int mountainX = (visualCenter.x < mapManager.mapWidth / 2)
         ? Mathf.Max(0, visualCenter.x - radius)
         : Mathf.Min(mapManager.mapWidth - 1, visualCenter.x + radius);

        PlaceGuaranteedTile(new Vector2Int(mountainX, visualCenter.y), MapData.TileType.Mountain);

        // 3. Place Forest (Vertical Edge)
        int forestY = (visualCenter.y < mapManager.mapHeight / 2)
            ? Mathf.Max(0, visualCenter.y - radius)
            : Mathf.Min(mapManager.mapHeight - 1, visualCenter.y + radius);

        PlaceGuaranteedTile(new Vector2Int(visualCenter.x, forestY), MapData.TileType.Forest);
    }

    private void PlaceGuaranteedTile(Vector2Int pos, MapData.TileType type)
    {
        if (pos.x >= 0 && pos.x < mapManager.mapWidth && pos.y >= 0 && pos.y < mapManager.mapHeight)
        {
            SetData(pos.x, pos.y, type);
        }
    }

}

