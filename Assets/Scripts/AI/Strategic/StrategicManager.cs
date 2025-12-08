using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SocialPlatforms.Impl;

public class StrategicManager : MonoBehaviour
{
    [Header("Configuration")]
    public int aiPlayerID = 1;

    [Header("Strategic Weights (Read-Only - Controlled by FSM)")]
    [SerializeField]
    private float aggressionLevel = 0.6f;
    [SerializeField]
    private float economicFocus = 0.3f;

    [Header("Current State (Read-Only)")]
    [SerializeField]
    private StrategicStateType currentStrategicState;

    [Header("References")]
    private GameManager gameManager;
    private HexGrid hexGrid;
    private InfluenceMap influenceMap;
    private TacticalWaypoints tacticalWaypoints;

    private List<Unit> friendlyUnits;
    private List<Unit> enemyUnits;

    private StrategicFSM strategicFSM;
    private StrategicContext strategicContext;

    void Start()
    {
        gameManager = GameManager.Instance;
        hexGrid = FindFirstObjectByType<HexGrid>();
        influenceMap = FindObjectOfType<InfluenceMap>();
        tacticalWaypoints = FindObjectOfType<TacticalWaypoints>();

        strategicContext = new StrategicContext();
        strategicFSM = new StrategicFSM(strategicContext);

        // Forzar OnEnter del estado inicial para establecer pesos
        strategicFSM.ForceInitialState();

        if (gameManager == null || hexGrid == null)
        {
            Debug.LogError("StrategicManager: Missing critical references!");
        }
    }

    public void ExecuteAITurn()
    {
        Debug.Log("======== STRATEGIC MANAGER: AI TURN START ========");

        // 1. Analizar estado del juego y actualizar sistemas estratégicos
        //    (incluye mapa de influencia y waypoints)
        AnalyzeGameState();

        // 2. Actualizar contexto y estado de la FSM
        UpdateStrategicContext();
        strategicFSM.Update();

        // 3. Asignar órdenes a las unidades
        AssignOrdersToUnits();

        // 4. Ejecutar los behavior trees de cada unidad
        ExecuteUnits();

        // 5. Decisiones de producción
        MakeProductionDecisions();

        Debug.Log("======== STRATEGIC MANAGER: AI TURN END ========");

        gameManager.EndTurn();
    }

    private void UpdateStrategicContext()
    {
        strategicContext.NumericalAdvantage = (float)friendlyUnits.Count / Mathf.Max(1, enemyUnits.Count);

        int enemyResources = gameManager.resourcesPerPlayer[1 - aiPlayerID];
        // strategicContext.ResourceAdvantage = (float)strategicContext.Resources / Mathf.Max(1, enemyResources);

        strategicContext.IsBaseThreatened = CheckEnemyThreatNearBase();

        strategicContext.TerritorialControl = CalculateTerritorialControl();

        // Debug.Log($"[Context] Units: {strategicContext.FriendlyUnitCount} vs {strategicContext.EnemyUnitCount}, " +
        //           $"NumAdv: {strategicContext.NumericalAdvantage:F2}, ResAdv: {strategicContext.ResourceAdvantage:F2}, " +
        //           $"Territory: {strategicContext.TerritorialControl:F2}, BaseThreat: {strategicContext.IsBaseThreatened}");
    }

    private float CalculateTerritorialControl()
    {
        if (influenceMap == null) return 0.5f;

        List<HexCell> allCells = hexGrid.GetAllCells();
        int friendlyControlled = 0;
        int totalCells = 0;

        foreach (HexCell cell in allCells)
        {
            if (cell.terrainType == TerrainType.Agua) continue;
            totalCells++;

            if (influenceMap.GetNetInfluence(cell) > 0)
                friendlyControlled++;
        }

        return totalCells > 0 ? (float)friendlyControlled / totalCells : 0.5f;
    }

    /// <summary>
    /// Analiza el estado del juego y actualiza los sistemas estratégicos (mapa de influencia y waypoints).
    /// Esta función centraliza toda la recopilación de información necesaria para la toma de decisiones.
    /// </summary>
    private void AnalyzeGameState()
    {
        // 1. Obtener todas las unidades
        friendlyUnits = gameManager.GetAllUnitsForPlayer(aiPlayerID);
        enemyUnits = gameManager.GetAllUnitsForPlayer(1 - aiPlayerID);

        Debug.Log($"[AnalyzeGameState] Friendly Units: {friendlyUnits.Count}, Enemy Units: {enemyUnits.Count}");

        // 2. Actualizar mapa de influencia
        if (influenceMap != null)
        {
            List<Unit> allUnits = new List<Unit>();
            allUnits.AddRange(friendlyUnits);
            allUnits.AddRange(enemyUnits);

            influenceMap.UpdateInfluence(allUnits, aiPlayerID);
            Debug.Log("[AnalyzeGameState] Influence map updated");
        }

        // 3. Actualizar waypoints tácticos (depende del mapa de influencia actualizado)
        if (tacticalWaypoints != null)
        {
            tacticalWaypoints.UpdateWaypoints(aiPlayerID, friendlyUnits, enemyUnits);
            Debug.Log("[AnalyzeGameState] Tactical waypoints updated");
        }
    }

