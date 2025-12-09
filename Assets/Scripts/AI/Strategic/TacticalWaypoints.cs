using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum WaypointType
{
    Attack,
    Defense,
    Rally,
    Resource,
    EnemyBase   // Prioridad 10 exclusiva - objetivo de invasión
}

public class Waypoint
{
    public HexCell cell;
    public WaypointType type;
    public int priority;
    public int ownerPlayerID;

    public Waypoint(HexCell cell, WaypointType type, int priority, int owner)
    {
        this.cell = cell;
        this.type = type;
        this.priority = priority;
        this.ownerPlayerID = owner;
    }
}

public class TacticalWaypoints : MonoBehaviour
{
    [Header("Configuration")]
    public bool visualizeWaypoints = true;

    private HexGrid hexGrid;
    private InfluenceMap influenceMap;
    private List<Waypoint> waypoints = new List<Waypoint>();

    void Start()
    {
        hexGrid = FindObjectOfType<HexGrid>();
        influenceMap = FindObjectOfType<InfluenceMap>();
    }

    public void UpdateWaypoints(int aiPlayerID, List<Unit> friendlyUnits, List<Unit> enemyUnits)
    {
        waypoints.Clear();

        GenerateAttackWaypoints(aiPlayerID);

        GenerateDefenseWaypoints(aiPlayerID);

        GenerateRallyWaypoints(aiPlayerID);

        GenerateResourceWaypoints(aiPlayerID);

        Debug.Log($"Generated {waypoints.Count} tactical waypoints for Player {aiPlayerID}");
    }

    private void GenerateAttackWaypoints(int aiPlayerID)
    {
        int enemyPlayerID = 1 - aiPlayerID;

        // Base enemiga como tipo EnemyBase (prioridad 10 exclusiva)
        HexCell enemyBase = hexGrid.GetPlayerBase(enemyPlayerID);
        if (enemyBase != null)
        {
            waypoints.Add(new Waypoint(enemyBase, WaypointType.EnemyBase, 10, aiPlayerID));
        }

        // Otros waypoints de ataque (recursos disputados)
        List<HexCell> allCells = hexGrid.GetAllCells();
        foreach (HexCell cell in allCells)
        {
            if (cell.IsOccupied() && cell.OwnerPlayerID == 0)
            {
                waypoints.Add(new Waypoint(cell, WaypointType.Attack, 6, aiPlayerID));
            }
        }
    }

    private void GenerateDefenseWaypoints(int aiPlayerID)
    {
        HexCell friendlyBase = hexGrid.GetPlayerBase(aiPlayerID);
        if (friendlyBase != null)
        {
            waypoints.Add(new Waypoint(friendlyBase, WaypointType.Defense, 9, aiPlayerID));
        }

        if (friendlyBase != null && influenceMap != null)
        {
            List<HexCell> nearBaseCells = GetCellsInRadius(friendlyBase, 3);
            foreach (HexCell cell in nearBaseCells)
            {
                if (influenceMap.GetNetInfluence(cell) > 0 && cell != friendlyBase)
                {
                    waypoints.Add(new Waypoint(cell, WaypointType.Defense, 7, aiPlayerID));
                }
            }
        }
    }

    private void GenerateRallyWaypoints(int aiPlayerID)
    {
        if (influenceMap == null)
            return;

        List<HexCell> safeZones = influenceMap.GetSafeZones();

        safeZones = safeZones.OrderByDescending(c => influenceMap.GetNetInfluence(c)).ToList();

        int count = Mathf.Min(5, safeZones.Count);
        for (int i = 0; i < count; i++)
        {
            HexCell rallyCell = safeZones[i];

            int priority = 5 + (count - i);

            waypoints.Add(new Waypoint(rallyCell, WaypointType.Rally, priority, aiPlayerID));
        }
    }

    private void GenerateResourceWaypoints(int aiPlayerID)
    {
        List<HexCell> allCells = hexGrid.GetAllCells();

        foreach (HexCell cell in allCells)
        {
            if (cell.isResourceNode && !cell.resourceCollected)
            {
                int priority = CalculateResourcePriority(cell, aiPlayerID);
                waypoints.Add(new Waypoint(cell, WaypointType.Resource, priority, aiPlayerID));
            }
        }
    }

    private int CalculateResourcePriority(HexCell resourceCell, int aiPlayerID)
    {
        int basePriority = 6;

        HexCell friendlyBase = hexGrid.GetPlayerBase(aiPlayerID);
        if (friendlyBase != null)
        {
            int distance = CombatSystem.HexDistance(resourceCell, friendlyBase);

            if (distance < 5)
                basePriority += 2;
            else if (distance > 10)
                basePriority -= 1;
        }

        if (influenceMap != null && influenceMap.IsSafeZone(resourceCell))
        {
            basePriority += 1;
        }

        return Mathf.Clamp(basePriority, 1, 10);
    }

    private bool CheckEnemyProximity(HexCell cell, int enemyPlayerID)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;

        List<Unit> enemyUnits = gm.GetAllUnitsForPlayer(enemyPlayerID);

        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.CurrentCell != null)
            {
                int distance = CombatSystem.HexDistance(cell, enemy.CurrentCell);
                if (distance <= 3)
                    return true;
            }
        }

        return false;
    }

    private List<HexCell> GetCellsInRadius(HexCell center, int radius)
    {
        List<HexCell> cells = new List<HexCell>();
        List<HexCell> allCells = hexGrid.GetAllCells();

        foreach (HexCell cell in allCells)
        {
            int distance = CombatSystem.HexDistance(center, cell);
            if (distance <= radius)
            {
                cells.Add(cell);
            }
        }

        return cells;
    }

    public List<Waypoint> GetWaypointsByType(WaypointType type)
    {
        return waypoints.FindAll(w => w.type == type);
    }

    public Waypoint GetHighestPriorityWaypoint(WaypointType type)
    {
        List<Waypoint> filtered = GetWaypointsByType(type);
        if (filtered.Count == 0)
            return null;

        return filtered.OrderByDescending(w => w.priority).First();
    }

    public Waypoint GetNearestWaypoint(HexCell from, WaypointType type)
    {
        List<Waypoint> filtered = GetWaypointsByType(type);
        if (filtered.Count == 0)
            return null;

        Waypoint nearest = null;
        int minDistance = int.MaxValue;

        foreach (Waypoint wp in filtered)
        {
            int distance = CombatSystem.HexDistance(from, wp.cell);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = wp;
            }
        }

        return nearest;
    }

    public List<Waypoint> GetAllWaypoints()
    {
        return new List<Waypoint>(waypoints);
    }

    void OnDrawGizmos()
    {
        if (!visualizeWaypoints || waypoints == null)
            return;

        foreach (Waypoint wp in waypoints)
        {
            if (wp.cell == null)
                continue;

            Color color;
            switch (wp.type)
            {
                case WaypointType.Attack:
                    color = Color.red;
                    break;
                case WaypointType.Defense:
                    color = Color.blue;
                    break;
                case WaypointType.Rally:
                    color = Color.green;
                    break;
                case WaypointType.Resource:
                    color = Color.yellow;
                    break;
                case WaypointType.EnemyBase:
                    color = Color.magenta;
                    break;
                default:
                    color = Color.white;
                    break;
            }

            Gizmos.color = color;
            Vector3 pos = wp.cell.transform.position + Vector3.up * 1.0f;
            Gizmos.DrawSphere(pos, 0.3f);

            float lineHeight = wp.priority * 0.1f;
            Gizmos.DrawLine(pos, pos + Vector3.up * lineHeight);
        }
    }
}
