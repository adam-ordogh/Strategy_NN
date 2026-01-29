using UnityEngine;
using UnityEngine.Tilemaps;

public class InfluenceVisualizer
{
    private Tilemap borderTilemap;
    private TileBase borderTile;

    public InfluenceVisualizer(Tilemap tilemap, TileBase tile)
    {
        this.borderTilemap = tilemap;
        this.borderTile = tile;
    }

    public void DrawBorders(MapData mapData)
    {
        borderTilemap.ClearAllTiles();

        for (int x = 0; x < mapData.mapWidth; x++)
        {
            for (int y = 0; y < mapData.mapHeight; y++)
            {
                int ownerId = mapData.influenceMap[x, y];
                if (ownerId != 0) 
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    borderTilemap.SetTile(tilePos, borderTile);
                    borderTilemap.SetTileFlags(tilePos, TileFlags.None);
                    borderTilemap.SetColor(tilePos, GetPlayerColor(ownerId));
                }
            }
        }
    }

    private Color GetPlayerColor(int id) => id == 1 ? new Color(0, 1, 1, 0.2f) : new Color(1, 0, 0, 0.3f);
}
