using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StrategicManager : MonoBehaviour
{
    [Header("Configuration")]
    public int aiPlayerID = 1;

    [Header("Strategic Weights")]
    [Range(0f, 1f)]
    public float aggressionLevel = 0.6f;
    [Range(0f, 1f)]
    public float economicFocus = 0.3f;

    [Header("References")]
    private GameManager gameManager;
    private HexGrid hexGrid;
    private InfluenceMap influenceMap;
    private TacticalWaypoints tacticalWaypoints;

    private List<Unit> friendlyUnits;
    private List<Unit> enemyUnits;
    private DijkstraPathfinding aiPathfinding;

    void Start()
    {
        gameManager = GameManager.Instance;
        hexGrid = FindObjectOfType<HexGrid>();
        influenceMap = FindObjectOfType<InfluenceMap>();
        tacticalWaypoints = FindObjectOfType<TacticalWaypoints>();
        aiPathfinding = new DijkstraPathfinding(hexGrid);

        if (gameManager == null || hexGrid == null)
        {
            Debug.LogError("StrategicManager: Missing critical references!");
        }
    }

    public void ExecuteAITurn()
    {
        Debug.Log("======== STRATEGIC MANAGER: AI TURN START ========");

        AnalyzeGameState();

        UpdateStrategicSystems();

        AssignOrdersToUnits();

        ExecuteUnitActions();

        MakeProductionDecisions();

        Debug.Log("======== STRATEGIC MANAGER: AI TURN END ========");

        gameManager.EndTurn();
    }

    private void AnalyzeGameState()
    {
        friendlyUnits = gameManager.GetAllUnitsForPlayer(aiPlayerID);
        enemyUnits = gameManager.GetAllUnitsForPlayer(1 - aiPlayerID);

        int friendlyCount = friendlyUnits.Count;
        int enemyCount = enemyUnits.Count;
        int resources = gameManager.resourcesPerPlayer[aiPlayerID]; //Se puede omitir

        Debug.Log($"[Analysis] Friendly Units: {friendlyCount}, Enemy Units: {enemyCount}, Resources: {resources}");

        float numericalAdvantage = (float)friendlyCount / Mathf.Max(1, enemyCount);
        Debug.Log($"[Analysis] Numerical Advantage: {numericalAdvantage:F2}");

        if (numericalAdvantage > 1.5f)
        {
            aggressionLevel = 0.8f;
            economicFocus = 0.2f;
        }
        else if (numericalAdvantage < 0.7f)
        {
            aggressionLevel = 0.3f;
            economicFocus = 0.4f;
        }
        else
        {
            aggressionLevel = 0.6f;
            economicFocus = 0.3f;
        }

        Debug.Log($"[Strategy] Aggression: {aggressionLevel:F2}, Economic Focus: {economicFocus:F2}");
    }

    private void UpdateStrategicSystems()
    {
        List<Unit> allUnits = new List<Unit>();
        allUnits.AddRange(friendlyUnits);
        allUnits.AddRange(enemyUnits);

        if (influenceMap != null)
        {
            influenceMap.UpdateInfluence(allUnits, aiPlayerID);
            Debug.Log("[Strategic Systems] Influence map updated");
        }

        if (tacticalWaypoints != null)
        {
            tacticalWaypoints.UpdateWaypoints(aiPlayerID, friendlyUnits, enemyUnits);
            Debug.Log("[Strategic Systems] Tactical waypoints updated");
        }
    }

    private void MakeProductionDecisions()
    {
        int resources = gameManager.resourcesPerPlayer[aiPlayerID];
        HexCell baseCell = hexGrid.GetPlayerBase(aiPlayerID);

        if (baseCell == null || baseCell.occupyingUnit != null)
        {
            Debug.Log("[Production] Base unavailable for production");
            return;
        }

        UnitType unitToProduce = DecideUnitType();
        HardcodedUnitStats stats = GetStatsForType(unitToProduce);

        if (resources >= stats.cost)
        {
            Unit newUnit = gameManager.SpawnUnit(unitToProduce, baseCell, aiPlayerID);
            if (newUnit != null)
            {
                Debug.Log($"[Production] Produced {unitToProduce} for {stats.cost} resources");
            }
        }
        else
        {
            Debug.Log($"[Production] Not enough resources. Need {stats.cost}, have {resources}");
        }
    }

    private UnitType DecideUnitType()
    {
        int infantryCount = friendlyUnits.Count(u => u.unitType == UnitType.Infantry);
        int cavalryCount = friendlyUnits.Count(u => u.unitType == UnitType.Cavalry);
        int artilleryCount = friendlyUnits.Count(u => u.unitType == UnitType.Artillery);

        Debug.Log($"[Production Analysis] Infantry: {infantryCount}, Cavalry: {cavalryCount}, Artillery: {artilleryCount}");

        bool enemyThreatNearBase = CheckEnemyThreatNearBase();
        bool needMobility = cavalryCount < friendlyUnits.Count * 0.3f;
        bool needFirepower = artilleryCount < friendlyUnits.Count * 0.2f;

        if (enemyThreatNearBase && artilleryCount < 2)
        {
            Debug.Log("[Production Decision] Enemy threat detected - producing Artillery");
            return UnitType.Artillery;
        }

        if (aggressionLevel > 0.7f && needMobility)
        {
            Debug.Log("[Production Decision] Aggressive strategy - producing Cavalry");
            return UnitType.Cavalry;
        }

        if (needFirepower && friendlyUnits.Count > 2)
        {
            Debug.Log("[Production Decision] Need firepower - producing Artillery");
            return UnitType.Artillery;
        }

        Debug.Log("[Production Decision] Default - producing Infantry");
        return UnitType.Infantry;
    }

    private bool CheckEnemyThreatNearBase()
    {
        HexCell baseCell = hexGrid.GetPlayerBase(aiPlayerID);
        if (baseCell == null) return false;

        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.CurrentCell != null)
            {
                int distance = CombatSystem.HexDistance(baseCell, enemy.CurrentCell);
                if (distance <= 5)
                    return true;
            }
        }

        return false;
    }

    private void AssignOrdersToUnits()
    {
        if (friendlyUnits.Count == 0)
        {
            Debug.Log("[Order Assignment] No units to assign orders");
            return;
        }

        int totalUnits = friendlyUnits.Count;
        int attackers = Mathf.CeilToInt(totalUnits * aggressionLevel);
        int gatherers = Mathf.CeilToInt(totalUnits * economicFocus);
        int defenders = totalUnits - attackers - gatherers;

        if (defenders < 1 && totalUnits > 0)
        {
            defenders = 1;
            attackers = Mathf.Max(0, attackers - 1);
        }

        Debug.Log($"[Order Assignment] Attackers: {attackers}, Defenders: {defenders}, Gatherers: {gatherers}");

        List<Unit> sortedUnits = SortUnitsByStrategicValue();

        int assigned = 0;

        for (int i = 0; i < attackers && assigned < sortedUnits.Count; i++, assigned++)
        {
            sortedUnits[assigned].currentOrder = OrderType.Attack;
            Debug.Log($"[Order] {sortedUnits[assigned].gameObject.name} assigned to ATTACK");
        }

        for (int i = 0; i < defenders && assigned < sortedUnits.Count; i++, assigned++)
        {
            sortedUnits[assigned].currentOrder = OrderType.Defend;
            Debug.Log($"[Order] {sortedUnits[assigned].gameObject.name} assigned to DEFEND");
        }

        for (int i = 0; i < gatherers && assigned < sortedUnits.Count; i++, assigned++)
        {
            sortedUnits[assigned].currentOrder = OrderType.GatherResources;
            Debug.Log($"[Order] {sortedUnits[assigned].gameObject.name} assigned to GATHER");
        }

        while (assigned < sortedUnits.Count)
        {
            sortedUnits[assigned].currentOrder = OrderType.Idle;
            assigned++;
        }
    }

    private List<Unit> SortUnitsByStrategicValue()
    {
        return friendlyUnits.OrderByDescending(u => u.GetHealthPercentage())
                           .ThenByDescending(u => u.attackPower)
                           .ToList();
    }

    private void ExecuteUnitActions()
    {
        foreach (Unit unit in friendlyUnits)
        {
            aiPathfinding.CreateMap(unit);
            ExecuteUnitOrder(unit);
        }
    }

    private void ExecuteUnitOrder(Unit unit)
    {
        if (unit == null || unit.CurrentCell == null)
            return;

        Debug.Log($"[Unit Action] {unit.gameObject.name} executing order: {unit.currentOrder}");

        switch (unit.currentOrder)
        {
            case OrderType.Attack:
                ExecuteAttackOrder(unit);
                break;

            case OrderType.Defend:
                ExecuteDefendOrder(unit);
                break;

            case OrderType.GatherResources:
                ExecuteGatherOrder(unit);
                break;

            case OrderType.Retreat:
                ExecuteRetreatOrder(unit);
                break;

            case OrderType.Idle:
                break;
        }
    }
    private void ExecuteAttackOrder(Unit unit)
    {
        Unit nearestEnemy = FindNearestEnemy(unit);
        if (nearestEnemy != null && CombatSystem.CanAttack(unit, nearestEnemy))
        {
            unit.AttackUnit(nearestEnemy);
            return;
        }

        Waypoint attackWaypoint = tacticalWaypoints?.GetHighestPriorityWaypoint(WaypointType.Attack);
        if (attackWaypoint != null && unit.remainingMovement > 0)
        {
            unit.MoveToCell(attackWaypoint.cell, aiPathfinding);
        }
    }

    private void ExecuteDefendOrder(Unit unit)
    {
        Unit nearestEnemy = FindNearestEnemy(unit);
        if (nearestEnemy != null && CombatSystem.CanAttack(unit, nearestEnemy))
        {
            unit.AttackUnit(nearestEnemy);
            return;
        }

        Waypoint defenseWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Defense);
        if (defenseWaypoint != null && unit.remainingMovement > 0)
        {
            int distance = CombatSystem.HexDistance(unit.CurrentCell, defenseWaypoint.cell);
            if (distance > 2)
            {
                unit.MoveToCell(defenseWaypoint.cell, aiPathfinding);
            }
        }
    }

    private void ExecuteGatherOrder(Unit unit)
    {
        Waypoint resourceWaypoint = tacticalWaypoints?.GetNearestWaypoint(unit.CurrentCell, WaypointType.Resource);
        if (resourceWaypoint != null && unit.remainingMovement > 0)
        {
            unit.MoveToCell(resourceWaypoint.cell, aiPathfinding);
        }
    }

    private void ExecuteRetreatOrder(Unit unit)
    {
        HexCell safeCell = influenceMap?.FindNearestSafeCell(unit.CurrentCell);
        if (safeCell != null && unit.remainingMovement > 0)
        {
            unit.MoveToCell(safeCell, aiPathfinding);
        }
    }

    private Unit FindNearestEnemy(Unit from)
    {
        Unit nearest = null;
        int minDistance = int.MaxValue;

        foreach (Unit enemy in enemyUnits)
        {
            if (enemy == null || enemy.CurrentCell == null)
                continue;

            int distance = CombatSystem.HexDistance(from.CurrentCell, enemy.CurrentCell);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
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
}
