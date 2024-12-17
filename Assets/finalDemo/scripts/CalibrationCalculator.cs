using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

public class CalibrationCalculator : MonoBehaviour
{
    [SerializeField] private ArUcoServer server;
    [SerializeField] private int anchorMarkerId;
    [SerializeField] private List<int> targetMarkerIds;
    [SerializeField] private float updateInterval = 1f;

    private Dictionary<int, (Vector3 relativePosition, float relativeRotation)> relativePositions 
        = new Dictionary<int, (Vector3, float)>();
    private float lastUpdateTime;

    void Start()
    {
        if (server == null)
        {
            Debug.LogError("ArUcoServer reference not set!");
            enabled = false;
            return;
        }

        Debug.Log($"Starting calibration calculator with anchor {anchorMarkerId} and targets {string.Join(", ", targetMarkerIds)}");
        StartCoroutine(PrintCalibrationRoutine());
    }

    void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
         // Check server status
        var allMarkers = server.GetAllMarkerData();
        Debug.Log($"Server has {allMarkers.Count} markers tracked");

        if (server.TryGetMarkerData(anchorMarkerId, out Vector3 anchorPos, out float anchorRot))
        {   
             Debug.Log($"Found anchor {anchorMarkerId} at {anchorPos}");
            relativePositions.Clear();

            foreach (int targetId in targetMarkerIds)
            {
                if (targetId == anchorMarkerId) continue;

                if (server.TryGetMarkerData(targetId, out Vector3 targetPos, out float targetRot))
                {   
                    Debug.Log($"Found target {targetId} at {targetPos}");
                    Vector3 relativePos = targetPos - anchorPos;
                    float relativeRot = targetRot - anchorRot;
                    relativePositions[targetId] = (relativePos, relativeRot);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Anchor marker {anchorMarkerId} not detected!");
        }

        lastUpdateTime = Time.time;
    }

    IEnumerator PrintCalibrationRoutine()
    {
        while (true)
        {
            PrintCalibrationData();
            yield return new WaitForSeconds(1f); // Print every second
        }
    }

     public void PrintDebugInfo()
    {
        Debug.Log($"=== Calibration Calculator Debug ===");
        Debug.Log($"Anchor ID: {anchorMarkerId}");
        Debug.Log($"Target IDs: {string.Join(", ", targetMarkerIds)}");
        Debug.Log($"Relative positions count: {relativePositions.Count}");
        
        var allServerMarkers = server.GetAllMarkerData();
        Debug.Log($"Server tracking {allServerMarkers.Count} markers: {string.Join(", ", allServerMarkers.Keys)}");
    }

    void PrintCalibrationData()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"\n=== Calibration Data at {DateTime.Now:HH:mm:ss} ===");
        sb.AppendLine($"Anchor ID: {anchorMarkerId}");
        
        if (relativePositions.Count == 0)
        {
            sb.AppendLine("No markers currently tracked");
        }
        else
        {
            foreach (var kvp in relativePositions)
            {
                sb.AppendLine($"Marker {kvp.Key}:");
                sb.AppendLine($"  Relative Position: {kvp.Value.relativePosition:F3}");
                sb.AppendLine($"  Relative Rotation: {kvp.Value.relativeRotation:F3}°");
            }
        }
        sb.AppendLine("=====================================");

        Debug.Log(sb.ToString());
    }

    public bool TryGetRelativePosition(int markerId, out Vector3 position, out float rotation)
    {
        if (relativePositions.TryGetValue(markerId, out var data))
        {
            position = data.relativePosition;
            rotation = data.relativeRotation;
            return true;
        }

        position = Vector3.zero;
        rotation = 0f;
        return false;
    }

    public Dictionary<int, (Vector3 position, float rotation)> GetAllRelativePositions()
    {
        return new Dictionary<int, (Vector3, float)>(relativePositions);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
}