    private void MakeProductionDecisions()
    {
        int resources = gameManager.resourcesPerPlayer[aiPlayerID];
        HexCell baseCell = hexGrid.GetPlayerBase(aiPlayerID);

        if (baseCell == null)
        {
            Debug.Log("[Production] Base not found");
            return;
        }

        // Buscar celda vecina libre para producir (no en la base misma)
        HexCell spawnCell = null;
        foreach (HexCell neighbor in baseCell.neighbors)
        {
            if (neighbor.IsPassable() && neighbor.occupyingUnit == null)
            {
                spawnCell = neighbor;
                break;
            }
        }

        if (spawnCell == null)
        {
            Debug.Log("[Production] No free adjacent cell for production");
            return;
        }

        if (strategicContext.NumericalAdvantage > 2f) return;

        UnitType unitToProduce = DecideUnitType();
        HardcodedUnitStats stats = GetStatsForType(unitToProduce);

        if (resources >= stats.cost)
        {
            Unit newUnit = gameManager.SpawnUnit(unitToProduce, spawnCell, aiPlayerID);
            if (newUnit != null)
            {
                Debug.Log($"[Production] Produced {unitToProduce} for {stats.cost} resources at {spawnCell.gridPosition}");
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

        Dictionary<UnitType, float> scores = new Dictionary<UnitType, float>()
        {
            { UnitType.Infantry, 0f },
            { UnitType.Cavalry, 0f },
            { UnitType.Artillery, 0f }
        };

        //Aumentan las probabilidades según el estilo estratégico adoptado
        scores[UnitType.Cavalry] += aggressionLevel * 30f;
        scores[UnitType.Artillery] += aggressionLevel * 15f;
        scores[UnitType.Infantry] += economicFocus * 25f;

        int totalUnits = friendlyUnits.Count;

        //Equilibrio dentro del conjunto del ejercito
        if (cavalryCount < totalUnits * 0.25f) scores[UnitType.Cavalry] += 15f;
        if (artilleryCount < totalUnits * 0.20f) scores[UnitType.Infantry] += 20f;
        if (infantryCount < totalUnits * 0.50f) scores[UnitType.Infantry] += 10f;

        //Si hay un enemigo cerca de la base se le da prioridad a crear una unidad igual
        if (CheckEnemyThreatNearBase())
        {
            scores[ObtainEnemyNearBase()] += 40f;
        }

        // bool enemyThreatNearBase = CheckEnemyThreatNearBase();
        // bool needMobility = cavalryCount < friendlyUnits.Count * 0.3f;
        // bool needFirepower = artilleryCount < friendlyUnits.Count * 0.2f;

        // if (enemyThreatNearBase && artilleryCount < 2)
        // {
        //     Debug.Log("[Production Decision] Enemy threat detected - producing Artillery");
        //     return UnitType.Artillery;
        // }

        // if (aggressionLevel > 0.7f && needMobility)
        // {
        //     Debug.Log("[Production Decision] Aggressive strategy - producing Cavalry");
        //     return UnitType.Cavalry;
        // }

        // if (needFirepower && friendlyUnits.Count > 2)
        // {
        //     Debug.Log("[Production Decision] Need firepower - producing Artillery");
        //     return UnitType.Artillery;
        // }

        // Debug.Log("[Production Decision] Default - producing Infantry");
        // return UnitType.Infantry;

        return scores.OrderByDescending(s => s.Value).First().Key;
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

    private UnitType ObtainEnemyNearBase()
    {
        HexCell baseCell = hexGrid.GetPlayerBase(aiPlayerID);

        UnitType tipoUnidadEnemiga = UnitType.Infantry;
        int maxDistance = 5;

        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.CurrentCell != null)
            {
                int distance = CombatSystem.HexDistance(baseCell, enemy.CurrentCell);
                if (distance <= maxDistance)
                {
                    switch (enemy.unitType)
                    {
                        case UnitType.Artillery: 
                            tipoUnidadEnemiga = UnitType.Artillery;
                            break;
                        case UnitType.Cavalry:
                            if (tipoUnidadEnemiga != UnitType.Artillery) tipoUnidadEnemiga = UnitType.Cavalry;
                            break;
                        case UnitType.Infantry:
                            break;
                    }
                }
                    
            }
        }

        return tipoUnidadEnemiga;
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

    private void ExecuteUnits()
    {
        foreach (Unit unit in friendlyUnits)
        {
            // Delegar la ejecución al behavior tree de cada unidad
            UnitAI unitAI = unit.GetComponent<UnitAI>();
            if (unitAI != null)
            {
                unitAI.ExecuteTurn();
            }
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
}
