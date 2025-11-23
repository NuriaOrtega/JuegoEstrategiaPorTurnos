using UnityEngine;


public class MouseInput : MonoBehaviour
{
    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private GameManager gameManager;

    private Camera mainCamera;
    private HexCell hoveredCell;
    private HexCell pressedCell;

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
        if (pressedCell != null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            HexCell cell = hit.collider.GetComponent<HexCell>();
            if (cell != null && cell != hoveredCell)
            {
                if (hoveredCell != null)
                {
                    hoveredCell.Highlight(false);
                }

                hoveredCell = cell;
                hoveredCell.Highlight(true, isHover: true);
            }
        }
        else
        {
            if (hoveredCell != null)
            {
                hoveredCell.Highlight(false);
                hoveredCell = null;
            }
        }
    }

    private void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                HexCell clickedCell = hit.collider.GetComponent<HexCell>()
                    ?? hit.collider.GetComponent<Unit>()?.CurrentCell;

                if (clickedCell != null)
                    OnCellPressed(clickedCell);
            }
        }

        // Mouse button released
        if (Input.GetMouseButtonUp(0))
        {
            if (pressedCell != null)
            {
                OnCellReleased(pressedCell);
            }
        }
    }

    private void OnCellPressed(HexCell cell)
    {
        if (hoveredCell == cell)
        {
            hoveredCell.Highlight(false);
        }

        pressedCell = cell;
        pressedCell.Highlight(true, isHover: false);
    }

    private void OnCellReleased(HexCell cell)
    {
        cell.Highlight(false);

        if (gameManager != null)
        {
            gameManager.OnCellSelected(cell);
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            HexCell currentCell = hit.collider.GetComponent<HexCell>();
            if (currentCell == cell)
            {
                hoveredCell = cell;
                hoveredCell.Highlight(true, isHover: true);
            }
        }

        pressedCell = null;
    }
}
