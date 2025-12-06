using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class HexGrid : MonoBehaviour
{
    [Header("Configuración de la cuadrícula")]
    public int gridWidth = 11;
    public int gridHeight = 11;
    public GameObject hexCellPrefab;

    [Header("Tamaño real del hexágono (unidades)")]
    public float hexWidth = 0.96f;
    public float hexHight = 0.83f;

    [Header("Generación de Mapa")]
    [Range(0f, 1f)]
    public float waterPercentage = 0.1f;
    [Range(0f, 1f)]
    public float forestPercentage = 0.25f;
    [Range(0f, 1f)]
    public float mountainPercentage = 0.15f;
    public int resourceNodeCount = 10;

    private HexCell[,] cells;

    void Start()
    {
        GenerateHexGrid();
    }

    void GenerateHexGrid()
    {
        if(hexCellPrefab == null) {
            Debug.LogError("HexGrid: falta asignar hexPrefab.");
            return;
        }

        cells = new HexCell[gridWidth, gridHeight];

        float xOffset = hexWidth * 0.75f;
        float zOffset = hexHight;

        // Generate all cells
        for (int col = 0; col < gridWidth; col++) {
            for (int row = 0; row < gridHeight; row++) {
                float xPos = col * xOffset;
                float zPos = row * zOffset;

                if (col % 2 == 1) zPos += zOffset * 0.5f;

                Vector3 spawnPos = new(xPos, 0f, zPos);

                Quaternion rotation = Quaternion.Euler(90f, 0f, 30f);
                GameObject hexGO = Instantiate(hexCellPrefab, spawnPos, rotation, transform);
                hexGO.name = $"Hex_{col}_{row}";

                HexCell cell = hexGO.GetComponent<HexCell>();
                if (cell != null) {
   
                    TerrainType terrain = GetRandomTerrain(col, row);
                    cell.Initialize(new Vector2Int(col, row), terrain);
                    cells[col, row] = cell;
                }
            }
        }


        SetupNeighbors();

        PlaceBases();

        EnsureBaseAccessibility();

        PlaceResourceNodes();

        Debug.Log($"HexGrid generated: {gridWidth}x{gridHeight} with {resourceNodeCount} resource nodes");
    }

 
    private TerrainType GetRandomTerrain(int col, int row)
    {
        bool nearBase = (col <= 1 && row <= 1) || (col >= gridWidth - 2 && row >= gridHeight - 2);

        if (nearBase)
        {
            return Random.value < 0.7f ? TerrainType.Llanura : TerrainType.Bosque;
        }


        float rand = Random.value;

        if (rand < waterPercentage)
            return TerrainType.Agua;
        else if (rand < waterPercentage + forestPercentage)
            return TerrainType.Bosque;
        else if (rand < waterPercentage + forestPercentage + mountainPercentage)
            return TerrainType.Montaña;
        else
            return TerrainType.Llanura;
    }


    private void SetupNeighbors()
    {
        for (int col = 0; col < gridWidth; col++)
        {
            for (int row = 0; row < gridHeight; row++)
            {
                HexCell cell = cells[col, row];
                if (cell == null) continue;

                cell.neighbors.Clear();
                List<Vector2Int> neighborOffsets = GetNeighborOffsets(col, row);

                foreach (Vector2Int offset in neighborOffsets)
                {
                    int neighborCol = col + offset.x;
                    int neighborRow = row + offset.y;

                    if (IsValidPosition(neighborCol, neighborRow))
                    {
                        HexCell neighbor = cells[neighborCol, neighborRow];
                        if (neighbor != null)
                        {
                            cell.neighbors.Add(neighbor);
                        }
                    }
                }
            }
        }
    }

    private List<Vector2Int> GetNeighborOffsets(int col, int row)
    {
        bool isOddCol = (col % 2 == 1);

        if (isOddCol) {
            Debug.Log(row + "Es impar.");

            return new List<Vector2Int>
                {
                    new(0, 1),   // N
                    new(1, 1),   // NE
                    new(1, 0),    // SE
                    new(0, -1),    // S
                    new(-1, 0),   // SW
                    new(-1, 1)   // NW
                };
        }
        else {
            Debug.Log(row + "Es par.");

            return new List<Vector2Int>
                {
                    new(0, 1),   // N
                    new(1, 0),   // NE
                    new(1, -1),    // SE
                    new(0, -1),    // S
                    new(-1, -1),   // SW
                    new(-1, 0)   // NW
                };
        }
    }
    private void PlaceBases()
    {
        HexCell player0Base = GetCell(0, 0);
        if (player0Base != null)
        {
            player0Base.isBase = true;
            player0Base.OwnerPlayerID = 0;
            player0Base.terrainType = TerrainType.Llanura; 
            player0Base.GetComponent<Renderer>().material.color = new Color(0.3f, 0.5f, 1f);
            player0Base.UpdateOriginalColor(); 
            Debug.Log("Player 0 base placed at (0, 0)");
        }

        HexCell player1Base = GetCell(gridWidth - 1, gridHeight - 1);
        if (player1Base != null)
        {
            player1Base.isBase = true;
            player1Base.OwnerPlayerID = 1;
            player1Base.terrainType = TerrainType.Llanura; 
            player1Base.GetComponent<Renderer>().material.color = new Color(1f, 0.3f, 0.3f);
            player1Base.UpdateOriginalColor(); 
            Debug.Log($"Player 1 (AI) base placed at ({gridWidth - 1}, {gridHeight - 1})");
        }
    }

    private void EnsureBaseAccessibility()
    {
        HexCell base0 = GetCell(0, 0);
        HexCell base1 = GetCell(gridWidth - 1, gridHeight - 1);

        if (base0 != null)
        {
            foreach (HexCell neighbor in base0.neighbors)
            {
                if (neighbor.terrainType == TerrainType.Agua)
                {
                    neighbor.terrainType = TerrainType.Llanura;
                    neighbor.GetComponent<Renderer>().material.color = TerrainType.Llanura.GetTerrainColor();
                    Debug.Log($"Converted water to plains at {neighbor.gridPosition} (near Player 0 base)");
                }
            }
        }

        if (base1 != null)
        {
            foreach (HexCell neighbor in base1.neighbors)
            {
                if (neighbor.terrainType == TerrainType.Agua)
                {
                    neighbor.terrainType = TerrainType.Llanura;
                    neighbor.GetComponent<Renderer>().material.color = TerrainType.Llanura.GetTerrainColor();
                    Debug.Log($"Converted water to plains at {neighbor.gridPosition} (near Player 1 base)");
                }
            }
        }
    }

    private void PlaceResourceNodes()
    {
        List<HexCell> forbiddenCells = new List<HexCell>();

        HexCell base0 = GetCell(0, 0);
        HexCell base1 = GetCell(gridWidth - 1, gridHeight - 1);

        if (base0 != null)
            forbiddenCells.AddRange(base0.neighbors);

        if (base1 != null)
            forbiddenCells.AddRange(base1.neighbors);

        int placed = 0;
        int attempts = 0;
        int maxAttempts = resourceNodeCount * 10;

        while (placed < resourceNodeCount && attempts < maxAttempts)
        {
            attempts++;

            int col = Random.Range(2, gridWidth - 2);
            int row = Random.Range(2, gridHeight - 2);

            HexCell cell = GetCell(col, row);

            if (cell != null &&
                !cell.isBase &&
                !cell.isResourceNode &&
                cell.terrainType != TerrainType.Agua &&
                !forbiddenCells.Contains(cell))  // NEW: Don't place resources next to bases
            {
                cell.isResourceNode = true;
                cell.resourceCollected = false;

                // Create 3D text showing "+10"
                GameObject textObj = new GameObject("ResourceText");
                textObj.transform.SetParent(cell.transform);
                textObj.transform.position = cell.transform.position + Vector3.up * 0.2f; 
                textObj.transform.rotation = Quaternion.Euler(90, 0, 0);

                TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
                textMesh.text = "+10";
                textMesh.fontSize = 3f;
                textMesh.color = new Color(0.4f, 0.25f, 0.1f); // Dark brown color
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.fontStyle = FontStyles.Bold;

                placed++;
                Debug.Log($"Resource node placed at ({col}, {row})");
            }
        }

        if (placed < resourceNodeCount)
        {
            Debug.LogWarning($"Only placed {placed}/{resourceNodeCount} resource nodes after {maxAttempts} attempts");
        }
    }

    private bool IsValidPosition(int col, int row)
    {
        return col >= 0 && col < gridWidth && row >= 0 && row < gridHeight;
    }

    public HexCell GetCell(int col, int row)
    {
        if (!IsValidPosition(col, row))
            return null;

        return cells[col, row];
    }

    public List<HexCell> GetAllCells()
    {
        List<HexCell> allCells = new List<HexCell>();

        for (int col = 0; col < gridWidth; col++)
        {
            for (int row = 0; row < gridHeight; row++)
            {
                if (cells[col, row] != null)
                {
                    allCells.Add(cells[col, row]);
                }
            }
        }

        return allCells;
    }

    public HexCell GetPlayerBase(int playerID)
    {
        foreach (HexCell cell in GetAllCells())
        {
            if (cell.isBase && cell.OwnerPlayerID == playerID)
                return cell;
        }
        return null;
    }

    public void ClearGridColours()
    {
        foreach (HexCell celda in cells)
        {
            celda.Clear();
        }
    }
}
