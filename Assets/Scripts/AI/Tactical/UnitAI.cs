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

    /// <summary>
    /// Condición para BT: ¿Puede la unidad alcanzar la base enemiga en este turno?
    /// </summary>
    protected bool CanInvadeEnemyBase()
    {
        if (unit.remainingMovement <= 0)
            return false;

        Waypoint enemyBaseWaypoint = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.EnemyBase);
        if (enemyBaseWaypoint == null)
            return false;

        int distance = CombatSystem.HexDistance(unit.CurrentCell, enemyBaseWaypoint.cell);
        return distance <= unit.remainingMovement;
    }

    /// <summary>
    /// Acción para BT: Invadir la base enemiga.
    /// </summary>
    protected NodeState InvadeEnemyBase()
    {
        Waypoint enemyBaseWaypoint = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.EnemyBase);
        if (enemyBaseWaypoint == null)
            return NodeState.Failure;

        bool moved = MoveTowardsTarget(enemyBaseWaypoint.cell, avoidDanger: false);
        if (moved)
        {
            Debug.Log($"[{unit.unitType}] Invading enemy base at {enemyBaseWaypoint.cell.gridPosition}!");
        }
        return moved ? NodeState.Success : NodeState.Failure;
    }

    #region Funciones Parametrizadas

    /// <summary>
    /// Obtiene celdas dentro de un rango usando BFS.
    /// </summary>
    protected List<HexCell> GetCellsInRange(HexCell center, int maxDistance)
    {
        List<HexCell> cells = new List<HexCell>();
        Queue<(HexCell cell, int dist)> queue = new Queue<(HexCell, int)>();
        HashSet<HexCell> visited = new HashSet<HexCell>();

        queue.Enqueue((center, 0));
        visited.Add(center);

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            cells.Add(current);

            if (dist < maxDistance)
            {
                foreach (HexCell neighbor in current.neighbors)
                {
                    if (!visited.Contains(neighbor) && neighbor.IsPassableForPlayer(unit.OwnerPlayerID))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, dist + 1));
                    }
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// Verifica si una celda es válida para movimiento.
    /// </summary>
    protected bool IsCellValidForMovement(HexCell cell)
    {
        if (cell.occupyingUnit != null && cell.occupyingUnit != unit)
            return false;

        if (!cell.IsPassableForPlayer(unit.OwnerPlayerID))
            return false;

        return true;
    }

    /// <summary>
    /// Evalúa el score de una celda según el perfil de terreno de la unidad.
    /// </summary>
    protected float EvaluateCellScore(HexCell cell, Unit enemy = null, int safeDistance = 0)
    {
        float score = 0f;

        // Bonus de terreno según tipo de unidad
        if (unit.terrainBonus != null && unit.terrainBonus.TryGetValue(cell.terrainType, out float bonus))
        {
            score += bonus;
        }

        // Bonus por zona segura
        if (influenceMap != null && !influenceMap.IsDangerZone(cell))
        {
            score += 2f;
        }

        // Evaluación respecto al enemigo
        if (enemy != null)
        {
            int distToEnemy = CombatSystem.HexDistance(cell, enemy.CurrentCell);

            // Bonus por estar en rango de ataque
            if (distToEnemy <= unit.attackRange)
            {
                score += 3f;
            }

            // Bonus por mantener distancia segura (Artillery)
            if (safeDistance > 0 && distToEnemy >= safeDistance)
            {
                score += 4f;
            }
        }

        return score;
    }

    /// <summary>
    /// Busca la mejor posición cerca de un enemigo.
    /// </summary>
    protected HexCell FindBestPosition(Unit enemy, int safeDistance = 0)
    {
        if (enemy == null)
            return null;

        List<HexCell> candidates = GetCellsInRange(unit.CurrentCell, unit.remainingMovement);
        HexCell bestCell = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in candidates)
        {
            if (!IsCellValidForMovement(cell))
                continue;

            int distToEnemy = CombatSystem.HexDistance(cell, enemy.CurrentCell);

            // Respetar distancia segura
            if (safeDistance > 0 && distToEnemy < safeDistance)
                continue;

            // Debe estar en rango de ataque o acercándose
            if (distToEnemy > unit.attackRange + 2)
                continue;

            float score = EvaluateCellScore(cell, enemy, safeDistance);

            // Penalización por distancia desde posición actual
            int distFromCurrent = CombatSystem.HexDistance(unit.CurrentCell, cell);
            score -= distFromCurrent * 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        return bestCell;
    }

    /// <summary>
    /// Acción de ataque parametrizada.
    /// </summary>
    protected NodeState ExecuteAttackAction(int safeDistance = 0, bool avoidDanger = false)
    {
        Unit enemy = FindNearestEnemy();
        if (enemy == null)
        {
            // Si no hay enemigo, ir a waypoint
            Waypoint wp = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.EnemyBase)
                       ?? tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.Attack);
            if (wp != null && unit.remainingMovement > 0)
            {
                return MoveTowardsTarget(wp.cell, avoidDanger) ? NodeState.Success : NodeState.Failure;
            }
            return NodeState.Failure;
        }

        int distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);

        // Atacar si está en rango y a distancia segura
        if (distance <= unit.attackRange && distance >= safeDistance && !unit.hasAttacked)
        {
            unit.AttackUnit(enemy);
        }

        // Moverse hacia posición óptima si aún puede
        if (unit.remainingMovement > 0)
        {
            HexCell bestPos = FindBestPosition(enemy, safeDistance);
            if (bestPos != null && bestPos != unit.CurrentCell)
            {
                bool moved = MoveTowardsTarget(bestPos, avoidDanger);

                // Atacar después de moverse si es posible
                if (moved && !unit.hasAttacked)
                {
                    distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);
                    if (distance <= unit.attackRange && distance >= safeDistance)
                    {
                        unit.AttackUnit(enemy);
                    }
                }
                return moved ? NodeState.Success : NodeState.Failure;
            }
        }

        return unit.hasAttacked ? NodeState.Success : NodeState.Failure;
    }

    /// <summary>
    /// Acción de defensa parametrizada.
    /// </summary>
    protected NodeState ExecuteDefendAction(int safeDistance = 0)
    {
        Unit enemy = FindNearestEnemy();
        if (enemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, enemy.CurrentCell);

            // Atacar si está en rango y a distancia segura
            if (distance <= unit.attackRange && distance >= safeDistance && !unit.hasAttacked)
            {
                unit.AttackUnit(enemy);
            }

            // Si está demasiado cerca (para Artillery), retroceder
            if (safeDistance > 0 && distance < safeDistance && unit.remainingMovement > 0)
            {
                HexCell safeCell = FindBestPosition(enemy, safeDistance);
                if (safeCell != null)
                {
                    bool moved = MoveTowardsTarget(safeCell, avoidDanger: true);
                    return moved ? NodeState.Success : NodeState.Failure;
                }
            }

            // Si está cerca, buscar posición defensiva
            if (distance <= 3 && unit.remainingMovement > 0)
            {
                HexCell defensiveCell = FindBestPosition(enemy, safeDistance);
                if (defensiveCell != null && defensiveCell != unit.CurrentCell)
                {
                    bool moved = MoveTowardsTarget(defensiveCell, avoidDanger: true);
                    return moved ? NodeState.Success : NodeState.Failure;
                }
            }
        }

        // Si no hay enemigo, ir al waypoint de defensa
        Waypoint defenseWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Defense);
        if (defenseWaypoint != null && unit.remainingMovement > 0)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, defenseWaypoint.cell);
            if (distance > 1)
            {
                bool moved = MoveTowardsTarget(defenseWaypoint.cell, avoidDanger: true);
                return moved ? NodeState.Success : NodeState.Failure;
            }
        }

        return NodeState.Success;
    }

    #endregion
}
