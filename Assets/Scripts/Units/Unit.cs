using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Type")]
    public UnitType unitType;

    [Header("Stats")]
    public int maxHealth;
    public int currentHealth;
    public int attackPower;
    public int attackRange;
    public int movementPoints;

    [Header("Current State")]
    public int remainingMovement;
    public bool hasAttacked;
    public bool hasMovedThisTurn;

    [Header("Ownership & Position")]
    public int OwnerPlayerID;
    public HexCell CurrentCell;

    [Header("AI Order")]
    public OrderType currentOrder = OrderType.Idle;

    public void Initialize(UnitType type, int playerID, HexCell startCell)
    {
        unitType = type;
        OwnerPlayerID = playerID;
        CurrentCell = startCell;

        HardcodedUnitStats stats = GetStatsForType(type);
        maxHealth = stats.maxHealth;
        currentHealth = maxHealth;
        attackPower = stats.attackPower;
        attackRange = stats.attackRange;
        movementPoints = stats.movementPoints;

        ResetForNewTurn();

        if (CurrentCell != null)
        {
            CurrentCell.occupyingUnit = this;
        }
    }

    private HardcodedUnitStats GetStatsForType(UnitType type)
    {
        switch (type)
        {
            case UnitType.Infantry:
                return HardcodedUnitStats.Infantry;
            case UnitType.Cavalry:
                return HardcodedUnitStats.Cavalry;
            case UnitType.Artillery:
                return HardcodedUnitStats.Artillery;
            default:
                return HardcodedUnitStats.Infantry;
        }
    }

    public bool MoveToCell(HexCell targetCell, DijkstraPathfinding pathfinding)
    {
        if (targetCell == null || targetCell == CurrentCell)
            return false;

        if (remainingMovement <= 0)
        {
            Debug.Log($"Unit {gameObject.name} has no movement remaining.");
            return false;
        }

        var path = pathfinding.ConstructPath(targetCell);

        if (path == null || path.Count == 0)
        {
            Debug.Log($"No valid path found from {CurrentCell.gridPosition} to {targetCell.gridPosition}");
            return false;
        }

        var resultado = pathfinding.GetCellsOnRange();
        List<HexCell> rangoMovimiento = resultado.Item2;

        if(!rangoMovimiento.Contains(targetCell) && OwnerPlayerID == 1) {
            targetCell = pathfinding.ClosestCellToDestiny(path);
            path = pathfinding.ConstructPath(targetCell);
        }

        float totalCost = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            totalCost += path[i].GetMovementCost();
        }

        if(!rangoMovimiento.Contains(targetCell) && OwnerPlayerID == 0 )
        {
            Debug.Log($"Not enough movement. Need {totalCost}, have {remainingMovement}");
            return false;
        } 
        
        if (totalCost > remainingMovement)
        {
            Debug.Log($"Not enough movement. Need {totalCost}, have {remainingMovement}");
            return false;
        }

        if (CurrentCell != null)
        {
            CurrentCell.occupyingUnit = null;
        }

        CurrentCell = targetCell;
        targetCell.occupyingUnit = this;
        remainingMovement -= Mathf.CeilToInt(totalCost);
        hasMovedThisTurn = true;

        transform.position = targetCell.transform.position + Vector3.up * 0.1f;

        Debug.Log($"Unit {gameObject.name} moved to {targetCell.gridPosition}");
        return true;
    }

    /// <summary>
    /// Mueve la unidad a lo largo de un camino pre-calculado (por TacticalPathfinding).
    /// El path debe incluir la celda actual como primer elemento.
    /// </summary>
    public bool MoveAlongPath(List<HexCell> path)
    {
        if (path == null || path.Count < 2)
            return false;

        if (remainingMovement <= 0)
        {
            Debug.Log($"Unit {gameObject.name} has no movement remaining.");
            return false;
        }

        // path[0] es la celda actual, path[last] es el destino
        HexCell targetCell = path[path.Count - 1];

        // Verificar que el destino es válido
        if (targetCell.IsOccupied() && targetCell.occupyingUnit != this)
        {
            Debug.Log($"Target cell {targetCell.gridPosition} is occupied.");
            return false;
        }

        // Calcular coste total del camino
        float totalCost = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            totalCost += path[i].GetMovementCost();
        }

        if (totalCost > remainingMovement)
        {
            Debug.Log($"Not enough movement for path. Need {totalCost}, have {remainingMovement}");
            return false;
        }

        // Ejecutar movimiento
        if (CurrentCell != null)
        {
            CurrentCell.occupyingUnit = null;
        }

        CurrentCell = targetCell;
        targetCell.occupyingUnit = this;
        remainingMovement -= Mathf.CeilToInt(totalCost);
        hasMovedThisTurn = true;

        transform.position = targetCell.transform.position + Vector3.up * 0.1f;

        Debug.Log($"Unit {gameObject.name} moved along path to {targetCell.gridPosition}");
        return true;
    }

    public bool AttackUnit(Unit target)
    {
        if (target == null)
            return false;

        if (hasAttacked)
        {
            Debug.Log($"Unit {gameObject.name} has already attacked this turn.");
            return false;
        }

        if (target.OwnerPlayerID == OwnerPlayerID)
        {
            Debug.Log("Cannot attack friendly units.");
            return false;
        }

        int distance = HexDistance(CurrentCell, target.CurrentCell);
        if (distance > attackRange)
        {
            Debug.Log($"Target out of range. Distance: {distance}, Range: {attackRange}");
            return false;
        }

        target.currentHealth -= attackPower;
        hasAttacked = true;

        Debug.Log($"{gameObject.name} attacked {target.gameObject.name} for {attackPower} damage. Target health: {target.currentHealth}");

        if (target.currentHealth <= 0)
        {
            target.Die();
        }

        return true;
    }

    public int HexDistance(HexCell a, HexCell b)
    {
        int q1 = a.gridPosition.x;
        int r1 = a.gridPosition.y;
        int q2 = b.gridPosition.x;
        int r2 = b.gridPosition.y;

        int x1 = q1 - (r1 - (r1 & 1)) / 2;
        int z1 = r1;
        int y1 = -x1 - z1;

        int x2 = q2 - (r2 - (r2 & 1)) / 2;
        int z2 = r2;
        int y2 = -x2 - z2;

        return (Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) + Mathf.Abs(z1 - z2)) / 2;
    }

    public void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");

        if (CurrentCell != null)
        {
            CurrentCell.occupyingUnit = null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveUnit(this);
        }

        Destroy(gameObject);
    }

    public void ResetForNewTurn()
    {
        remainingMovement = movementPoints;
        hasAttacked = false;
        hasMovedThisTurn = false;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    public virtual float GetTerrainMovementModifier(TerrainType terrain)
    {
        return 1.0f;
    }
}
