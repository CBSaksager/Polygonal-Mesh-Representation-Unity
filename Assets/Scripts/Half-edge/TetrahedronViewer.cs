using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TetrahedronViewer : MonoBehaviour
{
    void Start()
    {
        HalfEdgeMesh hem = HalfEdgeMesh.CreateTetrahedron();
        Mesh unityMesh = hem.ToUnityMesh();

        GetComponent<MeshFilter>().mesh = unityMesh;
    }
}
