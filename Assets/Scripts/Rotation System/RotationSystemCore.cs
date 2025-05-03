using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RsVertex
{
    public Vector3 position;
    public List<int> neighbors = new List<int>(); // ordered list of neighbor vertex indices

    public RsVertex(Vector3 pos)
    {
        position = pos;
    }
}
