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
            this.gCost = gCost; //Coste acumulado
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

        List<PathNode> openSet = new List<PathNode>(); //Nodos que aún quedan por explorar
        HashSet<HexCell> closedSet = new HashSet<HexCell>(); //Celdas ya visitadas

        PathNode nodoInicial = new PathNode(inicio, null, 0);
        openSet.Add(nodoInicial);

        while (openSet.Count > 0)
        {
            // Obtener nodo de menor coste
            PathNode nodoActual = GetLowestCostNode(openSet);

            //Comprobar si se ha alcanzado el nodo fin
            if (nodoActual.cell == fin)
            {
                return ConstructPath(nodoActual);
            }

            openSet.Remove(nodoActual);         //Elimina el nodo de la lista de nodos candidatos 
            closedSet.Add(nodoActual.cell);     //Añade el nodo al set de nodos que forman parte del camino definitivo

            List<HexCell> vecinos = nodoActual.cell.neighbors; //Vecinos del nodo seleccionado

            foreach (HexCell vecino in vecinos)
            {
                if (closedSet.Contains(vecino)) continue; //Si el nodo ya está en el conjunto de candidatos no se vuelve a añadir

                if (unit != null && (!vecino.IsPassableForPlayer(unit.OwnerPlayerID))) continue;

                //Calcular el coste al vecino
                float nuevoCoste = nodoActual.gCost + vecino.GetMovementCost();

                //Comprobar si este camino es mejor
                PathNode existingNode = openSet.Find(n => n.cell == vecino); //?

                if (existingNode == null) //Si no estaba entre los nodos candidatos se añade
                {
                    PathNode nuevoNodo = new PathNode(vecino, nodoActual, nuevoCoste);
                    openSet.Add(nuevoNodo);
                }
                else if (nuevoCoste < existingNode.gCost) //Si el nodo se que estaba entre los candidatos pero se ha encontrado un mejor camino se actualiza 
                {
                    existingNode.gCost = nuevoCoste;
                    existingNode.parent = nodoActual;
                }
            }
        }

        //No se ha encontrado ruta
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
