using UnityEngine;

public class Unit
{
    public enum UnitType
    {
        Soldier,
        Archer,
        Cavalry,
        Siege
    };

    public UnitData data;

    public int ownerId;
    public int currentHealth;
    public bool canAttack = true;
    public float remainingMovementPoints;
    public Vector2Int position;

    public event System.Action<Unit> OnUnitDeath;
    public event System.Action<int, int> OnUnitHealthChanged;

    public Unit(UnitData data, int ownerId, Vector2Int startPos)
    {
        this.data = data;
        this.ownerId = ownerId;
        this.currentHealth = data.maxHealth;
        this.remainingMovementPoints = data.movementRange;
        this.position = startPos;
    }

    public void Move(Vector2Int newPos)
    {
        this.position = newPos;
    }

    public void TakeDamage(int damage)
    {
        this.currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnUnitHealthChanged?.Invoke(currentHealth, data.maxHealth);
        if (this.currentHealth <= 0)
        {
            OnUnitDeath?.Invoke(this);
        }
    }    

    public void DealDamageToUnit(Unit target)
    {
        if (!canAttack)
        {
            //Debug.Log("This unit has already attacked this turn.");
            return;
        }

        float multiplier = CombatMath.GetMultiplier(this.data.attackType, target.data.armorType);

        int finalDamage = Mathf.RoundToInt(this.data.attackPower * multiplier);

        target.TakeDamage(finalDamage);
        canAttack = false;

        //Debug.Log($"Unit at {this.position} attacked unit at {target.position} for {finalDamage} damage.");


        if (target.currentHealth > 0 && target.data.unitType != UnitType.Archer)
        {
            int dist = Mathf.Max(Mathf.Abs(this.position.x - target.position.x), Mathf.Abs(this.position.y - target.position.y));

            if (dist <= target.data.attackRange)
            {
                float returnMultiplier = CombatMath.GetMultiplier(target.data.attackType, this.data.armorType);

                int returnDamage = Mathf.RoundToInt(this.data.attackPower * returnMultiplier * 0.5f);

                //Debug.Log($"Retaliation! {target.data.unitType} hits back for {returnDamage}.");
                this.TakeDamage(returnDamage);
            }
        }
        
    }

    public void DealDamageToBuilding(Building target)
    {
        if (!canAttack) return;

        float multiplier = CombatMath.GetMultiplier(this.data.attackType, target.data.armorType);

        int finalDamage = Mathf.RoundToInt(this.data.attackPower * multiplier);
        target.TakeDamage(finalDamage);

        canAttack = false;
    }
    public void ResetActions()
    {
        this.remainingMovementPoints = this.data.movementRange;
        this.canAttack = true;
    }
}
