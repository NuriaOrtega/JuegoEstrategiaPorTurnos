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
            bool moved = MoveTowardsTarget(safePosition, avoidDanger: true);

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
        // Primero intentar atacar
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

        // Luego reposicionarse si es necesario
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null && unit.remainingMovement > 0)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            // Si está demasiado cerca o fuera de rango, buscar posición óptima
            if (distance < SAFE_DISTANCE || distance > unit.attackRange)
            {
                HexCell optimalPosition = FindOptimalFiringPosition(nearestEnemy);
                if (optimalPosition != null)
                {
                    bool moved = MoveTowardsTarget(optimalPosition, avoidDanger: true);
                    return moved ? NodeState.Success : NodeState.Failure;
                }
            }
        }

        // Si no hay enemigo directo, acercarse a waypoint de ataque
        Waypoint attackWaypoint = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.Attack);
        if (attackWaypoint != null && unit.remainingMovement > 0)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, attackWaypoint.cell);
            if (distance > unit.attackRange)
            {
                bool moved = MoveTowardsTarget(attackWaypoint.cell, avoidDanger: true);
                return moved ? NodeState.Success : NodeState.Failure;
            }
        }

        return NodeState.Success;
    }

    private NodeState ExecuteArtilleryDefense()
    {
        // Primero intentar atacar
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

        // Retirarse si el enemigo está demasiado cerca
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, nearestEnemy.CurrentCell);

            if (distance < SAFE_DISTANCE && unit.remainingMovement > 0)
            {
                HexCell retreatCell = FindDefensivePosition();
                if (retreatCell != null)
                {
                    bool moved = MoveTowardsTarget(retreatCell, avoidDanger: true);
                    return moved ? NodeState.Success : NodeState.Failure;
                }
            }
        }

        // Si no hay amenaza inmediata, ir al waypoint de defensa
        Waypoint defenseWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Defense);
        if (defenseWaypoint != null && unit.remainingMovement > 0)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, defenseWaypoint.cell);
            if (distance > 2)
            {
                bool moved = MoveTowardsTarget(defenseWaypoint.cell, avoidDanger: true);
                return moved ? NodeState.Success : NodeState.Failure;
            }
        }

        return NodeState.Success;
    }

    /// <summary>
    /// Encuentra la mejor posición de disparo: a distancia segura del enemigo,
    /// dentro del rango de ataque, preferiblemente en terreno elevado.
    /// </summary>
    private HexCell FindOptimalFiringPosition(Unit enemy)
    {
        List<HexCell> candidates = GetCellsInRange(unit.CurrentCell, unit.remainingMovement);
        HexCell bestPosition = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in candidates)
        {
            if (cell.occupyingUnit != null && cell.occupyingUnit != unit)
                continue;

            if (!cell.IsPassableForPlayer(unit.OwnerPlayerID))
                continue;

            int distToEnemy = CombatSystem.HexDistance(cell, enemy.CurrentCell);

            // Debe estar dentro del rango de ataque
            if (distToEnemy > unit.attackRange)
                continue;

            float score = 0f;

            // Bonificación por mantener distancia segura
            if (distToEnemy >= SAFE_DISTANCE)
                score += 5f;

            // Bonificación por terreno elevado (artillería desde montaña es potente)
            if (cell.terrainType == TerrainType.Montaña)
                score += 2f;
            else if (cell.terrainType == TerrainType.Llanura)
                score += 1f;

            // Bonificación por zona segura
            if (influenceMap != null && !influenceMap.IsDangerZone(cell))
                score += 3f;

            // Bonificación por tener amigos cerca (protección)
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

    /// <summary>
    /// Encuentra posición defensiva: lejos del enemigo pero dentro de rango si es posible.
    /// </summary>
    private HexCell FindDefensivePosition()
    {
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null)
            return null;

        List<HexCell> candidates = GetCellsInRange(unit.CurrentCell, unit.remainingMovement);
        HexCell bestPosition = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in candidates)
        {
            if (cell.occupyingUnit != null && cell.occupyingUnit != unit)
                continue;

            if (!cell.IsPassableForPlayer(unit.OwnerPlayerID))
                continue;

            int distToEnemy = CombatSystem.HexDistance(cell, nearestEnemy.CurrentCell);

            // Debe estar a distancia segura
            if (distToEnemy < SAFE_DISTANCE)
                continue;

            float score = 0f;

            // Bonificación por poder atacar desde la posición
            if (distToEnemy <= unit.attackRange)
                score += 4f;

            // Bonificación por distancia (más lejos = más seguro)
            score += distToEnemy * 0.5f;

            // Bonificación por zona segura
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

    /// <summary>
    /// Encuentra el mejor objetivo para atacar.
    /// Prioriza unidades heridas y artillería enemiga.
    /// </summary>
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

            // Priorizar unidades heridas
            float healthPercent = enemy.GetHealthPercentage();
            if (healthPercent < 0.5f)
                score += 3f;

            // Priorizar artillería enemiga (amenaza a distancia)
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

    /// <summary>
    /// Obtiene celdas dentro de un rango usando BFS simple.
    /// </summary>
    private List<HexCell> GetCellsInRange(HexCell center, int maxDistance)
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
}
