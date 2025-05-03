using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RotationSystemTester : MonoBehaviour
{
    public RsMesh rsMesh;

    public void CreateTetrahedron()
    {
        rsMesh = RsMesh.CreateTetrahedron();
    }

    public void ClearMesh()
    {
        rsMesh = null;
    }

    public void SplitFace(int aIndex, int bIndex, int cIndex)
    {
        if (rsMesh == null) return;

        Vector3 posA = rsMesh.vertices[aIndex].position;
        Vector3 posB = rsMesh.vertices[bIndex].position;
        Vector3 posC = rsMesh.vertices[cIndex].position;

        // Step 1: Create the new center vertex at the face centroid
        Vector3 center = (posA + posB + posC) / 3f;
        int vIndex = rsMesh.vertices.Count;
        rsMesh.vertices.Add(new RsVertex(center));

        // Step 2: Update the new vertex’s neighbors (in correct order)
        rsMesh.vertices[vIndex].neighbors.AddRange(new int[] { aIndex, bIndex, cIndex });

        // Step 3: Update the original vertices’ neighbor cycles to insert V

        InsertAfterNeighbor(rsMesh.vertices[aIndex], cIndex, vIndex);
        InsertAfterNeighbor(rsMesh.vertices[bIndex], aIndex, vIndex);
        InsertAfterNeighbor(rsMesh.vertices[cIndex], bIndex, vIndex);
    }

    private void InsertAfterNeighbor(RsVertex v, int after, int insert)
    {
        int i = v.neighbors.IndexOf(after);
        if (i != -1)
        {
            v.neighbors.Insert((i + 1) % (v.neighbors.Count + 1), insert);
        }
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
    }
#endif

}