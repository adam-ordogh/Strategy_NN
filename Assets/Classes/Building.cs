using System.Collections.Generic;
using UnityEngine;

public class Building
{
    public enum BuildingType
    {
        TownHall,
        Barracks,
        House,
        Lumberyard,
        Farm,
        Mine
    };

    public BuildingType buildingType;
    public int ownerId;
    public Vector2Int position;
    public Vector2Int size;

    public int populationProvided; // Házaknak
    public int jobSlotsProvided;   // Alapanyag gyűjtő épületeknek
    public int influenceRadius;    // Határoknak

    public Building(BuildingType type, int ownerId, Vector2Int position)
    {
        this.buildingType = type;
        this.ownerId = ownerId;
        this.position = position;

        switch (type)
        {
            case BuildingType.TownHall:
                size = new Vector2Int(2, 2);
                influenceRadius = 5;
                populationProvided = 10;
                break;
            case BuildingType.Barracks:
                size = new Vector2Int(2, 1);
                break;
            case BuildingType.House:
                size = new Vector2Int(1, 1);
                populationProvided = 5;
                break;
            case BuildingType.Lumberyard:
                size = new Vector2Int(1, 1);
                jobSlotsProvided = 3;
                break;
            case BuildingType.Farm:
                size = new Vector2Int(2, 2);
                jobSlotsProvided = 3;
                break;
            case BuildingType.Mine:
                size = new Vector2Int(1, 1);
                jobSlotsProvided = 3;
                break;
            default:
                size = new Vector2Int(2, 2);
                break;
        }
    }

    public List<Vector2Int> GetOccupiedTiles()
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                tiles.Add(new Vector2Int(position.x + x, position.y + y));
            }
        }
        return tiles;
    }
}
