using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Custom/ColorTile")]
public class ColorTile : Tile
{
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);
        tileData.color = this.color; 
    }
}
