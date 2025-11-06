using UnityEngine;
using System.Collections.Generic;


public enum TerrainType
{
    Llanura,
    Bosque,
    Montaña,
    Agua
}


public class HexCell : MonoBehaviour
{
    public Vector2Int gridPosition;
    public TerrainType terrainType;
    public float movementCost;
    public float defensiveBonus;
    public bool isWalkable;
    public List<HexCell> neighbors = new();

    [HideInInspector] public float gCost; // Coste desde al inicio
    [HideInInspector] public float hCost; // Coste heurístico al objetivo
    public float fCost => gCost + hCost;
    [HideInInspector] public HexCell parent;

    private Renderer hexRenderer;
    private Color originalColor;
    private bool isHighlighted = false;

    void Awake()
    {
        hexRenderer = GetComponent<Renderer>();
        if (hexRenderer != null)
        {
            originalColor = hexRenderer.material.color;
        }
    }


    public void Highlight(bool highlight)
    {
        if (hexRenderer == null) return;

        isHighlighted = highlight;

        if (highlight)
        {
            hexRenderer.material.color = Color.yellow;
        }
        else
        {
            hexRenderer.material.color = originalColor;
        }
    }
}
