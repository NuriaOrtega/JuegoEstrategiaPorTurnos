using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class HexMeshGenerator : MonoBehaviour
{
    [Header("Tamaño del Hexágono (en unidades del mundo)")]
    public float width = 0.96f; // ancho del sprite en unidades
    public float height = 0.83f; // alto del sprite en unidades

    void Start()
    {
        GenerateHexMesh();
    }

    void GenerateHexMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "HexMesh";

        // Vértices del hexágono (vista desde arriba, en el plano XZ)
        Vector3[] vertices = new Vector3[6];
        float w = width;
        float h = height;

        vertices[0] = new Vector3(-w / 2f, 0, 0);
        vertices[1] = new Vector3(-w / 4f, 0, h / 2f);
        vertices[2] = new Vector3(w / 4f, 0, h / 2f);
        vertices[3] = new Vector3(w / 2f, 0, 0);
        vertices[4] = new Vector3(w / 4f, 0, -h / 2f);
        vertices[5] = new Vector3(-w / 4f, 0, -h / 2f);

        // Triángulos que forman la cara del hexágono
        int[] triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 5
        };

        // Normales (todas apuntando hacia arriba)
        Vector3[] normals = new Vector3[6];
        for (int i = 0; i < normals.Length; i++)
            normals[i] = Vector3.up;

        // UVs (para mapear textura si hace falta)
        Vector2[] uvs = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / w + 0.5f, vertices[i].z / h + 0.5f);
        }

        // Asignar todo al mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.RecalculateBounds();

        // Asignar al MeshFilter y MeshCollider
        GetComponent<MeshFilter>().mesh = mesh;
        MeshCollider mc = GetComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true; // importante si usarás físicas 3D
    }
}
