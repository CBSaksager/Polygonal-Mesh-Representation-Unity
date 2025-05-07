using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RotationSystemTester))]
public class RotationSystemTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RotationSystemTester tester = (RotationSystemTester)target;

        if (GUILayout.Button("Clear Mesh"))
        {
            tester.ClearMesh();
        }

        if (GUILayout.Button("Create Tetrahedron"))
        {
            tester.CreateTetrahedron();
        }

        if (GUILayout.Button("Select Random Face"))
        {
            tester.SelectRandomFace();
        }

        if (GUILayout.Button("Face Split"))
        {
            tester.SplitFace();
        }

        if (GUILayout.Button("Splt Random Face"))
        {
            tester.SelectRandomFace();
            tester.SplitFace();
        }

        SceneView.RepaintAll(); // Force Scene to refresh the Gizmos
    }
}
