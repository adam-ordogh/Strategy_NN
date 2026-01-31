using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class InfluenceManager : MonoBehaviour
{
    private MapData mapData;
    private List<Building>[,] buildingReachGrid;

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
    public void RecalculateAllInfluences()
    {
        System.Array.Clear(mapData.influenceMap, 0, mapData.influenceMap.Length);

        for (int x = 0; x < mapData.mapWidth; x++)
        {
            for (int y = 0; y < mapData.mapHeight; y++)
            {
                mapData.influenceMap[x, y] = GetDominantPlayerAt(new Vector2Int(x, y));
            }
        }

        OnInfluenceChanged?.Invoke(mapData);
    }

    // Amikor egy új épület kerül a térképre, csak az érintett területet számoljuk újra
    public void RecalculateInfluence(Building newBuilding)
    {
        int r = newBuilding.influenceRadius;

        int startX = Mathf.Max(0, newBuilding.position.x - r);
        int endX = Mathf.Min(mapData.mapWidth - 1, newBuilding.position.x + newBuilding.size.x + r);
        int startY = Mathf.Max(0, newBuilding.position.y - r);
        int endY = Mathf.Min(mapData.mapHeight - 1, newBuilding.position.y + newBuilding.size.y + r);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                mapData.influenceMap[x, y] = GetDominantPlayerAt(new Vector2Int(x, y));
            }
        }

        OnInfluenceChanged?.Invoke(mapData);
    }

    //private int GetDominantPlayerAt(Vector2Int tilePos)
    //{
    //    int bestPlayer = 0;
    //    float minDistanceSq = float.MaxValue;

    //    var candidates = buildingReachGrid[tilePos.x, tilePos.y];

    //    foreach (var b in candidates)
    //    {
    //        float centerX = b.position.x + (b.size.x - 1) / 2f;
    //        float centerY = b.position.y + (b.size.y - 1) / 2f;

    //        float dx = tilePos.x - centerX;
    //        float dy = tilePos.y - centerY;
    //        float distSq = (dx * dx) + (dy * dy);

    //        if (distSq <= b.influenceRadius * b.influenceRadius)
    //        {
    //            if (distSq < minDistanceSq)
    //            {
    //                minDistanceSq = distSq;
    //                bestPlayer = b.ownerId;
    //            }
    //        }
    //    }
    //    return bestPlayer;
    //}


    private int GetDominantPlayerAt(Vector2Int tilePos)
    {
        var buildings = buildingReachGrid[tilePos.x, tilePos.y];
        if (buildings.Count == 0) return 0;

        Dictionary<int, float> playerScores = new Dictionary<int, float>();

        foreach (var b in buildings)
        {
            // Épület közepe (pl., 2x2-es méret (0,0)-nél (1,1))
            float centerX = b.position.x + b.size.x / 2.0f;
            float centerY = b.position.y + b.size.y / 2.0f;

            // Cél mező közepe (pl., (5,0)-nél mező közepe (5.5, 0.5))
            float targetX = tilePos.x + 0.5f;
            float targetY = tilePos.y + 0.5f;

            float dx = targetX - centerX;
            float dy = targetY - centerY;
            float distSq = (dx * dx) + (dy * dy);
            float r = b.influenceRadius;

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
        int r = b.influenceRadius;
        // Bounding box
        int startX = Mathf.Max(0, b.position.x - r);
        int endX = Mathf.Min(mapData.mapWidth - 1, b.position.x + b.size.x + r);
        int startY = Mathf.Max(0, b.position.y - r);
        int endY = Mathf.Min(mapData.mapHeight - 1, b.position.y + b.size.y + r);

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
        int r = b.influenceRadius;
        // Bounding box
        int startX = Mathf.Max(0, b.position.x - r);
        int endX = Mathf.Min(mapData.mapWidth - 1, b.position.x + b.size.x + r);
        int startY = Mathf.Max(0, b.position.y - r);
        int endY = Mathf.Min(mapData.mapHeight - 1, b.position.y + b.size.y + r);

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

    private bool IsInsideMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mapData.mapWidth &&
               pos.y >= 0 && pos.y < mapData.mapHeight;
    }
}
