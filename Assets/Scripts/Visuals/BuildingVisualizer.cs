using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingVisualizer
{
    private Tilemap buildingTilemap;
    private Dictionary<Building, GameObject> spawnedBuildings = new Dictionary<Building, GameObject>();
    private GameObject buildingPrefab;
    private GameObject healthBarPrefab;
    private GameObject workerBarPrefab;
    private TileRegistry tileRegistry => Resources.Load<TileRegistry>("TileRegistry");

    public BuildingVisualizer(Tilemap buildingTilemap, GameObject buildingBasePrefab, GameObject healthBarPrefab, GameObject workerBarPrefab)
    {
        this.buildingTilemap = buildingTilemap;
        this.buildingPrefab = buildingBasePrefab;
        this.healthBarPrefab = healthBarPrefab;
        this.workerBarPrefab = workerBarPrefab;
    }

    public void ShowBuilding(Building building)
    {
        if (building.data.buildingType == Building.BuildingType.Road && building.isConstructed)
        {
            SetRoadTiles(building, tileRegistry.roadRuleTile);
            return;
        }

        Vector3 worldPos = new Vector3(building.position.x, building.position.y, 0);
        GameObject instance = Object.Instantiate(buildingPrefab, worldPos, Quaternion.identity);

        GameObject hpBarInstance = Object.Instantiate(healthBarPrefab, instance.transform);
        hpBarInstance.transform.localPosition = new Vector3(building.data.size.x / 2f, building.data.size.y + 0.2f, 0);
        HealthBarController hb = instance.GetComponentInChildren<HealthBarController>();
        if (hb != null)
        {
            hb.SetupForBuildings(building);
        }

        GameObject workerBarInstance = Object.Instantiate(workerBarPrefab, instance.transform);
        workerBarInstance.transform.localPosition = new Vector3(building.data.size.x / 2f, building.data.size.y + 0.6f, 0);
        WorkerBarController wb = workerBarInstance.GetComponent<WorkerBarController>();
        if (wb != null)
        {
            wb.Setup(building);
        }

        UpdateRendererState(instance, building);

        spawnedBuildings[building] = instance;
    }

    public void UpdateVisualsToFinished(Building building)
    {
        SetRoadTiles(building, tileRegistry.roadRuleTile);

        if (building.data.buildingType == Building.BuildingType.Road)
        {
            RemoveBuilding(building);
        }
        else
        {
            if (spawnedBuildings.TryGetValue(building, out GameObject instance))
            {
                UpdateRendererState(instance, building);
            }
            else
            {
                ShowBuilding(building);
            }
        }
    }

    private void UpdateRendererState(GameObject instance, Building building)
    {
        Transform mainSpriteTrans = instance.transform.Find("MainSprite");
        Transform trimTrans = instance.transform.Find("ColorTrim");

        SpriteRenderer baseSr = mainSpriteTrans.GetComponent<SpriteRenderer>();
        SpriteRenderer trimSr = trimTrans.GetComponent<SpriteRenderer>();

        if (building.data.buildingType == Building.BuildingType.Road)
        {
            baseSr.sortingLayerName = "GroundObjects";
            trimSr.sortingLayerName = "GroundObjects";
            baseSr.sortingOrder = -1;
        }
        else
        {
            baseSr.sortingLayerName = "WorldObjects";
            trimSr.sortingLayerName = "WorldObjects";
            RenderSorter.SortBuilding(instance, building.position.y);
        }

        string constructionObjName = "ConstructionLayer_TEMP";
        Transform existingConstruction = instance.transform.Find(constructionObjName);

        if (!building.isConstructed && building.data.constructionSprite != null)
        {
            // ============================
            // STATE: UNDER CONSTRUCTION
            // ============================

            baseSr.enabled = false;
            trimSr.enabled = false;

            GameObject constructionGO;
            SpriteRenderer constructionSr;

            if (existingConstruction == null)
            {
                constructionGO = new GameObject(constructionObjName);
                constructionGO.transform.SetParent(instance.transform);
                constructionGO.transform.localPosition = Vector3.zero;
                constructionGO.transform.localScale = Vector3.one;

                constructionSr = constructionGO.AddComponent<SpriteRenderer>();
            }
            else
            {
                constructionGO = existingConstruction.gameObject;
                constructionSr = constructionGO.GetComponent<SpriteRenderer>();
            }

            constructionSr.sprite = building.data.constructionSprite;
            constructionSr.sortingLayerName = baseSr.sortingLayerName;
            constructionSr.sortingOrder = baseSr.sortingOrder;

            constructionSr.drawMode = SpriteDrawMode.Tiled;
            constructionSr.tileMode = SpriteTileMode.Continuous;
            constructionSr.size = new Vector2(building.data.size.x, building.data.size.y);
        }
        else
        {
            // ============================
            // STATE: FINISHED
            // ============================

            if (existingConstruction != null)
            {
                Object.Destroy(existingConstruction.gameObject);
            }

            baseSr.enabled = true;
            baseSr.sprite = building.data.buildingSprite;

            baseSr.drawMode = SpriteDrawMode.Simple;
            mainSpriteTrans.localScale = Vector3.one;
            mainSpriteTrans.localPosition = Vector3.zero;

            trimSr.enabled = true;
            trimSr.sprite = building.data.buildingColorTrim;

            trimSr.drawMode = SpriteDrawMode.Simple;
            trimTrans.localScale = Vector3.one;
            trimTrans.localPosition = Vector3.zero;

            if (trimSr.sprite != null)
            {
                trimSr.color = GetPlayerColor(building.ownerId);
            }
            else
            {
                trimSr.enabled = false;
            }

            instance.transform.localScale = Vector3.one;
        }
    }

    public void RemoveBuilding(Building building)
    {
        // Clear tiles logic is up to you, usually we don't clear road tiles 
        // For now, keeping your original logic:
        //SetRoadTiles(building, null);

        if (spawnedBuildings.ContainsKey(building))
        {
            Object.Destroy(spawnedBuildings[building]);
            spawnedBuildings.Remove(building);

        }
    }

    private void SetRoadTiles(Building building, TileBase tile)
    {
        foreach (var localPos in building.GetOccupiedTiles())
        {
            Vector3Int tilePos = new Vector3Int(localPos.x, localPos.y, 0);
            buildingTilemap.SetTile(tilePos, tile);
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

    public GameObject GetVisualInstance(Building building)
    {
        if (spawnedBuildings.TryGetValue(building, out GameObject instance))
        {
            return instance;
        }
        return null;
    }
}
