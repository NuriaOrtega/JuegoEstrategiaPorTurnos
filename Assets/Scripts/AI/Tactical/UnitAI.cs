using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Unit))]
public class UnitAI : MonoBehaviour
{
    protected Unit unit;
    protected HexGrid hexGrid;
    protected InfluenceMap influenceMap;
    protected TacticalWaypoints tacticalWaypoints;
    protected GameManager gameManager;
    protected TacticalPathfinding tacticalPathfinding;

    protected BTNode behaviorTree;

    void Start()
    {
        unit = GetComponent<Unit>();
        hexGrid = FindObjectOfType<HexGrid>();
        influenceMap = FindObjectOfType<InfluenceMap>();
        tacticalWaypoints = FindObjectOfType<TacticalWaypoints>();
        gameManager = GameManager.Instance;
        tacticalPathfinding = new TacticalPathfinding(hexGrid, influenceMap);

        BuildBehaviorTree();
    }

    protected virtual void BuildBehaviorTree()
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

    protected bool IsEnemyInAttackRange()
    {
        if (unit.hasAttacked)
            return false;

        Unit nearestEnemy = FindNearestEnemy();
        return nearestEnemy != null && CombatSystem.CanAttack(unit, nearestEnemy);
    }

    protected bool IsInDanger()
    {
        if (unit.GetHealthPercentage() < 0.3f)
            return true;

        if (influenceMap != null && influenceMap.IsDangerZone(unit.CurrentCell))
            return true;

        return false;
    }

    protected NodeState AttackNearestEnemy()
    {
        Unit enemy = FindNearestEnemy();
        if (enemy != null && CombatSystem.CanAttack(unit, enemy))
        {
            bool success = unit.AttackUnit(enemy);
            return success ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    protected NodeState ExecuteAttackBehavior()
    {
        if (IsInDanger())
        {
            unit.currentOrder = OrderType.Retreat;
            return ExecuteRetreatBehavior();
        }

        // Primero buscar enemigo cercano
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            // Si está en rango, atacar
            if (distance <= unit.attackRange && !unit.hasAttacked)
            {
                return unit.AttackUnit(nearestEnemy) ? NodeState.Success : NodeState.Failure;
            }

            // Si está cerca, moverse hacia él (sin evitar peligro en modo ataque)
            if (distance <= 5 && unit.remainingMovement > 0)
            {
                bool moved = MoveTowardsTarget(nearestEnemy.CurrentCell, avoidDanger: false);
                return moved ? NodeState.Success : NodeState.Failure;
            }
        }

        // Si no hay enemigo directo, usar waypoint de ataque
        Waypoint attackWaypoint = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.Attack);
        if (attackWaypoint != null && unit.remainingMovement > 0)
        {
            bool moved = MoveTowardsTarget(attackWaypoint.cell, avoidDanger: false);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    protected NodeState ExecuteDefendBehavior()
    {
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            // Si está en rango, atacar
            if (distance <= unit.attackRange && !unit.hasAttacked)
            {
                return unit.AttackUnit(nearestEnemy) ? NodeState.Success : NodeState.Failure;
            }

            // Si está cerca, interceptar (pero con precaución)
            if (distance <= 4 && unit.remainingMovement > 0)
            {
                bool moved = MoveTowardsTarget(nearestEnemy.CurrentCell, avoidDanger: true);
                return moved ? NodeState.Success : NodeState.Failure;
            }
        }

        // Si no hay enemigo, ir al waypoint de defensa más cercano
        Waypoint defenseWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Defense);
        if (defenseWaypoint != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, defenseWaypoint.cell);

            if (distance > 2 && unit.remainingMovement > 0)
            {
                bool moved = MoveTowardsTarget(defenseWaypoint.cell, avoidDanger: true);
                return moved ? NodeState.Success : NodeState.Failure;
            }
        }

        return NodeState.Success;
    }

    protected NodeState ExecuteGatherBehavior()
    {
        // Si ya estamos en un nodo de recursos no recolectado, éxito
        if (unit.CurrentCell != null && unit.CurrentCell.isResourceNode && !unit.CurrentCell.resourceCollected)
        {
            return NodeState.Success;
        }

        // Buscar el waypoint de recursos más cercano
        Waypoint resourceWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Resource);
        if (resourceWaypoint != null && unit.remainingMovement > 0)
        {
            bool moved = MoveTowardsTarget(resourceWaypoint.cell, avoidDanger: true);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    protected NodeState ExecuteRetreatBehavior()
    {
        if (unit.remainingMovement <= 0)
            return NodeState.Failure;

        // Si ya estamos en zona segura, éxito
        if (influenceMap != null && influenceMap.IsSafeZone(unit.CurrentCell))
        {
            return NodeState.Success;
        }

        // Buscar la celda más segura cercana
        HexCell safeCell = tacticalPathfinding?.FindSafestCell(unit.CurrentCell, unit.remainingMovement, unit);

        if (safeCell != null && safeCell != unit.CurrentCell)
        {
            bool moved = MoveTowardsTarget(safeCell, avoidDanger: true);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        // Si no hay celda segura, buscar waypoint de rally
        Waypoint rallyWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Rally);
        if (rallyWaypoint != null)
        {
            bool moved = MoveTowardsTarget(rallyWaypoint.cell, avoidDanger: true);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        // Como último recurso, ir a la base amiga
        HexCell friendlyBase = hexGrid?.GetPlayerBase(unit.OwnerPlayerID);
        if (friendlyBase != null)
        {
            bool moved = MoveTowardsTarget(friendlyBase, avoidDanger: true);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    protected NodeState ExecuteIdleBehavior()
    {
        return NodeState.Success;
    }

    /// <summary>
    /// Mueve la unidad hacia la celda objetivo usando TacticalPathfinding.
    /// Si no puede llegar en este turno, se acerca lo más posible.
    /// </summary>
    protected bool MoveTowardsTarget(HexCell target, bool avoidDanger = true)
    {
        if (target == null || unit.remainingMovement <= 0)
            return false;

        if (target == unit.CurrentCell)
            return true; // Ya estamos en el objetivo

        // Calcular camino táctico (A* con influencia)
        var fullPath = tacticalPathfinding.FindTacticalPath(unit.CurrentCell, target, unit, avoidDanger);

        if (fullPath == null || fullPath.Count < 2)
        {
            Debug.Log($"[UnitAI] {gameObject.name} no path found to {target.gridPosition}");
            return false;
        }

        // Obtener el sub-camino alcanzable con los puntos de movimiento actuales
        var reachablePath = tacticalPathfinding.GetReachablePath(fullPath, unit.remainingMovement);

        if (reachablePath == null || reachablePath.Count < 2)
        {
            Debug.Log($"[UnitAI] {gameObject.name} cannot reach any cell on path");
            return false;
        }

        // Ejecutar movimiento
        bool moved = unit.MoveAlongPath(reachablePath);

        if (moved)
        {
            Debug.Log($"[UnitAI] {gameObject.name} moved towards {target.gridPosition}, reached {unit.CurrentCell.gridPosition}");
        }

        return moved;
    }

    protected Unit FindNearestEnemy()
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
}
