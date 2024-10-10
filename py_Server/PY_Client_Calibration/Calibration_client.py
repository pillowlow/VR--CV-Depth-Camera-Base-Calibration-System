import tkinter as tk
from tkinter import scrolledtext
import asyncio
import websockets
import json

class WebSocketClient:
    def __init__(self, root):
        self.root = root
        self.root.title("WebSocket Client")
        self.websocket = None
        self.client_id = "PythonClient1"  # Define a client ID

        # Set up the GUI
        self.main_frame = tk.Frame(root)
        self.main_frame.pack(fill=tk.BOTH, expand=True)

        # Server connection controls
        self.connect_button = tk.Button(self.main_frame, text="Connect", command=self.connect_to_server)
        self.connect_button.pack()

        self.disconnect_button = tk.Button(self.main_frame, text="Disconnect", command=self.disconnect_from_server, state=tk.DISABLED)
        self.disconnect_button.pack()

        # Input field to send messages
        self.input_field = tk.Entry(self.main_frame)
        self.input_field.pack(fill=tk.X)
        self.input_field.bind('<Return>', self.send_message)

        # Log display
        self.log_text = scrolledtext.ScrolledText(self.main_frame, state='disabled', height=20)
        self.log_text.pack(fill=tk.BOTH, expand=True)

    async def connect(self):
        try:
            self.websocket = await websockets.connect("ws://127.0.0.1:8080")
            self.log_message("Connected to server")
            self.connect_button.config(state=tk.DISABLED)
            self.disconnect_button.config(state=tk.NORMAL)

            # Send client ID to server in JSON format
            client_id_message = {
                "command": "client_id",
                "client_id": self.client_id
            }
            await self.websocket.send(json.dumps(client_id_message))
            self.log_message(f"Sent client ID: {self.client_id}")

            # Listen for messages
            await self.receive_messages()

        except Exception as e:
            self.log_message(f"Failed to connect: {e}")

    async def receive_messages(self):
        try:
            async for message in self.websocket:
                self.log_message(f"Received: {message}")
                # Handle the received JSON message
                await self.handle_server_message(message)
        except websockets.ConnectionClosed:
            self.log_message("Connection closed by server")
        except Exception as e:
            self.log_message(f"Error: {e}")

    async def handle_server_message(self, message):
        try:
            data = json.loads(message)
            command = data.get("command")

            if command == "stream_data":
                self.handle_stream_data(data)
            elif command == "broadcast":
                self.log_message(f"Broadcast: {data.get('data')}")
            else:
                self.log_message(f"Unknown command: {command}")
        except json.JSONDecodeError:
            self.log_message("Failed to parse JSON message.")

    def handle_stream_data(self, data):
        stream_name = data.get("stream_name")
        stream_data = data.get("data")
        self.log_message(f"Stream '{stream_name}': {stream_data}")

    async def disconnect(self):
        if self.websocket is not None:
            await self.websocket.close()
            self.log_message("Disconnected from server")
            self.connect_button.config(state=tk.NORMAL)
            self.disconnect_button.config(state=tk.DISABLED)
            self.websocket = None

    async def send(self, message):
        if self.websocket is not None and self.websocket.open:
            await self.websocket.send(message)
            self.log_message(f"Sent: {message}")

    def connect_to_server(self):
        asyncio.ensure_future(self.connect())

    def disconnect_from_server(self):
        asyncio.ensure_future(self.disconnect())

    def send_message(self, event=None):
        message = self.input_field.get()
        if message and self.websocket is not None:
            # Send message in JSON format
            message_data = {
                "command": "message",
                "client_id": self.client_id,
                "data": message
            }
            asyncio.ensure_future(self.send(json.dumps(message_data)))
            self.input_field.delete(0, tk.END)

    def log_message(self, message):
        self.log_text.config(state='normal')
        self.log_text.insert(tk.END, message + '\n')
        self.log_text.config(state='disabled')
        self.log_text.yview(tk.END)

if __name__ == "__main__":
    root = tk.Tk()
    client = WebSocketClient(root)
    root.mainloop()
