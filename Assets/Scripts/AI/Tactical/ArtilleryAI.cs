using UnityEngine;
using System.Collections.Generic;

public class ArtilleryAI : UnitAI
{
    private const int SAFE_DISTANCE = 3;

    protected override void BuildBehaviorTree()
    {
        behaviorTree = new BTSelector(new List<BTNode>
        {
            new BTSequence(new List<BTNode>
            {
                new BTCondition(IsEnemyTooClose),
                new BTAction(ExecuteRepositioning)
            }),

            new BTSequence(new List<BTNode>
            {
                new BTCondition(HasTargetInRange),
                new BTAction(ExecuteRangedAttack)
            }),

            new BTSelector(new List<BTNode>
            {
                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Attack),
                    new BTAction(ExecuteArtillerySupport)
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Defend),
                    new BTAction(ExecuteArtilleryDefense)
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

    private bool IsEnemyTooClose()
    {
        Unit enemy = FindNearestEnemy();
        if (enemy == null)
            return false;

        int distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);
        return distance < SAFE_DISTANCE && unit.remainingMovement > 0;
    }

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

    private NodeState ExecuteRepositioning()
    {
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null)
            return NodeState.Failure;

        HexCell safePosition = FindOptimalFiringPosition(nearestEnemy);
        if (safePosition != null && unit.remainingMovement > 0)
        {
            aiPathfinding.CreateMap(unit);
            bool moved = unit.MoveToCell(safePosition, aiPathfinding);

            if (moved && !unit.hasAttacked)
            {
                int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);
                if (distance <= unit.attackRange)
                {
                    unit.AttackUnit(nearestEnemy);
                }
            }
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteRangedAttack()
    {
        Unit bestTarget = FindBestTarget();
        if (bestTarget != null && !unit.hasAttacked)
        {
            return unit.AttackUnit(bestTarget) ? NodeState.Success : NodeState.Failure;
        }
        return NodeState.Failure;
    }

    private NodeState ExecuteArtillerySupport()
    {
        if (!unit.hasAttacked)
        {
            Unit bestTarget = FindBestTarget();
            if (bestTarget != null)
            {
                int distance = CombatSystem.HexDistance(unit.CurrentCell, bestTarget.CurrentCell);
                if (distance <= unit.attackRange)
                {
                    unit.AttackUnit(bestTarget);
                }
            }
        }

        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null && unit.remainingMovement > 0)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            if (distance < SAFE_DISTANCE || distance > unit.attackRange)
            {
                HexCell optimalPosition = FindOptimalFiringPosition(nearestEnemy);
                if (optimalPosition != null)
                {
                    aiPathfinding.CreateMap(unit);
                    return unit.MoveToCell(optimalPosition, aiPathfinding) ? NodeState.Success : NodeState.Failure;
                }
            }
        }

        Waypoint attackWaypoint = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.Attack);
        if (attackWaypoint != null && unit.remainingMovement > 0)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, attackWaypoint.cell);
            if (distance > unit.attackRange)
            {
                aiPathfinding.CreateMap(unit);
                return unit.MoveToCell(attackWaypoint.cell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
            }
        }

        return NodeState.Success;
    }

    private NodeState ExecuteArtilleryDefense()
    {
        if (!unit.hasAttacked)
        {
            Unit bestTarget = FindBestTarget();
            if (bestTarget != null)
            {
                int distance = CombatSystem.HexDistance(unit.CurrentCell, bestTarget.CurrentCell);
                if (distance <= unit.attackRange)
                {
                    unit.AttackUnit(bestTarget);
                }
            }
        }

        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            if (distance < SAFE_DISTANCE && unit.remainingMovement > 0)
            {
                HexCell retreatCell = FindDefensivePosition();
                if (retreatCell != null)
                {
                    aiPathfinding.CreateMap(unit);
                    return unit.MoveToCell(retreatCell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
                }
            }
        }

        Waypoint defenseWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Defense);
        if (defenseWaypoint != null && unit.remainingMovement > 0)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, defenseWaypoint.cell);
            if (distance > 2)
            {
                aiPathfinding.CreateMap(unit);
                return unit.MoveToCell(defenseWaypoint.cell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
            }
        }

        return NodeState.Success;
    }

    private HexCell FindOptimalFiringPosition(Unit enemy)
    {
        aiPathfinding.CreateMap(unit);
        var result = aiPathfinding.GetCellsOnRange();
        List<HexCell> reachableCells = result.Item2;

        HexCell bestPosition = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in reachableCells)
        {
            if (cell.occupyingUnit != null && cell.occupyingUnit != unit)
                continue;

            int distToEnemy = CombatSystem.HexDistance(cell, enemy.CurrentCell);

            if (distToEnemy > unit.attackRange)
                continue;

            float score = 0f;

            if (distToEnemy >= SAFE_DISTANCE)
                score += 5f;

            if (cell.terrainType == TerrainType.Montaña)
                score += 2f;
            else if (cell.terrainType == TerrainType.Llanura)
                score += 1f;

            if (influenceMap != null && !influenceMap.IsDangerZone(cell))
                score += 3f;

            List<Unit> friendlyUnits = gameManager.GetAllUnitsForPlayer(unit.OwnerPlayerID);
            foreach (Unit friendly in friendlyUnits)
            {
                if (friendly != unit && CombatSystem.HexDistance(cell, friendly.CurrentCell) <= 2)
                {
                    score += 1f;
                    break;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = cell;
            }
        }

        return bestPosition;
    }

    private HexCell FindDefensivePosition()
    {
        aiPathfinding.CreateMap(unit);
        var result = aiPathfinding.GetCellsOnRange();
        List<HexCell> reachableCells = result.Item2;

        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null)
            return null;

        HexCell bestPosition = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in reachableCells)
        {
            if (cell.occupyingUnit != null && cell.occupyingUnit != unit)
                continue;

            int distToEnemy = CombatSystem.HexDistance(cell, nearestEnemy.CurrentCell);
            if (distToEnemy < SAFE_DISTANCE)
                continue;

            float score = 0f;

            if (distToEnemy <= unit.attackRange)
                score += 4f;

            score += distToEnemy * 0.5f;

            if (influenceMap != null && influenceMap.IsSafeZone(cell))
                score += 3f;

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = cell;
            }
        }

        return bestPosition;
    }

    private Unit FindBestTarget()
    {
        List<Unit> enemies = gameManager.GetAllUnitsForPlayer(1 - unit.OwnerPlayerID);
        Unit bestTarget = null;
        float bestScore = float.MinValue;

        foreach (Unit enemy in enemies)
        {
            if (enemy == null || enemy.CurrentCell == null)
                continue;

            int distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);
            if (distance > unit.attackRange)
                continue;

            float score = 0f;

            float healthPercent = enemy.GetHealthPercentage();
            if (healthPercent < 0.5f)
                score += 3f;

            if (enemy.unitType == UnitType.Artillery)
                score += 2f;
            else if (enemy.unitType == UnitType.Cavalry)
                score += 1.5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = enemy;
            }
        }

        return bestTarget ?? FindNearestEnemy();
    }
}
