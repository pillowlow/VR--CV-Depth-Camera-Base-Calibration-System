using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;  // Add this line

public class ArUcoServer : MonoBehaviour
{
    private UdpClient server;
    private const int PORT = 12345;
    private bool isRunning = true;

    // test connetion
    private bool isClientConnected = false;
    private float lastMessageTime = 0f;
    private const float CONNECTION_TIMEOUT = 2f;
    private int messagesReceived = 0;

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
    public class MessageData
    {
        public double timestamp;
        public Dictionary<string, MarkerData> markers;
    }

    [System.Serializable]
    public class MarkerData  // Change to public
    {
        public Position position;
        public float rotation;
    }

    [System.Serializable]
    public class Position    // Change to public
    {
        public float x;
        public float y;
        public float z;

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }



    void Start()
    {
        InitializeServer();
    }
     private void Update()
    {
        if (isClientConnected && Time.time - lastMessageTime > CONNECTION_TIMEOUT)
        {
            isClientConnected = false;
            Debug.Log("Client disconnected (timeout)");
        }
    }

    // Public method to get marker data
    public bool TryGetMarkerData(int markerId, out Vector3 position, out float rotation)
    {   
        Debug.Log($"Trying to get data for marker {markerId}");
        Debug.Log($"Currently tracking {trackedMarkers.Count} markers: {string.Join(", ", trackedMarkers.Keys)}");
        
        if (trackedMarkers.TryGetValue(markerId, out TrackedMarker marker))
        {
            position = marker.position;
            rotation = marker.rotation;
            Debug.Log($"Found marker {markerId}: pos={position}, rot={rotation}");
            return true;
        }

        position = Vector3.zero;
        rotation = 0f;
        Debug.Log($"Marker {markerId} not found in tracked markers");
        return false;
    }
    // Add this method to ArUcoServer
    public void PrintServerStatus()
    {
        Debug.Log($"=== Server Status ===");
        Debug.Log($"Connected: {isClientConnected}");
        Debug.Log($"Total messages received: {messagesReceived}");
        Debug.Log($"Tracked markers count: {trackedMarkers.Count}");
        
        foreach (var marker in trackedMarkers)
        {
            Debug.Log($"Marker {marker.Key}:");
            Debug.Log($"  Position: {marker.Value.position:F3}");
            Debug.Log($"  Rotation: {marker.Value.rotation:F3}");
            Debug.Log($"  Last update: {Time.time - marker.Value.lastUpdateTime:F1}s ago");
        }
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
        //Debug.Log("Raw received JSON: " + jsonString);  // Print exact received JSON
        try
        {
            MessageData message = JsonConvert.DeserializeObject<MessageData>(jsonString);

            // Clear current frame data for all markers
            foreach (var marker in trackedMarkers.Values)
            {
                marker.currentFrameData.Clear();
            }

            
            Debug.Log($"Received message with {message.markers?.Count ?? 0} markers");


            if (message.markers != null)
            {   
                Debug.Log("Processing markers: " + string.Join(", ", message.markers.Keys));
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
                /*
                foreach (var marker in trackedMarkers)
                {   
                    
                    Debug.Log($"Updated Marker {marker.Key}: Position: {marker.Value.position:F3}, " +
                            $"Rotation: {marker.Value.rotation:F3}");
                }*/
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
        Debug.Log("Server started listening on port " + PORT);
        while (isRunning)
        {
            try
            {
                UdpReceiveResult result = await server.ReceiveAsync();
                
                // Add connection status logging
                if (!isClientConnected)
                {
                    isClientConnected = true;
                    Debug.Log($"Client connected from {result.RemoteEndPoint}");
                }

                lastMessageTime = Time.time;
                messagesReceived++;
                
                if (messagesReceived % 100 == 0)
                {
                    Debug.Log($"Connection active. Total messages: {messagesReceived}");
                }

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