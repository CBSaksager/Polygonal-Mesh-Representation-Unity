using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(RotationSystem2Tester))]
public class RotationSystem2TesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RotationSystem2Tester tester = (RotationSystem2Tester)target;

        if (GUILayout.Button("Clear Mesh"))
        {
            tester.ClearMesh();
        }

        if (GUILayout.Button("Create Tetrahedron"))
        {
            tester.CreateTetrahedron();
        }

        if (GUILayout.Button("Select Random Edge"))
        {
            tester.SelectRandomEdge();
        }

        if (GUILayout.Button("Next Edge of Vertex"))
        {
            tester.SelectNextEdgeOfVertex();
        }

        if (GUILayout.Button("Next Edge of Face"))
        {
            tester.SelectNextEdgeOfFace();
        }

        if (GUILayout.Button("Select Random Face"))
        {
            tester.SelectRandomFace();
        }

        if (GUILayout.Button("Split Face"))
        {
            tester.SplitFace();
        }

        SceneView.RepaintAll(); // Force Scene to refresh the Gizmos
    }
}