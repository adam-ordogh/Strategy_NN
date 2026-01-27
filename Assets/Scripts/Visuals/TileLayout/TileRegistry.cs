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
        public TileBase tile;
    }

    public List<TileEntry> tiles;

    private Dictionary<MapData.TileType, TileBase> lookup;

    public TileBase GetTile(MapData.TileType type)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<MapData.TileType, TileBase>();
            foreach (var entry in tiles)
                lookup[entry.type] = entry.tile;
        }

        return lookup[type];
    }
}
