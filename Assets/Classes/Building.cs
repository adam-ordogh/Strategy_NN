using UnityEngine;

public class Building
{
    public enum BuildingType
    {
        Barracks,
        ArcheryRange,
        Stable
    };

    public BuildingType buildingType;
    public int ownerId;
    public Vector2Int position;

    public Building(BuildingType type, int ownerId, Vector2Int startPos)
    {
        this.buildingType = type;
        this.ownerId = ownerId;
        this.position = startPos;
    }
}
