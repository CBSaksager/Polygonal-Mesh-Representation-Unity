using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RotationSystemTester : MonoBehaviour
{
    public RsMesh rsMesh;
    public List<RsVertex> selectedFace;

    public void SelectRandomFace()
    { // Fix: Clean
        if (rsMesh == null || rsMesh.vertices == null || rsMesh.vertices.Count < 3)
        {
            Debug.LogWarning("Not enough vertices to select a face.");
            return;
        }

        selectedFace = rsMesh.SelectRandomFace();
        if (selectedFace != null)
        {
            foreach (RsVertex vertex in selectedFace)
            {
                Debug.Log($"Selected vertex: {vertex.position}");
            }
        }
        else
        {
            Debug.LogWarning("No face selected.");
        }
    }

    public void CreateTetrahedron()
    {
        rsMesh = RsMesh.CreateTetrahedron();
    }

    public void ClearMesh()
    {
        selectedFace = null;
        rsMesh = null;
    }

    public void SplitFace()
    {
        if (rsMesh == null || rsMesh.vertices == null || selectedFace == null)
        {
            Debug.LogWarning("Not enough vertices to split a face.");
            return;
        }
        rsMesh.SplitFace(rsMesh, selectedFace);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (rsMesh == null || rsMesh.vertices == null)
            return;

        Handles.color = Color.black;

        for (int i = 0; i < rsMesh.vertices.Count; i++)
        {
            RsVertex v = rsMesh.vertices[i];
            Vector3 from = transform.TransformPoint(v.position);

            foreach (int neighborIndex in v.neighbors)
            {
                if (neighborIndex < 0 || neighborIndex >= rsMesh.vertices.Count)
                    continue;

                Vector3 to = transform.TransformPoint(rsMesh.vertices[neighborIndex].position);
                Vector3 mid = (from + to) * 0.5f;
                Vector3 dir = (to - from).normalized;

                Handles.DrawAAPolyLine(2.5f, from, to);
                Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), 0.1f, EventType.Repaint);
            }
        }

        // Draw the selected face in red
        if (selectedFace != null && selectedFace.Count >= 3)
        {
            Handles.color = Color.red;

            for (int i = 0; i < selectedFace.Count; i++)
            {
                Vector3 from = transform.TransformPoint(selectedFace[i].position);
                Vector3 to = transform.TransformPoint(selectedFace[(i + 1) % selectedFace.Count].position);
                Vector3 mid = (from + to) * 0.5f;
                Vector3 dir = (to - from).normalized;

                Handles.DrawAAPolyLine(2.5f, from, to);
                Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), 0.1f, EventType.Repaint);
            }
        }
    }
#endif

}