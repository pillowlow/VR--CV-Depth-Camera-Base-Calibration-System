using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Meta.Net.NativeWebSocket;
using System.Threading.Tasks;

public class PositionDataWebSocketClient : WebSocketClient
{
    [SerializeField]
    private string streamName = "aruco_position_stream";
    [SerializeField]
    private float requestInterval = 0.05f;

    private Dictionary<int, Vector3> lastKnownPositions = new Dictionary<int, Vector3>();
    private int counter = 0;

    // Define an array for allowed marker IDs
    [SerializeField]
    private int[] allowedMarkerIds = { 1, 2, 3, 4, 5 }; // Example IDs, adjust these based on your requirement

    void Start()
    {
        ConnectToWebSocket();
    }

    public override async void ConnectToWebSocket()
    {
        base.ConnectToWebSocket();
        StartCoroutine(RequestStreamDataCoroutine());
    }

    private IEnumerator RequestStreamDataCoroutine()
    {
        while (true)
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                var message = new Dictionary<string, string>
                {
                    { "command", "request_stream_data" },
                    { "client_id", clientId },
                    { "stream_name", streamName }
                };
                yield return SendWebSocketMessage(JsonConvert.SerializeObject(message));
            }
            yield return new WaitForSeconds(requestInterval);
            Log("Requested stream data.");
        }
    }

    private IEnumerator SendWebSocketMessage(string message)
    {
        Task sendTask = websocket.SendText(message);
        while (!sendTask.IsCompleted) yield return null;

        if (sendTask.IsFaulted)
        {
            Log("Failed to send message over WebSocket: " + sendTask.Exception);
        }
    }

    protected override void HandleServerMessage(string message)
    {
        try
        {
            Debug.Log("Received message from server: " + message);

            var streamDataMessage = JsonConvert.DeserializeObject<StreamDataMessage>(message);

            if (streamDataMessage != null && streamDataMessage.command == "stream_data" && streamDataMessage.data != null)
            {
                foreach (var marker in streamDataMessage.data)
                {
                    int markerId = marker.marker_id;

                    // Check if the marker ID is in the allowed list
                    if (System.Array.Exists(allowedMarkerIds, id => id == markerId))
                    {
                        Vector3 markerPosition = new Vector3(marker.x, marker.y, marker.z);

                        // Log each marker's data
                        Debug.Log($"Allowed Marker ID: {markerId} - Position: X={markerPosition.x}, Y={markerPosition.y}, Z={markerPosition.z} - Counter: {counter}");
                        StreamLog($"Allowed Marker ID: {markerId} - Position: X={markerPosition.x}, Y={markerPosition.y}, Z={markerPosition.z} - Counter: {counter}");

                        // Update the dictionary with the new position data
                        lastKnownPositions[markerId] = markerPosition;
                        counter++;
                    }
                   
                }
            }
            else
            {
                Log("Failed to handle stream data: Command mismatch or missing data.");
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
        Debug.Log("GetMarkerPositions called with data: " + lastKnownPositions);
        return new Dictionary<int, Vector3>(lastKnownPositions);
    }

    private void StreamLog(string message)
    {
        if (streamText != null)
        {
            streamText.text += message + "\n";
            if (streamText.text.Split('\n').Length > 6)
            {
                streamText.text = string.Join("\n", streamText.text.Split('\n')[1..]);
            }
            streamText.ForceMeshUpdate();
        }
    }
}

public class StreamDataMessage
{
    public string command { get; set; }
    public string stream_name { get; set; }
    public List<MarkerData> data { get; set; }
}

public class MarkerData
{
    public int marker_id { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
}

