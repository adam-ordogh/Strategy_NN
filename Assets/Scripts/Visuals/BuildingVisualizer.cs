using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class BuildingVisualizer
{
    private Tilemap buildingTilemap;

    private Dictionary<Building, GameObject> spawnedBuildings = new Dictionary<Building, GameObject>();
    private GameObject buildingPrefab;

    public BuildingVisualizer(Tilemap buildingTilemap, GameObject buildingBasePrefab)
    {
        this.buildingTilemap = buildingTilemap;
        this.buildingPrefab = buildingBasePrefab;
    }

    public void ShowBuilding(Building building)
    {
        Vector3 worldPos = new Vector3(building.position.x, building.position.y, 0);
        GameObject instance = Object.Instantiate(buildingPrefab, worldPos, Quaternion.identity);

        SpriteRenderer baseSr = instance.transform.Find("MainSprite").GetComponent<SpriteRenderer>();
        SpriteRenderer trimSr = instance.transform.Find("ColorTrim").GetComponent<SpriteRenderer>();

        if (building.data.buildingType == Building.BuildingType.Road)
        {
            baseSr.sortingLayerName = "GroundObjects";
            trimSr.sortingLayerName = "GroundObjects";
            // Az utak mindig a legalacsonyabb rétegen legyenek
            baseSr.sortingOrder = -1;
        }
        else
        {
            baseSr.sortingLayerName = "WorldObjects";
            trimSr.sortingLayerName = "WorldObjects";

            RenderSorter.SortBuilding(instance, building.position.y);
            //trimSr.transform.localPosition = new Vector3(0, 0, -0.01f);
        }
        //baseSr.sprite = building.data.buildingSprite;

        //trimSr.sprite = building.data.buildingColorTrim;
        if (!building.isConstructed && building.data.constructionSprite != null)
        {
            baseSr.sprite = building.data.constructionSprite;
            trimSr.enabled = false;

            // CONSTRUCTION SCALING: Squash to fit the footprint exactly
            float spriteW = baseSr.sprite.bounds.size.x;
            float spriteH = baseSr.sprite.bounds.size.y;

            float scaleX = building.data.size.x / spriteW;
            float scaleY = building.data.size.y / spriteH;

            instance.transform.localScale = new Vector3(scaleX, scaleY, 1);
        }
        else
        {
            baseSr.sprite = building.data.buildingSprite;
            trimSr.sprite = building.data.buildingColorTrim;
            trimSr.enabled = true;

            // FINISHED SCALING: Uniform scale based on width to keep "extrusion" height
            float spriteW = baseSr.sprite.bounds.size.x;
            float horizontalScale = building.data.size.x / spriteW;

            instance.transform.localScale = new Vector3(horizontalScale, horizontalScale, 1);
        }

        trimSr.color = GetPlayerColor(building.ownerId);

        spawnedBuildings[building] = instance;
    }

    public void UpdateVisualsToFinished(Building building)
    {
        if (spawnedBuildings.TryGetValue(building, out GameObject go))
        {
            SpriteRenderer baseSr = go.transform.Find("MainSprite").GetComponent<SpriteRenderer>();
            SpriteRenderer trimSr = go.transform.Find("ColorTrim").GetComponent<SpriteRenderer>();

            baseSr.sprite = building.data.buildingSprite;
            trimSr.sprite = building.data.buildingColorTrim;
            trimSr.enabled = true;

            // Reset to UNIFORM scale so the building looks tall again
            float spriteW = baseSr.sprite.bounds.size.x;
            float horizontalScale = building.data.size.x / spriteW;

            go.transform.localScale = new Vector3(horizontalScale, horizontalScale, 1);

            // Ensure color is updated
            trimSr.color = GetPlayerColor(building.ownerId);
        }
    }

    public void RemoveBuilding(Building building)
    {
        if (spawnedBuildings.ContainsKey(building))
        {
            Object.Destroy(spawnedBuildings[building]);
            spawnedBuildings.Remove(building);
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
