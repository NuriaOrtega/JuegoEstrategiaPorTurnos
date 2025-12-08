using UnityEngine;
using System.Collections.Generic;

public class InfantryAI : UnitAI
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

            // Atacar enemigo en rango
            new BTSequence(new List<BTNode>
            {
                new BTCondition(IsEnemyInAttackRange),
                new BTAction(AttackNearestEnemy)
            }),

            // (único) Buscar cobertura si hay enemigo y no está protegido
            new BTSequence(new List<BTNode>
            {
                new BTCondition(CanSeekCover),
                new BTAction(ExecuteSeekCover)
            }),

            // Retirarse si está muy herido y en peligro
            new BTSequence(new List<BTNode>
            {
                new BTCondition(() => IsInDanger() && unit.GetHealthPercentage() < 0.25f),
                new BTAction(ExecuteRetreatBehavior)
            }),

            // Ejecutar según orden actual
            new BTSelector(new List<BTNode>
            {
                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Attack),
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

    private bool CanSeekCover()
    {
        // No buscar si ya está en cobertura
        if (unit.CurrentCell.terrainType == TerrainType.Bosque ||
            unit.CurrentCell.terrainType == TerrainType.Montaña)
            return false;

        if (unit.remainingMovement <= 0)
            return false;

        // Solo si hay enemigo visible
        Unit enemy = FindNearestEnemy();
        if (enemy == null)
            return false;

        return FindCoverCell() != null;
    }

    private NodeState ExecuteSeekCover()
    {
        HexCell coverCell = FindCoverCell();
        if (coverCell == null)
            return NodeState.Failure;

        bool moved = MoveTowardsTarget(coverCell, avoidDanger: true);
        return moved ? NodeState.Success : NodeState.Failure;
    }

    private HexCell FindCoverCell()
    {
        List<HexCell> candidates = GetCellsInRange(unit.CurrentCell, unit.remainingMovement);
        HexCell bestCell = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in candidates)
        {
            if (!IsCellValidForMovement(cell))
                continue;

            if (cell.terrainType != TerrainType.Bosque && cell.terrainType != TerrainType.Montaña)
                continue;

            float score = EvaluateCellScore(cell);

            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        return bestCell;
    }

}
