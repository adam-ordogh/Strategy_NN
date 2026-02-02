using System.Collections.Generic;
using UnityEngine;
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

    //public Building PlaceBuilding(BuildingData template, Vector2Int pos, int ownerId)
    //{
    //    Building newBuilding = new Building(template, ownerId, pos);
    //    PlayerProfile owner = gameManager.GetPlayerProfile(ownerId);

    //    if (!CanAffordBuilding(template, owner) || !CanPlaceBuilding(template, pos, ownerId)) // Lehet kell majd jobb resource ellenőrzés, ezt csak rögtönöztem
    //        return null;

    //    UpdateOccupancy(newBuilding, true);
    //    mapData.buildings[pos] = newBuilding;

    //    if (owner != null)
    //    {
    //        owner.myBuildings.Add(newBuilding);
    //    }

    //    influenceManager.AddBuildingToReachGrid(newBuilding);
    //    influenceManager.RecalculateInfluence(newBuilding);

    //    owner.SpendResources(template.goldCost, template.woodCost, 0);

    //    // Később itt eventeket hívhatunk meg, pl. OnBuildingPlaced?.Invoke(temp);
    //    OnBuildingPlaced?.Invoke(newBuilding);
    //    return newBuilding;
    //}
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

        // Logic Split: Instant vs. Construction
        if (newBuilding.isConstructed)
        {
            // Instant build (e.g. Roads with 0 turns)
            ActivateBuildingEffects(newBuilding);
        }
        else
        {
            // Start Construction
            buildingsUnderConstruction.Add(newBuilding);
        }

        OnBuildingPlaced?.Invoke(newBuilding); // Spawns Scaffold
        return newBuilding;
    }

    public void AdvanceConstruction(int activePlayerId)
    {
        // Iterate backwards so we can remove items safely
        for (int i = buildingsUnderConstruction.Count - 1; i >= 0; i--)
        {
            Building b = buildingsUnderConstruction[i];

            // Only advance construction if it's THIS player's building
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
        ActivateBuildingEffects(building);
        OnConstructionCompleted?.Invoke(building); // Triggers visual swap
    }

    private void ActivateBuildingEffects(Building building)
    {
        // Now we apply the influence/stats
        influenceManager.AddBuildingToReachGrid(building);
        influenceManager.RecalculateInfluence(building);

        // If it's a Barracks/Mine, register it to ProductionManager here
        // productionManager.RegisterBuilding(building); 
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
                        if (neighborBuilding.buildingType == Building.BuildingType.Barracks)
                        {
                            return false;
                        }

                    }
                }
            }

            // --- Muszáj utat érinteni ---
            //if (playerHasBuildings && !touchesRoad)
            //{
            //    // Building must be next to a road (except for the very first building)
            //    return false;
            //}

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

            if (isAdding)
            {
                mapData.moveCostMap[tile.x, tile.y] = building.data.movementCostModifier;
            }
            else
            {
                if (!mapData.mapTiles[tile.x, tile.y].isPassable)
                    mapData.moveCostMap[tile.x, tile.y] = Mathf.Infinity;
                else if (mapData.mapTiles[tile.x, tile.y].type == MapData.TileType.Forest)
                    mapData.moveCostMap[tile.x, tile.y] = 2.0f;
                else
                    mapData.moveCostMap[tile.x, tile.y] = 1.0f;
            }
        }
    }
}
