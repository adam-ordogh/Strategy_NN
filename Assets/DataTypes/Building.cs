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

    public int ownerId;
    public Vector2Int position; 
    public int currentHp;

    public BuildingType buildingType => data.buildingType;
    public int maxHealth => data.maxHealth;
    public Vector2Int size => data.size;

    public Building(BuildingData data, int ownerId, Vector2Int position)
    {
        this.data = data;
        this.ownerId = ownerId;
        this.position = position;
        this.currentHp = data.maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        Debug.Log($"Building at {position} took {amount} damage. HP: {currentHp}/{data.maxHealth}");
    }

    public List<Vector2Int> GetOccupiedTiles()
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        for (int x = 0; x < data.size.x; x++)
        {
            for (int y = 0; y < data.size.y; y++)
            {
                tiles.Add(new Vector2Int(position.x + x, position.y + y));
            }
        }
        return tiles;
    }
}
