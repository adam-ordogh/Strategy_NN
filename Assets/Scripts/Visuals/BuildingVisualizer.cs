using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingVisualizer
{
    private Tilemap buildingTilemap;
    private Dictionary<Building, GameObject> spawnedBuildings = new Dictionary<Building, GameObject>();
    private GameObject buildingPrefab;
    private TileRegistry tileRegistry => Resources.Load<TileRegistry>("TileRegistry");

    public BuildingVisualizer(Tilemap buildingTilemap, GameObject buildingBasePrefab)
    {
        this.buildingTilemap = buildingTilemap;
        this.buildingPrefab = buildingBasePrefab;
    }

    public void ShowBuilding(Building building)
    {
        // 1. Handle Roads (Tilemap only, no prefab)
        if (building.data.buildingType == Building.BuildingType.Road && building.isConstructed)
        {
            SetRoadTiles(building, tileRegistry.roadRuleTile);
            return;
        }

        // 2. Instantiate Prefab
        // Since Pivot is Bottom-Left, instantiating at integer coordinates (position.x, position.y)
        // aligns the sprite's bottom-left corner with the grid cell corner.
        Vector3 worldPos = new Vector3(building.position.x, building.position.y, 0);
        GameObject instance = Object.Instantiate(buildingPrefab, worldPos, Quaternion.identity);

        // 3. Configure Renderer based on state
        UpdateRendererState(instance, building);

        // 4. Track instance
        spawnedBuildings[building] = instance;
    }

    public void UpdateVisualsToFinished(Building building)
    {
        // 1. Set the footprint on the tilemap (Roads/Foundations)
        SetRoadTiles(building, tileRegistry.roadRuleTile);

        // 2. Handle Logic
        if (building.data.buildingType == Building.BuildingType.Road)
        {
            // If it was a construction site, destroy it and leave just the tiles
            RemoveBuilding(building);
        }
        else
        {
            if (spawnedBuildings.TryGetValue(building, out GameObject instance))
            {
                // Re-run the renderer configuration to switch from Construction -> Finished
                UpdateRendererState(instance, building);
            }
            else
            {
                // Fallback: If for some reason the building isn't tracked, spawn it now
                ShowBuilding(building);
            }
        }
    }

    private void UpdateRendererState(GameObject instance, Building building)
    {
        // 1. Get references to the permanent building visuals
        Transform mainSpriteTrans = instance.transform.Find("MainSprite");
        Transform trimTrans = instance.transform.Find("ColorTrim");

        SpriteRenderer baseSr = mainSpriteTrans.GetComponent<SpriteRenderer>();
        SpriteRenderer trimSr = trimTrans.GetComponent<SpriteRenderer>();

        // 2. Handle Layer Sorting (Same as before)
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

        // 3. LOGIC SPLIT: Construction vs Finished
        string constructionObjName = "ConstructionLayer_TEMP";
        Transform existingConstruction = instance.transform.Find(constructionObjName);

        if (!building.isConstructed && building.data.constructionSprite != null)
        {
            // ============================
            // STATE: UNDER CONSTRUCTION
            // ============================

            // A. Hide the actual building visuals completely
            baseSr.enabled = false;
            trimSr.enabled = false;

            // B. Create (or get) the temporary construction object
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

            // C. Setup the Construction Sprite (Tiled)
            constructionSr.sprite = building.data.constructionSprite;
            constructionSr.sortingLayerName = baseSr.sortingLayerName;
            constructionSr.sortingOrder = baseSr.sortingOrder; // Same sort order as building

            constructionSr.drawMode = SpriteDrawMode.Tiled;
            constructionSr.tileMode = SpriteTileMode.Continuous;
            constructionSr.size = new Vector2(building.data.size.x, building.data.size.y);
        }
        else
        {
            // ============================
            // STATE: FINISHED
            // ============================

            // A. Destroy the temporary construction object if it exists
            if (existingConstruction != null)
            {
                Object.Destroy(existingConstruction.gameObject);
            }

            // B. Enable and Reset the Main Base Sprite
            baseSr.enabled = true;
            baseSr.sprite = building.data.buildingSprite;

            // STRICT RESET: Ensure no previous settings linger
            baseSr.drawMode = SpriteDrawMode.Simple;
            mainSpriteTrans.localScale = Vector3.one;
            mainSpriteTrans.localPosition = Vector3.zero;

            // C. Enable and Reset the Trim Sprite
            trimSr.enabled = true;
            trimSr.sprite = building.data.buildingColorTrim;

            // STRICT RESET: Ensure no previous settings linger
            trimSr.drawMode = SpriteDrawMode.Simple;
            trimTrans.localScale = Vector3.one;
            trimTrans.localPosition = Vector3.zero;

            // Apply Color
            if (trimSr.sprite != null)
            {
                trimSr.color = GetPlayerColor(building.ownerId);
            }
            else
            {
                trimSr.enabled = false;
            }

            // Ensure the main parent object is also scale 1
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
}
