using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System.Linq;
using UnityEngine.UIElements;

public class UnitVisualizer
{
    //private Tilemap unitTilemap;
    private Dictionary<Unit, GameObject> spawnedUnits = new Dictionary<Unit, GameObject>();
    private GameObject unitPrefab;
    private GameObject healthBarPrefab;

    private Tilemap highlightTilemap;
    private TileBase highlightTile;

    public VisualsManager runner;
    static private float defaultMoveSpeed = 5f;
    private float fastForwardMoveSpeed = defaultMoveSpeed * 2.5f;
    private float moveSpeed = 5f;

    private Queue<IEnumerator> animationQueue = new Queue<IEnumerator>();
    private bool isProcessingQueue = false;

    public bool IsBusy() => isProcessingQueue || animationQueue.Count > 0;

    public UnitVisualizer(GameObject unitPrefab, Tilemap highlightTilemap, TileBase highlightTile, GameObject healthBarPrefab)
    {
        this.unitPrefab = unitPrefab;
        this.highlightTilemap = highlightTilemap;
        this.highlightTile = highlightTile;
        this.healthBarPrefab = healthBarPrefab;
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
            TeleportUnit(unit, path.Last());
            return;
        }

        IEnumerator marchJob = AnimateMarch(unit, path);
        EnqueueAnimation(marchJob);
    }

    public void HandleUnitDied(Unit unit)
    {
        if (runner == null)
        {
            RemoveUnit(unit);
            return;
        }

        // Queue hogy előbb a mozgás animációk lefussanak
        EnqueueAnimation(AnimateDeath(unit));
    }

    // ---------------------- ANIMATION ROUTINES ----------------------
    private IEnumerator AnimateMarch(Unit unit, List<Vector2Int> path)
    {
        if (!spawnedUnits.TryGetValue(unit, out GameObject unitGO)) yield break;
        SpriteRenderer sr = unitGO.GetComponent<SpriteRenderer>(); // Cache the renderer

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 targetPos = new Vector3(path[i].x + 0.5f, path[i].y, 0);

            if (targetPos.x < unitGO.transform.position.x)
                unitGO.transform.localScale = new Vector3(-1, 1, 1);
            else if (targetPos.x > unitGO.transform.position.x)
                unitGO.transform.localScale = new Vector3(1, 1, 1);

            while (Vector3.Distance(unitGO.transform.position, targetPos) > 0.01f)
            {
                unitGO.transform.position = Vector3.MoveTowards(
                    unitGO.transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );

                RenderSorter.Sort(sr, unitGO.transform.position.y);

                yield return null;
            }
            unitGO.transform.position = targetPos;
        }
    }

    private IEnumerator AnimateDeath(Unit unit)
    {
        // Add death animation/particles here later!
        // yield return new WaitForSeconds(0.2f);

        RemoveUnit(unit);
        yield return null;
    }

    // ---------------------- HELPERS ----------------------
    public void ShowUnitAt(Unit unit, Vector2Int pos)
    {
        if (spawnedUnits.ContainsKey(unit)) return;

        Vector3 worldPos = new Vector3(pos.x + 0.5f, pos.y, 0);
        GameObject instance = Object.Instantiate(unitPrefab, worldPos, Quaternion.identity);

        // ------
        GameObject hpBarInstance = Object.Instantiate(healthBarPrefab, instance.transform);

        hpBarInstance.transform.localScale = new Vector3(0.005f, 0.01f, 0.01f); 
        hpBarInstance.transform.localPosition = new Vector3(0f, 1.2f, 0);

        HealthBarController hb = hpBarInstance.GetComponent<HealthBarController>();
        if (hb != null) hb.SetupForUnits(unit);

        // ---------------------

        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        sr.sprite = unit.data.unitSprite;
        sr.color = GetPlayerColor(unit.ownerId);

        sr.sortingLayerName = "WorldObjects";

        RenderSorter.Sort(sr, worldPos.y);

        spawnedUnits[unit] = instance;
    }

    public void TeleportUnit(Unit unit, Vector2Int to)
    {
        if (spawnedUnits.TryGetValue(unit, out GameObject go))
        {
            go.transform.position = new Vector3(to.x, to.y, 0);
        }
    }

    public void RemoveUnit(Unit unit)
    {
        if (spawnedUnits.TryGetValue(unit, out GameObject go))
        {
            Object.Destroy(go);
            spawnedUnits.Remove(unit);
        }
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
    private Color GetPlayerColor(int playerId)
    {
        return playerId switch
        {
            1 => Color.cyan,
            2 => new Color(1f, 0.3f, 0.3f), // Soft red
            _ => Color.white
        };
    }

}
