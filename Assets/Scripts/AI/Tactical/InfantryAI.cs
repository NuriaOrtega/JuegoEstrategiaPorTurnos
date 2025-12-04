using UnityEngine;
using System.Collections.Generic;

public class InfantryAI : UnitAI
{
    protected override void BuildBehaviorTree()
    {
        behaviorTree = new BTSelector(new List<BTNode>
        {
            new BTSequence(new List<BTNode>
            {
                new BTCondition(IsEnemyInAttackRange),
                new BTAction(AttackNearestEnemy)
            }),

            new BTSequence(new List<BTNode>
            {
                new BTCondition(() => IsInDanger() && unit.GetHealthPercentage() < 0.25f),
                new BTAction(ExecuteRetreatBehavior)
            }),

            new BTSelector(new List<BTNode>
            {
                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Attack),
                    new BTAction(ExecuteInfantryAttack)
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Defend),
                    new BTAction(ExecuteInfantryDefend)
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

    private NodeState ExecuteInfantryAttack()
    {
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            if (distance <= unit.attackRange && !unit.hasAttacked)
            {
                return unit.AttackUnit(nearestEnemy) ? NodeState.Success : NodeState.Failure;
            }

            if (unit.remainingMovement > 0)
            {
                aiPathfinding.CreateMap(unit);
                HexCell targetCell = FindBestApproachCell(nearestEnemy);
                if (targetCell != null)
                {
                    return unit.MoveToCell(targetCell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
                }
            }
        }

        Waypoint attackWaypoint = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.Attack);
        if (attackWaypoint != null && unit.remainingMovement > 0)
        {
            aiPathfinding.CreateMap(unit);
            return unit.MoveToCell(attackWaypoint.cell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteInfantryDefend()
    {
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            if (distance <= unit.attackRange && !unit.hasAttacked)
            {
                return unit.AttackUnit(nearestEnemy) ? NodeState.Success : NodeState.Failure;
            }

            if (distance <= 3 && unit.remainingMovement > 0)
            {
                HexCell defensiveCell = FindDefensivePosition(nearestEnemy);
                if (defensiveCell != null)
                {
                    aiPathfinding.CreateMap(unit);
                    return unit.MoveToCell(defensiveCell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
                }
            }
        }

        Waypoint defenseWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Defense);
        if (defenseWaypoint != null && unit.remainingMovement > 0)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, defenseWaypoint.cell);
            if (distance > 1)
            {
                aiPathfinding.CreateMap(unit);
                return unit.MoveToCell(defenseWaypoint.cell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
            }
        }

        return NodeState.Success;
    }

    private HexCell FindBestApproachCell(Unit enemy)
    {
        var result = aiPathfinding.GetCellsOnRange();
        List<HexCell> reachableCells = result.Item2;

        List<HexCell> neighbors = enemy.CurrentCell.neighbors;
        HexCell bestCell = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in neighbors)
        {
            if (cell.occupyingUnit != null)
                continue;

            if (!reachableCells.Contains(cell))
                continue;

            float score = 0f;

            if (cell.terrainType == TerrainType.Bosque)
                score += 2f;
            else if (cell.terrainType == TerrainType.Montaña)
                score += 1f;

            if (influenceMap != null && !influenceMap.IsDangerZone(cell))
                score += 1f;

            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        return bestCell;
    }

    private HexCell FindDefensivePosition(Unit enemy)
    {
        List<HexCell> reachableCells = GetReachableCells();
        HexCell bestCell = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in reachableCells)
        {
            if (cell.occupyingUnit != null && cell.occupyingUnit != unit)
                continue;

            float score = 0f;
            int distanceToEnemy = CombatSystem.HexDistance(cell, enemy.CurrentCell);

            if (distanceToEnemy <= unit.attackRange)
                score += 3f;

            if (cell.terrainType == TerrainType.Bosque)
                score += 2f;
            else if (cell.terrainType == TerrainType.Montaña)
                score += 1.5f;

            if (influenceMap != null && !influenceMap.IsDangerZone(cell))
                score += 1f;

            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        return bestCell;
    }

    private List<HexCell> GetReachableCells()
    {
        aiPathfinding.CreateMap(unit);
        var result = aiPathfinding.GetCellsOnRange();
        return result.Item2;
    }
}
