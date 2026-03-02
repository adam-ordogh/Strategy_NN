using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class InfluenceManager : MonoBehaviour
{
    private MapData mapData;
    private List<Building>[,] buildingReachGrid;

    private Dictionary<int, List<Vector2Int>> ownedTilesRegistry = new Dictionary<int, List<Vector2Int>>();

    public event System.Action<MapData> OnInfluenceChanged;

    public void Initialize(MapData mapData)
    {
        this.mapData = mapData;
        buildingReachGrid = new List<Building>[mapData.mapWidth, mapData.mapHeight];

        for (int x = 0; x < mapData.mapWidth; x++)
            for (int y = 0; y < mapData.mapHeight; y++)
                buildingReachGrid[x, y] = new List<Building>();

        RecalculateAllInfluences();
    }

    // Amikor egy épület eltűnik, újraszámoljuk az egész térképet
    //public void RecalculateAllInfluences()
    //{
    //    System.Array.Clear(mapData.influenceMap, 0, mapData.influenceMap.Length);

    //    for (int x = 0; x < mapData.mapWidth; x++)
    //    {
    //        for (int y = 0; y < mapData.mapHeight; y++)
    //        {
    //            if (mapData.mapTiles[x, y].type == MapData.TileType.Mountain) continue;
    //            mapData.influenceMap[x, y] = GetDominantPlayerAt(new Vector2Int(x, y));
    //        }
    //    }

    //    OnInfluenceChanged?.Invoke(mapData);
    //}

    public void RecalculateAllInfluences()
    {
        // Clear the existing registries
        ownedTilesRegistry.Clear();
        System.Array.Clear(mapData.influenceMap, 0, mapData.influenceMap.Length);

        for (int x = 0; x < mapData.mapWidth; x++)
        {
            for (int y = 0; y < mapData.mapHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (mapData.mapTiles[x, y].type == MapData.TileType.Mountain) continue;

                int owner = GetDominantPlayerAt(pos);
                mapData.influenceMap[x, y] = owner;

                // 2. Register the tile to the owner
                if (owner != 0) // Assuming 0 is neutral
                {
                    if (!ownedTilesRegistry.ContainsKey(owner))
                        ownedTilesRegistry[owner] = new List<Vector2Int>();

                    ownedTilesRegistry[owner].Add(pos);
                }
            }
        }
        OnInfluenceChanged?.Invoke(mapData);
    }

    // Amikor egy új épület kerül a térképre, csak az érintett területet számoljuk újra
    //public void RecalculateInfluence(Building newBuilding)
    //{
    //    int r = newBuilding.data.influenceRadius;

    //    int startX = Mathf.Max(0, newBuilding.position.x - r);
    //    int endX = Mathf.Min(mapData.mapWidth - 1, newBuilding.position.x + newBuilding.data.size.x + r);
    //    int startY = Mathf.Max(0, newBuilding.position.y - r);
    //    int endY = Mathf.Min(mapData.mapHeight - 1, newBuilding.position.y + newBuilding.data.size.y + r);

    //    for (int x = startX; x <= endX; x++)
    //    {
    //        for (int y = startY; y <= endY; y++)
    //        {
    //            if (mapData.mapTiles[x, y].type == MapData.TileType.Mountain) continue; // Nem számoljuk újra a hegyeket
    //            mapData.influenceMap[x, y] = GetDominantPlayerAt(new Vector2Int(x, y));
    //        }
    //    }

    //    OnInfluenceChanged?.Invoke(mapData);
    //}
    public void RecalculateInfluence(Building newBuilding)
    {
        int r = newBuilding.data.influenceRadius;

        int startX = Mathf.Max(0, newBuilding.position.x - r);
        int endX = Mathf.Min(mapData.mapWidth - 1, newBuilding.position.x + newBuilding.data.size.x + r);
        int startY = Mathf.Max(0, newBuilding.position.y - r);
        int endY = Mathf.Min(mapData.mapHeight - 1, newBuilding.position.y + newBuilding.data.size.y + r);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (mapData.mapTiles[x, y].type == MapData.TileType.Mountain) continue;

                // 1. Store the previous owner
                int oldOwner = mapData.influenceMap[x, y];

                // 2. Calculate new owner
                int newOwner = GetDominantPlayerAt(pos);

                // 3. If the owner changed, update the registry
                if (oldOwner != newOwner)
                {
                    // Remove from old registry
                    if (oldOwner != 0 && ownedTilesRegistry.ContainsKey(oldOwner))
                    {
                        ownedTilesRegistry[oldOwner].Remove(pos);
                    }

                    // Add to new registry
                    if (newOwner != 0)
                    {
                        if (!ownedTilesRegistry.ContainsKey(newOwner))
                            ownedTilesRegistry[newOwner] = new List<Vector2Int>();

                        // Safety check to prevent duplicates
                        if (!ownedTilesRegistry[newOwner].Contains(pos))
                            ownedTilesRegistry[newOwner].Add(pos);
                    }

                    // Update the map data
                    mapData.influenceMap[x, y] = newOwner;
                }
            }
        }

        OnInfluenceChanged?.Invoke(mapData);
    }

    private int GetDominantPlayerAt(Vector2Int tilePos)
    {
        var buildings = buildingReachGrid[tilePos.x, tilePos.y];
        if (buildings.Count == 0) return 0;

        Dictionary<int, float> playerScores = new Dictionary<int, float>();

        foreach (var b in buildings)
        {
            // Épület közepe (pl., 2x2-es méret (0,0)-nél (1,1))
            float centerX = b.position.x + b.data.size.x / 2.0f;
            float centerY = b.position.y + b.data.size.y / 2.0f;

            // Cél mező közepe (pl., (5,0)-nél mező közepe (5.5, 0.5))
            float targetX = tilePos.x + 0.5f;
            float targetY = tilePos.y + 0.5f;

            float dx = targetX - centerX;
            float dy = targetY - centerY;
            float distSq = (dx * dx) + (dy * dy);
            float r = b.data.influenceRadius;

            if (distSq <= r * r)
            {
                float dist = Mathf.Sqrt(distSq);
                float strength = r - dist + 1f;

                if (!playerScores.ContainsKey(b.ownerId)) playerScores[b.ownerId] = 0;
                playerScores[b.ownerId] += strength;
            }
        }

        int bestPlayer = 0;
        float maxScore = -1f;
        foreach (var entry in playerScores)
        {
            if (entry.Value > maxScore)
            {
                maxScore = entry.Value;
                bestPlayer = entry.Key;
            }
        }

        return bestPlayer;
    }

    public void AddBuildingToReachGrid(Building b)
    {
        int r = b.data.influenceRadius;
        // Bounding box
        int startX = Mathf.Max(0, b.position.x - r);
        int endX = Mathf.Min(mapData.mapWidth - 1, b.position.x + b.data.size.x + r);
        int startY = Mathf.Max(0, b.position.y - r);
        int endY = Mathf.Min(mapData.mapHeight - 1, b.position.y + b.data.size.y + r);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                buildingReachGrid[x, y].Add(b);
            }
        }
    }

    public void RemoveBuildingFromReachGrid(Building b)
    {
        int r = b.data.influenceRadius;
        // Bounding box
        int startX = Mathf.Max(0, b.position.x - r);
        int endX = Mathf.Min(mapData.mapWidth - 1, b.position.x + b.data.size.x + r);
        int startY = Mathf.Max(0, b.position.y - r);
        int endY = Mathf.Min(mapData.mapHeight - 1, b.position.y + b.data.size.y + r);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                buildingReachGrid[x, y].Remove(b);
            }
        }
    }

    public bool IsTileOwnedBy(Vector2Int pos, int ownerId)
    {
        if (!IsInsideMap(pos)) return false;
        return mapData.influenceMap[pos.x, pos.y] == ownerId;
    }

    public List<Vector2Int> GetTilesOwnedBy(int playerId)
    {
        if (ownedTilesRegistry.TryGetValue(playerId, out var tiles))
            return tiles;
        return new List<Vector2Int>();
    }

    private bool IsInsideMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mapData.mapWidth &&
               pos.y >= 0 && pos.y < mapData.mapHeight;
    }
}
