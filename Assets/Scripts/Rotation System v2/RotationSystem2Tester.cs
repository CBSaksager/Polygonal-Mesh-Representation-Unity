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
            Gizmos.DrawSphere(vertex.position, 0.05f);
        }

        foreach (var vertex in rsMesh.vertices)
        {
            foreach (var edge in vertex.edges)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawLine(edge.from.position, edge.to.position);
            }
        }

        // Draw selected edge
        if (selectedEdge != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(selectedEdge.from.position, selectedEdge.to.position);
        }

        // Draw selected face vertices and edges
        if (selectedFaceVertices != null && selectedFaceVertices.Count > 0)
        {
            Gizmos.color = Color.blue;

            // Draw vertices
            foreach (var vertex in selectedFaceVertices)
            {
                Gizmos.DrawSphere(vertex.position, 0.05f);
            }

            // Draw edges connecting the face vertices
            for (int i = 0; i < selectedFaceVertices.Count; i++)
            {
                int nextIndex = (i + 1) % selectedFaceVertices.Count;
                Gizmos.DrawLine(selectedFaceVertices[i].position, selectedFaceVertices[nextIndex].position);
            }
        }
    }
#endif
}