using UnityEngine;

/// <summary>
/// Maneja la entrada del ratón para selección e interacción con celdas.
/// </summary>
public class MouseInput : MonoBehaviour
{
    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private GameManager gameManager;

    private Camera mainCamera;
    private HexCell selectedCell;
    private HexCell hoveredCell;

    void Start()
    {
        mainCamera = Camera.main;

        if (hexGrid == null)
            hexGrid = FindObjectOfType<HexGrid>();

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        HandleMouseHover();
        HandleMouseClick();
    }

    private void HandleMouseHover()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            HexCell cell = hit.collider.GetComponent<HexCell>();
            if (cell != null && cell != hoveredCell)
            {
                if (hoveredCell != null && hoveredCell != selectedCell)
                {
                    hoveredCell.Highlight(false);
                }

                hoveredCell = cell;
                if (hoveredCell != selectedCell)
                {
                    hoveredCell.Highlight(true);
                }
            }
        }
        else
        {
            if (hoveredCell != null && hoveredCell != selectedCell)
            {
                hoveredCell.Highlight(false);
                hoveredCell = null;
            }
        }
    }

    private void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0)) // Click izquierdo
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                HexCell clickedCell = hit.collider.GetComponent<HexCell>();
                if (clickedCell != null)
                {
                    OnCellClicked(clickedCell);
                }
            }
        }
    }

    private void OnCellClicked(HexCell cell)
    {
        // Limpiar resaltado de selección anterior
        if (selectedCell != null)
        {
            selectedCell.Highlight(false);
        }

        selectedCell = cell;
        selectedCell.Highlight(true);

        // Notificar al GameManager sobre la selección de celda
        if (gameManager != null)
        {
            gameManager.OnCellSelected(cell);
        }
    }

    public HexCell GetSelectedCell()
    {
        return selectedCell;
    }

    public void ClearSelection()
    {
        if (selectedCell != null)
        {
            selectedCell.Highlight(false);
            selectedCell = null;
        }
    }
}
