using UnityEngine;
using System.Collections.Generic;

public class CavalryAI : UnitAI
{
    protected override void BuildBehaviorTree()
    {
        behaviorTree = new BTSelector(new List<BTNode>
        {
            new BTSequence(new List<BTNode>
            {
                new BTCondition(() => IsInDanger() && unit.GetHealthPercentage() < 0.4f),
                new BTAction(ExecuteFastRetreat)
            }),

            new BTSequence(new List<BTNode>
            {
                new BTCondition(CanFlankEnemy),
                new BTAction(ExecuteFlankAttack)
            }),

            new BTSequence(new List<BTNode>
            {
                new BTCondition(IsEnemyInAttackRange),
                new BTAction(AttackNearestEnemy)
            }),

            new BTSelector(new List<BTNode>
            {
                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Attack),
                    new BTAction(ExecuteCavalryCharge)
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Defend),
                    new BTAction(ExecuteCavalryPatrol)
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.GatherResources),
                    new BTAction(ExecuteResourceRaid)
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Retreat),
                    new BTAction(ExecuteFastRetreat)
                })
            }),

            new BTAction(ExecuteIdleBehavior)
        });
    }

    private bool CanFlankEnemy()
    {
        if (unit.hasAttacked || unit.remainingMovement < 2)
            return false;

        Unit enemy = FindNearestEnemy();
        if (enemy == null)
            return false;

        aiPathfinding.CreateMap(unit);
        HexCell flankCell = FindFlankingPosition(enemy);
        return flankCell != null;
    }

    private NodeState ExecuteFlankAttack()
    {
        Unit enemy = FindNearestEnemy();
        if (enemy == null)
            return NodeState.Failure;

        aiPathfinding.CreateMap(unit);
        HexCell flankCell = FindFlankingPosition(enemy);
        if (flankCell != null && unit.remainingMovement > 0)
        {
            bool moved = unit.MoveToCell(flankCell, aiPathfinding);

            if (moved && !unit.hasAttacked)
            {
                int distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);
                if (distance <= unit.attackRange)
                {
                    unit.AttackUnit(enemy);
                }
            }
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteCavalryCharge()
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
                HexCell chargeTarget = FindChargeTarget(nearestEnemy);
                if (chargeTarget != null)
                {
                    bool moved = unit.MoveToCell(chargeTarget, aiPathfinding);

                    if (moved && !unit.hasAttacked)
                    {
                        distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);
                        if (distance <= unit.attackRange)
                        {
                            unit.AttackUnit(nearestEnemy);
                        }
                    }
                    return moved ? NodeState.Success : NodeState.Failure;
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

    private NodeState ExecuteCavalryPatrol()
    {
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            if (distance <= unit.attackRange && !unit.hasAttacked)
            {
                return unit.AttackUnit(nearestEnemy) ? NodeState.Success : NodeState.Failure;
            }

            if (distance <= 4 && distance > unit.attackRange && unit.remainingMovement > 0)
            {
                HexCell interceptCell = FindInterceptPosition(nearestEnemy);
                if (interceptCell != null)
                {
                    aiPathfinding.CreateMap(unit);
                    return unit.MoveToCell(interceptCell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
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

    private NodeState ExecuteResourceRaid()
    {
        if (unit.CurrentCell != null && unit.CurrentCell.isResourceNode && !unit.CurrentCell.resourceCollected)
        {
            return NodeState.Success;
        }

        Waypoint resourceWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Resource);
        if (resourceWaypoint != null && unit.remainingMovement > 0)
        {
            aiPathfinding.CreateMap(unit);
            return unit.MoveToCell(resourceWaypoint.cell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteFastRetreat()
    {
        HexCell safeCell = influenceMap?.FindNearestSafeCell(unit.CurrentCell);

        if (safeCell != null && unit.remainingMovement > 0)
        {
            if (safeCell == unit.CurrentCell || influenceMap.IsSafeZone(unit.CurrentCell))
            {
                return NodeState.Success;
            }

            aiPathfinding.CreateMap(unit);
            return unit.MoveToCell(safeCell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
        }

        HexCell friendlyBase = hexGrid?.GetPlayerBase(unit.OwnerPlayerID);
        if (friendlyBase != null && unit.remainingMovement > 0)
        {
            aiPathfinding.CreateMap(unit);
            return unit.MoveToCell(friendlyBase, aiPathfinding) ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private HexCell FindFlankingPosition(Unit enemy)
    {
        var result = aiPathfinding.GetCellsOnRange();
        List<HexCell> reachableCells = result.Item2;

        List<HexCell> neighbors = enemy.CurrentCell.neighbors;
        HexCell bestFlank = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in neighbors)
        {
            if (cell.occupyingUnit != null)
                continue;

            if (!reachableCells.Contains(cell))
                continue;

            float score = 0f;

            if (cell.terrainType == TerrainType.Llanura)
                score += 3f;

            if (influenceMap != null && !influenceMap.IsDangerZone(cell))
                score += 2f;

            List<Unit> friendlyUnits = gameManager.GetAllUnitsForPlayer(unit.OwnerPlayerID);
            bool hasFriendlyNearby = false;
            foreach (Unit friendly in friendlyUnits)
            {
                if (friendly != unit && CombatSystem.HexDistance(cell, friendly.CurrentCell) <= 2)
                {
                    hasFriendlyNearby = true;
                    break;
                }
            }
            if (!hasFriendlyNearby)
                score += 1.5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestFlank = cell;
            }
        }

        return bestFlank;
    }

    private HexCell FindChargeTarget(Unit enemy)
    {
        var result = aiPathfinding.GetCellsOnRange();
        List<HexCell> reachableCells = result.Item2;

        List<HexCell> neighbors = enemy.CurrentCell.neighbors;
        HexCell bestTarget = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in neighbors)
        {
            if (cell.occupyingUnit != null)
                continue;

            if (!reachableCells.Contains(cell))
                continue;

            float score = 0f;

            if (cell.terrainType == TerrainType.Llanura)
                score += 2f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = cell;
            }
        }

        return bestTarget;
    }

    private HexCell FindInterceptPosition(Unit enemy)
    {
        HexCell enemyBase = hexGrid?.GetPlayerBase(1 - unit.OwnerPlayerID);
        HexCell friendlyBase = hexGrid?.GetPlayerBase(unit.OwnerPlayerID);

        if (friendlyBase == null)
            return enemy.CurrentCell;

        aiPathfinding.CreateMap(unit);
        var result = aiPathfinding.GetCellsOnRange();
        List<HexCell> reachableCells = result.Item2;

        HexCell bestIntercept = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in reachableCells)
        {
            if (cell.occupyingUnit != null && cell.occupyingUnit != unit)
                continue;

            int distToEnemy = CombatSystem.HexDistance(cell, enemy.CurrentCell);
            int distToBase = CombatSystem.HexDistance(cell, friendlyBase);

            float score = 0f;
            if (distToEnemy <= unit.attackRange + 1)
                score += 3f;
            if (distToBase < CombatSystem.HexDistance(enemy.CurrentCell, friendlyBase))
                score += 2f;

            if (score > bestScore)
            {
                bestScore = score;
                bestIntercept = cell;
            }
        }

        return bestIntercept;
    }
}
