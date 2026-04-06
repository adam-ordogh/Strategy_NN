using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public enum AttackType { Melee, Slashing, Piercing, Siege}
public enum ArmorType { Unarmored, Light, Heavy, Siege, Structure}

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Game/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Logic Stats")]
    public Unit.UnitType unitType;
    public AttackType attackType;
    public ArmorType armorType;
    public int maxHealth;
    public int attackPower;
    public int attackRange;
    public int movementRange;
    public int trainingTime;

    [Header("Production Costs")]
    public int populationCost;
    public int goldUpkeep;
    public int goldCost;
    public int foodCost;
    public int woodCost;

    [Header("Visuals")]
    public Sprite unitIcon;
    public Sprite unitSprite;
    public Sprite unitColorTrim;
    public GameObject attackProjectilePrefab;

    [Header("User Interaction")]
    public string unitName;
    public string description;
}