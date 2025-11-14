using UnityEngine;
public static class CombatSystem
{
    public static bool CanAttack(Unit attacker, Unit target)
    {
        if (attacker == null || target == null)
            return false;

        if (attacker.hasAttacked)
            return false;

        if (target.OwnerPlayerID == attacker.OwnerPlayerID)
            return false;

        int distance = HexDistance(attacker.CurrentCell, target.CurrentCell);
        return distance <= attacker.attackRange;
    }
    public static void ApplyDamage(Unit attacker, Unit target)
    {
        if (!CanAttack(attacker, target))
        {
            Debug.LogWarning("Cannot apply damage: attack is invalid");
            return;
        }

        int damage = attacker.attackPower;
        target.currentHealth -= damage;

        Debug.Log($"{attacker.gameObject.name} dealt {damage} damage to {target.gameObject.name}. Target HP: {target.currentHealth}/{target.maxHealth}");

        if (target.currentHealth <= 0)
        {
            target.Die();
        }
    }


    public static int HexDistance(HexCell a, HexCell b)
    {
        if (a == null || b == null)
            return int.MaxValue;

        int q1 = a.gridPosition.x;
        int r1 = a.gridPosition.y;
        int q2 = b.gridPosition.x;
        int r2 = b.gridPosition.y;

        int x1 = q1 - (r1 - (r1 & 1)) / 2;
        int z1 = r1;
        int y1 = -x1 - z1;

        int x2 = q2 - (r2 - (r2 & 1)) / 2;
        int z2 = r2;
        int y2 = -x2 - z2;

        return (Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) + Mathf.Abs(z1 - z2)) / 2;
    }

    public static bool IsInRange(HexCell from, HexCell to, int range)
    {
        return HexDistance(from, to) <= range;
    }

    public static int GetExpectedDamage(Unit attacker)
    {
        if (attacker == null)
            return 0;

        return attacker.attackPower;
    }
    public static bool WouldBeLethal(Unit attacker, Unit target)
    {
        if (attacker == null || target == null)
            return false;

        return target.currentHealth <= attacker.attackPower;
    }
}
