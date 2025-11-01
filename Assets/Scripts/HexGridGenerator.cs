using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("Configuración de la cuadrícula")]
    public int gridWidth = 10;
    public int gridHeight = 8;
    public GameObject hexPrefab;

    [Header("Tamaño real del hexágono (unidades)")]
    public float hexWidth = 0.96f;
    public float hexHight = 0.83f;

    void Start()
    {
        GenerateHexGrid();
    }

    // Update is called once per frame
    void GenerateHexGrid()
    {
        if(hexPrefab == null) {
            Debug.LogError("HexGridGenerator: falta asignar hexPrefb.");
            return;
        }

        float xOffset = hexWidth * 0.75f;
        float zOffset = hexHight;

        for (int col = 0; col < gridWidth; col++) {
            for (int row = 0; row < gridHeight; row++) {
                float xPos = col * xOffset;
                float zPos = row * zOffset;

                if (col % 2 == 1) zPos += zOffset * 0.5f;

                Vector3 spawnPos = new Vector3(xPos, 0f, zPos);

                Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
                GameObject hexGO = Instantiate(hexPrefab, spawnPos, rotation, this.transform);
                hexGO.name = $"Hex_{col}_{row}";

                HexTileInfo info = hexGO.GetComponent<HexTileInfo>();
                if (info != null) {
                    info.q = col;
                    info.r = row;
                }
            }
        }
    }
}
