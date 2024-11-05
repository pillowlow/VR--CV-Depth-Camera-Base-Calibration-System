using UnityEngine;
using TMPro;
using Meta.Net.NativeWebSocket;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class WebSocketClient : MonoBehaviour
{
    protected WebSocket websocket;
    public string clientId = "UnityClient1";  // Client ID sent to server
    public TMP_InputField inputField;
    public TextMeshProUGUI logText;
    public TextMeshProUGUI streamText;
    public string serverPort = "ws://192.168.100.157:8080/";

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
            Log("Connection open!");
            await SendClientId();
        };

        websocket.OnError += (e) =>
        {
            Log("Error: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            HandleServerMessage(message);
        };

        await websocket.Connect();
    }

    private async Task SendClientId()
    {
        var message = new Dictionary<string, string>
        {
            { "command", "client_id" },
            { "client_id", clientId }
        };
        await websocket.SendText(JsonConvert.SerializeObject(message));
    }

    public virtual async void DisconnectWebSocket()
    {
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

    protected virtual void HandleServerMessage(string message)
    {
        Log("Received message: " + message);
    }

    public void Log(string message)
    {
        if (logText != null)
        {
            logText.text += message + "\n";
            if (logText.text.Split('\n').Length > 6)
            {
                logText.text = string.Join("\n", logText.text.Split('\n')[1..]);
            }
            logText.ForceMeshUpdate();
        }
        else
        {
            Debug.LogError("logText is null. Assign it in the Inspector.");
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
}
