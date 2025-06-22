using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RSEdge
{
    public RSVertex from;
    public RSVertex to;

    public RSEdge(RSVertex from, RSVertex to)
    {
        this.from = from;
        this.to = to;
    }
}

[System.Serializable]
public class RSFace
{
    public List<RSVertex> vertices = new List<RSVertex>(); // ordered list of vertex indices

    public RSFace(List<RSVertex> vertices)
    {
        this.vertices = vertices;
    }
}

[System.Serializable]
public class RSVertex
{
    public Vector3 position;
    [System.NonSerialized] // To avoid serialization cycles
    public List<RSFace> faces = new List<RSFace>(); // Faces that this vertex belongs to
    [System.NonSerialized] // To avoid serialization cycles
    public List<RSEdge> edges = new List<RSEdge>(); // ordered list of neighbor vertices

    public RSVertex(Vector3 pos)
    {
        position = pos;
        faces = new List<RSFace>();
        edges = new List<RSEdge>();
    }
}
