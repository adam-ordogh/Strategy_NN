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

    public void ShowBuilding(Building building)
    {
        Vector3Int pos = new Vector3Int(building.position.x, building.position.y, 0);
        buildingTilemap.SetTile(pos, buildingTile);
    }

    public void RemoveBuilding(Vector2Int pos)
    {

    }
}
