using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Custom/TileRegistry")]
public class TileRegistry : ScriptableObject
{
    [System.Serializable]
    public struct TileEntry
    {
        public MapData.TileType type;
        public TileBase[] variants;
    }

    [System.Serializable]
    public struct FeatureEntry
    {
        public MapData.TileType type;
        public GameObject[] prefabs;
    }

    [System.Serializable]
    public struct MountainVariation
    {
        public int width; // 1, 2, or 3
        public GameObject[] tallPeakPrefabs;   // Use arrays for even more variety!
        public GameObject[] wideLaidOutPrefabs;
    }

    public List<TileEntry> tiles;
    public List<FeatureEntry> featurePrefabs;
    public List<MountainVariation> mountainVariations;

    private Dictionary<MapData.TileType, TileBase[]> lookup;

    public TileBase GetTile(MapData.TileType type)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<MapData.TileType, TileBase[]>();
            foreach (var entry in tiles)
                lookup[entry.type] = entry.variants;
        }

        if (!lookup.ContainsKey(type) || lookup[type].Length == 0) return null;

        TileBase[] choices = lookup[type];

        if (choices.Length == 1) return choices[0];

        return choices[Random.Range(0, choices.Length)];
    }

    public GameObject GetRandomFeaturePrefab(MapData.TileType type)
    {
        FeatureEntry entry = featurePrefabs.Find(e => e.type == type);
        if (entry.prefabs == null || entry.prefabs.Length == 0) return null;
        return entry.prefabs[Random.Range(0, entry.prefabs.Length)];
    }

    //public GameObject GetMountainPrefab(int width)
    //{
    //    var entry = mountainPrefabs.Find(e => e.width == width);
    //    if (entry.prefabs == null || entry.prefabs.Length == 0) return null;
    //    return entry.prefabs[Random.Range(0, entry.prefabs.Length)];
    //}
    //public GameObject GetSpecificMountainPrefab(int width, bool useTallVariant)
    //{
    //    MountainVariation variation = mountainVariations.Find(v => v.width == width);

    //    // Check if we found a valid variation
    //    if (variation.width == 0) return null;

    //    if (useTallVariant)
    //    {
    //        if (variation.tallPeakPrefabs == null || variation.tallPeakPrefabs.Length == 0) return null;
    //        return variation.tallPeakPrefabs[Random.Range(0, variation.tallPeakPrefabs.Length)];
    //    }
    //    else
    //    {
    //        if (variation.wideLaidOutPrefabs == null || variation.wideLaidOutPrefabs.Length == 0) return null;
    //        return variation.wideLaidOutPrefabs[Random.Range(0, variation.wideLaidOutPrefabs.Length)];
    //    }
    //}

    public GameObject GetSpecificMountainPrefab(int width, bool useTallVariant)
    {
        // Find the specific width entry
        MountainVariation variation = mountainVariations.Find(v => v.width == width);

        if (variation.width == 0) return null;

        if (useTallVariant)
        {
            // Randomly pick from the 'Tall' array
            if (variation.tallPeakPrefabs == null || variation.tallPeakPrefabs.Length == 0) return null;
            return variation.tallPeakPrefabs[Random.Range(0, variation.tallPeakPrefabs.Length)];
        }
        else
        {
            // Randomly pick from the 'Wide/Flat' array
            if (variation.wideLaidOutPrefabs == null || variation.wideLaidOutPrefabs.Length == 0) return null;
            return variation.wideLaidOutPrefabs[Random.Range(0, variation.wideLaidOutPrefabs.Length)];
        }
    }
}
