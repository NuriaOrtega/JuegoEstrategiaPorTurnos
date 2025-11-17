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

        TextMeshProUGUI propertyText = unitSelectedInfo.transform.Find("PropertyText").GetComponent<TextMeshProUGUI>();
        if (unidad.OwnerPlayerID == 0) propertyText.text = $"Unidad aliada";
        else propertyText.text = $"Unidad enemiga";

        
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
