using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RotationSystem2Tester : MonoBehaviour
{
    public RSMesh rsMesh;
    public RSEdge selectedEdge;
    public RSFace selectedFace;

    public void CreateTetrahedron()
    {
        rsMesh = RSMesh.CreateTetrahedron();
    }

    public void CreateCube()
    {
        rsMesh = RSMesh.CreateCube();
    }

    public void CreateDodecahedron()
    {
        rsMesh = RSMesh.CreateDodecahedron();
    }

    public void ClearMesh()
    {
        rsMesh = null;
        selectedEdge = null;
        selectedFace = null;
    }

    public void SelectRandomEdge()
    {
        if (rsMesh == null)
        {
            Debug.LogError("Mesh is not created yet.");
            return;
        }

        selectedEdge = rsMesh.SelectRandomEdge();
        if (selectedEdge != null)
        {
            Debug.Log($"Selected Edge: {selectedEdge.from.position} -> {selectedEdge.to.position}");
        }
    }

    public void Iota()
    {
        if (selectedEdge == null)
        {
            Debug.LogError("No edge selected.");
            return;
        }

        // Select the next edge of the vertex
        selectedEdge = rsMesh.Iota(selectedEdge);
        Debug.Log($"Selected Next Edge of Vertex: {selectedEdge.from.position} -> {selectedEdge.to.position}");
    }

    public void SelectNextEdgeOfVertex()
    {
        if (selectedEdge == null)
        {
            Debug.LogError("No edge selected.");
            return;
        }

        selectedEdge = rsMesh.Rho(selectedEdge);
        Debug.Log($"Selected Next Edge: {selectedEdge.from.position} -> {selectedEdge.to.position}");
    }

    public void SelectNextEdgeOfFace()
    {
        if (selectedEdge == null)
        {
            Debug.LogError("No edge selected.");
            return;
        }

        selectedEdge = rsMesh.Tau(selectedEdge);
        Debug.Log($"Selected Next Edge of Face: {selectedEdge.from.position} -> {selectedEdge.to.position}");
    }

    public void SelectRandomFace()
    {
        if (rsMesh == null)
        {
            Debug.LogError("Mesh is not created yet.");
            return;
        }

        if (rsMesh.faces.Count == 0)
        {
            Debug.LogError("No faces in mesh.");
            return;
        }

        // Select a random face
        selectedFace = rsMesh.faces[Random.Range(0, rsMesh.faces.Count)];

        if (selectedFace != null)
        {
            foreach (var vertex in selectedFace.vertices)
            {
                Debug.Log($"Face Vertex: {vertex.position}");
            }
        }
        else
        {
            Debug.LogError("No face selected.");
        }
    }

    public void SplitFace()
    {
        if (selectedFace == null)
        {
            Debug.LogError("Select a valid face to split.");
            return;
        }

        rsMesh.SplitFace(selectedFace);
        Debug.Log("Face split successfully.");

        // Clear the selected face as it no longer exists after splitting
        selectedFace = null;
    }

    public void ImportPLYFile()
    {
#if UNITY_EDITOR
        string filePath = EditorUtility.OpenFilePanel("Import PLY file", "", "ply");
        if (!string.IsNullOrEmpty(filePath))
        {
            rsMesh = RSMesh.ImportFromPLY(filePath);
            if (rsMesh != null)
            {
                selectedEdge = null;
                selectedFace = null;
                Debug.Log($"Imported PLY file with {rsMesh.vertices.Count} vertices and {rsMesh.faces.Count} faces");
            }
            else
            {
                Debug.LogError("Failed to import PLY file");
            }
        }
#endif
    }

    public void ValidateRotationSystem()
    {
        if (rsMesh == null)
        {
            Debug.LogError("No mesh to validate.");
            return;
        }

        int faceTraversalErrors = 0;
        int edgeConsistencyErrors = 0;

        // Validate face traversal
        foreach (var face in rsMesh.faces)
        {
            for (int i = 0; i < face.vertices.Count; i++)
            {
                var v1 = face.vertices[i];
                var v2 = face.vertices[(i + 1) % face.vertices.Count];
                var v3 = face.vertices[(i + 2) % face.vertices.Count];

                // Find the edge v1->v2
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
                    // Apply Tau (face traversal) and check if we get to v3
                    var nextEdge = rsMesh.Tau(edge);
                    if (nextEdge.from != v2 || nextEdge.to != v3)
                    {
                        faceTraversalErrors++;
                        Debug.LogError($"Face traversal error: Expected {v2.position}->{v3.position}, got {nextEdge.from.position}->{nextEdge.to.position}");
                    }
                }
            }
        }

        // Check edge consistency
        foreach (var vertex in rsMesh.vertices)
        {
            foreach (var edge in vertex.edges)
            {
                bool foundReverse = false;
                foreach (var otherEdge in edge.to.edges)
                {
                    if (otherEdge.to == vertex)
                    {
                        foundReverse = true;
                        break;
                    }
                }

                if (!foundReverse)
                {
                    edgeConsistencyErrors++;
                    Debug.LogError($"Edge consistency error: No reverse edge for {edge.from.position}->{edge.to.position}");
                }
            }
        }

        if (faceTraversalErrors == 0 && edgeConsistencyErrors == 0)
        {
            Debug.Log("Rotation system validation passed successfully!");
        }
        else
        {
            Debug.LogError($"Validation found {faceTraversalErrors} face traversal errors and {edgeConsistencyErrors} edge consistency errors.");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (rsMesh == null || rsMesh.vertices == null)
            return;

        foreach (var vertex in rsMesh.vertices)
        {
            Gizmos.color = Color.red;
            // Transform position from local to world space
            Vector3 worldPos = transform.TransformPoint(vertex.position);
            Gizmos.DrawSphere(worldPos, 0.05f);
        }

        foreach (var vertex in rsMesh.vertices)
        {
            if (vertex.edges == null)
                continue;

            foreach (var edge in vertex.edges)
            {
                if (edge == null || edge.from == null || edge.to == null)
                    continue;

                Gizmos.color = Color.black;
                // Transform positions from local to world space
                Vector3 fromPos = transform.TransformPoint(edge.from.position);
                Vector3 toPos = transform.TransformPoint(edge.to.position);
                Gizmos.DrawLine(fromPos, toPos);
            }
        }

        // Draw selected edge
        if (selectedEdge != null && selectedEdge.from != null && selectedEdge.to != null)
        {
            Gizmos.color = Color.green;
            // Transform positions from local to world space
            Vector3 fromPos = transform.TransformPoint(selectedEdge.from.position);
            Vector3 toPos = transform.TransformPoint(selectedEdge.to.position);
            Gizmos.DrawLine(fromPos, toPos);
        }

        // Draw selected face vertices and edges
        if (selectedFace != null && selectedFace.vertices != null && selectedFace.vertices.Count > 0)
        {
            Gizmos.color = Color.blue;

            // Draw vertices
            foreach (var vertex in selectedFace.vertices)
            {
                if (vertex == null)
                    continue;

                // Transform position to world space
                Vector3 worldPos = transform.TransformPoint(vertex.position);
                Gizmos.DrawSphere(worldPos, 0.05f);
            }

            // Draw edges connecting the face vertices
            for (int i = 0; i < selectedFace.vertices.Count; i++)
            {
                int nextIndex = (i + 1) % selectedFace.vertices.Count;

                // Check for null vertices
                if (selectedFace.vertices[i] == null || selectedFace.vertices[nextIndex] == null)
                    continue;

                // Transform positions to world space
                Vector3 fromPos = transform.TransformPoint(selectedFace.vertices[i].position);
                Vector3 toPos = transform.TransformPoint(selectedFace.vertices[nextIndex].position);
                Gizmos.DrawLine(fromPos, toPos);
            }
        }
    }
#endif
}