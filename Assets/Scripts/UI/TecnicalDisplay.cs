using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

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
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (influenceMap == null) influenceMap = FindObjectOfType<InfluenceMap>();
        if (hexGrid == null) hexGrid = FindObjectOfType<HexGrid>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (gameManager.selectedUnit != null) SetViewMode(ViewMode.Selection);
        else if (Input.GetKeyDown(KeyCode.I)) SetViewMode(ViewMode.Influence);
        else if (Input.GetKeyDown(KeyCode.W)) SetViewMode(ViewMode.Waypoint);
        else if (Input.GetKeyDown(KeyCode.N)) SetViewMode(ViewMode.Normal);
    }

    private void SetViewMode(ViewMode modoVisualizacion)
    {
        if (modoActual == modoVisualizacion) return;
    }

}
