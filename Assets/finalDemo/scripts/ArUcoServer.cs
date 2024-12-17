using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections.Generic;

public class ArUcoServer : MonoBehaviour
{
    private UdpClient server;
    private const int PORT = 12345;
    private bool isRunning = true;

    // Dictionary to store marker data
    private Dictionary<int, TrackedMarker> trackedMarkers = new Dictionary<int, TrackedMarker>();

    public class TrackedMarker
    {
        public Vector3 position;
        public float rotation;
        public double lastUpdateTime;
        public List<(Vector3 position, float rotation)> currentFrameData = new List<(Vector3, float)>();

        public void UpdateData()
        {
            if (currentFrameData.Count == 0) return;

            Vector3 avgPosition = Vector3.zero;
            float avgRotation = 0f;

            foreach (var data in currentFrameData)
            {
                avgPosition += data.position;
                avgRotation += data.rotation;
            }

            position = avgPosition / currentFrameData.Count;
            rotation = avgRotation / currentFrameData.Count;
            lastUpdateTime = Time.time;
            currentFrameData.Clear();
        }
    }

    [System.Serializable]
    private class Position
    {
        public float x;
        public float y;
        public float z;

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [System.Serializable]
    private class MarkerData
    {
        public Position position;
        public float rotation;
    }

    [System.Serializable]
    private class MessageData
    {
        public double timestamp;
        public Dictionary<string, MarkerData> markers;
    }

    void Start()
    {
        InitializeServer();
    }

    // Public method to get marker data
    public bool TryGetMarkerData(int markerId, out Vector3 position, out float rotation)
    {
        if (trackedMarkers.TryGetValue(markerId, out TrackedMarker marker))
        {
            position = marker.position;
            rotation = marker.rotation;
            return true;
        }

        position = Vector3.zero;
        rotation = 0f;
        return false;
    }

    // Get all tracked markers
    public Dictionary<int, (Vector3 position, float rotation)> GetAllMarkerData()
    {
        var result = new Dictionary<int, (Vector3, float)>();
        foreach (var marker in trackedMarkers)
        {
            result[marker.Key] = (marker.Value.position, marker.Value.rotation);
        }
        return result;
    }

    private void RegisterOrUpdateMarker(int id, Vector3 position, float rotation)
    {
        if (!trackedMarkers.ContainsKey(id))
        {
            trackedMarkers[id] = new TrackedMarker();
            Debug.Log($"New marker registered: {id}");
        }

        trackedMarkers[id].currentFrameData.Add((position, rotation));
    }

    private void ProcessMessage(string jsonString)
    {
        try
        {
            MessageData message = JsonUtility.FromJson<MessageData>(jsonString);

            // Clear current frame data for all markers
            foreach (var marker in trackedMarkers.Values)
            {
                marker.currentFrameData.Clear();
            }

            if (message.markers != null)
            {
                foreach (var marker in message.markers)
                {
                    int id = int.Parse(marker.Key);
                    Vector3 position = marker.Value.position.ToVector3();
                    float rotation = marker.Value.rotation;

                    RegisterOrUpdateMarker(id, position, rotation);
                }

                // Update all markers with their average positions
                foreach (var marker in trackedMarkers.Values)
                {
                    marker.UpdateData();
                }

                // Debug output
                foreach (var marker in trackedMarkers)
                {
                    Debug.Log($"Updated Marker {marker.Key}: Position: {marker.Value.position:F3}, " +
                            $"Rotation: {marker.Value.rotation:F3}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Processing Error: {e.Message}\nJSON: {jsonString}");
        }
    }

    // Rest of the server code remains the same...
    void InitializeServer()
    {
        server = new UdpClient(PORT);
        Debug.Log($"UDP Server started on port {PORT}");
        BeginReceiving();
    }

    async void BeginReceiving()
    {
        while (isRunning)
        {
            try
            {
                UdpReceiveResult result = await server.ReceiveAsync();
                string jsonString = Encoding.UTF8.GetString(result.Buffer);
                ProcessMessage(jsonString);

                byte[] ackData = Encoding.UTF8.GetBytes("ACK");
                await server.SendAsync(ackData, ackData.Length, result.RemoteEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"Server Error: {e.Message}");
            }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (server != null)
            server.Close();
    }
}