using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Pathfinding : MonoBehaviour
{
    public GameObject gridManager;

    public List<HexCell> FindPath(HexCell startTile, HexCell targetTile)
    {
        if (startTile == null || targetTile == null)
        {
            Debug.LogWarning("Pathfinder: Start o Target es nulo.");
        }

        List<HexCell> openSet = new();
        HashSet<HexCell> closedSet = new();

        openSet.Add(startTile);
        startTile.gCost = 0;
        startTile.hCost = Heuristic(startTile, targetTile);
        startTile.parent = null;

        while (openSet.Count > 0)
        {
            HexCell current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost || (openSet[i].fCost == current.fCost && openSet[i].hCost < current.hCost))
                {
                    current = openSet[i];
                }
            }

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == targetTile)
            {
                return RetracePath(startTile, targetTile);
            }

            foreach (HexCell neighbor in current.neighbors)
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor)) continue;

                float newCostToNeighbor = current.gCost + GetTerrainCost(neighbor);
                if (newCostToNeighbor < neighbor.gCost || closedSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = Heuristic(neighbor, targetTile);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }

        Debug.LogWarning("Pathfinder: No se encontró camino.");
        return null;
    }
    float GetTerrainCost(HexCell tile)
    {
        switch (tile.terrainType)
        {
            case TerrainType.Llanura: return 1.0f;
            case TerrainType.Bosque: return 1.5f;
            case TerrainType.Montaña: return 2.0f;
            case TerrainType.Agua: return Mathf.Infinity;
            default: return 1.0f;
        }
    }

    float Heuristic(HexCell a, HexCell b)
    {
        int dx = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
        int dy = Mathf.Abs(a.gridPosition.y - b.gridPosition.y);
        return Mathf.Max(dx, dy);
    }

    List<HexCell> RetracePath(HexCell start, HexCell end)
    {
        List<HexCell> path = new();
        HexCell current = end;

        while (current != start)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }
}