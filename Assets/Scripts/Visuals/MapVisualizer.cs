using UnityEngine.Tilemaps;
using UnityEngine;

public class MapVisualizer
{
    private Tilemap groundMap;
    private Tilemap featureMap;
    private TileRegistry tileRegistry;

    public MapVisualizer(Tilemap ground, Tilemap feature, TileRegistry registry)
    {
        groundMap = ground;
        featureMap = feature;
        tileRegistry = registry;
    }

    public void DrawMap(MapData data, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tileData = data.GetTileData(x, y);
                Vector3Int pos = new Vector3Int(x, y, 0);

                groundMap.SetTile(pos, tileRegistry.GetTile(MapData.TileType.Grass));

                if (tileData.type == MapData.TileType.Forest || tileData.type == MapData.TileType.Mountain)
                {
                    featureMap.SetTile(pos, tileRegistry.GetTile(tileData.type));
                    Debug.Log($"Set feature tile at ({x},{y}) to {tileData.type}");
                }
                else
                {
                    featureMap.SetTile(pos, null); 
                }
            }
        }
    }
}