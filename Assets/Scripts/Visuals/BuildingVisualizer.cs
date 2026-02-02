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
        baseSr.sprite = building.data.buildingSprite;

        trimSr.sprite = building.data.buildingColorTrim;
        trimSr.color = GetPlayerColor(building.ownerId);

        // Átméretezzük a sprite-ot hogy illeszkedjen a grid méretéhez
        float spriteW = baseSr.sprite.bounds.size.x;

        // Kiszámoljuk a szükséges vízszintes skálázást
        float horizontalScale = building.data.size.x / spriteW;

        // Ugyan azt a skálázást alkalmazzuk függőlegesen is
        instance.transform.localScale = new Vector3(horizontalScale, horizontalScale, 1);

        spawnedBuildings[building] = instance;
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
