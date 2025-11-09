using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class HexCell : MonoBehaviour
{
    public Vector2Int gridPosition;
    public TerrainType terrainType;
    public Unit occupyingUnit;
    public int OwnerPlayerID = -1; // -1 = neutral, 0/1 = jugador
    public bool isBase;

    public List<HexCell> neighbors = new();

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

    public bool IsOccupied()
    {
        return occupyingUnit != null;
    }

    public bool IsPassable()
    {
        return terrainType != TerrainType.Agua && !IsOccupied();
    }

    public bool IsPassableForPlayer(int playerID)
    {
        if (terrainType == TerrainType.Agua) return false;

        //Las bases amigas son impasables pero las enemigas son pasables (para ser capturadas)
        if (isBase && OwnerPlayerID == playerID) return false;

        //Las celdas normales ocupadas son impasables
        if (IsOccupied()) return false;

        return true;
    }

    public float GetMovementCost()
    {
        return terrainType.GetMovementCost();
    }
}
