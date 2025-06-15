using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using UnityEditor.SearchService;

[CustomEditor(typeof(HalfEdgeTester))]
public class HalfEdgeTesterEditor : Editor
{
    private int numberOfTests = 10; // Default value for the number of tests

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

        if (GUILayout.Button("Create Quad"))
        {
            tester.CreateQuad();
        }

        if (GUILayout.Button("Create Penta"))
        {
            tester.CreatePentagon();
        }

        if (GUILayout.Button("Create Hexa"))
        {
            tester.CreateHexagon();
        }

        if (GUILayout.Button("Create Septa"))
        {
            tester.CreateSeptagon();
        }

        if (GUILayout.Button("Create Octa"))
        {
            tester.CreateOctagon();
        }

        if (GUILayout.Button("Create  Nona"))
        {
            tester.CreateNonagon();
        }

        if (GUILayout.Button("Face Split"))
        {
            tester.SelectRandomFace();
            tester.SplitFace();
        }

        numberOfTests = EditorGUILayout.IntField("Number of Tests", numberOfTests);
        if (GUILayout.Button("Mass Test Split Face"))
        {
            for (int i = 0; i < numberOfTests; i++)
            {
                tester.SelectRandomFace();
                tester.SplitFace();
            }
        }

        // Future shapes here:
        // if (GUILayout.Button("Create Cube")) { ... }
        SceneView.RepaintAll(); // Force Scene to refresh the Gizmos
    }
}
