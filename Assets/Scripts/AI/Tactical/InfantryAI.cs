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

            // Si está en rango, atacar
            if (distance <= unit.attackRange && !unit.hasAttacked)
            {
                return unit.AttackUnit(nearestEnemy) ? NodeState.Success : NodeState.Failure;
            }

            // Si puede moverse, buscar mejor posición de aproximación
            if (unit.remainingMovement > 0)
            {
                HexCell targetCell = FindBestApproachCell(nearestEnemy);
                if (targetCell != null)
                {
                    bool moved = MoveTowardsTarget(targetCell, avoidDanger: false);
                    return moved ? NodeState.Success : NodeState.Failure;
                }
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

    private NodeState ExecuteInfantryDefend()
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

            // Si está cerca, buscar posición defensiva
            if (distance <= 3 && unit.remainingMovement > 0)
            {
                HexCell defensiveCell = FindDefensivePosition(nearestEnemy);
                if (defensiveCell != null)
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

    /// <summary>
    /// Busca la mejor celda vecina al enemigo para atacar.
    /// Prioriza terreno con cobertura (bosque, montaña) y zonas seguras.
    /// </summary>
    private HexCell FindBestApproachCell(Unit enemy)
    {
        List<HexCell> neighbors = enemy.CurrentCell.neighbors;
        HexCell bestCell = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in neighbors)
        {
            if (cell.occupyingUnit != null)
                continue;

            if (!cell.IsPassableForPlayer(unit.OwnerPlayerID))
                continue;

            float score = 0f;

            // Bonificación por terreno con cobertura
            if (cell.terrainType == TerrainType.Bosque)
                score += 2f;
            else if (cell.terrainType == TerrainType.Montaña)
                score += 1f;

            // Bonificación por zona segura
            if (influenceMap != null && !influenceMap.IsDangerZone(cell))
                score += 1f;

            // Penalización por distancia desde la posición actual
            int distance = CombatSystem.HexDistance(unit.CurrentCell, cell);
            score -= distance * 0.5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        return bestCell;
    }

    /// <summary>
    /// Busca una posición defensiva cerca del enemigo.
    /// Prioriza estar en rango de ataque y tener cobertura.
    /// </summary>
    private HexCell FindDefensivePosition(Unit enemy)
    {
        // Buscar celdas cercanas a la posición actual que ofrezcan buena defensa
        List<HexCell> candidates = GetCellsInRange(unit.CurrentCell, unit.remainingMovement);
        HexCell bestCell = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in candidates)
        {
            if (cell.occupyingUnit != null && cell.occupyingUnit != unit)
                continue;

            if (!cell.IsPassableForPlayer(unit.OwnerPlayerID))
                continue;

            float score = 0f;
            int distanceToEnemy = CombatSystem.HexDistance(cell, enemy.CurrentCell);

            // Bonificación por estar en rango de ataque
            if (distanceToEnemy <= unit.attackRange)
                score += 3f;

            // Bonificación por terreno con cobertura
            if (cell.terrainType == TerrainType.Bosque)
                score += 2f;
            else if (cell.terrainType == TerrainType.Montaña)
                score += 1.5f;

            // Bonificación por zona segura
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
