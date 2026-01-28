using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingVisualizer
{
    private Tilemap buildingTilemap;
    private TileBase buildingTile;

    public BuildingVisualizer(Tilemap buildingTilemap, TileBase buildingTile)
    {
        this.buildingTilemap = buildingTilemap;
        this.buildingTile = buildingTile;
    }

    //public void ShowBuilding(Building building)
    //{
    //    Vector3Int pos = new Vector3Int(building.position.x, building.position.y, 0);
    //    buildingTilemap.SetTile(pos, buildingTile);
    //}

    public void ShowBuilding(Building building)
    {
        // Loop through every tile the building covers
        foreach (var tilePos in building.GetOccupiedTiles())
        {
            Vector3Int pos = new Vector3Int(tilePos.x, tilePos.y, 0);
            buildingTilemap.SetTile(pos, buildingTile);

            // Optional: Tint the building based on owner
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
