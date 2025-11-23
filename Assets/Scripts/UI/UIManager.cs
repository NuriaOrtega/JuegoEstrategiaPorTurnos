using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HexGrid hexGrid;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI resourcesText;
    [SerializeField] private GameObject unitSelectedInfo;

    void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (hexGrid == null)
            hexGrid = FindObjectOfType<HexGrid>();
    }

    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (gameManager == null)
            return;

        int currentPlayer = gameManager.currentPlayerTurn;

        if (turnText != null)
        {
            turnText.text = $"Turno: Jugador {currentPlayer}";
        }

        if (resourcesText != null)
        {
            int resources = gameManager.resourcesPerPlayer[currentPlayer];
            resourcesText.text = $"Recursos: {resources}";
        }

        if (unitSelectedInfo != null)
        {
            if (gameManager.AUnitIsSelected()) UpdateUnitInfo(gameManager.selectedUnit);
            else unitSelectedInfo.SetActive(false);
        }
    }

    private void UpdateUnitInfo(Unit unidad)
    {
        unitSelectedInfo.SetActive(true);

        TextMeshProUGUI propietarioText = unitSelectedInfo.transform.Find("PropertyText").GetComponent<TextMeshProUGUI>();
        if (unidad.OwnerPlayerID == 0) propietarioText.text = $"Unidad aliada";
        else propietarioText.text = $"Unidad enemiga";

        TextMeshProUGUI tipoText = unitSelectedInfo.transform.Find("TypeText").GetComponent<TextMeshProUGUI>();
        switch (unidad.unitType)
        {
            case UnitType.Infantry:
                tipoText.text = $"Tipo: Infantería";
                break;
            case UnitType.Cavalry:
                tipoText.text = $"Tipo: Caballería";
                break;
            case UnitType.Artillery:
                tipoText.text = $"Tipo: Artillería";
                break;
            default:
                tipoText.text = $"Tipo: Infantería";
                break;
        }

        TextMeshProUGUI vidaText = unitSelectedInfo.transform.Find("HealthText").GetComponent<TextMeshProUGUI>();
        vidaText.text = $"Vida: {unidad.currentHealth}/{unidad.maxHealth}";

        TextMeshProUGUI movimientoText = unitSelectedInfo.transform.Find("MovementText").GetComponent<TextMeshProUGUI>();
        movimientoText.text = $"Movimientos disponibles: {unidad.remainingMovement}/{unidad.movementPoints}";

        TextMeshProUGUI posicionText = unitSelectedInfo.transform.Find("PositionText").GetComponent<TextMeshProUGUI>();
        posicionText.text = $"Ubicación: Celda ({unidad.CurrentCell.gridPosition.x},{unidad.CurrentCell.gridPosition.y})";
    
        TextMeshProUGUI poderDeAtaqueText = unitSelectedInfo.transform.Find("AttackPowerText").GetComponent<TextMeshProUGUI>();
        poderDeAtaqueText.text = $"Poder de ataque: {unidad.attackPower}";
    
        TextMeshProUGUI rangoDeAtaqueText = unitSelectedInfo.transform.Find("AttackRangeText").GetComponent<TextMeshProUGUI>();
        rangoDeAtaqueText.text = $"Rango de ataque: {unidad.attackRange}";
    
        TextMeshProUGUI haAtacadoText = unitSelectedInfo.transform.Find("HasAttackedText").GetComponent<TextMeshProUGUI>();
        if(unidad.hasAttacked) haAtacadoText.text = $"La unidad ya ha atacado";
        else haAtacadoText.text = $"La unidad todavía no ha atacado";
    }

    public void ProduceInfantry()
    {
        TryProduceUnit(UnitType.Infantry, 20);
    }

    public void ProduceCavalry()
    {
        TryProduceUnit(UnitType.Cavalry, 30);
    }

    public void ProduceArtillery()
    {
        TryProduceUnit(UnitType.Artillery, 40);
    }

    private void TryProduceUnit(UnitType unitType, int cost)
    {
        if (gameManager.currentPlayerTurn != 0)
        {
            Debug.Log("Cannot produce units: It's not your turn!");
            return;
        }

        if (gameManager.resourcesPerPlayer[0] < cost)
        {
            Debug.Log($"Cannot produce {unitType}: Not enough resources. Need {cost}, have {gameManager.resourcesPerPlayer[0]}");
            return;
        }

        HexCell playerBase = hexGrid.GetPlayerBase(0);
        if (playerBase == null)
        {
            Debug.LogError("Cannot produce unit: Player base not found!");
            return;
        }

        HexCell spawnCell = FindEmptyNeighbor(playerBase);
        if (spawnCell == null)
        {
            Debug.Log("Cannot produce unit: No empty cells adjacent to base!");
            return;
        }

        Unit newUnit = gameManager.SpawnUnit(unitType, spawnCell, 0);
        if (newUnit != null)
        {
            Debug.Log($"Successfully produced {unitType}!");
        }
    }

    private HexCell FindEmptyNeighbor(HexCell cell)
    {
        if (cell == null || cell.neighbors == null)
            return null;

        foreach (HexCell neighbor in cell.neighbors)
        {
            if (neighbor.occupyingUnit == null && neighbor.IsPassable())
            {
                return neighbor;
            }
        }

        return null;
    }
}
