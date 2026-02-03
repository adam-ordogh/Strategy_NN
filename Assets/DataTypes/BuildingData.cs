using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Game/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Logic Stats")]
    public Building.BuildingType buildingType;
    public Vector2Int size;
    public float movementCostModifier = Mathf.Infinity;
    public int constructionTurns;
    public int influenceRadius;

    [Header("Economy Stats")]
    public int goldCost;
    public int woodCost;
    
    public int populationProvided;
    public int storageProvided;
    public int jobSlotsProvided;

    [Header("Combat Stats")]
    public int maxHealth;
    public ArmorType armorType = ArmorType.Structure;

    [Header("Visuals")]
    public Sprite buildingSprite;
    public Sprite buildingColorTrim;
    public Sprite constructionSprite;

    [Header("User Interaction")]
    public bool isSelectable;
}
