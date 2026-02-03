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
        Road,
        Warehouse
    };

    public BuildingData data;

    public int ownerId;
    public Vector2Int position; 
    public int currentHp;
    public int turnsRemaining;
    public int assignedWorkers;

    public bool isConstructed { get; private set; } = false;
    public bool isConnectedToCapital;

    public BuildingType buildingType => data.buildingType;
    public int maxHealth => data.maxHealth;
    public Vector2Int size => data.size;

    public Building(BuildingData data, int ownerId, Vector2Int position)
    {
        this.data = data;
        this.ownerId = ownerId;
        this.position = position;
        this.currentHp = data.maxHealth;
        this.turnsRemaining = data.constructionTurns;
        this.isConstructed = data.constructionTurns <= 0;
    }

    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        Debug.Log($"Building at {position} took {amount} damage. HP: {currentHp}/{data.maxHealth}");
    }

    public void DecrementConstruction()
    {
        turnsRemaining--;
        if (turnsRemaining <= 0)
        {
            isConstructed = true;
            turnsRemaining = 0;
        }
    }

    public bool CanAcceptWorker()
    {
        return isConstructed && assignedWorkers < data.jobSlotsProvided;
    }
    public bool TryAssignWorker(PlayerProfile player)
    {
        if (CanAcceptWorker() && player.availablePopulation > 0)
        {
            assignedWorkers++;
            //player.OnResourcesChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool TryRemoveWorker(PlayerProfile player)
    {
        if (assignedWorkers > 0)
        {
            assignedWorkers--;
            //player.OnResourcesChanged?.Invoke();
            return true;
        }
        return false;
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
