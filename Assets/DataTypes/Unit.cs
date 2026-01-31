using UnityEngine;

public class Unit
{
    public enum UnitType
    {
        Soldier,
        Archer,
        Cavalry
    };

    public UnitType unitType;
    public int ownerId;
    public int health;
    public int attackPower;
    public int attackRange;
    public bool canAttack = true;
    public float movementPoints;
    public float remainingMovementPoints;
    public Vector2Int position;

    public event System.Action<Unit> OnUnitDeath;

    public UnitData data;

    public Unit(UnitData data, int ownerId, Vector2Int startPos)
    {
        this.data = data;
        this.unitType = data.unitType;
        this.ownerId = ownerId;
        this.health = data.maxHealth;
        this.attackPower = data.attackPower;
        this.attackRange = data.attackRange;
        this.movementPoints = (float)data.movementRange;
        this.remainingMovementPoints = movementPoints;
        this.position = startPos;
    }

    public void Move(Vector2Int newPos)
    {
        this.position = newPos;
    }

    public void TakeDamage(int damage)
    {
        this.health -= damage;
        if (this.health <= 0)
        {
            OnUnitDeath?.Invoke(this);
        }
    }    

    public void DealDamageToUnit(Unit target)
    {
        if (!canAttack)
        {
            Debug.Log("This unit has already attacked this turn.");
            return;
        }

        float multiplier = CombatMath.GetMultiplier(this.data.attackType, target.data.armorType);

        int finalDamage = Mathf.RoundToInt(this.attackPower * multiplier);

        target.TakeDamage(finalDamage);
        canAttack = false;

        Debug.Log($"Unit at {this.position} attacked unit at {target.position} for {finalDamage} damage.");


        if (target.health > 0 && target.unitType != UnitType.Archer)
        {
            int dist = Mathf.Max(Mathf.Abs(this.position.x - target.position.x), Mathf.Abs(this.position.y - target.position.y));

            if (dist <= target.attackRange)
            {
                float returnMultiplier = CombatMath.GetMultiplier(target.data.attackType, this.data.armorType);

                int returnDamage = Mathf.RoundToInt(this.attackPower * returnMultiplier * 0.5f);

                Debug.Log($"Retaliation! {target.unitType} hits back for {returnDamage}.");
                this.TakeDamage(returnDamage);
            }
        }
        
    }

    public void ResetActions()
    {
        this.remainingMovementPoints = this.movementPoints;
        this.canAttack = true;
    }
}
