using UnityEngine;
using UnityEngine.Tilemaps;

public class InfluenceVisualizer
{
    private GameManager gameManager;

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

    private Color GetPlayerColor(int playerId)
    {
        Color baseColor = gameManager.GetPlayerProfile(playerId).playerColor;
        return new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
    }

    public void SetGameManager(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }
}
