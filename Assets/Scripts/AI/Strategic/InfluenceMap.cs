using UnityEngine;
using System.Collections.Generic;


public class InfluenceMap : MonoBehaviour
{
    [Header("Configuration")]
    public float maxInfluenceRange = 5f;
    public bool visualizeInfluence = true;

    private HexGrid hexGrid;
    private float[,] friendlyInfluence;
    private float[,] enemyInfluence;
    private int gridWidth;
    private int gridHeight;

    void Start()
    {
        hexGrid = FindObjectOfType<HexGrid>();
        if (hexGrid != null)
        {
            gridWidth = hexGrid.gridWidth;
            gridHeight = hexGrid.gridHeight;
            friendlyInfluence = new float[gridWidth, gridHeight];
            enemyInfluence = new float[gridWidth, gridHeight];
        }
    }

    public void UpdateInfluence(List<Unit> allUnits, int aiPlayerID)
    {
        ClearInfluence();

        foreach (Unit unit in allUnits)
        {
            if (unit == null || unit.CurrentCell == null)
                continue;

            float unitStrength = CalculateUnitStrength(unit);
            bool isFriendly = (unit.OwnerPlayerID == aiPlayerID);

            PropagateInfluence(unit.CurrentCell, unitStrength, isFriendly);
        }
    }

    private float CalculateUnitStrength(Unit unit)
    {
        float baseStrength = unit.attackPower;

        float healthFactor = unit.GetHealthPercentage();

        float typeModifier = 1.0f;
        switch (unit.unitType)
        {
            case UnitType.Artillery:
                typeModifier = 1.5f;
                break;
            case UnitType.Cavalry:
                typeModifier = 1.2f;
                break;
            case UnitType.Infantry:
                typeModifier = 1.0f;
                break;
        }

        return baseStrength * healthFactor * typeModifier;
    }

    private void PropagateInfluence(HexCell sourceCell, float strength, bool isFriendly)
    {
        Dictionary<HexCell, float> costMap = new Dictionary<HexCell, float>();
        Queue<HexCell> openSet = new Queue<HexCell>();

        costMap[sourceCell] = 0f;
        openSet.Enqueue(sourceCell);

        while (openSet.Count > 0)
        {
            HexCell current = openSet.Dequeue();
            float currentCost = costMap[current];

            if (currentCost > maxInfluenceRange)
                continue;

            float distanceFactor = 1f - (currentCost / maxInfluenceRange);
            distanceFactor = Mathf.Max(0f, distanceFactor);

            float influence = strength * distanceFactor;

            Vector2Int pos = current.gridPosition;
            if (isFriendly)
                friendlyInfluence[pos.x, pos.y] += influence;
            else
                enemyInfluence[pos.x, pos.y] += influence;

            foreach (HexCell neighbor in current.neighbors)
            {
                float neighborCost = currentCost + neighbor.GetMovementCost();

                if (neighborCost <= maxInfluenceRange)
                {
                    if (!costMap.ContainsKey(neighbor) || neighborCost < costMap[neighbor])
                    {
                        costMap[neighbor] = neighborCost;
                        openSet.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    private void ClearInfluence()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                friendlyInfluence[x, y] = 0f;
                enemyInfluence[x, y] = 0f;
            }
        }
    }

    public float GetFriendlyInfluence(HexCell cell)
    {
        if (cell == null) return 0f;
        Vector2Int pos = cell.gridPosition;
        if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
            return 0f;
        return friendlyInfluence[pos.x, pos.y];
    }

    public float GetEnemyInfluence(HexCell cell)
    {
        if (cell == null) return 0f;
        Vector2Int pos = cell.gridPosition;
        if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
            return 0f;
        return enemyInfluence[pos.x, pos.y];
    }

    public float GetNetInfluence(HexCell cell)
    {
        return GetFriendlyInfluence(cell) - GetEnemyInfluence(cell);
    }

    public bool IsSafeZone(HexCell cell)
    {
        return GetNetInfluence(cell) > 0f;
    }

    public bool IsDangerZone(HexCell cell)
    {
        return GetNetInfluence(cell) < -5f;
    }

    public List<HexCell> GetSafeZones()
    {
        List<HexCell> safeZones = new List<HexCell>();
        List<HexCell> allCells = hexGrid.GetAllCells();

        foreach (HexCell cell in allCells)
        {
            if (IsSafeZone(cell))
            {
                safeZones.Add(cell);
            }
        }

        return safeZones;
    }
    public List<HexCell> GetDangerZones()
    {
        List<HexCell> dangerZones = new List<HexCell>();
        List<HexCell> allCells = hexGrid.GetAllCells();

        foreach (HexCell cell in allCells)
        {
            if (IsDangerZone(cell))
            {
                dangerZones.Add(cell);
            }
        }

        return dangerZones;
    }

    public HexCell FindNearestSafeCell(HexCell from)
    {
        List<HexCell> safeZones = GetSafeZones();
        if (safeZones.Count == 0)
            return null;

        HexCell nearest = null;
        int minDistance = int.MaxValue;

        foreach (HexCell safe in safeZones)
        {
            int distance = CombatSystem.HexDistance(from, safe);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = safe;
            }
        }

        return nearest;
    }

    void OnDrawGizmos()
    {
        if (!visualizeInfluence || hexGrid == null || friendlyInfluence == null)
            return;

        List<HexCell> allCells = hexGrid.GetAllCells();

        foreach (HexCell cell in allCells)
        {
            Vector2Int pos = cell.gridPosition;
            float friendly = friendlyInfluence[pos.x, pos.y];
            float enemy = enemyInfluence[pos.x, pos.y];
            float net = friendly - enemy;

            Color color;
            if (net > 0)
            {
                float intensity = Mathf.Clamp01(net / 20f);
                color = new Color(0f, intensity, 0f, 0.5f);
            }
            else if (net < 0)
            {
                float intensity = Mathf.Clamp01(-net / 20f);
                color = new Color(intensity, 0f, 0f, 0.5f);
            }
            else
            {
                color = new Color(1f, 1f, 0f, 0.3f);
            }

            Gizmos.color = color;
            Vector3 cellPos = cell.transform.position + Vector3.up * 0.1f;
            Gizmos.DrawCube(cellPos, new Vector3(0.8f, 0.05f, 0.8f));
        }
    }
}
