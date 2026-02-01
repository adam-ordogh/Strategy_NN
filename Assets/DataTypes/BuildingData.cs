using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Game/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Logic Stats")]
    public Building.BuildingType buildingType;
    public Vector2Int size;
    public int influenceRadius;
    public int populationProvided;
    public int jobSlotsProvided;

    public int goldCost;
    public int woodCost;

    [Header("Visuals")]
    public TileBase buildingTile;

    [Header("User Interaction")]
    public bool isSelectable;
}
