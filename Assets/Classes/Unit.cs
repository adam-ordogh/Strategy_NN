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
    public int movementPoints;
    public int remainingMovementPoints;
    public Vector2Int position;

    public event System.Action<Unit> OnUnitDeath;

    public Unit(UnitType type, int ownerId, int health, int attackPower, int attackRange, int movementRange, Vector2Int startPos)
    {
        this.unitType = type;
        this.ownerId = ownerId;
        this.health = health;
        this.attackPower = attackPower;
        this.attackRange = attackRange;
        this.movementPoints = movementRange;
        this.remainingMovementPoints = movementRange;
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
        if (canAttack)
        {
            int bonusAttack = 0;

            switch(this.unitType)
            {
                case UnitType.Soldier:
                    if (target.unitType == UnitType.Cavalry)
                        bonusAttack = 5;
                    break;
                case UnitType.Archer:
                    if (target.unitType == UnitType.Soldier)
                        bonusAttack = 3;
                    break;
                case UnitType.Cavalry:
                    if (target.unitType == UnitType.Archer)
                        bonusAttack = 3;
                    break;
            }

            int totalAttack = this.attackPower + bonusAttack;
            target.TakeDamage(totalAttack);
            canAttack = false;

            Debug.Log($"Unit at {this.position} attacked unit at {target.position} for {totalAttack} damage.");


            if (target.health > 0)
            {
                // Check distance for retaliation (e.g., Archers can't hit back at melee range if you want that rule, 
                // but for now let's assume simple adjacent retaliation)
                int dist = Mathf.Max(Mathf.Abs(this.position.x - target.position.x), Mathf.Abs(this.position.y - target.position.y));

                if (dist <= target.attackRange)
                {
                    // Retaliation is usually weaker (e.g., 50% damage)
                    int returnDamage = Mathf.FloorToInt(target.attackPower * 0.5f);
                    if (returnDamage < 1) returnDamage = 1;

                    Debug.Log($"Retaliation! {target.unitType} hits back for {returnDamage}.");
                    this.TakeDamage(returnDamage);
                }
            }
        }
        else
        {
            Debug.Log("This unit has already attacked this turn.");
        }
    }

    public void ResetActions()
    {
        this.remainingMovementPoints = this.movementPoints;
        this.canAttack = true;
    }
}
