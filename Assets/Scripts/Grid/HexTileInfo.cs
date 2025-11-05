using UnityEngine;
using System.Collections.Generic;

public enum TerrainType
{
    Llanura,
    Bosque,
    Montaña,
    Agua
}

public class HexTileInfo : MonoBehaviour
{
    public Vector2Int gridPosition;
    public TerrainType terrainType;
    public float movementCost;
    public float defensiveBonus;
    public bool isWalkable;
    public List<HexTileInfo> neighbors = new();

    [HideInInspector] public float gCost; // Coste desde al inicio
    [HideInInspector] public float hCost; // Coste heurístico al objetivo
    public float fCost => gCost + hCost;
    [HideInInspector] public HexTileInfo parent;
}
