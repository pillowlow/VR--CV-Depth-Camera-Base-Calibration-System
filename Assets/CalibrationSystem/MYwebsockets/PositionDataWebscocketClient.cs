using UnityEngine;
using TMPro;
using Meta.Net.NativeWebSocket;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;


public class PositionDataWebSocketClient : WebSocketClient
{
    [SerializeField]
    private string streamName = "aruco_position_stream";  // Serialized stream name to be set in Unity Inspector

    [SerializeField]
    private float requestInterval = 0.1f;  // Interval for stream data requests (in seconds)

    // Dictionary to store marker positions (ID and last known position)
    private Dictionary<int, Vector3> lastKnownPositions = new Dictionary<int, Vector3>();

    public class Position
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class MarkerData
    {
        public int marker_id { get; set; }
        public Position position { get; set; }
    }

    public class StreamDataMessage
    {
        public string command { get; set; }
        public string stream_name { get; set; }
        public List<MarkerData> data { get; set; }
    }



    void Start()
    {
        // Connect to WebSocket on start
        ConnectToWebSocket();
    }

    // Override the connection to automatically start requesting stream data after connection
    public override async void ConnectToWebSocket()
    {
        base.ConnectToWebSocket();  // Connect to the server

        // Start requesting stream data every 0.1 seconds once connected
        StartRequestingStreamData();
    }

    // Automatically request stream data every 0.1 seconds
    private async void StartRequestingStreamData()
    {
        while (true)
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                // Send the request for stream data
                var message = new Dictionary<string, string>
                {
                    { "command", "request_stream_data" },
                    { "client_id", clientId },
                    { "stream_name", streamName }
                };
                await websocket.SendText(JsonConvert.SerializeObject(message));
                //Log($"Requested stream data for '{streamName}'");
            }
            
            // Wait for the next request
            await Task.Delay((int)(requestInterval * 1000));  // 0.1 second interval
        }
    }

    // Handle incoming messages (override to handle position data)
   // Handle incoming messages (override to handle position data)
    protected override void HandleServerMessage(string message)
    {
        try
        {
            // Deserialize the incoming message as a StreamDataMessage object
            var streamDataMessage = JsonConvert.DeserializeObject<StreamDataMessage>(message);

            if (streamDataMessage != null && streamDataMessage.command == "stream_data")
            {
                if (streamDataMessage.data != null)
                {
                    foreach (var marker in streamDataMessage.data)
                    {
                        // Extract marker ID and position
                        int markerId = marker.marker_id;
                        Vector3 markerPosition = new Vector3(marker.position.x, marker.position.y, marker.position.z);

                        // Log the marker data
                        StreamLog($"Marker ID: {markerId} - Position: X={marker.position.x}, Y={marker.position.y}, Z={marker.position.z}");

                        // ** Update the lastKnownPositions dictionary with the new data **
                        lastKnownPositions[markerId] = markerPosition;
                    }
                }
            }
            else
            {
                Log("Failed to handle stream data. Command mismatch or missing data.");
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError("Failed to parse message as JSON: " + ex.Message);
            Log("Failed to parse message as JSON.");
        }
    }


   public Dictionary<int, Vector3> GetMarkerPositions()
    {   
        //Log("GetMarkerPositions called");
        // Log the content of the lastKnownPositions dictionary
        if (lastKnownPositions.Count > 0)
        {
            foreach (var marker in lastKnownPositions)
            {
                Log($"Marker ID: {marker.Key}, Position: X={marker.Value.x}, Y={marker.Value.y}, Z={marker.Value.z}");
            }
        }
        else
        {
            Log("No markers currently sent.");
        }

        // Return a copy of the dictionary
        return new Dictionary<int, Vector3>(lastKnownPositions);
    }

    // Removed UI elements for requesting stream data (stream input and button) as they are not needed now
    public void UpdateUI()
    {
        GUILayout.Label("WebSocket Client - Streaming ArUco Position Data");

        // Display the current marker positions in the log
        GUILayout.Label("Last Known Marker Positions:");
        foreach (var entry in lastKnownPositions)
        {
            GUILayout.Label($"Marker ID: {entry.Key} - Position: {entry.Value}");
        }
    }
}
