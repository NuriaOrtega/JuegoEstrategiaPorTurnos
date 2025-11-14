using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class DijkstraPathfinding : MonoBehaviour
{
    private class PathNode
    {
        public HexCell cell;
        public PathNode parent;
        public float gCost;
        public PathNode(HexCell cell, PathNode parent, float gCost)
        {
            this.cell = cell;
            this.parent = parent;
            this.gCost = gCost;
        }
    }

    private HexGrid hexGrid;
    public DijkstraPathfinding(HexGrid grid)
    {
        hexGrid = grid;
    }

    public List<HexCell> FindPath(HexCell inicio, HexCell fin, Unit unit = null)
    {
        if (inicio == null || fin == null) return null;

        if (inicio == fin) return new List<HexCell> { inicio };

        List<PathNode> openSet = new List<PathNode>();
        HashSet<HexCell> closedSet = new HashSet<HexCell>();

        PathNode nodoInicial = new PathNode(inicio, null, 0);
        openSet.Add(nodoInicial);

        while (openSet.Count > 0)
        {
            PathNode nodoActual = GetLowestCostNode(openSet);

            if (nodoActual.cell == fin)
            {
                return ConstructPath(nodoActual);
            }

            openSet.Remove(nodoActual);
            closedSet.Add(nodoActual.cell);

            List<HexCell> vecinos = nodoActual.cell.neighbors;

            foreach (HexCell vecino in vecinos)
            {
                if (closedSet.Contains(vecino)) continue;

                if (unit != null && (!vecino.IsPassableForPlayer(unit.OwnerPlayerID))) continue;

                float nuevoCoste = nodoActual.gCost + vecino.GetMovementCost();

                PathNode existingNode = openSet.Find(n => n.cell == vecino);

                if (existingNode == null)
                {
                    PathNode nuevoNodo = new PathNode(vecino, nodoActual, nuevoCoste);
                    openSet.Add(nuevoNodo);
                }
                else if (nuevoCoste < existingNode.gCost)
                {
                    existingNode.gCost = nuevoCoste;
                    existingNode.parent = nodoActual;
                }
            }
        }

        return null;
    }

    private PathNode GetLowestCostNode(List<PathNode> nodos)
    {
        PathNode menor = nodos[0];
        for (int i = 1; i < nodos.Count; i++)
        {
            if (nodos[i].gCost < menor.gCost)
            {
                menor = nodos[i];
            }
        }
        return menor;
    }

    private List<HexCell> ConstructPath(PathNode nodoFinal)
    {
        List<HexCell> camino = new List<HexCell>();
        PathNode nodoActual = nodoFinal;

        while (nodoActual != null)
        {
            camino.Add(nodoActual.cell);
            nodoActual = nodoActual.parent;
        }

        camino.Reverse();
        return camino;
    }
}
