using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class HalfEdgeMesh
{
    public List<HEVertex> vertices = new List<HEVertex>();
    public List<HEHalfEdge> halfEdges = new List<HEHalfEdge>();
    public List<HEFace> faces = new List<HEFace>();

    // Internal mapping to track edge pairs for twin linking. Used for stitching edges together.
    private Dictionary<(int, int), HEHalfEdge> edgeMap = new Dictionary<(int, int), HEHalfEdge>();

    public HEVertex AddVertex(Vector3 position)
    {
        var vertex = new HEVertex(position);
        vertices.Add(vertex);
        return vertex;
    }

    public void AddHalfEdge(HEVertex v1, HEVertex v2, HEFace sharedFace)
    {
        // Verify the vertices are in the face
        List<HEVertex> faceVerts = VerticesOfFace(sharedFace);
        if (!faceVerts.Contains(v1) || !faceVerts.Contains(v2))
        {
            UnityEngine.Debug.LogError("Vertices not found on the specified face.");
            return;
        }

        // Find the half-edges before v1 and v2 in the face loop
        HEHalfEdge e1Prev = null; // Half-edge before v1
        HEHalfEdge e2Prev = null; // Half-edge before v2

        HEHalfEdge current = sharedFace.edge;
        do
        {
            if (current.next.vertex == v1)
            {
                e1Prev = current;
            }
            if (current.next.vertex == v2)
            {
                e2Prev = current;
            }
            current = current.next;
        } while (current != sharedFace.edge);

        // Get the half-edges starting from v1 and v2
        HEHalfEdge e1Next = e1Prev.next;  // Half-edge starting from v1
        HEHalfEdge e2Next = e2Prev.next;  // Half-edge starting from v2

        // 1. Create the new half-edges
        HEHalfEdge he1 = new HEHalfEdge
        {
            vertex = v2,
            origin = v1
        };

        HEHalfEdge he2 = new HEHalfEdge
        {
            vertex = v1,
            origin = v2
        };

        // Set up twin relationship
        he1.twin = he2;
        he2.twin = he1;

        // 2. Update the next and prev pointers
        he1.next = e2Next;
        e1Prev.next = he1;

        he2.next = e1Next;
        e2Prev.next = he2;

        // Update prev pointers
        he1.prev = e1Prev;
        e2Next.prev = he1;

        he2.prev = e2Prev;
        e1Next.prev = he2;

        // 3. Create new faces
        HEFace face1 = new HEFace { edge = he1 };
        HEFace face2 = new HEFace { edge = he2 };

        // 4. Update face references for all affected half-edges
        // For he1's loop
        current = he1;
        do
        {
            current.face = face1;
            current = current.next;
        } while (current != he1);

        // For he2's loop
        current = he2;
        do
        {
            current.face = face2;
            current = current.next;
        } while (current != he2);

        // 5. Update the mesh data structures
        halfEdges.Add(he1);
        halfEdges.Add(he2);
        faces.Add(face1);
        faces.Add(face2);
        faces.Remove(sharedFace);

        // 6. Register the edge in the edge map
        int v1Index = vertices.IndexOf(v1);
        int v2Index = vertices.IndexOf(v2);
        edgeMap[(v1Index, v2Index)] = he1;
        edgeMap[(v2Index, v1Index)] = he2;

        return;
    }

    public HEFace RemoveEdge(HEHalfEdge edge)
    {
        // 1. Verify the edge and its twin exist
        if (edge == null || edge.twin == null)
        {
            UnityEngine.Debug.LogError("Cannot remove edge: edge is null or has no twin");
            return null;
        }

        HEHalfEdge twin = edge.twin;

        // 2. Identify the two faces
        HEFace face1 = edge.face;
        HEFace face2 = twin.face;

        // Check if we're working with two different faces
        bool differentFaces = face1 != face2;

        // If we're removing an edge between two different faces, we'll merge them
        HEFace keepFace = face1; // The face we'll keep

        // If either face uses the edge being removed as its reference, update it
        if (face1.edge == edge)
        {
            face1.edge = edge.next != edge ? edge.next : null;
        }

        if (face2.edge == twin)
        {
            if (differentFaces)
            {
                // We're merging faces, so the twin's face will be removed
                face2.edge = null;
            }
            else
            {
                // Single face case
                face2.edge = twin.next != twin ? twin.next : null;
            }
        }

        // 3. Update all half-edges in both face loops to point to the kept face
        if (differentFaces)
        {
            // Update all half-edges in face2 to point to face1
            HEHalfEdge current = twin.next;
            while (current != twin)
            {
                current.face = keepFace;
                current = current.next;
            }
        }

        // 4. Redirect next/prev pointers to bypass the removed edges
        // For the first half-edge
        edge.prev.next = edge.next;
        edge.next.prev = edge.prev;

        // For the twin half-edge
        twin.prev.next = twin.next;
        twin.next.prev = twin.prev;

        // 5. Remove the edges and update the mesh data structures
        halfEdges.Remove(edge);
        halfEdges.Remove(twin);

        // If we merged two faces, remove the second face
        if (differentFaces)
        {
            faces.Remove(face2);
        }

        // Remove from edge map
        int v1Index = -1, v2Index = -1;
        for (int i = 0; i < vertices.Count; i++)
        {
            if (vertices[i] == edge.origin) v1Index = i;
            if (vertices[i] == twin.origin) v2Index = i;
        }

        if (v1Index >= 0 && v2Index >= 0)
        {
            edgeMap.Remove((v1Index, v2Index));
            edgeMap.Remove((v2Index, v1Index));
        }

        return keepFace; // Return the kept face
    }

    public void EdgeFlip(HEHalfEdge edge)
    {
        // 1. Verify the edge and its twin exist
        if (edge == null || edge.twin == null)
        {
            UnityEngine.Debug.LogError("Cannot flip edge: edge is null or has no twin");
            return;
        }

        HEHalfEdge twin = edge.twin;

        // 2. Identify the two faces
        HEFace face1 = edge.face;
        HEFace face2 = twin.face;

        // 3. Get the vertices of the edge and its twin
        HEVertex v1 = edge.vertex;
        HEVertex v2 = twin.vertex;

        // 4. Get the corner vertices of the faces
        HEVertex v3 = edge.next.vertex; // Next vertex in face1
        HEVertex v4 = twin.next.vertex; // Next vertex in face2

        // 5. Check degrees of vertex v1 and v2
        // TODO

        // Check if the edge (v3,v4) does not already exist
        // if (edgeMap.ContainsKey((vertices.IndexOf(v3), vertices.IndexOf(v4))) ||
        //     edgeMap.ContainsKey((vertices.IndexOf(v4), vertices.IndexOf(v3))))
        // {
        //     UnityEngine.Debug.LogError("Edge already exists between the vertices of the original half-edges.");
        //     return;
        // }

        // 4. Remove the current edge and its twin from the mesh and get the resulting face
        HEFace resultingFace = RemoveEdge(edge);

        if (resultingFace == null)
        {
            UnityEngine.Debug.LogError("Failed to get valid face after edge removal");
            return;
        }

        // 5. Add a new edge between the opposite corners using the resulting face
        AddHalfEdge(v3, v4, resultingFace);
    }

    public HEFace AddFace(HEVertex v0, HEVertex v1, HEVertex v2)
    {
        HEHalfEdge he0 = new HEHalfEdge { vertex = v0 };
        HEHalfEdge he1 = new HEHalfEdge { vertex = v1 };
        HEHalfEdge he2 = new HEHalfEdge { vertex = v2 };

        // Set next relationships
        he0.next = he1;
        he1.next = he2;
        he2.next = he0;

        // Set prev relationships
        he0.prev = he2;
        he1.prev = he0;
        he2.prev = he1;

        // Set origin vertices (needed for drawing)
        he0.origin = v1;
        he1.origin = v2;
        he2.origin = v0;

        // Create face and link it
        HEFace face = new HEFace { edge = he0 };
        he0.face = face;
        he1.face = face;
        he2.face = face;

        // Register in mesh
        halfEdges.AddRange(new[] { he0, he1, he2 });
        faces.Add(face);

        // Assign one outgoing edge to each vertex (if not already set)
        if (v0.outgoing == null) v0.outgoing = he0;
        if (v1.outgoing == null) v1.outgoing = he1;
        if (v2.outgoing == null) v2.outgoing = he2;

        // Twin edge linking
        TrySetTwin(v0, v1, he0);
        TrySetTwin(v1, v2, he1);
        TrySetTwin(v2, v0, he2);

        return face;
    }

    public HEFace SelectRandomFace()
    {
        if (faces.Count == 0) return null;
        int randomIndex = Random.Range(0, faces.Count);
        HEFace selectedFace = faces[randomIndex];
        return selectedFace;
    }

    public List<HEVertex> VerticesOfFace(HEFace face)
    {
        if (face == null) return null;
        HEHalfEdge edge = face.edge;
        HEHalfEdge firstEdge = edge;
        List<HEVertex> vertices = new List<HEVertex>();
        do
        {
            vertices.Add(edge.vertex);
            edge = edge.next;
        } while (edge != firstEdge);
        return vertices;
    }

    public void SplitFace(HEFace face)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        if (face == null) return;
        List<HEVertex> vertices = VerticesOfFace(face);
        if (vertices == null || vertices.Count < 3) return; // Ensure we have at least 3 vertices

        // Step 1: Create the new center vertex at the face centroid
        Vector3 center = Vector3.zero;
        foreach (var vertex in vertices)
        {
            center += vertex.position;
        }
        center /= vertices.Count;
        HEVertex centerVertex = AddVertex(center);

        // Step 2: Save the original edges before we modify the structure
        HEHalfEdge currentEdge = face.edge;
        List<HEHalfEdge> originalEdges = new List<HEHalfEdge>();
        do
        {
            originalEdges.Add(currentEdge);
            currentEdge = currentEdge.next;
        } while (currentEdge != face.edge);

        // Step 3: Create new triangular faces connecting each original edge to the center vertex
        for (int i = 0; i < originalEdges.Count; i++)
        {
            HEVertex v1 = originalEdges[i].vertex;
            HEVertex v2 = originalEdges[i].next.vertex;

            // Create a new triangular face (maintaining proper orientation)
            AddFace(centerVertex, v1, v2);
        }

        // Step 4: Remove the original face from the mesh
        faces.Remove(face);

        stopwatch.Stop();
        File.AppendAllText("Assets/Tests/HEFaceSplitNona.txt", $"{stopwatch.Elapsed.TotalMilliseconds:F4} \n");

    }

    public string EdgeToString(HEHalfEdge edge)
    {
        if (edge == null) return "null"; // Watch out that the first vertex is not the origin of the edge
        return $"{VertexToString(edge.vertex)} -> {VertexToString(edge.next.vertex)}";
    }

    public string VertexToString(HEVertex vertex)
    {
        if (vertex == null) return "null";
        return $"{vertex.position}";
    }

    public string FaceToString(HEFace face)
    {
        if (face == null) return "null";
        return $"{EdgeToString(face.edge)}";
    }

    // Helper function that links a half-edge with its twin if the opposite edge already exists.
    private void TrySetTwin(HEVertex from, HEVertex to, HEHalfEdge edge)
    {
        int fromIndex = vertices.IndexOf(from);
        int toIndex = vertices.IndexOf(to);
        var key = (fromIndex, toIndex);
        var twinKey = (toIndex, fromIndex);

        edgeMap[key] = edge;

        if (edgeMap.TryGetValue(twinKey, out HEHalfEdge twin))
        {
            edge.twin = twin;
            twin.twin = edge;
        }
    }

    // Converts the current HalfEdgeMesh back into a Unity Mesh.
    // Old unused code. Was used for the abandoned PLY importer. 
    public Mesh ToUnityMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        Dictionary<HEVertex, int> vertIndexMap = new Dictionary<HEVertex, int>();
        int index = 0;
        foreach (var v in vertices)
        {
            verts.Add(v.position);
            vertIndexMap[v] = index++;
        }

        foreach (var face in faces)
        {
            HEHalfEdge e = face.edge;
            tris.Add(vertIndexMap[e.vertex]);
            tris.Add(vertIndexMap[e.next.vertex]);
            tris.Add(vertIndexMap[e.next.next.vertex]);
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    public static HalfEdgeMesh FromPlyData(List<Vector3> verts, List<int[]> faceIndices)
    {
        var hem = new HalfEdgeMesh();

        // Create HE vertices
        foreach (var pos in verts)
        {
            hem.vertices.Add(new HEVertex(pos));
        }

        // Add faces
        foreach (var face in faceIndices)
        {
            var v0 = hem.vertices[face[0]];
            var v1 = hem.vertices[face[1]];
            var v2 = hem.vertices[face[2]];
            hem.AddFace(v0, v1, v2);
        }

        return hem;
    }

    /*
    From here and down is the code for creating some simple mesh shapes.
    */

    public static HalfEdgeMesh CreateTetrahedron()
    {
        // TODO: Start the timer here instead of in HalfEdgeTesterEditor.cs

        var hem = new HalfEdgeMesh();

        // Define 4 vertices of a tetrahedron
        var v0 = new HEVertex(new Vector3(1, 1, 1));
        var v1 = new HEVertex(new Vector3(-1, -1, 1));
        var v2 = new HEVertex(new Vector3(-1, 1, -1));
        var v3 = new HEVertex(new Vector3(1, -1, -1));

        hem.vertices.AddRange(new[] { v0, v1, v2, v3 });

        // Create 4 triangular faces
        hem.AddFace(v2, v1, v0); // Base
        hem.AddFace(v0, v1, v3);
        hem.AddFace(v1, v2, v3);
        hem.AddFace(v2, v0, v3);

        hem.halfEdges[0].origin = v0;
        hem.halfEdges[1].origin = v1;
        hem.halfEdges[2].origin = v2;
        hem.halfEdges[3].origin = v3;
        hem.halfEdges[4].origin = v0;
        hem.halfEdges[5].origin = v1;
        hem.halfEdges[6].origin = v2;
        hem.halfEdges[7].origin = v3;
        hem.halfEdges[8].origin = v0;
        hem.halfEdges[9].origin = v1;
        hem.halfEdges[10].origin = v2;
        hem.halfEdges[11].origin = v3;

        hem.TrySetTwin(v0, v1, hem.halfEdges[0]);
        hem.TrySetTwin(v1, v2, hem.halfEdges[1]);
        hem.TrySetTwin(v2, v0, hem.halfEdges[2]);
        hem.TrySetTwin(v0, v3, hem.halfEdges[3]);
        hem.TrySetTwin(v1, v3, hem.halfEdges[4]);
        hem.TrySetTwin(v2, v3, hem.halfEdges[5]);
        hem.TrySetTwin(v0, v1, hem.halfEdges[6]);
        hem.TrySetTwin(v1, v2, hem.halfEdges[7]);
        hem.TrySetTwin(v2, v0, hem.halfEdges[8]);
        hem.TrySetTwin(v0, v3, hem.halfEdges[9]);
        hem.TrySetTwin(v1, v3, hem.halfEdges[10]);
        hem.TrySetTwin(v2, v3, hem.halfEdges[11]);

        return hem;
    }

    public static HalfEdgeMesh CreateCube()
    {
        var hem = new HalfEdgeMesh();

        // Define 8 vertices of a cube
        var v0 = new HEVertex(new Vector3(-1, -1, -1)); // Bottom back left
        var v1 = new HEVertex(new Vector3(1, -1, -1));  // Bottom back right
        var v2 = new HEVertex(new Vector3(1, -1, 1));   // Bottom front right
        var v3 = new HEVertex(new Vector3(-1, -1, 1));  // Bottom front left
        var v4 = new HEVertex(new Vector3(-1, 1, -1));  // Top back left
        var v5 = new HEVertex(new Vector3(1, 1, -1));   // Top back right
        var v6 = new HEVertex(new Vector3(1, 1, 1));    // Top front right
        var v7 = new HEVertex(new Vector3(-1, 1, 1));   // Top front left

        hem.vertices.AddRange(new[] { v0, v1, v2, v3, v4, v5, v6, v7 });

        // Create 12 triangular faces (2 for each side of the cube)

        // Bottom face
        hem.AddFace(v0, v2, v1);
        hem.AddFace(v0, v3, v2);

        // Top face
        hem.AddFace(v4, v5, v6);
        hem.AddFace(v4, v6, v7);

        // Front face
        hem.AddFace(v3, v7, v6);
        hem.AddFace(v3, v6, v2);

        // Back face
        hem.AddFace(v0, v1, v5);
        hem.AddFace(v0, v5, v4);

        // Left face
        hem.AddFace(v0, v4, v7);
        hem.AddFace(v0, v7, v3);

        // Right face
        hem.AddFace(v1, v2, v6);
        hem.AddFace(v1, v6, v5);

        return hem;
    }

    public static HalfEdgeMesh CreateQuad()
    {
        var hem = new HalfEdgeMesh();

        // Define 4 vertices of a quad
        var v0 = new HEVertex(new Vector3(1, 1, 0));
        var v1 = new HEVertex(new Vector3(-1, 1, 0));
        var v2 = new HEVertex(new Vector3(-1, -1, 0));
        var v3 = new HEVertex(new Vector3(1, -1, 0));

        hem.vertices.AddRange(new[] { v0, v1, v2, v3 });

        // Create quad face
        HEHalfEdge he0 = new HEHalfEdge { vertex = v0 };
        HEHalfEdge he1 = new HEHalfEdge { vertex = v1 };
        HEHalfEdge he2 = new HEHalfEdge { vertex = v2 };
        HEHalfEdge he3 = new HEHalfEdge { vertex = v3 };

        // Set next relationships
        he0.next = he1;
        he1.next = he2;
        he2.next = he3;
        he3.next = he0;

        // Set prev relationships
        he0.prev = he3;
        he1.prev = he0;
        he2.prev = he1;
        he3.prev = he2;

        // Set origin vertices for each half-edge
        he0.origin = v0;
        he1.origin = v1;
        he2.origin = v2;
        he3.origin = v3;

        // Create face and link it
        HEFace face = new HEFace { edge = he0 };
        he0.face = face;
        he1.face = face;
        he2.face = face;
        he3.face = face;

        // Register in mesh
        hem.halfEdges.AddRange(new[] { he0, he1, he2, he3 });
        hem.faces.Add(face);

        // Assign one outgoing edge to each vertex
        v0.outgoing = he0;
        v1.outgoing = he1;
        v2.outgoing = he2;
        v3.outgoing = he3;

        // Set up twin relationships
        hem.TrySetTwin(v0, v1, he0);
        hem.TrySetTwin(v1, v2, he1);
        hem.TrySetTwin(v2, v3, he2);
        hem.TrySetTwin(v3, v0, he3);

        return hem;
    }

    public static HalfEdgeMesh CreatePentagon()
    {
        var hem = new HalfEdgeMesh();

        // Define 5 vertices of a regular pentagon
        float radius = 1.0f;
        float angle = 2 * Mathf.PI / 5; // 72 degrees in radians

        var v0 = new HEVertex(new Vector3(radius * Mathf.Sin(0 * angle), radius * Mathf.Cos(0 * angle), 0));
        var v1 = new HEVertex(new Vector3(radius * Mathf.Sin(1 * angle), radius * Mathf.Cos(1 * angle), 0));
        var v2 = new HEVertex(new Vector3(radius * Mathf.Sin(2 * angle), radius * Mathf.Cos(2 * angle), 0));
        var v3 = new HEVertex(new Vector3(radius * Mathf.Sin(3 * angle), radius * Mathf.Cos(3 * angle), 0));
        var v4 = new HEVertex(new Vector3(radius * Mathf.Sin(4 * angle), radius * Mathf.Cos(4 * angle), 0));

        hem.vertices.AddRange(new[] { v0, v1, v2, v3, v4 });

        // Create pentagon face
        HEHalfEdge he0 = new HEHalfEdge { vertex = v0 };
        HEHalfEdge he1 = new HEHalfEdge { vertex = v1 };
        HEHalfEdge he2 = new HEHalfEdge { vertex = v2 };
        HEHalfEdge he3 = new HEHalfEdge { vertex = v3 };
        HEHalfEdge he4 = new HEHalfEdge { vertex = v4 };

        // Set next relationships
        he0.next = he1;
        he1.next = he2;
        he2.next = he3;
        he3.next = he4;
        he4.next = he0;

        // Set prev relationships
        he0.prev = he4;
        he1.prev = he0;
        he2.prev = he1;
        he3.prev = he2;
        he4.prev = he3;

        // Set origin vertices for each half-edge
        he0.origin = v1;
        he1.origin = v2;
        he2.origin = v3;
        he3.origin = v4;
        he4.origin = v0;

        // Create face and link it
        HEFace face = new HEFace { edge = he0 };
        he0.face = face;
        he1.face = face;
        he2.face = face;
        he3.face = face;
        he4.face = face;

        // Register in mesh
        hem.halfEdges.AddRange(new[] { he0, he1, he2, he3, he4 });
        hem.faces.Add(face);

        // Assign one outgoing edge to each vertex
        v0.outgoing = he0;
        v1.outgoing = he1;
        v2.outgoing = he2;
        v3.outgoing = he3;
        v4.outgoing = he4;

        // Set up twin relationships
        hem.TrySetTwin(v0, v1, he0);
        hem.TrySetTwin(v1, v2, he1);
        hem.TrySetTwin(v2, v3, he2);
        hem.TrySetTwin(v3, v4, he3);
        hem.TrySetTwin(v4, v0, he4);

        return hem;
    }

    public static HalfEdgeMesh CreateHexagon()
    {
        var hem = new HalfEdgeMesh();

        // Define 6 vertices of a regular hexagon
        float radius = 1.0f;
        float angle = 2 * Mathf.PI / 6; // 60 degrees in radians

        var v0 = new HEVertex(new Vector3(radius * Mathf.Sin(0 * angle), radius * Mathf.Cos(0 * angle), 0));
        var v1 = new HEVertex(new Vector3(radius * Mathf.Sin(1 * angle), radius * Mathf.Cos(1 * angle), 0));
        var v2 = new HEVertex(new Vector3(radius * Mathf.Sin(2 * angle), radius * Mathf.Cos(2 * angle), 0));
        var v3 = new HEVertex(new Vector3(radius * Mathf.Sin(3 * angle), radius * Mathf.Cos(3 * angle), 0));
        var v4 = new HEVertex(new Vector3(radius * Mathf.Sin(4 * angle), radius * Mathf.Cos(4 * angle), 0));
        var v5 = new HEVertex(new Vector3(radius * Mathf.Sin(5 * angle), radius * Mathf.Cos(5 * angle), 0));

        hem.vertices.AddRange(new[] { v0, v1, v2, v3, v4, v5 });

        // Create hexagon face
        HEHalfEdge he0 = new HEHalfEdge { vertex = v0 };
        HEHalfEdge he1 = new HEHalfEdge { vertex = v1 };
        HEHalfEdge he2 = new HEHalfEdge { vertex = v2 };
        HEHalfEdge he3 = new HEHalfEdge { vertex = v3 };
        HEHalfEdge he4 = new HEHalfEdge { vertex = v4 };
        HEHalfEdge he5 = new HEHalfEdge { vertex = v5 };

        // Set next relationships
        he0.next = he1;
        he1.next = he2;
        he2.next = he3;
        he3.next = he4;
        he4.next = he5;
        he5.next = he0;

        // Set prev relationships
        he0.prev = he5;
        he1.prev = he0;
        he2.prev = he1;
        he3.prev = he2;
        he4.prev = he3;
        he5.prev = he4;

        // Set origin vertices for each half-edge
        he0.origin = v1;
        he1.origin = v2;
        he2.origin = v3;
        he3.origin = v4;
        he4.origin = v5;
        he5.origin = v0;

        // Create face and link it
        HEFace face = new HEFace { edge = he0 };
        he0.face = face;
        he1.face = face;
        he2.face = face;
        he3.face = face;
        he4.face = face;
        he5.face = face;

        // Register in mesh
        hem.halfEdges.AddRange(new[] { he0, he1, he2, he3, he4, he5 });
        hem.faces.Add(face);

        // Assign one outgoing edge to each vertex
        v0.outgoing = he0;
        v1.outgoing = he1;
        v2.outgoing = he2;
        v3.outgoing = he3;
        v4.outgoing = he4;
        v5.outgoing = he5;

        // Set up twin relationships
        hem.TrySetTwin(v0, v1, he0);
        hem.TrySetTwin(v1, v2, he1);
        hem.TrySetTwin(v2, v3, he2);
        hem.TrySetTwin(v3, v4, he3);
        hem.TrySetTwin(v4, v5, he4);
        hem.TrySetTwin(v5, v0, he5);

        return hem;
    }

    public static HalfEdgeMesh CreateSeptagon()
    {
        var hem = new HalfEdgeMesh();

        // Define 7 vertices of a regular septagon
        float radius = 1.0f;
        float angle = 2 * Mathf.PI / 7; // ~51.43 degrees in radians

        var v0 = new HEVertex(new Vector3(radius * Mathf.Sin(0 * angle), radius * Mathf.Cos(0 * angle), 0));
        var v1 = new HEVertex(new Vector3(radius * Mathf.Sin(1 * angle), radius * Mathf.Cos(1 * angle), 0));
        var v2 = new HEVertex(new Vector3(radius * Mathf.Sin(2 * angle), radius * Mathf.Cos(2 * angle), 0));
        var v3 = new HEVertex(new Vector3(radius * Mathf.Sin(3 * angle), radius * Mathf.Cos(3 * angle), 0));
        var v4 = new HEVertex(new Vector3(radius * Mathf.Sin(4 * angle), radius * Mathf.Cos(4 * angle), 0));
        var v5 = new HEVertex(new Vector3(radius * Mathf.Sin(5 * angle), radius * Mathf.Cos(5 * angle), 0));
        var v6 = new HEVertex(new Vector3(radius * Mathf.Sin(6 * angle), radius * Mathf.Cos(6 * angle), 0));

        hem.vertices.AddRange(new[] { v0, v1, v2, v3, v4, v5, v6 });

        // Create septagon face
        HEHalfEdge he0 = new HEHalfEdge { vertex = v0 };
        HEHalfEdge he1 = new HEHalfEdge { vertex = v1 };
        HEHalfEdge he2 = new HEHalfEdge { vertex = v2 };
        HEHalfEdge he3 = new HEHalfEdge { vertex = v3 };
        HEHalfEdge he4 = new HEHalfEdge { vertex = v4 };
        HEHalfEdge he5 = new HEHalfEdge { vertex = v5 };
        HEHalfEdge he6 = new HEHalfEdge { vertex = v6 };

        // Set next relationships
        he0.next = he1;
        he1.next = he2;
        he2.next = he3;
        he3.next = he4;
        he4.next = he5;
        he5.next = he6;
        he6.next = he0;

        // Set prev relationships
        he0.prev = he6;
        he1.prev = he0;
        he2.prev = he1;
        he3.prev = he2;
        he4.prev = he3;
        he5.prev = he4;
        he6.prev = he5;

        // Set origin vertices for each half-edge
        he0.origin = v1;
        he1.origin = v2;
        he2.origin = v3;
        he3.origin = v4;
        he4.origin = v5;
        he5.origin = v6;
        he6.origin = v0;

        // Create face and link it
        HEFace face = new HEFace { edge = he0 };
        he0.face = face;
        he1.face = face;
        he2.face = face;
        he3.face = face;
        he4.face = face;
        he5.face = face;
        he6.face = face;

        // Register in mesh
        hem.halfEdges.AddRange(new[] { he0, he1, he2, he3, he4, he5, he6 });
        hem.faces.Add(face);

        // Assign one outgoing edge to each vertex
        v0.outgoing = he0;
        v1.outgoing = he1;
        v2.outgoing = he2;
        v3.outgoing = he3;
        v4.outgoing = he4;
        v5.outgoing = he5;
        v6.outgoing = he6;

        // Set up twin relationships
        hem.TrySetTwin(v0, v1, he0);
        hem.TrySetTwin(v1, v2, he1);
        hem.TrySetTwin(v2, v3, he2);
        hem.TrySetTwin(v3, v4, he3);
        hem.TrySetTwin(v4, v5, he4);
        hem.TrySetTwin(v5, v6, he5);
        hem.TrySetTwin(v6, v0, he6);

        return hem;
    }

    public static HalfEdgeMesh CreateOctagon()
    {
        var hem = new HalfEdgeMesh();

        // Define 8 vertices of a regular octagon
        float radius = 1.0f;
        float angle = 2 * Mathf.PI / 8; // 45 degrees in radians

        var v0 = new HEVertex(new Vector3(radius * Mathf.Sin(0 * angle), radius * Mathf.Cos(0 * angle), 0));
        var v1 = new HEVertex(new Vector3(radius * Mathf.Sin(1 * angle), radius * Mathf.Cos(1 * angle), 0));
        var v2 = new HEVertex(new Vector3(radius * Mathf.Sin(2 * angle), radius * Mathf.Cos(2 * angle), 0));
        var v3 = new HEVertex(new Vector3(radius * Mathf.Sin(3 * angle), radius * Mathf.Cos(3 * angle), 0));
        var v4 = new HEVertex(new Vector3(radius * Mathf.Sin(4 * angle), radius * Mathf.Cos(4 * angle), 0));
        var v5 = new HEVertex(new Vector3(radius * Mathf.Sin(5 * angle), radius * Mathf.Cos(5 * angle), 0));
        var v6 = new HEVertex(new Vector3(radius * Mathf.Sin(6 * angle), radius * Mathf.Cos(6 * angle), 0));
        var v7 = new HEVertex(new Vector3(radius * Mathf.Sin(7 * angle), radius * Mathf.Cos(7 * angle), 0));

        hem.vertices.AddRange(new[] { v0, v1, v2, v3, v4, v5, v6, v7 });

        // Create octagon face
        HEHalfEdge he0 = new HEHalfEdge { vertex = v0 };
        HEHalfEdge he1 = new HEHalfEdge { vertex = v1 };
        HEHalfEdge he2 = new HEHalfEdge { vertex = v2 };
        HEHalfEdge he3 = new HEHalfEdge { vertex = v3 };
        HEHalfEdge he4 = new HEHalfEdge { vertex = v4 };
        HEHalfEdge he5 = new HEHalfEdge { vertex = v5 };
        HEHalfEdge he6 = new HEHalfEdge { vertex = v6 };
        HEHalfEdge he7 = new HEHalfEdge { vertex = v7 };

        // Set next relationships
        he0.next = he1;
        he1.next = he2;
        he2.next = he3;
        he3.next = he4;
        he4.next = he5;
        he5.next = he6;
        he6.next = he7;
        he7.next = he0;

        // Set prev relationships
        he0.prev = he7;
        he1.prev = he0;
        he2.prev = he1;
        he3.prev = he2;
        he4.prev = he3;
        he5.prev = he4;
        he6.prev = he5;
        he7.prev = he6;

        // Set origin vertices for each half-edge
        he0.origin = v1;
        he1.origin = v2;
        he2.origin = v3;
        he3.origin = v4;
        he4.origin = v5;
        he5.origin = v6;
        he6.origin = v7;
        he7.origin = v0;

        // Create face and link it
        HEFace face = new HEFace { edge = he0 };
        he0.face = face;
        he1.face = face;
        he2.face = face;
        he3.face = face;
        he4.face = face;
        he5.face = face;
        he6.face = face;
        he7.face = face;

        // Register in mesh
        hem.halfEdges.AddRange(new[] { he0, he1, he2, he3, he4, he5, he6, he7 });
        hem.faces.Add(face);

        // Assign one outgoing edge to each vertex
        v0.outgoing = he0;
        v1.outgoing = he1;
        v2.outgoing = he2;
        v3.outgoing = he3;
        v4.outgoing = he4;
        v5.outgoing = he5;
        v6.outgoing = he6;
        v7.outgoing = he7;

        // Set up twin relationships
        hem.TrySetTwin(v0, v1, he0);
        hem.TrySetTwin(v1, v2, he1);
        hem.TrySetTwin(v2, v3, he2);
        hem.TrySetTwin(v3, v4, he3);
        hem.TrySetTwin(v4, v5, he4);
        hem.TrySetTwin(v5, v6, he5);
        hem.TrySetTwin(v6, v7, he6);
        hem.TrySetTwin(v7, v0, he7);

        return hem;
    }

    public static HalfEdgeMesh CreateNonagon()
    {
        var hem = new HalfEdgeMesh();

        // Define 9 vertices of a regular nonagon
        float radius = 1.0f;
        float angle = 2 * Mathf.PI / 9; // 40 degrees in radians

        var v0 = new HEVertex(new Vector3(radius * Mathf.Sin(0 * angle), radius * Mathf.Cos(0 * angle), 0));
        var v1 = new HEVertex(new Vector3(radius * Mathf.Sin(1 * angle), radius * Mathf.Cos(1 * angle), 0));
        var v2 = new HEVertex(new Vector3(radius * Mathf.Sin(2 * angle), radius * Mathf.Cos(2 * angle), 0));
        var v3 = new HEVertex(new Vector3(radius * Mathf.Sin(3 * angle), radius * Mathf.Cos(3 * angle), 0));
        var v4 = new HEVertex(new Vector3(radius * Mathf.Sin(4 * angle), radius * Mathf.Cos(4 * angle), 0));
        var v5 = new HEVertex(new Vector3(radius * Mathf.Sin(5 * angle), radius * Mathf.Cos(5 * angle), 0));
        var v6 = new HEVertex(new Vector3(radius * Mathf.Sin(6 * angle), radius * Mathf.Cos(6 * angle), 0));
        var v7 = new HEVertex(new Vector3(radius * Mathf.Sin(7 * angle), radius * Mathf.Cos(7 * angle), 0));
        var v8 = new HEVertex(new Vector3(radius * Mathf.Sin(8 * angle), radius * Mathf.Cos(8 * angle), 0));

        hem.vertices.AddRange(new[] { v0, v1, v2, v3, v4, v5, v6, v7, v8 });

        // Create nonagon face
        HEHalfEdge he0 = new HEHalfEdge { vertex = v0 };
        HEHalfEdge he1 = new HEHalfEdge { vertex = v1 };
        HEHalfEdge he2 = new HEHalfEdge { vertex = v2 };
        HEHalfEdge he3 = new HEHalfEdge { vertex = v3 };
        HEHalfEdge he4 = new HEHalfEdge { vertex = v4 };
        HEHalfEdge he5 = new HEHalfEdge { vertex = v5 };
        HEHalfEdge he6 = new HEHalfEdge { vertex = v6 };
        HEHalfEdge he7 = new HEHalfEdge { vertex = v7 };
        HEHalfEdge he8 = new HEHalfEdge { vertex = v8 };

        // Set next relationships
        he0.next = he1;
        he1.next = he2;
        he2.next = he3;
        he3.next = he4;
        he4.next = he5;
        he5.next = he6;
        he6.next = he7;
        he7.next = he8;
        he8.next = he0;

        // Set prev relationships
        he0.prev = he8;
        he1.prev = he0;
        he2.prev = he1;
        he3.prev = he2;
        he4.prev = he3;
        he5.prev = he4;
        he6.prev = he5;
        he7.prev = he6;
        he8.prev = he7;

        // Set origin vertices for each half-edge
        he0.origin = v1;
        he1.origin = v2;
        he2.origin = v3;
        he3.origin = v4;
        he4.origin = v5;
        he5.origin = v6;
        he6.origin = v7;
        he7.origin = v8;
        he8.origin = v0;

        // Create face and link it
        HEFace face = new HEFace { edge = he0 };
        he0.face = face;
        he1.face = face;
        he2.face = face;
        he3.face = face;
        he4.face = face;
        he5.face = face;
        he6.face = face;
        he7.face = face;
        he8.face = face;

        // Register in mesh
        hem.halfEdges.AddRange(new[] { he0, he1, he2, he3, he4, he5, he6, he7, he8 });
        hem.faces.Add(face);

        // Assign one outgoing edge to each vertex
        v0.outgoing = he0;
        v1.outgoing = he1;
        v2.outgoing = he2;
        v3.outgoing = he3;
        v4.outgoing = he4;
        v5.outgoing = he5;
        v6.outgoing = he6;
        v7.outgoing = he7;
        v8.outgoing = he8;

        // Set up twin relationships
        hem.TrySetTwin(v0, v1, he0);
        hem.TrySetTwin(v1, v2, he1);
        hem.TrySetTwin(v2, v3, he2);
        hem.TrySetTwin(v3, v4, he3);
        hem.TrySetTwin(v4, v5, he4);
        hem.TrySetTwin(v5, v6, he5);
        hem.TrySetTwin(v6, v7, he6);
        hem.TrySetTwin(v7, v8, he7);
        hem.TrySetTwin(v8, v0, he8);

        return hem;
    }

}
