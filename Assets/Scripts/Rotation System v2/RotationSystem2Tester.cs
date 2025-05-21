using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RotationSystem2Tester : MonoBehaviour
{
    public RSMesh rsMesh;

    public void CreateTetrahedron()
    {
        rsMesh = RSMesh.CreateTetrahedron();
    }

    public void ClearMesh()
    {
        rsMesh = null;
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
    }
#endif
}