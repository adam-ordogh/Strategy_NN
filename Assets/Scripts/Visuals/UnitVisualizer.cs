using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

public class UnitVisualizer
{
    private Tilemap unitTilemap;
    private TileRegistry tileRegistry;

    private Tilemap highlightTilemap;
    private TileBase highlightTile;

    public VisualsManager runner;
    private float moveSpeed = 5f;
    private Coroutine activeMarchCoroutine;

    public bool IsBusy() => activeMarchCoroutine != null;

    public UnitVisualizer(Tilemap unitTilemap, TileRegistry tileRegistry, Tilemap highlightTilemap, TileBase highlightTile)
    {
        this.unitTilemap = unitTilemap;
        this.tileRegistry = tileRegistry;
        this.highlightTilemap = highlightTilemap;
        this.highlightTile = highlightTile;
    }

    public void SetAnimationRunner(VisualsManager runner)
    {
        this.runner = runner;
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

    public void HandleUnitMoved(Unit unit, List<Vector2Int> path)
    {
        if (runner == null || path == null || path.Count < 2) return;

        activeMarchCoroutine = runner.StartCoroutine(AnimateMarch(unit, path));

    }

    private IEnumerator AnimateMarch(Unit unit, List<Vector2Int> path)
    {
        Vector3Int startCell = new Vector3Int(path[0].x, path[0].y, 0);

        // 1. Jelenlegi egység sprite lekérése
        TileBase startTile = unitTilemap.GetTile(startCell);
        Sprite unitSprite = null;
        if (startTile is Tile t) unitSprite = t.sprite;

        // 2. Átmeneti objektum létrehozása a mozgáshoz
        GameObject walker = new GameObject("WalkingUnit");
        SpriteRenderer sr = walker.AddComponent<SpriteRenderer>();
        sr.sprite = unitSprite;
        sr.color = GetPlayerColor(unit.ownerId); 
        sr.sortingOrder = 10; 
       
        walker.transform.position = unitTilemap.GetCellCenterWorld(startCell);

        // 3. Az eredeti tile eltávolítása a kezdőpozícióból
        unitTilemap.SetTile(startCell, null);

        // 4. Menetelés a megadott útvonalon
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 targetPos = unitTilemap.GetCellCenterWorld(new Vector3Int(path[i].x, path[i].y, 0));

            while (Vector3.Distance(walker.transform.position, targetPos) > 0.05f)
            {
                walker.transform.position = Vector3.MoveTowards(
                    walker.transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );
                yield return null;
            }
        }

        // 5. Az objektum eltávolítása és az egység megjelenítése a célpozíción
        GameObject.Destroy(walker);
        ShowUnit(unit);
        activeMarchCoroutine = null;
    }
    public void HandleUnitDied(Vector2Int pos)
    {
        if (runner != null)
        {            
            runner.StartAnimation(DelayedDeathRoutine(pos));
        }
        else
        {
            RemoveUnit(pos);
        }
    }

    private IEnumerator DelayedDeathRoutine(Vector2Int pos)
    {
        if (activeMarchCoroutine != null)
        {
            yield return activeMarchCoroutine;
        }

        RemoveUnit(pos);
        activeMarchCoroutine = null;
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
