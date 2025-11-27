using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class DijkstraPathfinding : MonoBehaviour
{
    public class PathNode
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
    private Dictionary<HexCell, PathNode> movementMap = new Dictionary<HexCell, PathNode>();
    private List<HexCell> cellsOnAttackRange = new List<HexCell>();
    private List<HexCell> cellsOnMovementRange = new List<HexCell>();
    public DijkstraPathfinding(HexGrid grid)
    {
        hexGrid = grid;
    }

    public void CreateMap(Unit unit = null) //Guarda en el diccionario de la clase una entrada por cada nodo y el coste desde el origen hasta él
    {
        ClearPathfinding();

        List<PathNode> openSet = new List<PathNode>();

        PathNode nodoInicial = new PathNode(unit.CurrentCell, null, 0);
        openSet.Add(nodoInicial);

        while (openSet.Count > 0)
        {
            PathNode nodoActual = GetLowestCostNode(openSet);

            openSet.Remove(nodoActual);
            movementMap.Add(nodoActual.cell, nodoActual);

            List<HexCell> vecinos = nodoActual.cell.neighbors;

            foreach (HexCell vecino in vecinos)
            {
                if (movementMap.ContainsKey(vecino)) continue;

                if (unit != null && (!vecino.IsPassableForPlayer(unit.OwnerPlayerID))) continue;

                float nuevoCoste = nodoActual.gCost + vecino.GetMovementCost();

                PathNode existingNode = openSet.Find(n => n.cell == vecino);

                if (existingNode == null) //Si el nodo no estaba en la OpenSet de añade
                {
                    PathNode nuevoNodo = new PathNode(vecino, nodoActual, nuevoCoste);
                    openSet.Add(nuevoNodo);
                }
                else if (nuevoCoste < existingNode.gCost) //Si si estaba pero el coste es mejor, se reestablece
                {
                    existingNode.gCost = nuevoCoste;
                    existingNode.parent = nodoActual;
                }
            }
        }

        CreateLists(unit);
    }

    private PathNode GetLowestCostNode(List<PathNode> nodos) //Obtiene el nodo con mínimo coste de una lista de nodos
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

    public List<HexCell> ConstructPath(HexCell nodoFinal)
    {
        List<HexCell> camino = new List<HexCell>();
        PathNode nodoActual = movementMap[nodoFinal];

        while (nodoActual != null)
        {
            camino.Add(nodoActual.cell);
            nodoActual = nodoActual.parent;
        }

        camino.Reverse();
        return camino;
    }

    private void CreateLists(Unit unit = null)
    {

        if (unit == null || movementMap.Count == 0) return;

        int distanciaAtaque = unit.attackRange;
        int distanciaDesplazamiento = unit.remainingMovement;
        
        foreach (KeyValuePair<HexCell, PathNode> entrada in movementMap)
        {
            HexCell hexCell = entrada.Key;
            PathNode nodo = entrada.Value;

            // Si la celda esta en rango de ataque y en la celda hay una unidad enemiga --> añadir a la lista
            if (nodo.gCost <= distanciaAtaque && hexCell.IsOccupied() && hexCell.occupyingUnit.OwnerPlayerID != unit.OwnerPlayerID) cellsOnAttackRange.Add(hexCell);

            // Si la celda esta en rango de desplazamiento --> ñadir a la lista
            if (nodo.gCost <= distanciaDesplazamiento) cellsOnMovementRange.Add(hexCell);
        }
    }

    private void ClearPathfinding()
    {
        movementMap.Clear();
        cellsOnAttackRange.Clear();
        cellsOnMovementRange.Clear();
    }

    public (List<HexCell>, List<HexCell>) GetCellsOnRange()
    {
        return (cellsOnAttackRange, cellsOnMovementRange);
    }
}


