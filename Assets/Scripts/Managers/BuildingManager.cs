using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public MapData mapData;

    public void Initialize(MapData mapData)
    {
        this.mapData = mapData;
    }

    public Building PlaceBuilding(Building.BuildingType type, Vector2Int pos, int ownerId)
    {
        Building temp = new Building(type, ownerId, pos);

        if (!CanPlaceBuilding(pos, temp.size))
            return null;

        mapData.buildings[pos] = temp;

        // Később itt eventeket hívhatunk meg, pl. OnBuildingPlaced?.Invoke(temp);
        return temp;
    }

    public void RemoveBuidling(Building building)
    {
        if (mapData.buildings.ContainsKey(building.position))
        {
            mapData.buildings.Remove(building.position);
        }
    }

    public bool CanPlaceBuilding(Vector2Int pos, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int checkPos = new Vector2Int(pos.x + x, pos.y + y);

                if (!IsTileValidForPlacement(checkPos)) return false;

                // Van-e már épület itt?
                if (GetBuildingAtTile(checkPos) != null) return false;

                if (mapData.units.ContainsKey(checkPos)) return false;
            }
        }
        return true;
    }

    // Segédfüggvény ami megnézi, hogy van-e épület egy adott mezőn, még ha az épület több mezőt is elfoglal
    public Building GetBuildingAtTile(Vector2Int pos)
    {
        // Jelenleg lassú de biztonságos, mivel minden épületet végignéz
        // Később optimalizálhatjuk ezt úgy, hogy a mezőket egy külön Lookup-ban tároljuk
        foreach (var b in mapData.buildings.Values)
        {
            if (pos.x >= b.position.x && pos.x < b.position.x + b.size.x &&
                pos.y >= b.position.y && pos.y < b.position.y + b.size.y)
            {
                return b;
            }
        }
        return null;
    }

    private bool IsTileValidForPlacement(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= mapData.mapWidth || pos.y < 0 || pos.y >= mapData.mapHeight)
            return false;
        if (!mapData.mapTiles[pos.x, pos.y].isPassable)
            return false;
        if (mapData.units.ContainsKey(pos))
            return false;
        return true;
    }
}
