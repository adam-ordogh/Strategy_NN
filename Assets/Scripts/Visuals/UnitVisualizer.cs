using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class UnitVisualizer
{
    private Tilemap unitTilemap;
    private TileRegistry tileRegistry;

    private Tilemap highlightTilemap;
    private TileBase highlightTile;

    public UnitVisualizer(Tilemap unitTilemap, TileRegistry tileRegistry, Tilemap highlightTilemap, TileBase highlightTile)
    {
        this.unitTilemap = unitTilemap;
        this.tileRegistry = tileRegistry;
        this.highlightTilemap = highlightTilemap;
        this.highlightTile = highlightTile;
    }

    public void ShowUnit(Unit unit)
    {
        Vector3Int pos = new Vector3Int(unit.position.x, unit.position.y, 0);

        TileBase unitTile = tileRegistry.GetTile(GetTileTypeFromUnit(unit.unitType));
        unitTilemap.SetTile(pos, unitTile);

        unitTilemap.SetTileFlags(pos, TileFlags.None);
        unitTilemap.SetColor(pos, GetPlayerColor(unit.ownerId));
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

    private MapData.TileType GetTileTypeFromUnit(Unit.UnitType unitType)
    {
        return unitType switch
        {
            Unit.UnitType.Soldier => MapData.TileType.UnitType_Soldier,
            Unit.UnitType.Archer => MapData.TileType.UnitType_Archer,
            Unit.UnitType.Cavalry => MapData.TileType.UnitType_Cavalry,
            _ => MapData.TileType.UnitType_Soldier
        };
    }

    public void MoveUnit(Unit unit, Vector2Int from, Vector2Int to)
    {
        unitTilemap.SetTile(new Vector3Int(from.x, from.y, 0), null);
        ShowUnit(unit);
    }

    public void RemoveUnit(Vector2Int pos)
    {
        unitTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), null);
    }

    // Event handlers ------------------------------
    public void HandleUnitMoved(Unit unit, Vector2Int fromPos, Vector2Int toPos)
    {
        MoveUnit(unit, fromPos, toPos);
    }

    public void HandleUnitDied(Vector2Int pos)
    {
        RemoveUnit(pos);
    }

    // Highlighting methods -------------------------  
    public void ShowHighlights(IEnumerable<Vector2Int> positions, Color color)
    {
        foreach (var pos in positions)
        {
            Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
            highlightTilemap.SetTile(tilePos, highlightTile);

            // Unlock color and apply the tint
            highlightTilemap.SetTileFlags(tilePos, TileFlags.None);
            highlightTilemap.SetColor(tilePos, color);
        }
    }

    public void ClearHighlights()
    {
        highlightTilemap.ClearAllTiles();
    }


}
