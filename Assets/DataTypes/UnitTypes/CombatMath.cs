using UnityEngine;

public static class CombatMath
{
    public static float GetMultiplier(AttackType attack, ArmorType armor)
    {
        return (attack, armor) switch
        {
            // Soldiers (Melee) vs Archers (Unarmored)
            (AttackType.Melee, ArmorType.Unarmored) => 1.5f,

            // Soldiers (Melee) vs Cavalry (Light)
            (AttackType.Melee, ArmorType.Light) => 2.0f,

            // Cavalry (Slashing) vs Archers (Unarmored)
            (AttackType.Slashing, ArmorType.Unarmored) => 2.0f,

            // Archers (Piercing) vs Soldiers (Heavy)
            (AttackType.Piercing, ArmorType.Heavy) => 0.6f,

            _ => 1.0f
        };
    }
}
