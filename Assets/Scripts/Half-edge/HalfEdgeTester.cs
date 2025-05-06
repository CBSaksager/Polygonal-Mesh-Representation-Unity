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
            List<HEHalfEdge> verticesOfFace = hem.VerticesOfFace(hem.SelectRandomFace());
            foreach (var HEHalfEdge in verticesOfFace)
            {
                Debug.Log(hem.EdgeToString(HEHalfEdge));
            }
        }
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
            Handles.DrawAAPolyLine(2.5f, from, to);

            // Draw arrow head
            float arrowSize = 0.1f;
            Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), arrowSize, EventType.Repaint);
            
        }
    }
#endif

}
