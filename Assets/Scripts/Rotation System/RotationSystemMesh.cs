using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[System.Serializable]
public class RsMesh
{
    public List<RsVertex> vertices = new List<RsVertex>();
    public int FaceCount => EstimateFaceCount();

    public RsMesh() { }

    public static RsMesh CreateTetrahedron()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        RsMesh mesh = new RsMesh();

        mesh.vertices.Add(new RsVertex(new Vector3(1, 1, 1)));  // 0
        mesh.vertices.Add(new RsVertex(new Vector3(-1, -1, 1))); // 1
        mesh.vertices.Add(new RsVertex(new Vector3(-1, 1, -1))); // 2
        mesh.vertices.Add(new RsVertex(new Vector3(1, -1, -1))); // 3

        // Define cyclic neighbor lists (rotation systems)
        mesh.vertices[0].neighbors.AddRange(new int[] { 2, 3, 1 });
        mesh.vertices[1].neighbors.AddRange(new int[] { 0, 3, 2 });
        mesh.vertices[2].neighbors.AddRange(new int[] { 0, 1, 3 });
        mesh.vertices[3].neighbors.AddRange(new int[] { 0, 2, 1 });

        stopwatch.Stop();
        UnityEngine.Debug.Log($"Rotation System Tetrahedron created in {stopwatch.Elapsed.TotalMilliseconds:F4} ms");

        return mesh;
    }

    public List<RsVertex> SelectRandomFace()
    {
        /*
        This just selects a random vertex and its neighbors
        This does not form a face!!! 
        */
        if (vertices.Count < 3)
        {
            UnityEngine.Debug.LogError("Not enough vertices to select a face.");
            return null;
        }
        int randomIndex = Random.Range(0, vertices.Count);
        RsVertex selectedVertex = vertices[randomIndex];
        List<RsVertex> faceVertices = new List<RsVertex>
        {
            selectedVertex
        };

        GetCyclicNeighbors(selectedVertex).ForEach(v =>
        {
            faceVertices.Add(v);
        });

        return faceVertices;
    }

    public void SplitFace(RsMesh rsMesh, List<RsVertex> selectedFace)
    {
        if (selectedFace == null || selectedFace.Count < 3)
        {
            UnityEngine.Debug.LogWarning("Not enough vertices to split a face.");
            return;
        }

        // Get the vertex indices of the selected face
        int v1 = vertices.IndexOf(selectedFace[0]);
        int v2 = vertices.IndexOf(selectedFace[1]);
        int v3 = vertices.IndexOf(selectedFace[2]);

        // Check if the indices are valid
        if (v1 == -1 || v2 == -1 || v3 == -1)
        {
            UnityEngine.Debug.LogWarning("Invalid vertex indices for splitting the face.");
            return;
        }

        Vector3 posA = rsMesh.vertices[v1].position;
        Vector3 posB = rsMesh.vertices[v2].position;
        Vector3 posC = rsMesh.vertices[v3].position;

        // Step 1: Create the new center vertex at the face centroid
        Vector3 center = (posA + posB + posC) / 3f;
        int vIndex = rsMesh.vertices.Count;
        rsMesh.vertices.Add(new RsVertex(center));

        // Step 2: Update the new vertex’s neighbors (in correct order)
        rsMesh.vertices[vIndex].neighbors.AddRange(new int[] { v1, v2, v3 });

        // Step 3: Update the original vertices’ neighbor cycles to insert V
        InsertAfterNeighbor(rsMesh.vertices[v1], v3, vIndex);
        InsertAfterNeighbor(rsMesh.vertices[v2], v1, vIndex);
        InsertAfterNeighbor(rsMesh.vertices[v3], v2, vIndex);
    }

    private void InsertAfterNeighbor(RsVertex v, int after, int insert)
    {
        int i = v.neighbors.IndexOf(after);
        if (i != -1)
        {
            v.neighbors.Insert((i + 1) % (v.neighbors.Count + 1), insert);
        }
    }

    public List<RsVertex> GetCyclicNeighbors(RsVertex vertex)
    {
        List<RsVertex> cyclicNeighbors = new List<RsVertex>();
        int startIndex = vertices.IndexOf(vertex);
        if (startIndex == -1) return cyclicNeighbors;

        for (int i = 1; i < 3; i++)
        {
            int neighborIndex = vertex.neighbors[(startIndex + i) % vertex.neighbors.Count];
            cyclicNeighbors.Add(vertices[neighborIndex]);
        }
        return cyclicNeighbors;
    }

    private int EstimateFaceCount()
    {
        // Euler's formula estimation can go here if needed
        return 0; // placeholder
    }

}

