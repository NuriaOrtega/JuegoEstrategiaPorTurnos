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

    private enum ViewMode
    {
        Normal,
        Selection,
        Influence,
        Waypoint
    }

    private ViewMode modoActual = ViewMode.Normal;
    private Unit lastSelectedUnit = null;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (influenceMap == null) influenceMap = FindObjectOfType<InfluenceMap>();
        if (hexGrid == null) hexGrid = FindObjectOfType<HexGrid>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (gameManager.selectedUnit != null && lastSelectedUnit !=gameManager.selectedUnit) SetViewMode(ViewMode.Selection);
        else if (Input.GetKeyDown(KeyCode.I)) SetViewMode(ViewMode.Influence);
        else if (Input.GetKeyDown(KeyCode.W)) SetViewMode(ViewMode.Waypoint);
        else if (Input.GetKeyDown(KeyCode.N)) SetViewMode(ViewMode.Normal);
    }

    private void SetViewMode(ViewMode nuevoModo)
    {
        if (modoActual == nuevoModo) return;

        modoActual = nuevoModo;

        StartCoroutine(ExecuteTransition());
    }

    private IEnumerator ExecuteTransition()
    {
        HexCell centro = hexGrid.GetCell(hexGrid.gridWidth/2, hexGrid.gridHeight/2);

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

        for (int i = 0; i < maxDist+1; i++)
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
                Debug.Log("Activar modo de vista normal.");
                break;
            case ViewMode.Influence: 
                Debug.Log("Activar modo de vista influence.");
                celda.SetColor();
                break;
            case ViewMode.Selection:
                break;
            case ViewMode.Waypoint:
                break;
        }
    }

    // private void Activate
}
