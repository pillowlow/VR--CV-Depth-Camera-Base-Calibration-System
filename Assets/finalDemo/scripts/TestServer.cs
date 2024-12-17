using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestServer : MonoBehaviour
{
    private ArUcoServer arucoServer;

    void Start()
    {
        arucoServer = FindObjectOfType<ArUcoServer>();
    }

    void Update()
    {
        // Get single marker
        if (arucoServer.TryGetMarkerData(1, out Vector3 pos, out float rot))
        {
            Debug.Log($"Marker 1: Pos={pos}, Rot={rot}");
        }

        // Get all markers
        var allMarkers = arucoServer.GetAllMarkerData();
        foreach (var marker in allMarkers)
        {
            Debug.Log($"Marker {marker.Key}: Pos={marker.Value.position}, Rot={marker.Value.rotation}");
        }
    }
}