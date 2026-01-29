using UnityEngine;

public class InfluenceManager : MonoBehaviour
{
    private MapData mapData;

    public event System.Action<MapData> OnInfluenceChanged;

    public void Initialize(MapData mapData)
    {
        this.mapData = mapData;
        RecalculateInfluence();
    }

    public void RecalculateInfluence()
    {
        System.Array.Clear(mapData.influenceMap, 0, mapData.influenceMap.Length);

        // Később logika kell a határok eldöntésére ha egymásba lódnak az influence radiusok,
        // pl. ki van közelebb, jelenleg az utolsó épület felülírja a korábbiakat
        foreach (var building in mapData.buildings.Values)
        {
            ApplyInfluence(building);
        }

        OnInfluenceChanged?.Invoke(mapData);
    }

    private void ApplyInfluence(Building b)
    {
        int r = b.influenceRadius;
        float rSquared = r * r;

        float centerX = b.position.x + (b.size.x - 1) / 2f;
        float centerY = b.position.y + (b.size.y - 1) / 2f;

        for (int x = b.position.x - r; x < b.position.x + b.size.x + r; x++)
        {
            for (int y = b.position.y - r; y < b.position.y + b.size.y + r; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (IsInsideMap(pos))
                {
                    float dx = x - centerX;
                    float dy = y - centerY;

                    if ((dx * dx) + (dy * dy) <= rSquared)
                    {
                        mapData.influenceMap[pos.x, pos.y] = b.ownerId;
                    }
                }
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
