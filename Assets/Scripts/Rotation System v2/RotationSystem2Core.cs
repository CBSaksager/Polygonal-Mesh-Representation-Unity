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

public class RSVertex
{
    public Vector3 position;
    public List<RSEdge> edges = new List<RSEdge>(); // ordered list of neighbor vertex indices

    public RSVertex(Vector3 pos)
    {
        position = pos;
    }
}
