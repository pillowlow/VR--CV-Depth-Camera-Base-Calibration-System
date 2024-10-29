import asyncio
import websockets
import json
import tkinter as tk
from tkinter import messagebox
import threading

class WebsocketClient:
    def __init__(self):
        self.window = tk.Tk()
        self.window.title("Client Connection")

        tk.Label(self.window, text="Server Host:").grid(row=0, column=0)
        tk.Label(self.window, text="Server Port:").grid(row=1, column=0)
        tk.Label(self.window, text="Client ID:").grid(row=2, column=0)

        self.host_entry = tk.Entry(self.window)
        self.port_entry = tk.Entry(self.window)
        self.client_id_entry = tk.Entry(self.window)

        self.host_entry.grid(row=0, column=1)
        self.port_entry.grid(row=1, column=1)
        self.client_id_entry.grid(row=2, column=1)

        self.connect_button = tk.Button(self.window, text="Connect", command=self.start_connection)
        self.connect_button.grid(row=3, column=0, columnspan=2)

        self.window.protocol("WM_DELETE_WINDOW", self.on_close)
        self.websocket = None
        self.loop = asyncio.get_event_loop()

    def start_connection(self):
        host = self.host_entry.get()
        port = self.port_entry.get()
        client_id = self.client_id_entry.get()

        if not host or not port or not client_id:
            messagebox.showerror("Input Error", "Please provide host, port, and client ID.")
            return

        # Start the connection in a new thread to avoid blocking the GUI
        threading.Thread(target=self.loop.run_until_complete, args=(self.run_client(host, port, client_id),), daemon=True).start()

    async def send_id(self, websocket, client_id):
        await websocket.send(json.dumps({"client_id": client_id}))
        print(f"Sent client_id: {client_id} to server")

    async def listen_to_server(self, websocket):
        try:
            async for message in websocket:
                data = json.loads(message)
                print(f"Message received from server: {data}")
                if data.get("command") == "REQUEST_ID":
                    await self.send_id(websocket, self.client_id_entry.get())
        except websockets.ConnectionClosed:
            print("Connection to server closed.")
        except Exception as e:
            print(f"Error while listening to server: {e}")

    async def run_client(self, host, port, client_id):
        uri = f"ws://{host}:{port}"
        try:
            async with websockets.connect(uri) as websocket:
                self.websocket = websocket
                print(f"Connected to server at {uri}")
                await self.listen_to_server(websocket)
        except Exception as e:
            messagebox.showerror("Connection Error", f"Could not connect to server: {e}")
            print(f"Could not connect to server: {e}")

    def on_close(self):
        if self.websocket and not self.websocket.closed:
            self.loop.run_until_complete(self.websocket.close())
        self.window.destroy()

    def run(self):
        self.window.mainloop()


if __name__ == "__main__":
    client = WebSocketClient()
    client.run()
