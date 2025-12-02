using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Unit))]
public class UnitAI : MonoBehaviour
{
    private Unit unit;
    private HexGrid hexGrid;
    private InfluenceMap influenceMap;
    private TacticalWaypoints tacticalWaypoints;
    private GameManager gameManager;
    private DijkstraPathfinding aiPathfinding;

    private BTNode behaviorTree;

    void Start()
    {
        unit = GetComponent<Unit>();
        hexGrid = FindObjectOfType<HexGrid>();
        influenceMap = FindObjectOfType<InfluenceMap>();
        tacticalWaypoints = FindObjectOfType<TacticalWaypoints>();
        gameManager = GameManager.Instance;
        aiPathfinding = new DijkstraPathfinding(hexGrid);

        BuildBehaviorTree();
    }

    private void BuildBehaviorTree()
    {
        behaviorTree = new BTSelector(new List<BTNode>
        {
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
                    new BTAction(ExecuteAttackBehavior)
                }),

                new BTSequence(new List<BTNode>
                {
                    new BTCondition(() => unit.currentOrder == OrderType.Defend),
                    new BTAction(ExecuteDefendBehavior)
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

    public void ExecuteTurn()
    {
        if (unit == null || behaviorTree == null)
            return;

        NodeState result = behaviorTree.Evaluate();
        Debug.Log($"[UnitAI] {gameObject.name} behavior tree result: {result}");
    }

    private bool IsEnemyInAttackRange()
    {
        if (unit.hasAttacked)
            return false;

        Unit nearestEnemy = FindNearestEnemy();
        return nearestEnemy != null && CombatSystem.CanAttack(unit, nearestEnemy);
    }

    private bool IsInDanger()
    {
        if (unit.GetHealthPercentage() < 0.3f)
            return true;

        if (influenceMap != null && influenceMap.IsDangerZone(unit.CurrentCell))
            return true;

        return false;
    }

    private NodeState AttackNearestEnemy()
    {
        Unit enemy = FindNearestEnemy();
        if (enemy != null && CombatSystem.CanAttack(unit, enemy))
        {
            bool success = unit.AttackUnit(enemy);
            return success ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteAttackBehavior()
    {
        if (IsInDanger())
        {
            unit.currentOrder = OrderType.Retreat;
            return ExecuteRetreatBehavior();
        }

        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            if (distance <= unit.attackRange && !unit.hasAttacked)
            {
                return unit.AttackUnit(nearestEnemy) ? NodeState.Success : NodeState.Failure;
            }
            else if (distance <= 5 && unit.remainingMovement > 0)
            {
                aiPathfinding.CreateMap(unit);
                return unit.MoveToCell(nearestEnemy.CurrentCell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
            }
        }

        Waypoint attackWaypoint = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.Attack);
        if (attackWaypoint != null && unit.remainingMovement > 0)
        {
            aiPathfinding.CreateMap(unit);
            bool moved = unit.MoveToCell(attackWaypoint.cell, aiPathfinding);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteDefendBehavior()
    {
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            if (distance <= unit.attackRange && !unit.hasAttacked)
            {
                return unit.AttackUnit(nearestEnemy) ? NodeState.Success : NodeState.Failure;
            }

            if (distance <= 4 && unit.remainingMovement > 0)
            {
                aiPathfinding.CreateMap(unit);
                return unit.MoveToCell(nearestEnemy.CurrentCell, aiPathfinding) ? NodeState.Success : NodeState.Failure;
            }
        }

        Waypoint defenseWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Defense);
        if (defenseWaypoint != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, defenseWaypoint.cell);

            if (distance > 2 && unit.remainingMovement > 0)
            {
                aiPathfinding.CreateMap(unit);
                bool moved = unit.MoveToCell(defenseWaypoint.cell, aiPathfinding);
                return moved ? NodeState.Success : NodeState.Failure;
            }
        }

        return NodeState.Success;
    }

    private NodeState ExecuteGatherBehavior()
    {
        if (unit.CurrentCell != null && unit.CurrentCell.isResourceNode && !unit.CurrentCell.resourceCollected)
        {
            return NodeState.Success;
        }

        Waypoint resourceWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Resource);
        if (resourceWaypoint != null && unit.remainingMovement > 0)
        {
            aiPathfinding.CreateMap(unit);
            bool moved = unit.MoveToCell(resourceWaypoint.cell, aiPathfinding);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteRetreatBehavior()
    {
        HexCell safeCell = influenceMap?.FindNearestSafeCell(unit.CurrentCell);

        if (safeCell != null && unit.remainingMovement > 0)
        {
            if (safeCell == unit.CurrentCell || influenceMap.IsSafeZone(unit.CurrentCell))
            {
                return NodeState.Success;
            }

            aiPathfinding.CreateMap(unit);
            bool moved = unit.MoveToCell(safeCell, aiPathfinding);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        HexCell friendlyBase = hexGrid?.GetPlayerBase(unit.OwnerPlayerID);
        if (friendlyBase != null && unit.remainingMovement > 0)
        {
            aiPathfinding.CreateMap(unit);
            bool moved = unit.MoveToCell(friendlyBase, aiPathfinding);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteIdleBehavior()
    {
        return NodeState.Success;
    }

    private Unit FindNearestEnemy()
    {
        if (gameManager == null)
            return null;

        List<Unit> enemies = gameManager.GetAllUnitsForPlayer(1 - unit.OwnerPlayerID);
        Unit nearest = null;
        int minDistance = int.MaxValue;

        foreach (Unit enemy in enemies)
        {
            if (enemy == null || enemy.CurrentCell == null)
                continue;

            int distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private Unit FindBestTarget()
    {
        if (gameManager == null)
            return null;

        List<Unit> enemies = gameManager.GetAllUnitsForPlayer(1 - unit.OwnerPlayerID);
        List<Unit> inRange = enemies.Where(e =>
            e != null &&
            e.CurrentCell != null &&
            CombatSystem.IsInRange(unit.CurrentCell, e.CurrentCell, unit.attackRange)
        ).ToList();

        if (inRange.Count == 0)
            return null;

        Unit bestTarget = inRange
            .OrderByDescending(e => CombatSystem.WouldBeLethal(unit, e) ? 1 : 0)
            .ThenByDescending(e => e.attackPower)
            .ThenBy(e => e.currentHealth)
            .First();

        return bestTarget;
    }
}
