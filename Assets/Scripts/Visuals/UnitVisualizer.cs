using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System.Linq;

public class UnitVisualizer
{
    private Tilemap unitTilemap;
    private TileRegistry tileRegistry;

    private Tilemap highlightTilemap;
    private TileBase highlightTile;

    public VisualsManager runner;
    static private float defaultMoveSpeed = 5f;
    private float fastForwardMoveSpeed = defaultMoveSpeed * 2.5f;
    private float moveSpeed = 5f;

    private Queue<IEnumerator> animationQueue = new Queue<IEnumerator>();
    private bool isProcessingQueue = false;

    public bool IsBusy() => isProcessingQueue || animationQueue.Count > 0;

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

    // ---------------------- QUEUE MANAGEMENT ----------------------

    private void EnqueueAnimation(IEnumerator animationRoutine)
    {
        animationQueue.Enqueue(animationRoutine);

        if (!isProcessingQueue && runner != null)
        {
            runner.StartAnimation(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        isProcessingQueue = true;

        while (animationQueue.Count > 0)
        {
            IEnumerator job = animationQueue.Dequeue();
            yield return runner.StartCoroutine(job);
        }

        moveSpeed = defaultMoveSpeed;
        isProcessingQueue = false;
    }

    public void FastForward()
    {
        moveSpeed = fastForwardMoveSpeed;
    }   

    // ---------------------- EVENT HANDLERS ----------------------

    public void HandleUnitMoved(Unit unit, List<Vector2Int> path)
    {
        if (runner == null || path == null || path.Count < 2)
        {
            MoveUnit(unit, path[0], path.Last());
            return;
        }

        Sprite unitSprite = GetUnitSprite(unit);
        IEnumerator marchJob = AnimateMarch(unit, path, unitSprite);
        EnqueueAnimation(marchJob);
    }

    public void HandleUnitDied(Vector2Int pos)
    {
        if (runner == null)
        {
            RemoveUnit(pos);
            return;
        }

        EnqueueAnimation(AnimateDeath(pos));
    }

    // ---------------------- ANIMATION ROUTINES ----------------------

    private IEnumerator AnimateMarch(Unit unit, List<Vector2Int> path, Sprite sprite)
    {
        Vector3Int startCell = new Vector3Int(path[0].x, path[0].y, 0);

        GameObject walker = new GameObject($"Walker_{unit.unitType}");
        SpriteRenderer sr = walker.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = GetPlayerColor(unit.ownerId);
        sr.sortingOrder = 10;

        walker.transform.position = unitTilemap.GetCellCenterWorld(startCell);

        unitTilemap.SetTile(startCell, null);

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

        GameObject.Destroy(walker);

        ShowUnitAt(unit, path.Last());
    }

    private IEnumerator AnimateDeath(Vector2Int pos)
    {
        // Később egy rövid késleltetésetést vagy "villanás" effektust is hozzáadhatunk ide
        // yield return new WaitForSeconds(0.2f);

        RemoveUnit(pos);
        yield return null;
    }

    // ---------------------- HELPERS ----------------------

    private Sprite GetUnitSprite(Unit unit)
    {
        var tileType = GetTileTypeFromUnit(unit.unitType);
        var tileBase = tileRegistry.GetTile(tileType);

        if (tileBase is Tile tile) return tile.sprite;
        return null;
    }

    public void ShowUnitAt(Unit unit, Vector2Int pos)
    {
        Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
        TileBase unitTile = tileRegistry.GetTile(GetTileTypeFromUnit(unit.unitType));

        unitTilemap.SetTile(tilePos, unitTile);
        unitTilemap.SetTileFlags(tilePos, TileFlags.None);
        unitTilemap.SetColor(tilePos, GetPlayerColor(unit.ownerId));
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
        ShowUnitAt(unit, to);
    }

    public void RemoveUnit(Vector2Int pos)
    {
        unitTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), null);
    }

    public void ShowHighlights(IEnumerable<Vector2Int> positions, Color color)
    {
        foreach (var pos in positions)
        {
            Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
            highlightTilemap.SetTile(tilePos, highlightTile);

            highlightTilemap.SetTileFlags(tilePos, TileFlags.None);
            highlightTilemap.SetColor(tilePos, color);
        }
    }

    public void ClearHighlights()
    {
        highlightTilemap.ClearAllTiles();
    }

}
