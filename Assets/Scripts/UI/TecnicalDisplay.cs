using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Collections;

public class TecnicalDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InfluenceMap influenceMap;
    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TacticalWaypoints waypoints;

    public enum ViewMode
    {
        Normal,
        Selection,
        Influence,
        Waypoint
    }

    public ViewMode modoActual = ViewMode.Normal;
    private Unit unidadSeleccionada = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (influenceMap == null) influenceMap = FindObjectOfType<InfluenceMap>();
        if (hexGrid == null) hexGrid = FindObjectOfType<HexGrid>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) SetViewMode(ViewMode.Influence);
        else if (Input.GetKeyDown(KeyCode.W)) SetViewMode(ViewMode.Waypoint);
        else if (Input.GetKeyDown(KeyCode.N)) SetViewMode(ViewMode.Normal);
    }

    private void SetViewMode(ViewMode nuevoModo)
    {
        if (modoActual == nuevoModo) return;

        modoActual = nuevoModo;

        HexCell centro = hexGrid.GetCell(hexGrid.gridWidth/2, hexGrid.gridHeight/2);

        unidadSeleccionada = null;

        List<HexCell> allCells = hexGrid.GetAllCells();

        Dictionary<int, List<HexCell>> anillos = new Dictionary<int, List<HexCell>>();

        int maxDist = 0;

        foreach(HexCell celda in allCells)
        {
            int d = CombatSystem.HexDistance(centro, celda);

            if(!anillos.ContainsKey(d)) anillos[d] = new List<HexCell>();

            anillos[d].Add(celda);

            if(d > maxDist) maxDist = d;
        }

        StartCoroutine(ExecuteTransition(anillos, maxDist));
    }

    public void SetViewModeSelection(Unit unidad)
    {
        Debug.Log("Modo de visualización: Unidad seleccionada.");

        modoActual = ViewMode.Selection;

        unidadSeleccionada = unidad;

        var resultado = gameManager.pathfinding.GetCellsOnRange();
        List<HexCell> rangoMovimiento = resultado.Item2;

        Dictionary<int, List<HexCell>> anillos = new Dictionary<int, List<HexCell>>();

        int maxDist = unidad.remainingMovement;

        foreach(HexCell celda in rangoMovimiento)
        {
            Debug.Log("Se ha añadido una celda al rango de movimiento.");
            int d = CombatSystem.HexDistance(unidad.CurrentCell, celda);

            if(!anillos.ContainsKey(d)) anillos[d] = new List<HexCell>();

            anillos[d].Add(celda);
        }

        gameManager.hexGrid.ClearGridColours();

        StartCoroutine(ExecuteTransition(anillos, maxDist));
    }

    private IEnumerator ExecuteTransition(Dictionary<int, List<HexCell>> anillos, int maxDist)
    {
        Debug.Log("Ejecutando animacion de los anillos.");

        for (int i = 0; i <= maxDist; i++)
        {
            if(anillos.ContainsKey(i))
            {
                foreach(HexCell celda in anillos[i])
                {
                    ChangeCellView(celda);
                }
            }
            yield return new WaitForSeconds(0.03f);
        }
    }

    private void ChangeCellView(HexCell celda)
    {
        switch(modoActual)
        {
            case ViewMode.Normal:
                celda.ResetColor();
                break;
            case ViewMode.Selection:
                celda.SetColor(gameManager.pathfinding.ColorByRange(celda));
                break;
            case ViewMode.Influence: 
                celda.SetColor(influenceMap.ObtainColorByNetInfluence(celda));
                break;
            case ViewMode.Waypoint:
                break;
        }
    }
}
