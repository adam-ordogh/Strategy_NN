using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System.Linq;
using UnityEngine.UIElements;

public class UnitVisualizer
{
    GameManager gameManager;

    private Dictionary<Unit, GameObject> spawnedUnits = new Dictionary<Unit, GameObject>();
    private GameObject unitPrefab;
    private GameObject healthBarPrefab;
    //private GameObject projectilePrefab;

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
        //this.projectilePrefab = projectilePrefab;
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

        EnqueueAnimation(AnimateDeath(unit));
    }

    //public void HandleUnitAttacked(Unit attacker, Unit target, bool isRetaliation)
    //{
    //    if (runner == null) return;

    //    EnqueueAnimation(AnimateAttack(attacker, target, isRetaliation));
    //}
    public void HandleEntityAttacked(Unit attacker, Vector2Int targetGridPos, bool isRetaliation)
    {
        if (runner == null) return;

        // Convert the grid position to world position, matching your ShowUnitAt logic
        Vector3 targetWorldPos = new Vector3(targetGridPos.x + 0.5f, targetGridPos.y, 0);

        targetWorldPos.y += 0.5f;

        EnqueueAnimation(AnimateAttack(attacker, targetWorldPos, isRetaliation));
    }

    // ---------------------- ANIMATION ROUTINES ----------------------
    private IEnumerator AnimateMarch(Unit unit, List<Vector2Int> path)
    {
        if (!spawnedUnits.TryGetValue(unit, out GameObject unitGO)) yield break;

        Transform mainSpriteTrans = unitGO.transform.Find("MainSprite");
        Transform trimTrans = unitGO.transform.Find("ColorTrim");

        SpriteRenderer baseSr = mainSpriteTrans != null ? mainSpriteTrans.GetComponent<SpriteRenderer>() : unitGO.GetComponent<SpriteRenderer>();
        SpriteRenderer trimSr = trimTrans != null ? trimTrans.GetComponent<SpriteRenderer>() : null;

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

                if (baseSr != null)
                {
                    RenderSorter.Sort(baseSr, unitGO.transform.position.y);
                    if (trimSr != null)
                    {
                        trimSr.sortingOrder = baseSr.sortingOrder - 1;
                    }
                }

                yield return null;
            }
            unitGO.transform.position = targetPos;
        }
    }

    private IEnumerator AnimateAttack(Unit attacker, Vector3 targetWorldPos, bool isRetaliation)
    {
        if (!spawnedUnits.TryGetValue(attacker, out GameObject attackerGO)) yield break;

        GameObject specificProjectile = attacker.data.attackProjectilePrefab;

        if (specificProjectile != null)
        {
            yield return RunProjectileAnimation(attackerGO, targetWorldPos, specificProjectile, isRetaliation);
        }
        else
        {
            yield return RunMeleeBumpAnimation(attackerGO, targetWorldPos);
        }

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator RunProjectileAnimation(GameObject attackerGO, Vector3 targetPos, GameObject prefab, bool isRetaliation)
    {
        Vector3 startPos = attackerGO.transform.position + new Vector3(0, 0.5f, 0);

        //GameObject projectile = Object.Instantiate(prefab, attackerGO.transform.position, Quaternion.identity);
        GameObject projectile = Object.Instantiate(prefab, startPos, Quaternion.identity);

        SpriteRenderer sr = projectile.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "WorldObjects";
            sr.sortingOrder = 100;
        }

        //Vector3 startPos = attackerGO.transform.position;
        float distance = Vector3.Distance(startPos, targetPos);
        if (distance <= 0.01f) distance = 1f;

        float duration = distance / (moveSpeed * 1.5f);
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, percent);

            float arcHeight = distance * 0.3f;
            currentPos.y += Mathf.Sin(percent * Mathf.PI) * arcHeight;

            projectile.transform.position = currentPos;

            Vector3 dir = targetPos - projectile.transform.position;
            if (dir != Vector3.zero)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                //projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                projectile.transform.rotation = Quaternion.AngleAxis(angle + 180f, Vector3.forward);
            }

            yield return null;
        }

        Object.Destroy(projectile);
    }

    //private IEnumerator RunMeleeBumpAnimation(GameObject attackerGO, GameObject targetGO)
    //{
    //    Vector3 startPos = attackerGO.transform.position;
    //    Vector3 targetPos = targetGO.transform.position;
    //    Vector3 peakPos = Vector3.Lerp(startPos, targetPos, 0.4f);

    //    float elapsed = 0;
    //    float duration = 0.15f;

    //    while (elapsed < duration)
    //    {
    //        attackerGO.transform.position = Vector3.Lerp(startPos, peakPos, elapsed / duration);
    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }

    //    elapsed = 0;
    //    while (elapsed < duration)
    //    {
    //        attackerGO.transform.position = Vector3.Lerp(peakPos, startPos, elapsed / duration);
    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }

    //    attackerGO.transform.position = startPos;
    //}
    private IEnumerator RunMeleeBumpAnimation(GameObject attackerGO, Vector3 targetPos)
    {
        Vector3 startPos = attackerGO.transform.position;
        // Lunge 40% of the way toward the target position
        Vector3 peakPos = Vector3.Lerp(startPos, targetPos, 0.4f);

        float elapsed = 0;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            attackerGO.transform.position = Vector3.Lerp(startPos, peakPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0;
        while (elapsed < duration)
        {
            attackerGO.transform.position = Vector3.Lerp(peakPos, startPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        attackerGO.transform.position = startPos;
    }

    private IEnumerator AnimateDeath(Unit unit)
    {
        RemoveUnit(unit);
        yield return null;
    }

    // ---------------------- HELPERS ----------------------
    public void ShowUnitAt(Unit unit, Vector2Int pos)
    {
        if (spawnedUnits.ContainsKey(unit)) return;

        Vector3 worldPos = new Vector3(pos.x + 0.5f, pos.y, 0);
        GameObject instance = Object.Instantiate(unitPrefab, worldPos, Quaternion.identity);

        GameObject hpBarInstance = Object.Instantiate(healthBarPrefab, instance.transform);

        hpBarInstance.transform.localScale = new Vector3(0.005f, 0.01f, 0.01f);
        hpBarInstance.transform.localPosition = new Vector3(0f, 1.2f, 0);

        HealthBarController hb = hpBarInstance.GetComponent<HealthBarController>();
        if (hb != null) hb.SetupForUnits(unit);

        // --- NEW TRIM LOGIC ---
        Transform mainSpriteTrans = instance.transform.Find("MainSprite");
        Transform trimTrans = instance.transform.Find("ColorTrim");

        if (mainSpriteTrans != null && trimTrans != null)
        {
            SpriteRenderer baseSr = mainSpriteTrans.GetComponent<SpriteRenderer>();
            SpriteRenderer trimSr = trimTrans.GetComponent<SpriteRenderer>();

            // Setup Base Sprite (Icon/Token Base)
            baseSr.sprite = unit.data.unitSprite;
            baseSr.color = Color.white;
            baseSr.sortingLayerName = "WorldObjects";
            //RenderSorter.Sort(baseSr, worldPos.y);
            RenderSorter.Sort(baseSr, worldPos.y - 0.01f);

            // Setup Color Trim
            if (unit.data.unitColorTrim != null)
            {
                trimSr.enabled = true;
                trimSr.sprite = unit.data.unitColorTrim;
                trimSr.color = GetPlayerColor(unit.ownerId);
                trimSr.sortingLayerName = "WorldObjects";
                trimSr.sortingOrder = baseSr.sortingOrder - 1; // Always render on top of the base
            }
            else
            {
                trimSr.enabled = false;
            }
        }
        else
        {
            // Fallback just in case the prefab hasn't been updated yet
            SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = unit.data.unitSprite;
                sr.color = GetPlayerColor(unit.ownerId);
                sr.sortingLayerName = "WorldObjects";
                RenderSorter.Sort(sr, worldPos.y);
            }
        }
        // ----------------------

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

    public void ShowHighlight(Vector2Int pos, Color color)
    {
        ShowHighlights(new[] { pos }, color);
    }

    public void ClearHighlights()
    {
        highlightTilemap.ClearAllTiles();
    }

    private Color GetPlayerColor(int playerId)
    {
        return gameManager.GetPlayerProfile(playerId).playerColor;
    }

    public void SetGameManager(GameManager gm)
    {
        this.gameManager = gm;
    }

    public void ClearAllVisuals()
    {
        foreach (var go in spawnedUnits.Values)
        {
            Object.Destroy(go);
        }
        spawnedUnits.Clear();
        ClearHighlights();
    }
}
