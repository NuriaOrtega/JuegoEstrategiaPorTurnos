using UnityEngine;
using System.Collections.Generic;

public class TacticalPathfinding
{
    public class TacticalNode
    {
        public HexCell cell;
        public TacticalNode parent;
        public float gCost;      // Coste acumulado de movimiento desde el inicio
        public float hCost;      // Heurística: distancia hex al destino
        public float tacticalCost; // Coste táctico (influencia, cobertura)

        public TacticalNode(HexCell cell, TacticalNode parent, float gCost, float hCost, float tacticalCost)
        {
            this.cell = cell;
            this.parent = parent;
            this.gCost = gCost;
            this.hCost = hCost;
            this.tacticalCost = tacticalCost;
        }

        // f(n) = g(n) + h(n) + tactical(n)
        public float TotalCost => gCost + hCost + tacticalCost;
    }

    private HexGrid hexGrid;
    private InfluenceMap influenceMap;
    private Dictionary<HexCell, TacticalNode> nodeMap;

    public float dangerWeight = 2.0f;
    public float coverWeight = 1.5f;
    public float distanceWeight = 1.0f;

    public TacticalPathfinding(HexGrid grid, InfluenceMap influence)
    {
        hexGrid = grid;
        influenceMap = influence;
        nodeMap = new Dictionary<HexCell, TacticalNode>();
    }

    public List<HexCell> FindTacticalPath(HexCell start, HexCell goal, Unit unit, bool avoidDanger = true)
    {
        if (start == null || goal == null)
            return null;

        nodeMap.Clear();
        List<TacticalNode> openSet = new List<TacticalNode>();

        // Heurística inicial: distancia hex desde start hasta goal
        float initialHCost = CombatSystem.HexDistance(start, goal);
        TacticalNode startNode = new TacticalNode(start, null, 0, initialHCost, 0);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            TacticalNode current = GetLowestCostNode(openSet);

            if (current.cell == goal)
            {
                return ReconstructPath(current);
            }

            openSet.Remove(current);
            nodeMap[current.cell] = current;

            foreach (HexCell neighbor in current.cell.neighbors)
            {
                if (nodeMap.ContainsKey(neighbor))
                    continue;

                if (!neighbor.IsPassableForPlayer(unit.OwnerPlayerID))
                    continue;

                // g(n): Coste de movimiento acumulado
                float movementCost = neighbor.GetMovementCost();
                float newGCost = current.gCost + (movementCost * distanceWeight);

                // h(n): Heurística - distancia hex al destino
                float newHCost = CombatSystem.HexDistance(neighbor, goal);

                // tactical(n): Coste táctico basado en influencia y terreno
                float tacticalModifier = CalculateTacticalCost(neighbor, unit, avoidDanger);
                float newTacticalCost = current.tacticalCost + tacticalModifier;

                TacticalNode existingNode = openSet.Find(n => n.cell == neighbor);

                if (existingNode == null)
                {
                    TacticalNode newNode = new TacticalNode(neighbor, current, newGCost, newHCost, newTacticalCost);
                    openSet.Add(newNode);
                }
                else if (newGCost + newHCost + newTacticalCost < existingNode.TotalCost)
                {
                    existingNode.gCost = newGCost;
                    existingNode.hCost = newHCost;
                    existingNode.tacticalCost = newTacticalCost;
                    existingNode.parent = current;
                }
            }
        }

