using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[System.Serializable]
public class RSMesh
{
    public List<RSVertex> vertices = new List<RSVertex>();
    public List<RSFace> faces = new List<RSFace>();

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

    public void SplitFace(RSFace face)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // Step 1: Create a new vertex at the centroid of the face
        Vector3 centroid = Vector3.zero;
        foreach (var vertex in face.vertices)
        {
            centroid += vertex.position;
        }
        centroid /= face.vertices.Count;
        RSVertex newVertex = new RSVertex(centroid);
        vertices.Add(newVertex);

        // Step 2: Create new edges connecting the new vertex to each vertex of the face and maintain cyclic order
        for (int i = 0; i < face.vertices.Count; i++)
        {
            RSVertex currentVertex = face.vertices[i];
            RSVertex nextVertex = face.vertices[(i + 1) % face.vertices.Count]; // Next vertex in the face

            // Create a new edge from the current vertex to the center
            RSEdge newEdge = new RSEdge(currentVertex, newVertex);

            // Find the edge from current vertex to next vertex in the face
            int edgeToNextIndex = -1;
            for (int j = 0; j < currentVertex.edges.Count; j++)
            {
                if (currentVertex.edges[j].to == nextVertex)
                {
                    edgeToNextIndex = j;
                    break;
                }
            }

            if (edgeToNextIndex != -1)
            {
                // Insert the new edge right before the edge to the next vertex
                // This maintains the proper face traversal order
                currentVertex.edges.Insert(edgeToNextIndex, newEdge);
            }
            else
            {
                // Fallback if we can't find the edge (shouldn't happen in a well-formed mesh)
                UnityEngine.Debug.LogWarning("Could not find edge to next vertex when splitting face");
                currentVertex.edges.Add(newEdge);
            }

            // Add the corresponding edge to the new center vertex
            newVertex.edges.Add(new RSEdge(newVertex, currentVertex));
        }

        // Step 3: Create new triangular faces
        for (int i = 0; i < face.vertices.Count; i++)
        {
            RSVertex currentVertex = face.vertices[i];
            RSVertex nextVertex = face.vertices[(i + 1) % face.vertices.Count];

            // Create a new triangular face
            List<RSVertex> newFaceVertices = new List<RSVertex> { currentVertex, nextVertex, newVertex };
            RSFace newFace = new RSFace(newFaceVertices);

            // Add the new face to the mesh
            faces.Add(newFace);

            // Update the faces lists of the vertices
            currentVertex.faces.Add(newFace);
            nextVertex.faces.Add(newFace);
            newVertex.faces.Add(newFace);
        }

        // Step 4: Remove the old face
        // Remove the face from each vertex's faces list
        foreach (var vertex in face.vertices)
        {
            vertex.faces.Remove(face);
        }

        // Remove the face from the mesh
        faces.Remove(face);

        stopwatch.Stop();
        UnityEngine.Debug.Log($"Rotation System Face Split completed in {stopwatch.Elapsed.TotalMilliseconds:F4} ms");

        return;
    }

    public static RSMesh CreateTetrahedron()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        RSMesh mesh = new RSMesh();

        // Define vertices of a tetrahedron
        mesh.vertices.Add(new RSVertex(new Vector3(1, 1, 1)));  // 0
        mesh.vertices.Add(new RSVertex(new Vector3(-1, -1, 1))); // 1
        mesh.vertices.Add(new RSVertex(new Vector3(-1, 1, -1))); // 2
        mesh.vertices.Add(new RSVertex(new Vector3(1, -1, -1))); // 3

        // Define cyclic neighbor lists (rotation systems)
        mesh.vertices[0].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[0], mesh.vertices[2]), new RSEdge(mesh.vertices[0], mesh.vertices[3]), new RSEdge(mesh.vertices[0], mesh.vertices[1]) });
        mesh.vertices[1].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[1], mesh.vertices[0]), new RSEdge(mesh.vertices[1], mesh.vertices[3]), new RSEdge(mesh.vertices[1], mesh.vertices[2]) });
        mesh.vertices[2].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[2], mesh.vertices[0]), new RSEdge(mesh.vertices[2], mesh.vertices[1]), new RSEdge(mesh.vertices[2], mesh.vertices[3]) });
        mesh.vertices[3].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[3], mesh.vertices[0]), new RSEdge(mesh.vertices[3], mesh.vertices[2]), new RSEdge(mesh.vertices[3], mesh.vertices[1]) });

        // Define faces
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[0], mesh.vertices[1], mesh.vertices[2] })); // Face 0
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[0], mesh.vertices[1], mesh.vertices[3] })); // Face 1
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[0], mesh.vertices[2], mesh.vertices[3] })); // Face 2
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[1], mesh.vertices[2], mesh.vertices[3] })); // Face 3

        // Assign faces to vertices
        foreach (var face in mesh.faces)
        {
            foreach (var vertex in face.vertices)
            {
                vertex.faces.Add(face);
            }
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log($"Rotation System Tetrahedron created in {stopwatch.Elapsed.TotalMilliseconds:F4} ms");

        return mesh;
    }
}