using System.Collections.Generic;
using UnityEngine;

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

public class RSFace
{
    public List<RSVertex> vertices = new List<RSVertex>(); // ordered list of vertex indices

    public RSFace(List<RSVertex> vertices)
    {
        this.vertices = vertices;
    }
}

public class RSVertex
{
    public Vector3 position;
    public List<RSFace> faces = new List<RSFace>(); // Faces that this vertex belongs to
    public List<RSEdge> edges = new List<RSEdge>(); // ordered list of neighbor vertices

    public RSVertex(Vector3 pos)
    {
        position = pos;
    }
}
