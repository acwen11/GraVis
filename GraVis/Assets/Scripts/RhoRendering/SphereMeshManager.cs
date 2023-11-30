using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SphereMeshManager : MonoBehaviour
{
    private int Subdivisions = 40;
    private int currentVertexOffset = 0;


    public Material mat;
    private float radius;
    private float Radius = 0.5f;

    // Create the mesh at the beginning
    void Start()
    {
        Generate();
    }

    struct PartMesh
    {
        public List<Vector3> vertices;
        public List<int> indices;

    }

    PartMesh GeneratePlane(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int subdivisions)
    {

        Vector3 stepsizeRow = (b - a) / (subdivisions + 1);
        Vector3 stepsizeCol = (c - a) / (subdivisions + 1);

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        // Create vertices
        for (int i = 0; i < subdivisions + 2; i++)
        {
            for (int j = 0; j < subdivisions + 2; j++)
            {
                vertices.Add(a + (j * stepsizeRow) + (i * stepsizeCol));
            }
        }
        // Connect vertices
        for (int i = 0; i < subdivisions + 1; i++)
        {
            for (int j = 0; j < subdivisions + 1; j++)
            {
                indices.Add(currentVertexOffset + (i + 1) + (j + 1) * (subdivisions + 2));
                indices.Add(currentVertexOffset + (i + 1) + j * (subdivisions + 2));
                indices.Add(currentVertexOffset + i + j * (subdivisions + 2));

                indices.Add(currentVertexOffset + i + j * (subdivisions + 2));
                indices.Add(currentVertexOffset + i + (j + 1) * (subdivisions + 2));
                indices.Add(currentVertexOffset + (i + 1) + (j + 1) * (subdivisions + 2));
            }
        }

        PartMesh outMesh = new PartMesh();
        outMesh.vertices = vertices;
        outMesh.indices = indices;

        return outMesh;
    }

    void Generate()
    {

        Mesh mesh = new Mesh();
        currentVertexOffset = 0;
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        // Generate Cube Points
        Vector3 A = new Vector3(0, 0, 0);
        Vector3 B = new Vector3(0, 0, 1);
        Vector3 C = new Vector3(0, 1, 0);
        Vector3 D = new Vector3(0, 1, 1);
        Vector3 E = new Vector3(1, 0, 0);
        Vector3 F = new Vector3(1, 0, 1);
        Vector3 G = new Vector3(1, 1, 0);
        Vector3 H = new Vector3(1, 1, 1);

        PartMesh M1 = GeneratePlane(C, D, A, B, Subdivisions);
        currentVertexOffset += M1.vertices.Count;
        PartMesh M2 = GeneratePlane(H, G, F, E, Subdivisions);
        currentVertexOffset += M2.vertices.Count;
        PartMesh M3 = GeneratePlane(D, H, B, F, Subdivisions);
        currentVertexOffset += M3.vertices.Count;
        PartMesh M4 = GeneratePlane(G, C, E, A, Subdivisions);
        currentVertexOffset += M4.vertices.Count;
        PartMesh M5 = GeneratePlane(A, B, E, F, Subdivisions);
        currentVertexOffset += M5.vertices.Count;
        PartMesh M6 = GeneratePlane(G, H, C, D, Subdivisions);

        vertices.AddRange(M1.vertices);
        indices.AddRange(M1.indices);
        vertices.AddRange(M2.vertices);
        indices.AddRange(M2.indices);
        vertices.AddRange(M3.vertices);
        indices.AddRange(M3.indices);
        vertices.AddRange(M4.vertices);
        indices.AddRange(M4.indices);
        vertices.AddRange(M5.vertices);
        indices.AddRange(M5.indices);
        vertices.AddRange(M6.vertices);
        indices.AddRange(M6.indices);


        Vector3 Mid = new Vector3(0.5f, 0.5f, 0.5f);
        // Project on sphere
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = (vertices[i] - Mid).normalized * Radius;
            normals.Add((vertices[i] - new Vector3(0.0f, 0.0f, 0.0f)).normalized);
        }


        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.normals = normals.ToArray();

        GetComponent<MeshRenderer>().material = mat;

        //mesh.RecalculateNormals();
        vertices.Clear();
        indices.Clear();
        normals.Clear();
        uv.Clear();
        if (this != null)
            GetComponent<MeshFilter>().sharedMesh = mesh;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
