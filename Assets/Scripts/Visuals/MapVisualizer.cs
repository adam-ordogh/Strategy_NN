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
               runLength < 3)
        {
            runLength++;
        }

        GameObject prefab = tileRegistry.GetMountainPrefab(runLength);

        if (prefab != null)
        {
            float minYOffset = 0f; 
            float maxYOffset = 0.2f; 

            float randomY = y + Random.Range(minYOffset, maxYOffset);

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

                // Random pozíció generálása a tile-on belül
                Vector3 randomPos = new Vector3(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY),
                    0
                );

                GameObject tree = Object.Instantiate(prefab, randomPos, Quaternion.identity, featureContainer);

                SpriteRenderer sr = tree.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    RenderSorter.Sort(sr, tree.transform.position.y);
                }
            }
        }
    }
}