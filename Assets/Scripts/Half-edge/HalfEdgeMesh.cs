using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class HalfEdgeMesh
{
    public List<HEVertex> vertices = new List<HEVertex>();
    public List<HEHalfEdge> halfEdges = new List<HEHalfEdge>();
    public List<HEFace> faces = new List<HEFace>();

    // Internal mapping to track edge pairs for twin linking
    private Dictionary<(int, int), HEHalfEdge> edgeMap = new Dictionary<(int, int), HEHalfEdge>();

    public HEVertex AddVertex(Vector3 position)
    {
        var vertex = new HEVertex(position);
        vertices.Add(vertex);
        return vertex;
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
        File.AppendAllText("Assets/Tests/HEFaceSplit.txt", $"{stopwatch.Elapsed.TotalMilliseconds:F4} \n");

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

    /// <summary>
    /// Builds a HalfEdgeMesh from a Unity Mesh.
    /// </summary>
    public static HalfEdgeMesh FromUnityMesh(Mesh mesh)
    {
        var hem = new HalfEdgeMesh();
        Vector3[] meshVerts = mesh.vertices;
        int[] meshTris = mesh.triangles;

        // Create vertices
        for (int i = 0; i < meshVerts.Length; i++)
        {
            hem.vertices.Add(new HEVertex(meshVerts[i]));
        }

        // Process triangles
        for (int i = 0; i < meshTris.Length; i += 3)
        {
            HEVertex v0 = hem.vertices[meshTris[i]];
            HEVertex v1 = hem.vertices[meshTris[i + 1]];
            HEVertex v2 = hem.vertices[meshTris[i + 2]];
            hem.AddFace(v0, v1, v2);
        }

        return hem;
    }

    /// <summary>
    /// Converts the current HalfEdgeMesh back into a Unity Mesh.
    /// </summary>
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

    /// <summary>
    /// Links a half-edge with its twin if the opposite edge already exists.
    /// </summary>
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

}
