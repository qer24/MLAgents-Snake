using System;
using System.Collections.Generic;
using UnityEngine;

public class SnakeMarkerManager : MonoBehaviour
{
    public readonly List<SnakeMarker> Markers = new();

    public void AddMarker()
    {
        Markers.Add(new SnakeMarker(transform.localPosition, transform.rotation));
    }

    public void ResetMarkers(bool addFirstMarker = true)
    {
        Markers.Clear();

        if (addFirstMarker) Markers.Add(new SnakeMarker(transform.localPosition, transform.rotation));
    }
}