        return null;
    }

    private float CalculateTacticalCost(HexCell cell, Unit unit, bool avoidDanger)
    {
        float tacticalCost = 0f;

        if (influenceMap != null && avoidDanger)
        {
            float enemyInfluence = influenceMap.GetEnemyInfluence(cell);
            float friendlyInfluence = influenceMap.GetFriendlyInfluence(cell);
            float netInfluence = friendlyInfluence - enemyInfluence;

            if (netInfluence < 0)
            {
                tacticalCost += Mathf.Abs(netInfluence) * dangerWeight;
            }
            else
            {
                tacticalCost -= netInfluence * 0.1f;
            }
        }

        switch (cell.terrainType)
        {
            case TerrainType.Bosque:
                tacticalCost -= coverWeight;
                break;
            case TerrainType.Montaña:
                if (unit.unitType == UnitType.Artillery)
                    tacticalCost -= coverWeight * 1.5f;
                else
                    tacticalCost -= coverWeight * 0.5f;
                break;
        }

        return Mathf.Max(0, tacticalCost);
    }

    private TacticalNode GetLowestCostNode(List<TacticalNode> nodes)
    {
        TacticalNode lowest = nodes[0];
        for (int i = 1; i < nodes.Count; i++)
        {
            if (nodes[i].TotalCost < lowest.TotalCost)
            {
                lowest = nodes[i];
            }
        }
        return lowest;
    }

    private List<HexCell> ReconstructPath(TacticalNode endNode)
    {
        List<HexCell> path = new List<HexCell>();
        TacticalNode current = endNode;

        while (current != null)
        {
            path.Add(current.cell);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    public HexCell FindSafestCell(HexCell from, int maxDistance, Unit unit)
    {
        if (influenceMap == null)
            return from;

        List<HexCell> candidates = new List<HexCell>();
        Queue<(HexCell cell, int dist)> queue = new Queue<(HexCell, int)>();
        HashSet<HexCell> visited = new HashSet<HexCell>();

        queue.Enqueue((from, 0));
        visited.Add(from);

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();

            if (dist <= maxDistance)
            {
                candidates.Add(current);
            }

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

        HexCell safest = from;
        float bestSafety = float.MinValue;

        foreach (HexCell cell in candidates)
        {
            float safety = influenceMap.GetNetInfluence(cell);

            if (cell.terrainType == TerrainType.Bosque)
                safety += 2f;
            else if (cell.terrainType == TerrainType.Montaña)
                safety += 1f;

            if (safety > bestSafety)
            {
                bestSafety = safety;
                safest = cell;
            }
        }

        return safest;
    }

    public HexCell FindBestAttackPosition(HexCell from, Unit target, Unit attacker, int maxDistance)
    {
        List<HexCell> attackPositions = new List<HexCell>();

        foreach (HexCell neighbor in target.CurrentCell.neighbors)
        {
            if (neighbor.occupyingUnit == null && neighbor.IsPassableForPlayer(attacker.OwnerPlayerID))
            {
                int dist = CombatSystem.HexDistance(from, neighbor);
                if (dist <= maxDistance)
                {
                    attackPositions.Add(neighbor);
                }
            }
        }

        if (attackPositions.Count == 0)
            return null;

        HexCell best = attackPositions[0];
        float bestScore = float.MinValue;

        foreach (HexCell cell in attackPositions)
        {
            float score = 0f;

            if (influenceMap != null)
            {
                score += influenceMap.GetNetInfluence(cell) * 0.5f;
            }

            if (cell.terrainType == TerrainType.Bosque)
                score += 2f;
            else if (cell.terrainType == TerrainType.Llanura && attacker.unitType == UnitType.Cavalry)
                score += 1.5f;

            int distFromStart = CombatSystem.HexDistance(from, cell);
            score -= distFromStart * 0.2f;

            if (score > bestScore)
            {
                bestScore = score;
                best = cell;
            }
        }

        return best;
    }

    /// <summary>
    /// Dado un camino y los puntos de movimiento disponibles, retorna la celda más lejana
    /// a la que puede llegar la unidad en este turno.
    /// Si puede llegar al destino, retorna el destino. Si no, retorna la celda más cercana al destino.
    /// </summary>
    public HexCell GetClosestReachableCell(List<HexCell> path, int movementPoints)
    {
        if (path == null || path.Count == 0)
            return null;

        if (path.Count == 1)
            return path[0]; // Ya está en el destino

        float accumulatedCost = 0f;
        HexCell lastReachable = path[0]; // Empezamos en la celda actual

        for (int i = 1; i < path.Count; i++)
        {
            accumulatedCost += path[i].GetMovementCost();

            if (accumulatedCost <= movementPoints)
            {
                // Verificar que la celda no está ocupada (excepto si es el destino final)
                if (!path[i].IsOccupied() || i == path.Count - 1)
                {
                    lastReachable = path[i];
                }
            }
            else
            {
                break; // Ya no podemos avanzar más
            }
        }

        return lastReachable;
    }

    /// <summary>
    /// Retorna el sub-camino desde el inicio hasta la celda alcanzable más lejana.
    /// Útil para pasar a Unit.MoveAlongPath()
    /// </summary>
    public List<HexCell> GetReachablePath(List<HexCell> fullPath, int movementPoints)
    {
        if (fullPath == null || fullPath.Count == 0)
            return null;

        List<HexCell> reachablePath = new List<HexCell>();
        reachablePath.Add(fullPath[0]);

        float accumulatedCost = 0f;

        for (int i = 1; i < fullPath.Count; i++)
        {
            accumulatedCost += fullPath[i].GetMovementCost();

            if (accumulatedCost <= movementPoints)
            {
                if (!fullPath[i].IsOccupied() || i == fullPath.Count - 1)
                {
                    reachablePath.Add(fullPath[i]);
                }
            }
            else
            {
                break;
            }
        }

        return reachablePath;
    }
}
