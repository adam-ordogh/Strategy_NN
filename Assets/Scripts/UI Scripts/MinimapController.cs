using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    public RawImage minimapDisplay;
    private Texture2D minimapTexture;
    private MapData mapData;
    private GameManager gameManager;

    public void Initialize(MapData data, GameManager gm)
    {
        mapData = data;
        gameManager = gm;

        minimapTexture = new Texture2D(mapData.mapWidth, mapData.mapHeight);

        minimapTexture.filterMode = FilterMode.Point;

        minimapDisplay.texture = minimapTexture;

        UpdateMinimap();
    }

    public void UpdateMinimap()
    {
        Color[] pixels = new Color[mapData.mapWidth * mapData.mapHeight];

        for (int y = 0; y < mapData.mapHeight; y++)
        {
            for (int x = 0; x < mapData.mapWidth; x++)
            {
                int index = (y * mapData.mapWidth) + x;
                Vector2Int pos = new Vector2Int(x, y);

                // ==========================================
                // FUTURE FOG OF WAR LOGIC GOES HERE
                // ==========================================

                Color pixelColor = GetTerrainColor(mapData.GetTileData(x, y).type);

                int ownerId = mapData.influenceMap[x, y];
                if (ownerId != 0)
                {
                    Color influenceColor = GetPlayerColor(ownerId);
                    pixelColor = Color.Lerp(pixelColor, influenceColor, 0.3f);
                }

                // ==========================================
                // FUTURE FOG OF WAR VISIBILITY GOES HERE
                // ==========================================

                if (mapData.buildings.TryGetValue(pos, out Building b))
                {
                    pixelColor = GetPlayerColor(b.ownerId);
                }
                else if (mapData.units.TryGetValue(pos, out Unit u))
                {
                    pixelColor = GetPlayerColor(u.ownerId);
                }

                pixels[index] = pixelColor;
            }
        }

        minimapTexture.SetPixels(pixels);
        minimapTexture.Apply();
    }
    private Color GetTerrainColor(MapData.TileType type)
    {
        return type switch
        {
            MapData.TileType.Grass => new Color(0.2f, 0.6f, 0.2f),
            MapData.TileType.Forest => new Color(0.1f, 0.4f, 0.1f),
            MapData.TileType.Mountain => Color.gray,
            MapData.TileType.Gold => Color.yellow,
            MapData.TileType.Road => new Color(0.6f, 0.4f, 0.2f),
            _ => Color.black
        };
    }

    private Color GetPlayerColor(int id)
    {
        return gameManager.GetPlayerProfile(id).playerColor;
    }

    public byte[] GetMinimapPngBytes()
    {
        if (minimapTexture == null) return null;

        UpdateMinimap();

        return minimapTexture.EncodeToPNG();
    }
}