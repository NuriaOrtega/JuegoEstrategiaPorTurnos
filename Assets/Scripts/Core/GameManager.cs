using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estado del Juego")]
    public int currentPlayerTurn = 0; // 0 = Jugador, 1 = IA

    [Header("Unidades")]
    public List<Unit> player0Units = new List<Unit>();
    public List<Unit> player1Units = new List<Unit>();

    [Header("Recursos")]
    public int[] resourcesPerPlayer = new int[2] { 20, 20 }; 
    private const int RESOURCES_PER_TURN = 10;
    private const int RESOURCES_PER_NODE = 10;

    [Header("Referencias")]
    public HexGrid hexGrid;
    private StrategicManager strategicManager;

    [Header("Estado de Selección")]
    public Unit selectedUnit;
    public DijkstraPathfinding pathfinding;
    public TecnicalDisplay tecnicalDisplay;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [System.Obsolete]
    void Start()
    {
        if (hexGrid == null)
            hexGrid = FindObjectOfType<HexGrid>();

        strategicManager = FindObjectOfType<StrategicManager>();
        tecnicalDisplay = FindObjectOfType<TecnicalDisplay>();
        pathfinding = new DijkstraPathfinding(hexGrid);

        StartCoroutine(SpawnStartingUnits());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndTurn();
        }
    }

    public List<Unit> GetAllUnitsForPlayer(int playerID)
    {
        return playerID == 0 ? player0Units : player1Units;
    }

    public List<Unit> GetAllUnits()
    {
        List<Unit> allUnits = new List<Unit>();
        allUnits.AddRange(player0Units);
        allUnits.AddRange(player1Units);
        return allUnits;
    }
    
    public Unit SpawnUnit(UnitType type, HexCell spawnCell, int playerID)
    {
        if (spawnCell == null)
        {
            Debug.LogError("Cannot spawn unit: spawnCell is null");
            return null;
        }

        if (spawnCell.occupyingUnit != null)
        {
            Debug.LogError($"Cannot spawn unit at {spawnCell.gridPosition}: cell is occupied");
            return null;
        }

        HardcodedUnitStats stats = GetStatsForType(type);
        if (resourcesPerPlayer[playerID] < stats.cost)
        {
            Debug.Log($"Player {playerID} doesn't have enough resources to spawn {type}. Need {stats.cost}, have {resourcesPerPlayer[playerID]}");
            return null;
        }

        GameObject unitGO = new GameObject($"{type}_{playerID}_{System.Guid.NewGuid().ToString().Substring(0, 8)}");
        unitGO.transform.position = spawnCell.transform.position + Vector3.up * 0.1f;

        GameObject visual;
        switch (type)
        {
            case UnitType.Infantry:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                break;

            case UnitType.Cavalry:
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                break;

            case UnitType.Artillery:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.transform.localScale = new Vector3(0.15f, 0.1f, 0.15f);
                break;

            default:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                break;
        }

        visual.transform.SetParent(unitGO.transform);
        visual.transform.localPosition = Vector3.zero;

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color unitColor = playerID == 0 ? new Color(0.3f, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f); // Blue for Player 0, Red for Player 1
            renderer.material.color = unitColor;
        }

        // Add collider to parent for selection
        BoxCollider collider = unitGO.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.3f, 0.3f, 0.3f);

        Unit unit;
        switch (type)
        {
            case UnitType.Infantry:
                unit = unitGO.AddComponent<InfantryUnit>();
                break;
            case UnitType.Cavalry:
                unit = unitGO.AddComponent<CavalryUnit>();
                break;
            case UnitType.Artillery:
                unit = unitGO.AddComponent<ArtilleryUnit>();
                break;
            default:
                unit = unitGO.AddComponent<InfantryUnit>();
                break;
        }

        switch (type)
        {
            case UnitType.Infantry:
                unitGO.AddComponent<InfantryAI>();
                break;
            case UnitType.Cavalry:
                unitGO.AddComponent<CavalryAI>();
                break;
            case UnitType.Artillery:
                unitGO.AddComponent<ArtilleryAI>();
                break;
            default:
                unitGO.AddComponent<InfantryAI>();
                break;
        }

        unit.Initialize(type, playerID, spawnCell);

        resourcesPerPlayer[playerID] -= stats.cost;

        if (playerID == 0)
            player0Units.Add(unit);
        else
            player1Units.Add(unit);

        Debug.Log($"Spawned {type} for Player {playerID} at {spawnCell.gridPosition}. Resources remaining: {resourcesPerPlayer[playerID]}");
        return unit;
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
    public void AddResources(int playerID, int amount)
    {
        resourcesPerPlayer[playerID] += amount;
        Debug.Log($"Player {playerID} gained {amount} resources. Total: {resourcesPerPlayer[playerID]}");
    }

    public void RemoveUnit(Unit unit)
    {
        if (unit.OwnerPlayerID == 0)
            player0Units.Remove(unit);
        else
            player1Units.Remove(unit);
    }

    public void OnCellSelected(HexCell cell)
    {
        if (cell == null) return;
        if (currentPlayerTurn != 0) return;

        Debug.Log($"Celda seleccionada en posición: {cell.gridPosition}");

        if (selectedUnit == null)
        {
            if (cell.occupyingUnit != null)
            {
                selectedUnit = cell.occupyingUnit;
                pathfinding.CreateMap(selectedUnit);
                tecnicalDisplay.SetViewModeSelection(selectedUnit);
                Debug.Log($"Selected unit: {selectedUnit.unitType}");
            }
        }
        else if (selectedUnit.OwnerPlayerID == 0)
        {
            if (cell.occupyingUnit != null)
            {
                if (cell.occupyingUnit.OwnerPlayerID != 0)
                {
                    selectedUnit.AttackUnit(cell.occupyingUnit);
                    selectedUnit = null;
                    hexGrid.ClearGridColours();
                } else
                {
                    selectedUnit = cell.occupyingUnit;
                    pathfinding.CreateMap(selectedUnit);
                    tecnicalDisplay.SetViewModeSelection(selectedUnit);
                    Debug.Log($"Selected unit: {selectedUnit.unitType}");
                }
                
            }
            else
            {
                selectedUnit.MoveToCell(cell, pathfinding);
                CheckVictoryConditions();
                selectedUnit = null;
                hexGrid.ClearGridColours();
            }
        }
        else
        {
            if (cell.occupyingUnit != null)
            {
                selectedUnit = cell.occupyingUnit;
                pathfinding.CreateMap(selectedUnit);
                tecnicalDisplay.SetViewModeSelection(selectedUnit);
                Debug.Log($"Selected unit: {selectedUnit.unitType}");
            }
            else
            {
                selectedUnit = null;
                hexGrid.ClearGridColours();
            }
        }
    }
    
    public bool AUnitIsSelected ()
    {
        return selectedUnit != null;
    }
    public void EndTurn()
    {
        List<Unit> currentPlayerUnits = GetAllUnitsForPlayer(currentPlayerTurn);
        foreach (Unit unit in currentPlayerUnits)
        {
            unit.ResetForNewTurn();
        }

        AddResources(currentPlayerTurn, RESOURCES_PER_TURN);

        CollectResourcesFromNodes(currentPlayerTurn);

        if (CheckVictoryConditions())
        {
            return; 
        }

        currentPlayerTurn = (currentPlayerTurn + 1) % 2;
        Debug.Log($"==================== TURN START: Player {currentPlayerTurn} ====================");

        if (currentPlayerTurn == 1 && strategicManager != null)
        {
            Debug.Log("Empieza el turno de la IA");
            strategicManager.ExecuteAITurn();
        }
    }

    private void CollectResourcesFromNodes(int playerID)
    {
        List<Unit> units = GetAllUnitsForPlayer(playerID);
        foreach (Unit unit in units)
        {
            if (unit.CurrentCell != null && unit.CurrentCell.isResourceNode && !unit.CurrentCell.resourceCollected)
            {
                AddResources(playerID, RESOURCES_PER_NODE);
                unit.CurrentCell.resourceCollected = true;

                // Destruir el texto visual "+10"
                Transform resourceText = unit.CurrentCell.transform.Find("ResourceText");
                if (resourceText != null)
                {
                    Destroy(resourceText.gameObject);
                }

                Debug.Log($"Player {playerID} collected resource node at {unit.CurrentCell.gridPosition}");
            }
        }
    }

    private bool CheckVictoryConditions()
    {
        List<HexCell> allCells = hexGrid.GetAllCells();
        HexCell player0Base = allCells.FirstOrDefault(c => c.isBase && c.OwnerPlayerID == 0);
        HexCell player1Base = allCells.FirstOrDefault(c => c.isBase && c.OwnerPlayerID == 1);

        if (player0Base != null && player0Base.occupyingUnit != null && player0Base.occupyingUnit.OwnerPlayerID == 1)
        {
            Debug.Log("==================== PLAYER 1 (AI) WINS! ====================");
            Debug.Log("AI captured the player's base!");
            return true;
        }

        if (player1Base != null && player1Base.occupyingUnit != null && player1Base.occupyingUnit.OwnerPlayerID == 0)
        {
            Debug.Log("==================== PLAYER 0 (HUMAN) WINS! ====================");
            Debug.Log("Player captured the AI's base!");
            return true;
        }

        return false;
    }
    public int GetCurrentPlayerResources()
    {
        return resourcesPerPlayer[currentPlayerTurn];
    }

    private System.Collections.IEnumerator SpawnStartingUnits()
    {
        yield return new WaitForSeconds(0.5f);

        HexCell base0 = hexGrid.GetPlayerBase(0);
        HexCell base1 = hexGrid.GetPlayerBase(1);

        if (base0 == null || base1 == null)
        {
            Debug.LogError("Cannot spawn starting units: bases not found!");
            yield break;
        }

        // Spawn Player 0 units en las primeras 2 celdas vecinas pasables
        int spawned0 = 0;
        foreach (HexCell neighbor in base0.neighbors)
        {
            if (spawned0 >= 2) break;
            if (neighbor.IsPassable() && neighbor.occupyingUnit == null)
            {
                SpawnUnit(UnitType.Infantry, neighbor, 0);
                spawned0++;
            }
        }
        Debug.Log($"Player 0: {spawned0} starting units spawned near base");

        // Spawn Player 1 (IA) units en las primeras 2 celdas vecinas pasables
        int spawned1 = 0;
        foreach (HexCell neighbor in base1.neighbors)
        {
            if (spawned1 >= 2) break;
            if (neighbor.IsPassable() && neighbor.occupyingUnit == null)
            {
                SpawnUnit(UnitType.Infantry, neighbor, 1);
                spawned1++;
            }
        }
        Debug.Log($"Player 1 (AI): {spawned1} starting units spawned near base");
    }
}
