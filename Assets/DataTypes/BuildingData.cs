using Google.Protobuf.WellKnownTypes;
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
    [Header("Production")]
    [Tooltip("Termelés munkásonként. Index 0 = 1 munkás, Index 1 = 2 munkás, stb.")]
    public int[] productionPerWorkerCount;

    public int GetWorkerOutput(int workers)
    {
        if (workers <= 0) return 0;
        if (workers <= productionPerWorkerCount.Length)
        {
            return productionPerWorkerCount[workers - 1];
        }

        int lastValue = productionPerWorkerCount[productionPerWorkerCount.Length - 1];

        return lastValue + (workers - productionPerWorkerCount.Length);
    }

    [Header("Combat Stats")]
    public int maxHealth;
    public ArmorType armorType = ArmorType.Structure;

    [Header("Visuals")]
    public Sprite buildingIcon;
    public Sprite buildingSprite;
    public Sprite buildingColorTrim;
    public Sprite constructionSprite;

    [Header("User Interaction")]
    public bool isSelectable;
    public string buildingName;
    public string description;
}
