using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class PlyImporter
{
    public static void LoadPlyFromText(string plyText, out List<Vector3> vertices, out List<int[]> faces)
    {
        vertices = new List<Vector3>();
        faces = new List<int[]>();

        StringReader reader = new StringReader(plyText);
        string line;
        int vertexCount = 0, faceCount = 0;
        bool headerEnded = false;

        // PLY header
        while ((line = reader.ReadLine()) != null && !headerEnded)
        {
            if (line.StartsWith("element vertex"))
                int.TryParse(line.Split()[2], out vertexCount);
            else if (line.StartsWith("element face"))
                int.TryParse(line.Split()[2], out faceCount);
            else if (line.StartsWith("end_header"))
                headerEnded = true;
        }

        // Vertices
        for (int i = 0; i < vertexCount; i++)
        {
            line = reader.ReadLine();
            if (line == null)
            {
                Debug.LogError("Unexpected end of file while reading vertices.");
                break;
            }

            var parts = line.Split();
            if (parts.Length < 3)
            {
                Debug.LogError($"Vertex line has too few components: {line}");
                continue;
            }

            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                vertices.Add(new Vector3(x, y, z));
            }
            else
            {
                Debug.LogError($"Failed to parse vertex line: {line}");
            }
        }


        // Faces
        for (int i = 0; i < faceCount; i++)
        {
            line = reader.ReadLine();
            if (line == null)
            {
                Debug.LogError("Unexpected end of file while reading faces.");
                break;
            }

            var parts = line.Split();
            if (parts.Length < 4)
            {
                Debug.LogError($"Face line too short: {line}");
                continue;
            }

            if (int.Parse(parts[0]) != 3)
            {
                Debug.LogWarning($"Non-triangle face skipped: {line}");
                continue;
            }

            int a = int.Parse(parts[1]);
            int b = int.Parse(parts[2]);
            int c = int.Parse(parts[3]);
            faces.Add(new[] { a, b, c });
        }

    }
}
