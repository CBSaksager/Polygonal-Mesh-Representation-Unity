using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[System.Serializable]
public class RSMesh
{
    public List<RSVertex> vertices = new List<RSVertex>();

    public RSMesh() { }

    public static RSMesh CreateTetrahedron()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        RSMesh mesh = new RSMesh();

        mesh.vertices.Add(new RSVertex(new Vector3(1, 1, 1)));  // 0
        mesh.vertices.Add(new RSVertex(new Vector3(-1, -1, 1))); // 1
        mesh.vertices.Add(new RSVertex(new Vector3(-1, 1, -1))); // 2
        mesh.vertices.Add(new RSVertex(new Vector3(1, -1, -1))); // 3

        // Define cyclic neighbor lists (rotation systems)
        mesh.vertices[0].edges.AddRange(new List<RSEdge> { new RSEdge { from = mesh.vertices[0], to = mesh.vertices[2] }, new RSEdge { from = mesh.vertices[0], to = mesh.vertices[3] }, new RSEdge { from = mesh.vertices[0], to = mesh.vertices[1] } });
        mesh.vertices[1].edges.AddRange(new List<RSEdge> { new RSEdge { from = mesh.vertices[1], to = mesh.vertices[0] }, new RSEdge { from = mesh.vertices[1], to = mesh.vertices[3] }, new RSEdge { from = mesh.vertices[1], to = mesh.vertices[2] } });
        mesh.vertices[2].edges.AddRange(new List<RSEdge> { new RSEdge { from = mesh.vertices[2], to = mesh.vertices[0] }, new RSEdge { from = mesh.vertices[2], to = mesh.vertices[1] }, new RSEdge { from = mesh.vertices[2], to = mesh.vertices[3] } });
        mesh.vertices[3].edges.AddRange(new List<RSEdge> { new RSEdge { from = mesh.vertices[3], to = mesh.vertices[0] }, new RSEdge { from = mesh.vertices[3], to = mesh.vertices[2] }, new RSEdge { from = mesh.vertices[3], to = mesh.vertices[1] } });

        stopwatch.Stop();
        UnityEngine.Debug.Log($"Rotation System Tetrahedron created in {stopwatch.Elapsed.TotalMilliseconds:F4} ms");

        return mesh;
    }
}