using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingVisualizer
{
    private Tilemap buildingTilemap;

    public BuildingVisualizer(Tilemap buildingTilemap)
    {
        this.buildingTilemap = buildingTilemap;
    }

    public void ShowBuilding(Building building)
    {
        foreach (var tilePos in building.GetOccupiedTiles())
        {
            Vector3Int pos = new Vector3Int(tilePos.x, tilePos.y, 0);
            buildingTilemap.SetTile(pos, building.data.buildingTile);

            buildingTilemap.SetTileFlags(pos, TileFlags.None);
            buildingTilemap.SetColor(pos, GetPlayerColor(building.ownerId));
        }
    }

    private Color GetPlayerColor(int playerId)
    {
        return playerId switch
        {
            1 => Color.cyan,
            2 => new Color(1f, 0.3f, 0.3f), // Soft red
            _ => Color.white
        };
    }

    public void RemoveBuilding(Vector2Int pos)
    {

    }
}
