using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

public class HexCell : MonoBehaviour
{
    public Vector2Int gridPosition;
    public TerrainType terrainType;
    public Unit occupyingUnit;
    public int OwnerPlayerID = -1; // -1 = neutral, 0/1 = jugador
    public bool isBase;
    public bool isResourceNode;
    public bool resourceCollected;

    public List<HexCell> neighbors = new();

    private Renderer hexRenderer;
    public Color originalColor;
    private Color actualColor;
    private bool isHighlighted = false;

    public void Initialize(Vector2Int coordinates, TerrainType terrain)
    {
        gridPosition = coordinates;
        terrainType = terrain;

        hexRenderer = GetComponent<Renderer>();
        if (hexRenderer != null)
        {
            originalColor = terrainType.GetTerrainColor();
            actualColor = originalColor;
            hexRenderer.material.color = originalColor;
        }
    }

    public void UpdateOriginalColor()
    {
        if (hexRenderer != null)
        {
            originalColor = hexRenderer.material.color;
            actualColor = originalColor;
        }
    }

    public void Highlight(bool highlight, bool isHover = false)
    {
        if (hexRenderer == null) return;

        isHighlighted = highlight;

        if (highlight)
        {
            if (isHover)
            {
                hexRenderer.material.color = actualColor * 0.7f;
            }
            else
            {
                hexRenderer.material.color = Color.yellow;
            }
        }
        else
        {
            hexRenderer.material.color = actualColor;
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

    public void Clear()
    {
        hexRenderer.material.color = originalColor;
        actualColor = originalColor;
    }

    public void ResetColor()
    {
        StartCoroutine(Bounce(originalColor));
        actualColor = originalColor;
    }

    public void SetColor(Color nuevoColor)
    {
        StartCoroutine(Bounce(nuevoColor));
        actualColor = nuevoColor;
    }

    private IEnumerator Bounce(Color nuevoColor)
    {
        float height = 0.2f;
        float duration = 0.1f;

        Vector3 startPos = transform.position;
        Vector3 upPos = startPos + Vector3.up * height;

        float t = 0f;

        // Subida
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, upPos, t / duration);
            yield return null;
        }

        hexRenderer.material.color = nuevoColor;

        t = 0f;
        // Bajada
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(upPos, startPos, t / duration);
            yield return null;
        }
    }
}
