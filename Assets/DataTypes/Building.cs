using System.Collections.Generic;
using UnityEngine;

public class Building
{
    public enum BuildingType
    {
        TownHall,
        Barracks,
        House,
        Lumberyard, //Woodcamp jobban hangzik
        Farm,
        Mine,
        Outpost,
        Road
    };

    public BuildingData data;

    public BuildingType buildingType;
    public int ownerId;
    public Vector2Int position;
    public Vector2Int size;

    public int populationProvided; // Házaknak
    public int jobSlotsProvided;   // Alapanyag gyűjtő épületeknek
    public int influenceRadius;    // Határoknak

    public Building(BuildingData data, int ownerId, Vector2Int position)
    {
        this.data = data;
        this.buildingType = data.buildingType;
        this.size = data.size;
        this.populationProvided = data.populationProvided;
        this.jobSlotsProvided = data.jobSlotsProvided;
        this.influenceRadius = data.influenceRadius;
        this.ownerId = ownerId;
        this.position = position;
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
