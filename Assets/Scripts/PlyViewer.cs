using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlyViewer : MonoBehaviour
{
    [Header("Drag a .ply file here (TextAsset)")]
    public TextAsset plyFile;

    void Start()
    {
        if (plyFile == null)
        {
            Debug.LogError("No PLY file assigned.");
            return;
        }

        PlyImporter.LoadPlyFromText(plyFile.text, out var verts, out var faces);

        if (verts.Count == 0 || faces.Count == 0)
        {
            Debug.LogError("PLY import failed.");
            return;
        }

        HalfEdgeMesh hem = HalfEdgeMesh.FromPlyData(verts, faces);
        Mesh unityMesh = hem.ToUnityMesh();

        GetComponent<MeshFilter>().mesh = unityMesh;
    }
}
