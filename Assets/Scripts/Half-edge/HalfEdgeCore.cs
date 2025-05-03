using UnityEngine;

public class HEVertex
{
    public Vector3 position;
    public HEHalfEdge outgoing;

    public HEVertex(Vector3 pos)
    {
        position = pos;
    }
}

public class HEHalfEdge
{
    public HEVertex vertex;
    public HEVertex origin;
    public HEHalfEdge twin;
    public HEHalfEdge next;
    public HEFace face;
}

public class HEFace
{
    public HEHalfEdge edge;
}
