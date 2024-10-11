import asyncio
import websockets
import json
import tkinter as tk
from tkinter import messagebox
import threading
import base64
import numpy as np
import cv2
from PIL import Image, ImageTk

class StreamRequestClient:
    def __init__(self):
        self.window = tk.Tk()
        self.window.title("Stream Request Client")

        # GUI for input fields
        tk.Label(self.window, text="Server Host:").grid(row=0, column=0)
        tk.Label(self.window, text="Server Port:").grid(row=1, column=0)
        tk.Label(self.window, text="Client ID:").grid(row=2, column=0)
        tk.Label(self.window, text="Stream Name:").grid(row=3, column=0)

        self.host_entry = tk.Entry(self.window)
        self.port_entry = tk.Entry(self.window)
        self.client_id_entry = tk.Entry(self.window)
        self.stream_name_entry = tk.Entry(self.window)

        self.host_entry.grid(row=0, column=1)
        self.port_entry.grid(row=1, column=1)
        self.client_id_entry.grid(row=2, column=1)
        self.stream_name_entry.grid(row=3, column=1)

        self.connect_button = tk.Button(self.window, text="Connect", command=self.start_connection)
        self.connect_button.grid(row=4, column=0, columnspan=2)

        # Label for displaying received frames
        self.frame_label = tk.Label(self.window)
        self.frame_label.grid(row=5, column=0, columnspan=2)

        self.window.protocol("WM_DELETE_WINDOW", self.on_close)
        self.websocket = None
        self.loop = asyncio.get_event_loop()
        self.running = True

    def start_connection(self):
        host = self.host_entry.get()
        port = self.port_entry.get()
        client_id = self.client_id_entry.get()
        stream_name = self.stream_name_entry.get()

        if not host or not port or not client_id or not stream_name:
            messagebox.showerror("Input Error", "Please provide host, port, client ID, and stream name.")
            return

        # Start the connection in a new thread to avoid blocking the GUI
        threading.Thread(target=self.run_client_thread, args=(host, port, client_id, stream_name), daemon=True).start()

    def run_client_thread(self, host, port, client_id, stream_name):
        self.loop.run_until_complete(self.run_client(host, port, client_id, stream_name))

    async def run_client(self, host, port, client_id, stream_name):
        uri = f"ws://{host}:{port}"
        while self.running:
            try:
                async with websockets.connect(uri) as websocket:
                    self.websocket = websocket
                    print(f"Connected to server at {uri}")

                    # Send initial client ID
                    await self.send_id(client_id)

                    # Create tasks for requesting frames and listening for frames
                    request_task = asyncio.create_task(self.request_stream_data(stream_name))
                    listen_task = asyncio.create_task(self.listen_for_frames())

                    # Wait for both tasks to complete
                    await asyncio.gather(request_task, listen_task)
            except Exception as e:
                print(f"Connection Error: {e}. Reconnecting in 2 seconds...")
                await asyncio.sleep(2)

    async def send_id(self, client_id):
        message = {"client_id": client_id}
        await self.websocket.send(json.dumps(message))
        print(f"Sent client_id: {client_id} to server")

    async def request_stream_data(self, stream_name):
        while self.running:
            try:
                if self.websocket and self.websocket.open:
                    message = {"command": "request_stream_data", "stream_name": stream_name}
                    await self.websocket.send(json.dumps(message))
                    print(f"Requested stream: {stream_name}")
                
                # Wait a short time before sending the next request
                await asyncio.sleep(0.1)  # Adjust the interval as needed
            except Exception as e:
                print(f"Error requesting stream data: {e}")
                break

    async def listen_for_frames(self):
        try:
            async for message in self.websocket:
                data = json.loads(message)
                if data.get("command") == "stream_data":
                    frame_type = data.get("frame_type", "rgb")
                    base64_frame = data.get("data")
                    self.display_frame(base64_frame, frame_type)
        except websockets.ConnectionClosed:
            print("Connection to server closed.")
        except Exception as e:
            print(f"Error while receiving frames: {e}")

    def display_frame(self, base64_frame, frame_type):
        try:
            # Decode the base64 frame
            frame_bytes = base64.b64decode(base64_frame)
            if frame_type == "rgb":
                # Decode RGB frame from JPEG bytes
                np_arr = np.frombuffer(frame_bytes, np.uint8)
                frame = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)  # Reads in BGR format
            
            elif frame_type == "depth":
                # Decode depth frame from raw bytes (adjust shape as necessary)
                frame = np.frombuffer(frame_bytes, dtype=np.uint16).reshape((480, 640))
                frame = cv2.convertScaleAbs(frame, alpha=0.03)  # Adjust scaling as needed

            # Convert the frame to an ImageTk format and display it
            image = ImageTk.PhotoImage(Image.fromarray(frame))
            self.frame_label.configure(image=image)
            self.frame_label.image = image
        except Exception as e:
            print(f"Error displaying frame: {e}")


    def on_close(self):
        self.running = False
        if self.websocket and not self.websocket.closed:
            self.loop.run_until_complete(self.websocket.close())
        self.window.destroy()

    def run(self):
        self.window.mainloop()


if __name__ == "__main__":
    client = StreamRequestClient()
    client.run()
