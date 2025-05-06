using UnityEditor;
using UnityEngine;
using System.Diagnostics;

[CustomEditor(typeof(HalfEdgeTester))]
public class HalfEdgeTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HalfEdgeTester tester = (HalfEdgeTester)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear Mesh"))
        {
            Stopwatch stopwatch = new Stopwatch(); // TODO Remove this and place it in the mesh class where the function is defined
            stopwatch.Start();

            tester.ClearMesh();

            stopwatch.Stop();
            UnityEngine.Debug.Log($"Mesh cleared in {stopwatch.Elapsed.TotalMilliseconds:F4} ms.");
        }

        if (GUILayout.Button("Select Random Face"))
        {
            tester.SelectRandomFace();
        }

        if (GUILayout.Button("Create Tetrahedron"))
        {
            Stopwatch stopwatch = new Stopwatch(); // TODO Remove this and place it in the mesh class where the function is defined
            stopwatch.Start();

            tester.CreateTetrahedron();

            stopwatch.Stop();
            UnityEngine.Debug.Log($"Tetrahedron created in {stopwatch.Elapsed.TotalMilliseconds:F4} ms.");
        }

        // Future shapes here:
        // if (GUILayout.Button("Create Cube")) { ... }
    }
}
