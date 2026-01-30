using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public MapData mapData;
    private Building[,] occupancyGrid;
    public List<BuildingData> buildingTemplates;

    public InfluenceManager influenceManager;
    public GameManager gameManager;

    public void Initialize(MapData mapData, InfluenceManager influenceManager, GameManager gameManager)
    {
        this.mapData = mapData;
        this.influenceManager = influenceManager;
        this.gameManager = gameManager;

        occupancyGrid = new Building[mapData.mapWidth, mapData.mapHeight];

        foreach (var building in mapData.buildings.Values)
        {
            UpdateOccupancy(building, true);
        }
    }

    public Building PlaceBuilding(Building.BuildingType type, Vector2Int pos, int ownerId)
    {
        BuildingData template = buildingTemplates.Find(t => t.buildingType == type);
        Building newBuilding = new Building(template, ownerId, pos);

        if (!CanPlaceBuilding(pos, newBuilding.size, ownerId))
            return null;

        UpdateOccupancy(newBuilding, true);
        mapData.buildings[pos] = newBuilding;

        PlayerProfile owner = gameManager.GetPlayerProfile(newBuilding.ownerId);
        if (owner != null)
        {
            owner.myBuildings.Add(newBuilding);
        }

        influenceManager.AddBuildingToReachGrid(newBuilding);
        influenceManager.RecalculateInfluence(newBuilding);

        // Később itt eventeket hívhatunk meg, pl. OnBuildingPlaced?.Invoke(temp);
        return newBuilding;
    }

    public void RemoveBuilding(Building building)
    {
        if (mapData.buildings.ContainsKey(building.position))
        {
            UpdateOccupancy(building, false);
            mapData.buildings.Remove(building.position);
            influenceManager.RemoveBuildingFromReachGrid(building);
            influenceManager.RecalculateAllInfluences();

            PlayerProfile owner = gameManager.GetPlayerProfile(building.ownerId);
            if (owner != null)
            {
                owner.myBuildings.Remove(building);
            }
        }
    }

    public bool CanPlaceBuilding(Vector2Int pos, Vector2Int size, int ownerId)
    {
        bool playerHasBuildings = false;
        foreach (var b in mapData.buildings.Values)
        {
            if (b.ownerId == ownerId) { playerHasBuildings = true; break; }
        }

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int checkPos = new Vector2Int(pos.x + x, pos.y + y);
                if (!IsTileValidForPlacement(checkPos)) return false;

                int currentTileOwner = mapData.influenceMap[checkPos.x, checkPos.y];
                if (currentTileOwner != 0 && currentTileOwner != ownerId)
                    return false;

                if (playerHasBuildings && !influenceManager.IsTileOwnedBy(checkPos, ownerId))
                    return false;
            }
        }
        return true;
    }

    // Segédfüggvény ami megnézi, hogy van-e épület egy adott mezőn, még ha az épület több mezőt is elfoglal
    public Building GetBuildingAtTile(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= mapData.mapWidth || pos.y < 0 || pos.y >= mapData.mapHeight)
            return null;

        return occupancyGrid[pos.x, pos.y];
    }

    private bool IsTileValidForPlacement(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= mapData.mapWidth || pos.y < 0 || pos.y >= mapData.mapHeight)
            return false;
        if (!mapData.mapTiles[pos.x, pos.y].isPassable)
            return false;
        if (mapData.units.ContainsKey(pos))
            return false;
        if (GetBuildingAtTile(pos) != null)
            return false;
        return true;
    }

    private void UpdateOccupancy(Building building, bool isAdding)
    {
        foreach (var tile in building.GetOccupiedTiles())
        {
            occupancyGrid[tile.x, tile.y] = isAdding ? building : null;
        }
    }
}
