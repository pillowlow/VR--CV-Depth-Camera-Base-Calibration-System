import tkinter as tk
from tkinter import scrolledtext, messagebox
import asyncio
import websockets
import json
import threading

class WebSocketClient:
    def __init__(self, uri):
        self.uri = uri
        self.websocket = None
        self.connected = False
        self.loop = asyncio.new_event_loop()

    async def connect(self):
        try:
            self.websocket = await websockets.connect(self.uri)
            self.connected = True
            print("Connected to the server.")
        except Exception as e:
            print(f"Failed to connect: {e}")
            self.connected = False

    async def send_message(self, message):
        if self.connected and self.websocket:
            try:
                await self.websocket.send(json.dumps(message))
                print(f"Sent: {message}")
            except Exception as e:
                print(f"Failed to send message: {e}")

    async def receive_message(self):
        if self.connected and self.websocket:
            try:
                message = await self.websocket.recv()
                return json.loads(message)
            except Exception as e:
                print(f"Failed to receive message: {e}")
                return None

    async def close_connection(self):
        if self.connected and self.websocket:
            await self.websocket.close()
            self.connected = False
            print("Disconnected from the server.")

class WebSocketClientGUI:
    def __init__(self, master):
        self.master = master
        self.master.title("WebSocket Client")
        
        # GUI Components
        self.url_label = tk.Label(master, text="Server URL:")
        self.url_label.pack(pady=5)
        
        self.url_entry = tk.Entry(master, width=50)
        self.url_entry.pack(pady=5)
        self.url_entry.insert(0, "ws://192.168.1.114:8080")  # Default server URL
        
        self.connect_button = tk.Button(master, text="Connect", command=self.connect_to_server)
        self.connect_button.pack(pady=5)
        
        self.disconnect_button = tk.Button(master, text="Disconnect", state=tk.DISABLED, command=self.disconnect_from_server)
        self.disconnect_button.pack(pady=5)
        
        self.message_label = tk.Label(master, text="Message:")
        self.message_label.pack(pady=5)
        
        self.message_entry = tk.Entry(master, width=50)
        self.message_entry.pack(pady=5)
        
        self.send_button = tk.Button(master, text="Send", state=tk.DISABLED, command=self.send_message)
        self.send_button.pack(pady=5)
        
        self.log_text = scrolledtext.ScrolledText(master, state='disabled', height=15, width=60)
        self.log_text.pack(pady=5)
        
        # WebSocket Client
        self.client = None
        self.connected = False

    def connect_to_server(self):
        uri = self.url_entry.get()
        self.client = WebSocketClient(uri)
        
        def run_loop():
            asyncio.set_event_loop(self.client.loop)
            self.client.loop.run_until_complete(self.client.connect())
            if self.client.connected:
                self.start_receiving()
        
        threading.Thread(target=run_loop).start()
        
        if self.client.connected:
            self.connect_button.config(state=tk.DISABLED)
            self.disconnect_button.config(state=tk.NORMAL)
            self.send_button.config(state=tk.NORMAL)
            self.log_message("Connected to the server.")
        else:
            self.log_message("Failed to connect to the server.")

    def disconnect_from_server(self):
        if self.client:
            asyncio.run_coroutine_threadsafe(self.client.close_connection(), self.client.loop)
            self.connect_button.config(state=tk.NORMAL)
            self.disconnect_button.config(state=tk.DISABLED)
            self.send_button.config(state=tk.DISABLED)
            self.log_message("Disconnected from the server.")

    def send_message(self):
        message = self.message_entry.get()
        if message:
            asyncio.run_coroutine_threadsafe(self.client.send_message({"command": "message", "data": message}), self.client.loop)
            self.message_entry.delete(0, tk.END)
            self.log_message(f"Sent: {message}")

    def log_message(self, message):
        self.log_text.config(state='normal')
        self.log_text.insert(tk.END, message + '\n')
        self.log_text.config(state='disabled')
        self.log_text.yview(tk.END)

    def start_receiving(self):
        async def receive():
            while self.client.connected:
                message = await self.client.receive_message()
                if message:
                    self.log_message(f"Received: {message}")
        
        threading.Thread(target=lambda: asyncio.run_coroutine_threadsafe(receive(), self.client.loop)).start()

if __name__ == "__main__":
    root = tk.Tk()
    gui = WebSocketClientGUI(root)
    root.mainloop()
