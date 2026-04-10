using UnityEngine;

public static class CombatMath
{
    public static float GetMultiplier(AttackType attack, ArmorType armor)
    {
        return (attack, armor) switch
        {
            (AttackType.Melee, ArmorType.Unarmored) => 1.5f,
            (AttackType.Melee, ArmorType.Light) => 2.0f,
            (AttackType.Slashing, ArmorType.Unarmored) => 2.0f,

            (AttackType.Piercing, ArmorType.Heavy) => 1.5f,

            (AttackType.Melee, ArmorType.Siege) => 2.0f,
            (AttackType.Slashing, ArmorType.Siege) => 2.5f, 

            // ---------------- ÉPÜLETEK ------------------
            (AttackType.Melee, ArmorType.Structure) => 0.5f,
            (AttackType.Piercing, ArmorType.Structure) => 0.1f,
            (AttackType.Slashing, ArmorType.Structure) => 0.2f,
            (AttackType.Siege, ArmorType.Structure) => 3.0f,

            _ => 1.0f
        };
    }
}
