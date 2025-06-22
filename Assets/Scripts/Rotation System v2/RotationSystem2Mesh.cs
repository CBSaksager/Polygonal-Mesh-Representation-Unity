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

    /*
    From here and down is the code for creating some simple mesh shapes.
    */

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

    public static RSMesh CreateCube()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        RSMesh mesh = new RSMesh();

        // Define vertices of a dodecahedron
        mesh.vertices.Add(new RSVertex(new Vector3(1, 1, 1)));      // 0
        mesh.vertices.Add(new RSVertex(new Vector3(1, 1, -1)));     // 1
        mesh.vertices.Add(new RSVertex(new Vector3(-1, 1, -1)));    // 2
        mesh.vertices.Add(new RSVertex(new Vector3(-1, 1, 1)));     // 3

        mesh.vertices.Add(new RSVertex(new Vector3(1, -1, 1)));    // 4
        mesh.vertices.Add(new RSVertex(new Vector3(1, -1, -1)));     // 5
        mesh.vertices.Add(new RSVertex(new Vector3(-1, -1, -1)));    // 6
        mesh.vertices.Add(new RSVertex(new Vector3(-1, -1, 1)));   // 7

        // Define cyclic neighbor lists (rotation systems)
        mesh.vertices[0].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[0], mesh.vertices[1]), new RSEdge(mesh.vertices[0], mesh.vertices[3]), new RSEdge(mesh.vertices[0], mesh.vertices[4]) });
        mesh.vertices[1].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[1], mesh.vertices[0]), new RSEdge(mesh.vertices[1], mesh.vertices[2]), new RSEdge(mesh.vertices[1], mesh.vertices[5]) });
        mesh.vertices[2].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[2], mesh.vertices[1]), new RSEdge(mesh.vertices[2], mesh.vertices[3]), new RSEdge(mesh.vertices[2], mesh.vertices[6]) });
        mesh.vertices[3].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[3], mesh.vertices[0]), new RSEdge(mesh.vertices[3], mesh.vertices[2]), new RSEdge(mesh.vertices[3], mesh.vertices[7]) });
        mesh.vertices[4].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[4], mesh.vertices[0]), new RSEdge(mesh.vertices[4], mesh.vertices[5]), new RSEdge(mesh.vertices[4], mesh.vertices[7]) });
        mesh.vertices[5].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[5], mesh.vertices[1]), new RSEdge(mesh.vertices[5], mesh.vertices[4]), new RSEdge(mesh.vertices[5], mesh.vertices[6]) });
        mesh.vertices[6].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[6], mesh.vertices[2]), new RSEdge(mesh.vertices[6], mesh.vertices[5]), new RSEdge(mesh.vertices[6], mesh.vertices[7]) });
        mesh.vertices[7].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[7], mesh.vertices[3]), new RSEdge(mesh.vertices[7], mesh.vertices[4]), new RSEdge(mesh.vertices[7], mesh.vertices[6]) });

        // Define faces based on the PLY file data
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[0], mesh.vertices[1], mesh.vertices[2], mesh.vertices[3] })); // Face 0
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[0], mesh.vertices[1], mesh.vertices[5], mesh.vertices[4] })); // Face 1
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[1], mesh.vertices[2], mesh.vertices[6], mesh.vertices[5] })); // Face 2
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[2], mesh.vertices[3], mesh.vertices[7], mesh.vertices[6] })); // Face 3
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[3], mesh.vertices[0], mesh.vertices[4], mesh.vertices[7] })); // Face 4
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[4], mesh.vertices[5], mesh.vertices[6], mesh.vertices[7] })); // Face 5

        // Assign faces to vertices
        foreach (var face in mesh.faces)
        {
            foreach (var vertex in face.vertices)
            {
                vertex.faces.Add(face);
            }
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log($"Rotation System Dodecahedron created in {stopwatch.Elapsed.TotalMilliseconds:F4} ms");

        return mesh;
    }

    public static RSMesh CreateDodecahedron()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        RSMesh mesh = new RSMesh();

        // Define vertices of a dodecahedron
        mesh.vertices.Add(new RSVertex(new Vector3(-0.57735f, -0.57735f, 0.57735f)));       // 0
        mesh.vertices.Add(new RSVertex(new Vector3(0.934172f, 0.356822f, 0)));              // 1
        mesh.vertices.Add(new RSVertex(new Vector3(0.934172f, -0.356822f, 0)));             // 2
        mesh.vertices.Add(new RSVertex(new Vector3(-0.934172f, 0.356822f, 0)));             // 3
        mesh.vertices.Add(new RSVertex(new Vector3(-0.934172f, -0.356822f, 0)));            // 4
        mesh.vertices.Add(new RSVertex(new Vector3(0, 0.934172f, 0.356822f)));              // 5
        mesh.vertices.Add(new RSVertex(new Vector3(0, 0.934172f, -0.356822f)));             // 6
        mesh.vertices.Add(new RSVertex(new Vector3(0.356822f, 0, -0.934172f)));            // 7
        mesh.vertices.Add(new RSVertex(new Vector3(-0.356822f, 0, -0.934172f)));             // 8 //
        mesh.vertices.Add(new RSVertex(new Vector3(0, -0.934172f, -0.356822f)));            // 9
        mesh.vertices.Add(new RSVertex(new Vector3(0, -0.934172f, 0.356822f)));             // 10
        mesh.vertices.Add(new RSVertex(new Vector3(0.356822f, 0, 0.934172f)));              // 11
        mesh.vertices.Add(new RSVertex(new Vector3(-0.356822f, 0, 0.934172f)));             // 12 //
        mesh.vertices.Add(new RSVertex(new Vector3(0.57735f, 0.57735f, -0.57735f)));        // 13
        mesh.vertices.Add(new RSVertex(new Vector3(0.57735f, 0.57735f, 0.57735f)));         // 14
        mesh.vertices.Add(new RSVertex(new Vector3(-0.57735f, 0.57735f, -0.57735f)));       // 15
        mesh.vertices.Add(new RSVertex(new Vector3(-0.57735f, 0.57735f, 0.57735f)));        // 16
        mesh.vertices.Add(new RSVertex(new Vector3(0.57735f, -0.57735f, -0.57735f)));       // 17
        mesh.vertices.Add(new RSVertex(new Vector3(0.57735f, -0.57735f, 0.57735f)));        // 18
        mesh.vertices.Add(new RSVertex(new Vector3(-0.57735f, -0.57735f, -0.57735f)));      // 19

        // Define cyclic neighbor lists (rotation systems)
        mesh.vertices[0].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[0], mesh.vertices[4]), new RSEdge(mesh.vertices[0], mesh.vertices[10]), new RSEdge(mesh.vertices[0], mesh.vertices[12]) });
        mesh.vertices[1].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[1], mesh.vertices[2]), new RSEdge(mesh.vertices[1], mesh.vertices[13]), new RSEdge(mesh.vertices[1], mesh.vertices[14]) });
        mesh.vertices[2].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[2], mesh.vertices[18]), new RSEdge(mesh.vertices[2], mesh.vertices[1]), new RSEdge(mesh.vertices[2], mesh.vertices[17]) });
        mesh.vertices[3].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[3], mesh.vertices[4]), new RSEdge(mesh.vertices[3], mesh.vertices[16]), new RSEdge(mesh.vertices[3], mesh.vertices[15]) });
        mesh.vertices[4].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[4], mesh.vertices[19]), new RSEdge(mesh.vertices[4], mesh.vertices[3]), new RSEdge(mesh.vertices[4], mesh.vertices[0]) });
        mesh.vertices[5].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[5], mesh.vertices[6]), new RSEdge(mesh.vertices[5], mesh.vertices[14]), new RSEdge(mesh.vertices[5], mesh.vertices[16]) });
        mesh.vertices[6].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[6], mesh.vertices[5]), new RSEdge(mesh.vertices[6], mesh.vertices[13]), new RSEdge(mesh.vertices[6], mesh.vertices[15]) });
        mesh.vertices[7].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[7], mesh.vertices[17]), new RSEdge(mesh.vertices[7], mesh.vertices[8]), new RSEdge(mesh.vertices[7], mesh.vertices[13]) });
        mesh.vertices[8].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[8], mesh.vertices[15]), new RSEdge(mesh.vertices[8], mesh.vertices[19]), new RSEdge(mesh.vertices[8], mesh.vertices[7]) });
        mesh.vertices[9].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[9], mesh.vertices[10]), new RSEdge(mesh.vertices[9], mesh.vertices[19]), new RSEdge(mesh.vertices[9], mesh.vertices[17]) });
        mesh.vertices[10].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[10], mesh.vertices[18]), new RSEdge(mesh.vertices[10], mesh.vertices[9]), new RSEdge(mesh.vertices[10], mesh.vertices[0]) });
        mesh.vertices[11].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[11], mesh.vertices[14]), new RSEdge(mesh.vertices[11], mesh.vertices[12]), new RSEdge(mesh.vertices[11], mesh.vertices[18]) });
        mesh.vertices[12].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[12], mesh.vertices[0]), new RSEdge(mesh.vertices[12], mesh.vertices[16]), new RSEdge(mesh.vertices[12], mesh.vertices[11]) });
        mesh.vertices[13].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[13], mesh.vertices[7]), new RSEdge(mesh.vertices[13], mesh.vertices[1]), new RSEdge(mesh.vertices[13], mesh.vertices[6]) });
        mesh.vertices[14].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[14], mesh.vertices[1]), new RSEdge(mesh.vertices[14], mesh.vertices[5]), new RSEdge(mesh.vertices[14], mesh.vertices[11]) });
        mesh.vertices[15].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[15], mesh.vertices[3]), new RSEdge(mesh.vertices[15], mesh.vertices[6]), new RSEdge(mesh.vertices[15], mesh.vertices[8]) });
        mesh.vertices[16].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[16], mesh.vertices[12]), new RSEdge(mesh.vertices[16], mesh.vertices[3]), new RSEdge(mesh.vertices[16], mesh.vertices[5]) });
        mesh.vertices[17].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[17], mesh.vertices[2]), new RSEdge(mesh.vertices[17], mesh.vertices[9]), new RSEdge(mesh.vertices[17], mesh.vertices[7]) });
        mesh.vertices[18].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[18], mesh.vertices[11]), new RSEdge(mesh.vertices[18], mesh.vertices[2]), new RSEdge(mesh.vertices[18], mesh.vertices[10]) });
        mesh.vertices[19].edges.AddRange(new List<RSEdge> { new RSEdge(mesh.vertices[19], mesh.vertices[8]), new RSEdge(mesh.vertices[19], mesh.vertices[4]), new RSEdge(mesh.vertices[19], mesh.vertices[9]) });

        // Define faces based on the PLY file data
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[1], mesh.vertices[2], mesh.vertices[18], mesh.vertices[11], mesh.vertices[14] })); // Face 0
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[1], mesh.vertices[13], mesh.vertices[7], mesh.vertices[17], mesh.vertices[2] })); // Face 1
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[3], mesh.vertices[4], mesh.vertices[19], mesh.vertices[8], mesh.vertices[15] })); // Face 2
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[3], mesh.vertices[16], mesh.vertices[12], mesh.vertices[0], mesh.vertices[4] })); // Face 3
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[3], mesh.vertices[15], mesh.vertices[6], mesh.vertices[5], mesh.vertices[16] })); // Face 4
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[1], mesh.vertices[14], mesh.vertices[5], mesh.vertices[6], mesh.vertices[13] })); // Face 5
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[2], mesh.vertices[17], mesh.vertices[9], mesh.vertices[10], mesh.vertices[18] })); // Face 6
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[4], mesh.vertices[0], mesh.vertices[10], mesh.vertices[9], mesh.vertices[19] })); // Face 7
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[7], mesh.vertices[8], mesh.vertices[19], mesh.vertices[9], mesh.vertices[17] })); // Face 8
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[6], mesh.vertices[15], mesh.vertices[8], mesh.vertices[7], mesh.vertices[13] })); // Face 9
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[5], mesh.vertices[14], mesh.vertices[11], mesh.vertices[12], mesh.vertices[16] })); // Face 10
        mesh.faces.Add(new RSFace(new List<RSVertex> { mesh.vertices[10], mesh.vertices[0], mesh.vertices[12], mesh.vertices[11], mesh.vertices[18] })); // Face 11

        // Assign faces to vertices
        foreach (var face in mesh.faces)
        {
            foreach (var vertex in face.vertices)
            {
                vertex.faces.Add(face);
            }
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Rotation System Dodecahedron created in {stopwatch.Elapsed.TotalMilliseconds:F4} ms");
        return mesh;
    }
}