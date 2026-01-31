using UnityEngine;
using UnityEngine.Tilemaps;

public enum AttackType { Melee, Slashing, Piercing}
public enum ArmorType { Unarmored, Light, Heavy}

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
    public int goldCost;
    public int foodCost;
    public int woodCost;

    [Header("Visuals")]
    public TileBase unitTile;
}