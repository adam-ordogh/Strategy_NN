using UnityEngine;

public enum AttackType { Melee, Slashing, Piercing}
public enum ArmorType { Unarmored, Light, Heavy}

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Game/Unit Data")]
public class UnitData : ScriptableObject
{
    public Unit.UnitType unitType;
    public AttackType attackType;
    public ArmorType armorType;
    public int maxHealth;
    public int attackPower;
    public int attackRange;
    public int movementRange;
    public int trainingTime;
}