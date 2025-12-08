using UnityEngine;
using System.Collections.Generic;

public class ArtilleryAI : UnitAI
{
    private const int SAFE_DISTANCE = 3;

    protected override void BuildBehaviorTree()
    {
        behaviorTree = new BTSelector(new List<BTNode>
        {
            //Invadir base enemiga si es alcanzable
            new BTSequence(new List<BTNode>
            {
                new BTCondition(CanInvadeEnemyBase),
                new BTAction(InvadeEnemyBase)
            }),

            // Reposicionarse si el enemigo está demasiado cerca
            new BTSequence(new List<BTNode>
            {
                new BTCondition(IsEnemyTooClose),
                new BTAction(() => ExecuteDefendAction(safeDistance: SAFE_DISTANCE))
            }),

            // Atacar enemigo en rango (respetando distancia segura)
            new BTSequence(new List<BTNode>
            {
                new BTCondition(HasTargetInRange),
                new BTAction(AttackNearestEnemy)
            }),

            // Ejecutar según orden actual
            new BTSelector(new List<BTNode>
            {
                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Attack),
                    // Artillery mantiene distancia y evita peligro
                    new BTAction(() => ExecuteAttackAction(safeDistance: SAFE_DISTANCE, avoidDanger: true))
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Defend),
                    new BTAction(() => ExecuteDefendAction(safeDistance: SAFE_DISTANCE))
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.GatherResources),
                    new BTAction(ExecuteGatherBehavior)
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Retreat),
                    new BTAction(ExecuteRetreatBehavior)
                })
            }),

            new BTAction(ExecuteIdleBehavior)
        });
    }

    /// Condición: ¿Hay un enemigo demasiado cerca?
    private bool IsEnemyTooClose()
    {
        Unit enemy = FindNearestEnemy();
        if (enemy == null)
            return false;

        int distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);
        return distance < SAFE_DISTANCE && unit.remainingMovement > 0;
    }

    /// Condición: ¿Hay un objetivo en rango y a distancia segura?
    private bool HasTargetInRange()
    {
        if (unit.hasAttacked)
            return false;

        Unit enemy = FindNearestEnemy();
        if (enemy == null)
            return false;

        int distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);
        return distance <= unit.attackRange && distance >= SAFE_DISTANCE - 1;
    }
}
