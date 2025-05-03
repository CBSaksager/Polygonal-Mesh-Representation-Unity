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

        if (GUILayout.Button("Face Split (0-1-2)"))
        {
            tester.SplitFace(0, 1, 2); // Test on triangle 0-1-2
        }


        SceneView.RepaintAll(); // Force Scene to refresh the Gizmos
    }
}
