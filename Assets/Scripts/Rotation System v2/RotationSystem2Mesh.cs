using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.IO;
using System.Globalization;
using System.Linq;

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

        // Create temporary lists to store new faces and edges
        List<RSFace> newFaces = new List<RSFace>();
        List<RSEdge> centroidEdges = new List<RSEdge>();

        // Step 2: Create new triangular faces
        for (int i = 0; i < face.vertices.Count; i++)
        {
            RSVertex currentVertex = face.vertices[i];
            RSVertex nextVertex = face.vertices[(i + 1) % face.vertices.Count];

            // Create new edges from the vertices to the new center vertex
            RSEdge edgeFromCurrent = new RSEdge(currentVertex, newVertex);
            RSEdge edgeFromNext = new RSEdge(nextVertex, newVertex);
            RSEdge edgeFromCenter = new RSEdge(newVertex, currentVertex);
            RSEdge edgeFromCenterToNext = new RSEdge(newVertex, nextVertex);

            // Find the existing edge between current and next
            RSEdge existingEdge = null;
            foreach (var edge in currentVertex.edges)
            {
                if (edge.to == nextVertex)
                {
                    existingEdge = edge;
                    break;
                }
            }

            // Create a new triangular face
            List<RSVertex> newFaceVertices = new List<RSVertex> { currentVertex, nextVertex, newVertex };
            RSFace newFace = new RSFace(newFaceVertices);
            newFaces.Add(newFace);

            // Add the new face to vertices' face lists
            currentVertex.faces.Add(newFace);
            nextVertex.faces.Add(newFace);
            newVertex.faces.Add(newFace);

            // Store the edge from the center to current vertex to organize them later
            centroidEdges.Add(edgeFromCenter);

            // Carefully insert the new edge into the current vertex's edge list
            if (existingEdge != null)
            {
                // Find the position of the existing edge
                int edgePos = currentVertex.edges.IndexOf(existingEdge);

                // Insert the new edge right before the existing edge
                // This maintains proper counter-clockwise ordering
                currentVertex.edges.Insert(edgePos, edgeFromCurrent);
            }
            else
            {
                // If we can't find the existing edge (shouldn't happen in valid mesh)
                currentVertex.edges.Add(edgeFromCurrent);
            }
        }

        // Add all new faces to the mesh
        faces.AddRange(newFaces);

        // Handle the order of edges for the new center vertex
        // The edges should be in the opposite order of the original face vertices
        // This ensures proper traversal with Tau
        for (int i = 0; i < face.vertices.Count; i++)
        {
            // Add in reverse order to ensure correct traversal
            newVertex.edges.Add(centroidEdges[face.vertices.Count - 1 - i]);
        }

        // Step 4: Remove the old face
        // Remove the face from each vertex's faces list
        foreach (var vertex in face.vertices)
        {
            vertex.faces.Remove(face);
        }

        // Remove the face from the mesh
        faces.Remove(face);

        // Final validation to ensure proper face traversal
        // This handles any edge cases or complex geometries
        ValidateAndFixSplitFaces(newVertex, newFaces);

        stopwatch.Stop();
        UnityEngine.Debug.Log($"Rotation System Face Split completed in {stopwatch.Elapsed.TotalMilliseconds:F4} ms");
    }

    private void ValidateAndFixSplitFaces(RSVertex centerVertex, List<RSFace> newFaces)
    {
        // For each new face, ensure that Tau traverses the face correctly
        foreach (var face in newFaces)
        {
            bool faceValid = true;

            // Test each edge in the face to ensure proper traversal
            for (int i = 0; i < face.vertices.Count; i++)
            {
                var v1 = face.vertices[i];
                var v2 = face.vertices[(i + 1) % face.vertices.Count];
                var v3 = face.vertices[(i + 2) % face.vertices.Count];

                // Find edge v1->v2
                RSEdge edge = null;
                foreach (var e in v1.edges)
                {
                    if (e.to == v2)
                    {
                        edge = e;
                        break;
                    }
                }

                if (edge != null)
                {
                    // Apply Tau and check if it gets to v3
                    var tauEdge = Tau(edge);
                    if (tauEdge.to != v3)
                    {
                        faceValid = false;

                        // Fix the edge order
                        UnityEngine.Debug.LogWarning("Face traversal error detected, fixing edge order.");
                        ReorderEdgesForFace(v2, Iota(edge), v3);
                    }
                }
            }

            if (!faceValid)
            {
                UnityEngine.Debug.LogWarning("Fixed traversal order for a new face after split");
            }
        }

        // Recheck the ordering of edges around the new center vertex
        if (centerVertex.faces.Count > 0 && centerVertex.edges.Count > 0)
        {
            // Build a map of each edge and which face it belongs to
            Dictionary<RSEdge, RSFace> edgeToFace = new Dictionary<RSEdge, RSFace>();

            foreach (var face in centerVertex.faces)
            {
                for (int i = 0; i < face.vertices.Count - 1; i++)
                {
                    if (face.vertices[i] == centerVertex)
                    {
                        // Find edge to next vertex in face
                        foreach (var edge in centerVertex.edges)
                        {
                            if (edge.to == face.vertices[i + 1])
                            {
                                edgeToFace[edge] = face;
                                break;
                            }
                        }
                    }
                }
            }

            // Reorder the edges if necessary to maintain proper face traversal
            List<RSEdge> orderedEdges = new List<RSEdge>();
            RSFace currentFace = centerVertex.faces[0];

            // Start with an edge from the first face
            RSEdge startEdge = null;
            foreach (var edge in centerVertex.edges)
            {
                if (edgeToFace.ContainsKey(edge) && edgeToFace[edge] == currentFace)
                {
                    startEdge = edge;
                    break;
                }
            }

            if (startEdge != null)
            {
                orderedEdges.Add(startEdge);
                currentFace = edgeToFace[startEdge];

                // Now follow the faces around the vertex
                while (orderedEdges.Count < centerVertex.edges.Count)
                {
                    // Find the next face that shares an edge with the current face
                    bool foundNextEdge = false;

                    foreach (var face in centerVertex.faces)
                    {
                        if (face != currentFace)
                        {
                            // Check if this face shares a vertex with current face
                            foreach (var edge in centerVertex.edges)
                            {
                                if (!orderedEdges.Contains(edge) && edgeToFace.ContainsKey(edge) && edgeToFace[edge] == face)
                                {
                                    orderedEdges.Add(edge);
                                    currentFace = face;
                                    foundNextEdge = true;
                                    break;
                                }
                            }

                            if (foundNextEdge) break;
                        }
                    }

                    // If we can't find a connected face, just add any remaining edges
                    if (!foundNextEdge)
                    {
                        foreach (var edge in centerVertex.edges)
                        {
                            if (!orderedEdges.Contains(edge))
                            {
                                orderedEdges.Add(edge);
                                // Don't update currentFace as we're just filling in gaps
                            }
                        }
                    }
                }

                // Replace the edges list with our ordered version
                centerVertex.edges = orderedEdges;
            }
        }
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

    public static RSMesh ImportFromPLY(string filePath)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        RSMesh mesh = new RSMesh();

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            int vertexCount = 0;
            int faceCount = 0;
            int headerEndIndex = 0;

            // Parse header
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("element vertex"))
                {
                    vertexCount = int.Parse(line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)[2]);
                }
                else if (line.StartsWith("element face"))
                {
                    faceCount = int.Parse(line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)[2]);
                }
                else if (line == "end_header")
                {
                    headerEndIndex = i + 1;
                    break;
                }
            }

            // Read vertices
            for (int i = 0; i < vertexCount; i++)
            {
                string[] components = lines[headerEndIndex + i].Trim().Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                float x = float.Parse(components[0], CultureInfo.InvariantCulture);
                float y = float.Parse(components[1], CultureInfo.InvariantCulture);
                float z = float.Parse(components[2], CultureInfo.InvariantCulture);

                mesh.vertices.Add(new RSVertex(new Vector3(x, y, z)));
            }

            // Read faces and establish connectivity
            for (int i = 0; i < faceCount; i++)
            {
                string[] components = lines[headerEndIndex + vertexCount + i].Trim().Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                int vertexPerFace = int.Parse(components[0]);

                List<RSVertex> faceVertices = new List<RSVertex>();
                for (int j = 0; j < vertexPerFace; j++)
                {
                    int vertexIndex = int.Parse(components[j + 1]);
                    faceVertices.Add(mesh.vertices[vertexIndex]);
                }

                // Create face
                RSFace face = new RSFace(faceVertices);
                mesh.faces.Add(face);

                // Add face to vertices
                foreach (var vertex in faceVertices)
                {
                    vertex.faces.Add(face);
                }

                // Create edges between consecutive vertices
                for (int j = 0; j < vertexPerFace; j++)
                {
                    RSVertex current = faceVertices[j];
                    RSVertex next = faceVertices[(j + 1) % vertexPerFace];

                    // Check if edge already exists
                    bool edgeExists = false;
                    foreach (var edge in current.edges)
                    {
                        if (edge.to == next)
                        {
                            edgeExists = true;
                            break;
                        }
                    }

                    if (!edgeExists)
                    {
                        // Add edge in proper position to maintain rotation system
                        current.edges.Add(new RSEdge(current, next));

                        // Also add the opposite edge
                        next.edges.Add(new RSEdge(next, current));
                    }
                }
            }

            // Organize edges to maintain proper rotation system order
            OrganizeEdges(mesh);

            stopwatch.Stop();
            UnityEngine.Debug.Log($"PLY Import completed in {stopwatch.Elapsed.TotalMilliseconds:F4} ms with {mesh.vertices.Count} vertices, {mesh.faces.Count} faces");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error importing PLY file: {e.Message}");
            return null;
        }

        return mesh;
    }

    private static void OrganizeEdges(RSMesh mesh)
    {
        // For each vertex, organize its edges in counter-clockwise order
        foreach (var vertex in mesh.vertices)
        {
            if (vertex.faces.Count == 0 || vertex.edges.Count <= 1) continue;

            // Step 1: Compute a local coordinate system
            // Find an approximate normal vector at the vertex by averaging face normals
            Vector3 normal = Vector3.zero;
            foreach (var face in vertex.faces)
            {
                Vector3 faceNormal = ComputeFaceNormal(face);
                normal += faceNormal;
            }
            normal.Normalize();

            if (normal.magnitude < 0.001f)
            {
                // Fallback if we can't determine a good normal
                normal = Vector3.up;
            }

            // Step 2: Choose a reference vector perpendicular to the normal
            Vector3 reference = Vector3.Cross(normal, (normal.normalized != Vector3.up) ? Vector3.up : Vector3.forward);
            reference.Normalize();

            // Step 3: Create a tangent plane basis
            Vector3 tangent = reference;
            Vector3 bitangent = Vector3.Cross(normal, tangent);
            bitangent.Normalize();

            // Step 4: Calculate angles for each edge on the tangent plane
            Dictionary<RSEdge, float> edgeAngles = new Dictionary<RSEdge, float>();

            foreach (var edge in vertex.edges)
            {
                Vector3 direction = (edge.to.position - vertex.position).normalized;

                // Project the direction onto the tangent plane
                Vector3 projection = direction - Vector3.Dot(direction, normal) * normal;
                projection.Normalize();

                // Calculate the angle in the tangent plane
                float angle = Mathf.Atan2(
                    Vector3.Dot(projection, bitangent),
                    Vector3.Dot(projection, tangent)
                );

                // Convert to degrees and ensure positive angles (0-360)
                angle = angle * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                edgeAngles[edge] = angle;
            }

            // Step 5: Sort edges by their angle (counter-clockwise order)
            vertex.edges = vertex.edges.OrderBy(edge => edgeAngles[edge]).ToList();
        }

        // Final verification pass to make sure faces can be properly traversed
        VerifyFaceTraversal(mesh);
    }

    private static Vector3 ComputeFaceNormal(RSFace face)
    {
        if (face.vertices.Count < 3) return Vector3.up;

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < face.vertices.Count; i++)
        {
            Vector3 v1 = face.vertices[i].position;
            Vector3 v2 = face.vertices[(i + 1) % face.vertices.Count].position;
            Vector3 v3 = face.vertices[(i + 2) % face.vertices.Count].position;

            Vector3 crossProduct = Vector3.Cross(v2 - v1, v3 - v1);
            sum += crossProduct;
        }

        return sum.normalized;
    }

    private static void VerifyFaceTraversal(RSMesh mesh)
    {
        foreach (var face in mesh.faces)
        {
            // For each face, verify that we can properly traverse it using Tau
            for (int i = 0; i < face.vertices.Count; i++)
            {
                RSVertex current = face.vertices[i];
                RSVertex next = face.vertices[(i + 1) % face.vertices.Count];

                // Find the edge from current to next
                RSEdge edge = null;
                foreach (var e in current.edges)
                {
                    if (e.to == next)
                    {
                        edge = e;
                        break;
                    }
                }

                if (edge != null)
                {
                    // Apply Tau operation
                    RSEdge iota = null;
                    foreach (var e in next.edges)
                    {
                        if (e.to == current)
                        {
                            iota = e;
                            break;
                        }
                    }

                    if (iota != null)
                    {
                        // Find the next edge in next's rotation system
                        int edgeIdx = -1;
                        for (int j = 0; j < next.edges.Count; j++)
                        {
                            if (next.edges[j] == iota)
                            {
                                edgeIdx = j;
                                break;
                            }
                        }

                        if (edgeIdx != -1)
                        {
                            int nextIdx = (edgeIdx + 1) % next.edges.Count;
                            RSEdge tau = next.edges[nextIdx];

                            // Check if Tau leads to the correct next vertex in the face
                            RSVertex expected = face.vertices[(i + 2) % face.vertices.Count];
                            if (tau.to != expected)
                            {
                                // If not, try to fix the ordering for this vertex
                                ReorderEdgesForFace(next, iota, expected);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void ReorderEdgesForFace(RSVertex vertex, RSEdge fromEdge, RSVertex targetVertex)
    {
        // Find the edge to the target vertex
        RSEdge targetEdge = null;
        foreach (var edge in vertex.edges)
        {
            if (edge.to == targetVertex)
            {
                targetEdge = edge;
                break;
            }
        }

        if (targetEdge != null)
        {
            // Remove the target edge from its current position
            vertex.edges.Remove(targetEdge);

            // Find the position of the fromEdge
            int fromEdgeIdx = vertex.edges.IndexOf(fromEdge);

            // Insert the target edge after the fromEdge
            if (fromEdgeIdx != -1)
            {
                vertex.edges.Insert(fromEdgeIdx + 1, targetEdge);
            }
            else
            {
                // Fallback if fromEdge not found
                vertex.edges.Add(targetEdge);
            }
        }
    }
}