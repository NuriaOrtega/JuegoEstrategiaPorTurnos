using UnityEngine;


public class HexGrid : MonoBehaviour
{
    [Header("Configuración de la cuadrícula")]
    public int gridWidth = 10;
    public int gridHeight = 8;
    public GameObject hexCellPrefab;

    [Header("Tamaño real del hexágono (unidades)")]
    public float hexWidth = 0.96f;
    public float hexHight = 0.83f;

    void Start()
    {
        GenerateHexGrid();
    }

    void GenerateHexGrid()
    {
        if(hexCellPrefab == null) {
            Debug.LogError("HexGrid: falta asignar hexPrefb.");
            return;
        }

        float xOffset = hexWidth * 0.75f;
        float zOffset = hexHight;

        for (int col = 0; col < gridWidth; col++) {
            for (int row = 0; row < gridHeight; row++) {
                float xPos = col * xOffset;
                float zPos = row * zOffset;

                if (col % 2 == 1) zPos += zOffset * 0.5f;

                Vector3 spawnPos = new(xPos, 0f, zPos);

                Quaternion rotation = Quaternion.Euler(90f, 0f, 30f);
                GameObject hexGO = Instantiate(hexCellPrefab, spawnPos, rotation, transform);
                hexGO.name = $"Hex_{col}_{row}";

                HexCell info = hexGO.GetComponent<HexCell>();
                if (info != null) {
                    info.gridPosition.x = col;
                    info.gridPosition.y = row;
                }
            }
        }
    }
}
