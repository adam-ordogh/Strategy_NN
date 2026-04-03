using UnityEngine.Tilemaps;
using UnityEngine;
using System.Collections.Generic;

public class MapVisualizer
{
    private Tilemap groundMap;
    private Tilemap featureMap;
    private TileRegistry tileRegistry;
    private Transform featureContainer;

    private HashSet<Vector2Int> occupiedTiles = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, List<GameObject>> spawnedFeatures = new Dictionary<Vector2Int, List<GameObject>>();

    public MapVisualizer(Tilemap ground, Tilemap feature, TileRegistry registry, Transform container)
    {
        groundMap = ground;
        featureMap = feature;
        tileRegistry = registry;
        featureContainer = container;
    }

    public void DrawMap(MapData data, int width, int height)
    {
        // Régi spriteokat (fákat) eltüntetni újrarajzolás előtt
        occupiedTiles.Clear();
        spawnedFeatures.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tileData = data.GetTileData(x, y);
                Vector3Int posInt = new Vector3Int(x, y, 0);
                Vector3 worldPos = new Vector3(x, y, 0);

                groundMap.SetTile(posInt, tileRegistry.GetTile(MapData.TileType.Grass));

                if (tileData.type == MapData.TileType.Forest)
                {
                    //featureMap.SetTile(posInt, tileRegistry.GetTile(tileData.type));
                    HandleForestSpawning(x, y, clusterSize: 4);
                }
                else if (tileData.type == MapData.TileType.Mountain)
                {
                    featureMap.SetTile(posInt, tileRegistry.GetTile(tileData.type));
                    HandleMountainSpawning(data, x, y, width, height);
                }
                else
                {
                    featureMap.SetTile(posInt, null);
                }
            }
        }
    }

    private void HandleMountainSpawning(MapData data, int x, int y, int width, int height)
    {
        int runLength = 1;
        while (x + runLength < width &&
               data.GetTileData(x + runLength, y).type == MapData.TileType.Mountain &&
               runLength < 3 &&
               !occupiedTiles.Contains(new Vector2Int(x + runLength, y))) 
        {
            runLength++;
        }

        bool hasMountainAbove = (y + 1 < height) && (data.GetTileData(x, y + 1).type == MapData.TileType.Mountain);
        bool hasMountainLeft = (x - 1 >= 0) && (data.GetTileData(x - 1, y).type == MapData.TileType.Mountain);
        
        bool mustUseTallVariant = !hasMountainAbove || !hasMountainLeft;

        GameObject prefab = tileRegistry.GetSpecificMountainPrefab(runLength, mustUseTallVariant);

        if (prefab != null)
        {
            float randomY = y + Random.Range(0f, 0.2f);
            Vector3 spawnPos = new Vector3(x, randomY, 0);

            GameObject mountain = Object.Instantiate(prefab, spawnPos, Quaternion.identity, featureContainer);

            SpriteRenderer sr = mountain.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                RenderSorter.Sort(sr, randomY);
            }

            for (int i = 0; i < runLength; i++)
            {
                occupiedTiles.Add(new Vector2Int(x + i, y));
            }
        }
    }

    private void HandleForestSpawning(int x, int y, int clusterSize)
    {
        Vector2Int pos = new Vector2Int(x, y);

        // Ha már van itt valami, biztos ami biztos töröljük (vagy inicializáljuk a listát)
        if (!spawnedFeatures.ContainsKey(pos))
            spawnedFeatures[pos] = new List<GameObject>();

        for (int i = 0; i < clusterSize; i++)
        {
            GameObject prefab = tileRegistry.GetRandomFeaturePrefab(MapData.TileType.Forest);
            if (prefab != null)
            {
                // Fák pozicionálása úgy, hogy ne lógjanak ki a tile-ból és ne takarják el teljesen a tile fölött lévő egységeket
                float treePadding = 0.1f;

                // Határok a random pozícióhoz, hogy a fa ne lógjon ki a tile-ból
                float minX = x + treePadding;
                float maxX = x + 1 - treePadding;

                // - Y pozicónál a fa magasságának nagy részét a tile alján helyezzük el, hogy ne takarják el a tile fölött lévő egységeket
                float minY = y + treePadding;
                float maxY = y + 0.5f; // Fák magasságának nagy részét a tile alján helyezzük el, hogy ne takarják el a tile fölött lévő egységeket

                Vector3 randomPos = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0);

                GameObject tree = Object.Instantiate(prefab, randomPos, Quaternion.identity, featureContainer);

                // ELMENTJÜK a fát a listába
                spawnedFeatures[pos].Add(tree);

                SpriteRenderer sr = tree.GetComponent<SpriteRenderer>();
                if (sr != null) { RenderSorter.Sort(sr, tree.transform.position.y); }
            }
        }
    }

    public void HandleEnvironmentChange(Vector2Int pos, MapData.TileType newType)
    {
        if (spawnedFeatures.TryGetValue(pos, out List<GameObject> features))
        {
            foreach (var feature in features)
            {
                if (feature != null) Object.Destroy(feature);
            }
            spawnedFeatures.Remove(pos);
        }
        groundMap.SetTile(new Vector3Int(pos.x, pos.y, 0), tileRegistry.GetTile(newType));
    }
}