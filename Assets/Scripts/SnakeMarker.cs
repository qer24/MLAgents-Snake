using UnityEngine;

public class SnakeMarker
{
    public Vector3 position;
    public Quaternion rotation;

    public SnakeMarker(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }
}
