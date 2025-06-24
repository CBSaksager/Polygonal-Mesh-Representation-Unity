using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;



#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HalfEdgeTester : MonoBehaviour
{
    private HalfEdgeMesh hem;
    private HEFace selectedFace;
    private HEHalfEdge selectedEdge;

    public void ClearMesh()
    {
        hem = null;
        selectedFace = null;
        selectedEdge = null;

        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = null;
        }
    }

    public void SelectRandomFace()
    {
        if (hem == null || hem.faces == null || hem.faces.Count == 0)
        {
            UnityEngine.Debug.LogWarning("No faces available to select.");
            selectedFace = null;
            return;
        }
        else
        {
            selectedFace = hem.SelectRandomFace();
            List<HEVertex> verticesOfFace = hem.VerticesOfFace(hem.SelectRandomFace());
            foreach (var HEVertex in verticesOfFace)
            {
                UnityEngine.Debug.Log(hem.VertexToString(HEVertex)); // Fix: Make it one message so v1 -> v2 -> v3
            }
        }
    }

    public void SelectRandomEdge()
    {
        if (hem == null || hem.halfEdges == null || hem.halfEdges.Count == 0)
        {
            UnityEngine.Debug.LogWarning("No edges available to select.");
            selectedEdge = null;
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, hem.halfEdges.Count);
        selectedEdge = hem.halfEdges[randomIndex]; // This was missing!

        if (selectedEdge != null)
        {
            UnityEngine.Debug.Log($"Selected Edge: {hem.EdgeToString(selectedEdge)}");
        }
    }

    public void SplitFace()
    {
        if (hem == null || selectedFace == null)
        {
            UnityEngine.Debug.LogWarning("No face selected or mesh is null.");
            return;
        }

        hem.SplitFace(selectedFace);
    }

    public void EdgeFlip()
    {
        if (hem == null || selectedEdge == null)
        {
            UnityEngine.Debug.LogWarning("No edge selected or mesh is null.");
            return;
        }

        hem.EdgeFlip(selectedEdge);
    }

    public void CreateTetrahedron()
    {
        selectedFace = null;
        hem = HalfEdgeMesh.CreateTetrahedron();
    }

    public void CreateCube()
    {
        selectedFace = null;
        hem = HalfEdgeMesh.CreateCube();
    }

    public void CreateQuad()
    {
        selectedFace = null;
        hem = HalfEdgeMesh.CreateQuad();
    }

    public void CreatePentagon()
    {
        selectedFace = null;
        hem = HalfEdgeMesh.CreatePentagon();
    }

    public void CreateHexagon()
    {
        selectedFace = null;
        hem = HalfEdgeMesh.CreateHexagon();
    }

    public void CreateSeptagon()
    {
        selectedFace = null;
        hem = HalfEdgeMesh.CreateSeptagon();
    }

    public void CreateOctagon()
    {
        selectedFace = null;
        hem = HalfEdgeMesh.CreateOctagon();
    }

    public void CreateNonagon()
    {
        selectedFace = null;
        hem = HalfEdgeMesh.CreateNonagon();
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (hem == null || hem.halfEdges == null) return;

        // First pass: Draw all edges in black
        foreach (var he in hem.halfEdges)
        {
            if (he == null || he.origin == null || he.next?.origin == null)
                continue;

            // Check if this edge belongs to the selected face or is the selected edge
            bool isSelectedFaceEdge = (selectedFace != null && he.face == selectedFace);
            bool isSelectedEdge = (selectedEdge != null && (he == selectedEdge || he == selectedEdge.twin));

            // Skip highlighted edges in this pass
            if (isSelectedFaceEdge || isSelectedEdge)
                continue;

            Handles.color = Color.black;

            Vector3 from = transform.TransformPoint(he.origin.position);
            Vector3 to = transform.TransformPoint(he.next.origin.position);
            Vector3 mid = (from + to) * 0.5f;
            Vector3 dir = (to - from).normalized;

            // Draw the edge line
            Handles.DrawAAPolyLine(2.5f, from, to);

            // Draw arrow head
            float arrowSize = 0.1f;
            Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), arrowSize, EventType.Repaint);
        }

        // Second pass: Draw selected face edges in a different color
        if (selectedFace != null)
        {
            Handles.color = Color.red; // Change to your preferred highlight color

            List<HEVertex> verticesOfFace = hem.VerticesOfFace(selectedFace);

            // Start with the face's half-edge and traverse all edges of the face
            HEHalfEdge startEdge = selectedFace.edge;
            HEHalfEdge currentEdge = startEdge;

            do
            {
                if (currentEdge != null && currentEdge.origin != null && currentEdge.next?.origin != null)
                {
                    Vector3 from = transform.TransformPoint(currentEdge.origin.position);
                    Vector3 to = transform.TransformPoint(currentEdge.next.origin.position);
                    Vector3 mid = (from + to) * 0.5f;
                    Vector3 dir = (to - from).normalized;

                    // Draw the edge line
                    Handles.DrawAAPolyLine(2.5f, from, to);

                    // Draw arrow head
                    float arrowSize = 0.1f;
                    Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), arrowSize, EventType.Repaint);
                }

                currentEdge = currentEdge.next;
            } while (currentEdge != null && currentEdge != startEdge);
        }

        // Third pass: Draw selected edge in red
        if (selectedEdge != null)
        {
            Handles.color = Color.red;

            if (selectedEdge.origin != null && selectedEdge.vertex != null)
            {
                Vector3 from = transform.TransformPoint(selectedEdge.origin.position);
                Vector3 to = transform.TransformPoint(selectedEdge.vertex.position);
                Vector3 mid = (from + to) * 0.5f;
                Vector3 dir = (to - from).normalized;

                // Draw the edge line with increased thickness
                Handles.DrawAAPolyLine(3f, from, to);

                // Draw arrow head
                float arrowSize = 0.12f;
                Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), arrowSize, EventType.Repaint);
            }

            // Also draw the twin edge if it exists
            if (selectedEdge.twin != null && selectedEdge.twin.origin != null && selectedEdge.twin.vertex != null)
            {
                Vector3 from = transform.TransformPoint(selectedEdge.twin.origin.position);
                Vector3 to = transform.TransformPoint(selectedEdge.twin.vertex.position);
                Vector3 mid = (from + to) * 0.5f;
                Vector3 dir = (to - from).normalized;

                // Draw the twin edge line
                Handles.DrawAAPolyLine(3f, from, to);

                // Draw arrow head
                float arrowSize = 0.12f;
                Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), arrowSize, EventType.Repaint);
            }
        }
    }
#endif

}
