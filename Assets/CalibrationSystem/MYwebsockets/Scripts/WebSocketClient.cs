using UnityEngine;
using TMPro;
using Meta.Net.NativeWebSocket;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

public class WebSocketClient : MonoBehaviour
{
    protected WebSocket websocket;
    public string clientId = "UnityClient1"; // The ID that the client will send to the server
    public TMP_InputField inputField;
    public TMP_InputField streamRequestInput; // Input field for stream request
    public TextMeshProUGUI logText;
    public TextMeshProUGUI streamText;
    public string serverPort = "ws://192.168.100.157:8080/"; 

    private string currentStream = ""; // Track the currently requested stream

    void Start()
    {
        logText.text = "WebSocket Client Ready.\n";
    }

    public virtual async void ConnectToWebSocket()
    {
        Log("Attempting to connect...");

        websocket = new WebSocket(serverPort);

        websocket.OnOpen += async () =>
        {
            Debug.Log("Connection open!");
            Log("Connection open!");

            // Send client ID to the server in JSON format
            var message = new Dictionary<string, string>
            {
                { "command", "client_id" },
                { "client_id", clientId }
            };
            await websocket.SendText(JsonConvert.SerializeObject(message));
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
            Log("Error: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
            Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = Encoding.UTF8.GetString(bytes);
            Debug.Log("OnMessage! " + message);
            HandleServerMessage(message);
        };

        await websocket.Connect();
    }

    public virtual async void DisconnectWebSocket()
    {
        Log("Disconnecting...");
        if (websocket != null)
        {
            await websocket.Close();
            Log("Disconnected from WebSocket.");
        }
    }

    public virtual async void SendMessage()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var message = new Dictionary<string, string>
            {
                { "command", "message" },
                { "client_id", clientId },
                { "data", inputField.text }
            };
            await websocket.SendText(JsonConvert.SerializeObject(message));
            Log("Sent: " + inputField.text);
        }
        else
        {
            Log("WebSocket is not connected.");
        }
    }

    public virtual async void RequestStreamData()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            currentStream = streamRequestInput.text;
            var message = new Dictionary<string, string>
            {
                { "command", "request_stream_data" },
                { "client_id", clientId },
                { "stream_name", currentStream }
            };
            await websocket.SendText(JsonConvert.SerializeObject(message));
            Log("Requested stream: " + currentStream);
        }
        else
        {
            Log("WebSocket is not connected.");
        }
    }

    protected virtual void HandleServerMessage(string message)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);

            if (data.ContainsKey("command"))
            {
                var command = data["command"];

                switch (command)
                {
                    case "stream_data":
                        HandleStreamData(data);
                        break;
                    case "broadcast":
                        Log("Broadcast: " + data["data"]);
                        break;
                    case "REQUEST_ID":
                        Log("Send IO to server");
                        break;
                    default:
                        Log("Unknown command: " + command);
                        break;
                }
            }
            else
            {
                Log("Received unrecognized message format.");
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError("Failed to parse message as JSON: " + ex.Message);
            Log("Failed to parse message as JSON.");
        }
    }

    protected virtual void HandleStreamData(Dictionary<string, string> data)
    {
        if (data.ContainsKey("stream_name") && data.ContainsKey("data"))
        {
            string streamName = data["stream_name"];
            if (streamName == currentStream)
            {
                if (float.TryParse(data["data"], out float streamValue))
                {
                    Log(streamValue.ToString());  // Call unified method for updating UI
                }
                else
                {
                    Log("Received invalid stream data.");
                }
            }
        }
        else
        {
            Log("Received incomplete stream data.");
        }
    }

    // Virtual method to handle any UI update: Value text and log text
    

    public void Log(string message)
    {
        Debug.Log("Log: " + message);

        if (logText != null)
        {
            logText.text += message + "\n";
            string[] lines = logText.text.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length > 6)
            {
                logText.text = string.Join("\n", lines, 1, lines.Length - 1) + "\n";
            }

            logText.ForceMeshUpdate();
        }
        else
        {
            Debug.LogError("logText is null. Make sure it is assigned in the Inspector.");
        }
    }

    public void StreamLog(string message)
    {
        Debug.Log("Log: " + message);

        if (streamText != null)
        {
            streamText.text += message + "\n";
            string[] lines = streamText.text.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length > 6)
            {
                streamText.text = string.Join("\n", lines, 1, lines.Length - 1) + "\n";
            }

            streamText.ForceMeshUpdate();
        }
        else
        {
            Debug.LogError("streamText is null. Make sure it is assigned in the Inspector.");
        }
    }

    void Update()
    {
        if (websocket != null)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
            #endif
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }

    public virtual void TestButton (string message)
    {
        Log("Clicked" + message);
        Debug.Log("Clicked" + message);
    }
}
