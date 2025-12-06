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

        HexCell flankCell = FindFlankingPosition(enemy);
        return flankCell != null;
    }

    private NodeState ExecuteFlankAttack()
    {
        Unit enemy = FindNearestEnemy();
        if (enemy == null)
            return NodeState.Failure;

        HexCell flankCell = FindFlankingPosition(enemy);
        if (flankCell != null && unit.remainingMovement > 0)
        {
            bool moved = MoveTowardsTarget(flankCell, avoidDanger: false);

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

            // Si está en rango, atacar
            if (distance <= unit.attackRange && !unit.hasAttacked)
            {
                return unit.AttackUnit(nearestEnemy) ? NodeState.Success : NodeState.Failure;
            }

            // Buscar posición de carga
            if (unit.remainingMovement > 0)
            {
                HexCell chargeTarget = FindChargeTarget(nearestEnemy);
                if (chargeTarget != null)
                {
                    bool moved = MoveTowardsTarget(chargeTarget, avoidDanger: false);

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

        // Si no hay enemigo directo, usar waypoint de ataque
        Waypoint attackWaypoint = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.Attack);
        if (attackWaypoint != null && unit.remainingMovement > 0)
        {
            bool moved = MoveTowardsTarget(attackWaypoint.cell, avoidDanger: false);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteCavalryPatrol()
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

            // Interceptar enemigos cercanos
            if (distance <= 4 && distance > unit.attackRange && unit.remainingMovement > 0)
            {
                HexCell interceptCell = FindInterceptPosition(nearestEnemy);
                if (interceptCell != null)
                {
                    bool moved = MoveTowardsTarget(interceptCell, avoidDanger: true);
                    return moved ? NodeState.Success : NodeState.Failure;
                }
            }
        }

        // Si no hay enemigo, patrullar hacia waypoint de defensa
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

    private NodeState ExecuteResourceRaid()
    {
        // Si ya estamos en un nodo de recursos, éxito
        if (unit.CurrentCell != null && unit.CurrentCell.isResourceNode && !unit.CurrentCell.resourceCollected)
        {
            return NodeState.Success;
        }

        // Buscar waypoint de recursos
        Waypoint resourceWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Resource);
        if (resourceWaypoint != null && unit.remainingMovement > 0)
        {
            // La caballería es rápida, puede arriesgarse más
            bool moved = MoveTowardsTarget(resourceWaypoint.cell, avoidDanger: false);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    private NodeState ExecuteFastRetreat()
    {
        // Buscar celda segura más cercana
        HexCell safeCell = influenceMap?.FindNearestSafeCell(unit.CurrentCell);

        if (safeCell != null && unit.remainingMovement > 0)
        {
            if (safeCell == unit.CurrentCell || influenceMap.IsSafeZone(unit.CurrentCell))
            {
                return NodeState.Success;
            }

            bool moved = MoveTowardsTarget(safeCell, avoidDanger: true);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        // Si no hay celda segura, buscar waypoint de rally
        Waypoint rallyWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Rally);
        if (rallyWaypoint != null && unit.remainingMovement > 0)
        {
            bool moved = MoveTowardsTarget(rallyWaypoint.cell, avoidDanger: true);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        // Como último recurso, ir a la base
        HexCell friendlyBase = hexGrid?.GetPlayerBase(unit.OwnerPlayerID);
        if (friendlyBase != null && unit.remainingMovement > 0)
        {
            bool moved = MoveTowardsTarget(friendlyBase, avoidDanger: true);
            return moved ? NodeState.Success : NodeState.Failure;
        }

        return NodeState.Failure;
    }

    /// <summary>
    /// Busca posición de flanqueo - la caballería prefiere llanuras para cargar.
    /// </summary>
    private HexCell FindFlankingPosition(Unit enemy)
    {
        List<HexCell> neighbors = enemy.CurrentCell.neighbors;
        HexCell bestFlank = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in neighbors)
        {
            if (cell.occupyingUnit != null)
                continue;

            if (!cell.IsPassableForPlayer(unit.OwnerPlayerID))
                continue;

            float score = 0f;

            // La caballería prefiere llanuras para carga
            if (cell.terrainType == TerrainType.Llanura)
                score += 3f;

            // Bonificación por zona segura
            if (influenceMap != null && !influenceMap.IsDangerZone(cell))
                score += 2f;

            // Bonificación por no tener amigos cerca (flanqueo real)
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

            // Penalización por distancia
            int distance = CombatSystem.HexDistance(unit.CurrentCell, cell);
            score -= distance * 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                bestFlank = cell;
            }
        }

        return bestFlank;
    }

    /// <summary>
    /// Busca la mejor posición para cargar contra el enemigo.
    /// </summary>
    private HexCell FindChargeTarget(Unit enemy)
    {
        List<HexCell> neighbors = enemy.CurrentCell.neighbors;
        HexCell bestTarget = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in neighbors)
        {
            if (cell.occupyingUnit != null)
                continue;

            if (!cell.IsPassableForPlayer(unit.OwnerPlayerID))
                continue;

            float score = 0f;

            // Preferir llanuras para carga
            if (cell.terrainType == TerrainType.Llanura)
                score += 2f;

            // Penalización por distancia
            int distance = CombatSystem.HexDistance(unit.CurrentCell, cell);
            score -= distance * 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = cell;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Busca posición para interceptar al enemigo antes de que llegue a la base.
    /// </summary>
    private HexCell FindInterceptPosition(Unit enemy)
    {
        HexCell friendlyBase = hexGrid?.GetPlayerBase(unit.OwnerPlayerID);

        if (friendlyBase == null)
            return enemy.CurrentCell;

        // Buscar celdas cercanas donde pueda interceptar
        List<HexCell> candidates = GetCellsInRange(unit.CurrentCell, unit.remainingMovement);
        HexCell bestIntercept = null;
        float bestScore = float.MinValue;

        foreach (HexCell cell in candidates)
        {
            if (cell.occupyingUnit != null && cell.occupyingUnit != unit)
                continue;

            if (!cell.IsPassableForPlayer(unit.OwnerPlayerID))
                continue;

            int distToEnemy = CombatSystem.HexDistance(cell, enemy.CurrentCell);
            int distToBase = CombatSystem.HexDistance(cell, friendlyBase);

            float score = 0f;

            // Bonificación por estar cerca del enemigo
            if (distToEnemy <= unit.attackRange + 1)
                score += 3f;

            // Bonificación por estar entre el enemigo y la base
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
