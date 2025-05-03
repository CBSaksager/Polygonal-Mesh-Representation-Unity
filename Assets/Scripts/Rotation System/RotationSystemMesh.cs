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

    private int EstimateFaceCount()
    {
        // Euler's formula estimation can go here if needed
        return 0; // placeholder
    }

}

