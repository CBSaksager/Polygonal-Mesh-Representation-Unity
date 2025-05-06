using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HalfEdgeTester : MonoBehaviour
{
    private HalfEdgeMesh hem;
    private HEFace selectedFace;

    public void ClearMesh()
    {
        hem = null;

        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = null;
        }
    }

    public void SelectRandomFace(){
        if (hem == null || hem.faces == null || hem.faces.Count == 0){
            Debug.LogWarning("No faces available to select.");
            selectedFace = null;
            return;
        }
        else
        {
            selectedFace = hem.SelectRandomFace();
            List<HEVertex> verticesOfFace = hem.VerticesOfFace(hem.SelectRandomFace());
            foreach (var HEVertex in verticesOfFace)
            {
                Debug.Log(hem.VertexToString(HEVertex)); // Fix: Make it one message so v1 -> v2 -> v3
            }
        }
    }

    public void SplitFace(){
        if (hem == null || selectedFace == null)
        {
            Debug.LogWarning("No face selected or mesh is null.");
            return;
        }
        hem.SplitFace(selectedFace);
    }

    public void CreateTetrahedron()
    {
        hem = HalfEdgeMesh.CreateTetrahedron();
        UpdateUnityMesh();
    }

    private void UpdateUnityMesh()
    {
        if (hem == null) return;
        var mesh = hem.ToUnityMesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (hem == null || hem.halfEdges == null) return;

        Handles.color = Color.black;

        foreach (var he in hem.halfEdges)
        {
            if (he == null || he.origin == null || he.next?.origin == null)
                continue;

            Vector3 from = transform.TransformPoint(he.origin.position);
            Vector3 to = transform.TransformPoint(he.next.origin.position);
            Vector3 mid = (from + to) * 0.5f;
            Vector3 dir = (to - from).normalized;

            // Draw the edge line
            Handles.DrawAAPolyLine(4f, from, to);

            // Draw arrow head
            float arrowSize = 0.4f;
            Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), arrowSize, EventType.Repaint);
            
        }

        // Draw vertices as dots
        Handles.color = Color.blue;
        float vertexSize = 0.03f;
        
        // Collect unique vertices from half-edges
        HashSet<HEVertex> uniqueVertices = new HashSet<HEVertex>();
        foreach (var he in hem.halfEdges)
        {
            if (he != null && he.origin != null)
            {
                uniqueVertices.Add(he.origin);
            }
        }
        
        // Draw each vertex as a dot
        foreach (var vertex in uniqueVertices)
        {
            Vector3 position = transform.TransformPoint(vertex.position);
            Handles.SphereHandleCap(0, position, Quaternion.identity, vertexSize, EventType.Repaint);
        }
    }
#endif

}
