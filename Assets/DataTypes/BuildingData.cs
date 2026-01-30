using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Game/Building Data")]
public class BuildingData : ScriptableObject
{
    public Building.BuildingType buildingType;
    public Vector2Int size;
    public int influenceRadius;
    public int populationProvided;
    public int jobSlotsProvided;

    public int goldCost;
    public int woodCost;

    //public TileBase buildingTile;
}
