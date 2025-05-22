using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[System.Serializable]
public class RSMesh
{
    public List<RSVertex> vertices = new List<RSVertex>();

    public RSMesh() { }

    // Traversal Functions:
    public RSEdge Iota(RSEdge edge)
    {
        // Find the existing edge from 'to' vertex to 'from' vertex
        foreach (RSEdge oppositeEdge in edge.to.edges)
        {
            if (oppositeEdge.to == edge.from)
            {
                return oppositeEdge;
            }
        }

        // Fallback in case the mesh structure is incomplete
        UnityEngine.Debug.LogWarning("Could not find existing opposite edge - creating new one");
        return new RSEdge(edge.to, edge.from);
    }

    // Rho: returns the next edge in the cyclic order around 'from' vertex
    public RSEdge Rho(RSEdge edge)
    {
        // Find the position of the current edge in the 'from' vertex's edge list
        int edgeIdx = -1;
        for (int i = 0; i < edge.from.edges.Count; i++)
        {
            if (edge.from.edges[i].to == edge.to)
            {
                edgeIdx = i;
                break;
            }
        }

        if (edgeIdx == -1)
        {
            UnityEngine.Debug.LogError("Edge not found in vertex's edge list");
            return edge; // Return original edge if not found
        }

        // Get the next edge in the cyclic order (wrap around)
        int nextIdx = (edgeIdx + 1) % edge.from.edges.Count;
        return edge.from.edges[nextIdx];
    }

    // Tau: returns the next edge in the face cycle
    public RSEdge Tau(RSEdge edge)
    {
        // Tau = Rho ∘ Iota
        return Rho(Iota(edge));
    }



    public RSEdge SelectRandomEdge()
    {
        if (vertices.Count == 0)
            return null;

        // Select a random vertex
        RSVertex randomVertex = vertices[Random.Range(0, vertices.Count)];

        // Select a random edge from the vertex's edge list
        if (randomVertex.edges.Count == 0)
            return null;

        RSEdge selectedEdge = randomVertex.edges[Random.Range(0, randomVertex.edges.Count)];
        return selectedEdge;
    }

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
        mesh.vertices[0].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[0], mesh.vertices[2]), new RSEdge(mesh.vertices[0], mesh.vertices[3]), new RSEdge(mesh.vertices[0], mesh.vertices[1]) });
        mesh.vertices[1].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[1], mesh.vertices[0]), new RSEdge(mesh.vertices[1], mesh.vertices[3]), new RSEdge(mesh.vertices[1], mesh.vertices[2]) });
        mesh.vertices[2].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[2], mesh.vertices[0]), new RSEdge(mesh.vertices[2], mesh.vertices[1]), new RSEdge(mesh.vertices[2], mesh.vertices[3]) });
        mesh.vertices[3].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[3], mesh.vertices[0]), new RSEdge(mesh.vertices[3], mesh.vertices[2]), new RSEdge(mesh.vertices[3], mesh.vertices[1]) });

        stopwatch.Stop();
        UnityEngine.Debug.Log($"Rotation System Tetrahedron created in {stopwatch.Elapsed.TotalMilliseconds:F4} ms");

        return mesh;
    }
}