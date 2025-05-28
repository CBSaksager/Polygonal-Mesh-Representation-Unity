using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RotationSystem2Tester : MonoBehaviour
{
    public RSMesh rsMesh;
    public RSEdge selectedEdge;
    public List<RSVertex> selectedFaceVertices;

    public void CreateTetrahedron()
    {
        rsMesh = RSMesh.CreateTetrahedron();
    }

    public void ClearMesh()
    {
        rsMesh = null;
        selectedEdge = null;
        selectedFaceVertices = null;
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

        selectedFaceVertices = rsMesh.SelectRandomFace();
        if (selectedFaceVertices != null)
        {
            foreach (var vertex in selectedFaceVertices)
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
        if (selectedFaceVertices == null || selectedFaceVertices.Count < 3)
        {
            Debug.LogError("Select a valid face to split.");
            return;
        }

        rsMesh.SplitFace(selectedFaceVertices);
        Debug.Log("Face split successfully.");
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
            foreach (var edge in vertex.edges)
            {
                Gizmos.color = Color.black;
                // Transform positions from local to world space
                Vector3 fromPos = transform.TransformPoint(edge.from.position);
                Vector3 toPos = transform.TransformPoint(edge.to.position);
                Gizmos.DrawLine(fromPos, toPos);
            }
        }

        // Draw selected edge
        if (selectedEdge != null)
        {
            Gizmos.color = Color.green;
            // Transform positions from local to world space
            Vector3 fromPos = transform.TransformPoint(selectedEdge.from.position);
            Vector3 toPos = transform.TransformPoint(selectedEdge.to.position);
            Gizmos.DrawLine(fromPos, toPos);
        }

        // Draw selected face vertices and edges
        if (selectedFaceVertices != null && selectedFaceVertices.Count > 0)
        {
            Gizmos.color = Color.blue;

            // Draw vertices
            foreach (var vertex in selectedFaceVertices)
            {
                // Transform position to world space
                Vector3 worldPos = transform.TransformPoint(vertex.position);
                Gizmos.DrawSphere(worldPos, 0.05f);
            }

            // Draw edges connecting the face vertices
            for (int i = 0; i < selectedFaceVertices.Count; i++)
            {
                int nextIndex = (i + 1) % selectedFaceVertices.Count;
                // Transform positions to world space
                Vector3 fromPos = transform.TransformPoint(selectedFaceVertices[i].position);
                Vector3 toPos = transform.TransformPoint(selectedFaceVertices[nextIndex].position);
                Gizmos.DrawLine(fromPos, toPos);
            }
        }
    }
#endif
}