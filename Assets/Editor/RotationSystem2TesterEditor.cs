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

        SceneView.RepaintAll(); // Force Scene to refresh the Gizmos
    }
}