using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Game/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Logic Stats")]
    public Building.BuildingType buildingType;
    public Vector2Int size;
    public float movementCostModifier = Mathf.Infinity;
    public int influenceRadius;
    public int populationProvided;
    public int jobSlotsProvided;

    public int goldCost;
    public int woodCost;

    [Header("Combat Stats")]
    public int maxHealth;
    public ArmorType armorType = ArmorType.Structure;

    [Header("Visuals")]
    public Sprite buildingSprite;
    public Sprite buildingColorTrim;

    [Header("User Interaction")]
    public bool isSelectable;
}
