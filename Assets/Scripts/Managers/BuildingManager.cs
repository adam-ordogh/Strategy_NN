using System.Collections.Generic;
using UnityEngine;
using static MapData;
using static Unit;
using static UnityEditor.PlayerSettings;

public class BuildingManager : MonoBehaviour
{
    public MapData mapData;
    private Building[,] occupancyGrid;
    public List<BuildingData> buildingTemplates;

    public InfluenceManager influenceManager;
    public GameManager gameManager;

    private List<Building> buildingsUnderConstruction = new List<Building>();

    public event System.Action<Building> OnBuildingPlaced;
    public event System.Action<Building> OnBuildingRemoved;
    public event System.Action<Building> OnConstructionCompleted;

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

    public Building PlaceBuilding(BuildingData template, Vector2Int pos, int ownerId)
    {
        Building newBuilding = new Building(template, ownerId, pos);
        PlayerProfile owner = gameManager.GetPlayerProfile(ownerId);

        if (!CanAffordBuilding(template, owner) || !CanPlaceBuilding(template, pos, ownerId))
            return null;

        UpdateOccupancy(newBuilding, true);
        mapData.buildings[pos] = newBuilding;

        if (owner != null)
        {
            owner.myBuildings.Add(newBuilding);
            owner.SpendResources(template.goldCost, template.woodCost, 0);
        }

        if (newBuilding.isConstructed)
        {
            ApplyBuildingEffects(newBuilding, true);
        }
        else
        {
            buildingsUnderConstruction.Add(newBuilding);
        }

        OnBuildingPlaced?.Invoke(newBuilding); // Scaffold event
        return newBuilding;
    }

    public void AdvanceConstruction(int activePlayerId)
    {
        for (int i = buildingsUnderConstruction.Count - 1; i >= 0; i--)
        {
            Building b = buildingsUnderConstruction[i];

            if (b.ownerId == activePlayerId)
            {
                b.DecrementConstruction();

                if (b.isConstructed)
                {
                    CompleteBuilding(b);
                    buildingsUnderConstruction.RemoveAt(i);
                }
            }
        }
    }

    private void CompleteBuilding(Building building)
    {
        building.DecrementConstruction(); 
        ApplyBuildingEffects(building, true);
        OnConstructionCompleted?.Invoke(building); 
    }

    public void RemoveBuilding(Building building)
    {
        if (mapData.buildings.ContainsKey(building.position))
        {
            UpdateOccupancy(building, false);
            ApplyBuildingEffects(building, false);
            mapData.buildings.Remove(building.position);

            PlayerProfile owner = gameManager.GetPlayerProfile(building.ownerId);
            if (owner != null)
            {
                owner.myBuildings.Remove(building);
            }
            OnBuildingRemoved?.Invoke(building);
        }
    }

    public void CheckBuildingHealth(Building building)
    {
        if (building.currentHp <= 0)
        {
            RemoveBuilding(building);
        }
    }

    public bool CanAffordBuilding(BuildingData data, PlayerProfile owner)
    {
        return owner != null && owner.CanAfford(data.goldCost, data.woodCost, 0);
    }

    public bool CanPlaceBuilding(BuildingData building, Vector2Int pos, int ownerId)
    {
        if (!CheckEnvironmentalRequirements(building, pos))
        {
            Debug.Log("Placement Failed: Environmental requirements not met.");
            return false;
        }

        // --- Van-e épülete a játékosnak ---
        bool playerHasBuildings = false;
        foreach (var b in mapData.buildings.Values)
        {
            if (b.ownerId == ownerId) { playerHasBuildings = true; break; }
        }

        // --- Az épület belső mezői ellenérzése ---
        for (int x = 0; x < building.size.x; x++)
        {
            for (int y = 0; y < building.size.y; y++)
            {
                Vector2Int checkPos = new Vector2Int(pos.x + x, pos.y + y);
                if (!IsTileValidForPlacement(checkPos, building.buildingType)) return false;

                int currentTileOwner = mapData.influenceMap[checkPos.x, checkPos.y];
                if (currentTileOwner != 0 && currentTileOwner != ownerId)
                    return false;

                if (playerHasBuildings && !influenceManager.IsTileOwnedBy(checkPos, ownerId))
                    return false;
            }
        }

        // --- Szomszédság megnézése ---
        // Utak nem blokkolnak semmit, így őket kihagyjuk
        if (building.buildingType != Building.BuildingType.Road)
        {
            //bool touchesRoad;

            for (int x = -1; x <= building.size.x; x++)
            {
                for (int y = -1; y <= building.size.y; y++)
                {
                    if (x >= 0 && x < building.size.x && y >= 0 && y < building.size.y) continue;

                    Vector2Int neighborPos = new Vector2Int(pos.x + x, pos.y + y);

                    if (neighborPos.x < 0 || neighborPos.x >= mapData.mapWidth ||
                        neighborPos.y < 0 || neighborPos.y >= mapData.mapHeight) continue;

                    Building neighborBuilding = GetBuildingAtTile(neighborPos);
                    if (neighborBuilding != null)
                    {
                        //if (neighborBuilding.buildingType == Building.BuildingType.Road)
                        //{
                        //    touchesRoad = true;
                        //}

                        // ---  1 mezőnyi rés ---
                        if (neighborBuilding.buildingType == Building.BuildingType.Barracks || neighborBuilding.buildingType == Building.BuildingType.TownCenter)
                        {
                            return false;
                        }

                    }
                }
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

    private bool CheckEnvironmentalRequirements(BuildingData template, Vector2Int pos)
    {
        // 1. Mines must be next to a Mountain
        if (template.buildingType == Building.BuildingType.Mine)
        {
            return HasAdjacentTileType(pos, TileType.Mountain);
        }

        // 2. Lumberyards must be next to a Forest
        if (template.buildingType == Building.BuildingType.Woodcutter)
        {
            return HasAdjacentTileType(pos, TileType.Forest);
        }

        return true;
    }

    private bool HasAdjacentTileType(Vector2Int center, TileType type)
    {
        // Check 8 neighbors (including diagonals if you want, or just 4 cardinals)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector2Int checkPos = new Vector2Int(center.x + x, center.y + y);

                // Boundary check
                if (checkPos.x < 0 || checkPos.x >= mapData.mapWidth ||
                    checkPos.y < 0 || checkPos.y >= mapData.mapHeight) continue;

                if (mapData.mapTiles[checkPos.x, checkPos.y].type == type)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsTileValidForPlacement(Vector2Int pos, Building.BuildingType type)
    {
        if (pos.x < 0 || pos.x >= mapData.mapWidth || pos.y < 0 || pos.y >= mapData.mapHeight)
            return false;
        if (!mapData.mapTiles[pos.x, pos.y].isPassable)
            return false;
        if (type != Building.BuildingType.Road && mapData.units.ContainsKey(pos))
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

    private void ApplyBuildingEffects(Building building, bool isApplying)
    {
        foreach (var tile in building.GetOccupiedTiles())
        {
            if (isApplying)
            {   
                mapData.moveCostMap[tile.x, tile.y] = building.data.movementCostModifier;
            }
            else
            {
                mapData.moveCostMap[tile.x, tile.y] = mapData.mapTiles[tile.x, tile.y].isPassable ? 1f : Mathf.Infinity;
            }
        }

        if (isApplying)
        {
            influenceManager.AddBuildingToReachGrid(building);
            influenceManager.RecalculateInfluence(building);
        }
    }

}
