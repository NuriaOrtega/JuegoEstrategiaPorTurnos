using UnityEngine;
using System.Collections.Generic;

public class CavalryAI : UnitAI
{
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

            // )único) Cargar contra enemigo a distancia media
            new BTSequence(new List<BTNode>
            {
                new BTCondition(CanCharge),
                new BTAction(ExecuteCharge)
            }),

            // Retirarse si está muy herido y en peligro (umbral más alto que Infantry)
            new BTSequence(new List<BTNode>
            {
                new BTCondition(() => IsInDanger() && unit.GetHealthPercentage() < 0.4f),
                new BTAction(ExecuteRetreatBehavior)
            }),

            // Atacar enemigo en rango
            new BTSequence(new List<BTNode>
            {
                new BTCondition(IsEnemyInAttackRange),
                new BTAction(AttackNearestEnemy)
            }),

            // Ejecutar según orden actual
            new BTSelector(new List<BTNode>
            {
                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Attack),
                    // Cavalry es agresivo: no evita peligro
                    new BTAction(() => ExecuteAttackAction(safeDistance: 0, avoidDanger: false))
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Defend),
                    new BTAction(() => ExecuteDefendAction(safeDistance: 0))
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.GatherResources),
                    // Cavalry es rápido, puede arriesgarse más para recursos
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

    private bool CanCharge()
    {
        if (unit.remainingMovement <= 0)
            return false;

        Unit enemy = FindNearestEnemy();
        if (enemy == null)
            return false;

        int distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);
        // Cargar si está a distancia media (2-4 celdas), no ya en rango ni muy lejos
        return distance >= 2 && distance <= 4;
    }


    private NodeState ExecuteCharge()
    {
        Unit enemy = FindNearestEnemy();
        if (enemy == null)
            return NodeState.Failure;

        HexCell chargeTarget = FindChargeTarget(enemy);
        if (chargeTarget == null)
            chargeTarget = enemy.CurrentCell; // Ir directo si no hay llanura

        bool moved = MoveTowardsTarget(chargeTarget, avoidDanger: false);

        // Atacar después de cargar si es posible
        if (moved && !unit.hasAttacked && IsEnemyInAttackRange())
        {
            AttackNearestEnemy();
        }

        return moved ? NodeState.Success : NodeState.Failure;
    }

    private HexCell FindChargeTarget(Unit enemy)
    {
        HexCell bestCell = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in enemy.CurrentCell.neighbors)
        {
            if (!IsCellValidForMovement(cell))
                continue;

            float score = 0f;

            // Bonus por llanura (terreno preferido de Cavalry)
            if (cell.terrainType == TerrainType.Llanura)
                score += 3f;

            // Bonus si es alcanzable en este turno
            int dist = CombatSystem.HexDistance(unit.CurrentCell, cell);
            if (dist <= unit.remainingMovement)
                score += 2f;

            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        return bestCell;
    }

}
