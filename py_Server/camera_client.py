import asyncio
import websockets
import json
import tkinter as tk
from tkinter import messagebox
import threading
import pyrealsense2 as rs
import numpy as np
import cv2
from PIL import Image, ImageTk
import base64

class WebSocketClient:
    def __init__(self):
        self.window = tk.Tk()
        self.window.title("Client Connection")

        # GUI for input fields
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

        # Labels for RGB and Depth frames
        self.rgb_label = tk.Label(self.window)
        self.rgb_label.grid(row=4, column=0, columnspan=2)
        self.depth_label = tk.Label(self.window)
        self.depth_label.grid(row=5, column=0, columnspan=2)

        self.window.protocol("WM_DELETE_WINDOW", self.on_close)
        self.websocket = None
        self.loop = asyncio.get_event_loop()

        # RealSense pipeline setup
        self.pipeline = rs.pipeline()
        self.config = rs.config()
        self.config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 15)
        self.config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 15)

        self.rgb_image = None
        self.depth_image = None

        # Start the RealSense stream
        self.pipeline.start(self.config)
        self.running = True
        threading.Thread(target=self.update_frames, daemon=True).start()

    def start_connection(self):
        host = self.host_entry.get()
        port = self.port_entry.get()
        client_id = self.client_id_entry.get()

        if not host or not port or not client_id:
            messagebox.showerror("Input Error", "Please provide host, port, and client ID.")
            return

        # Start the connection in a new thread to avoid blocking the GUI
        threading.Thread(target=self.run_client_thread, args=(host, port, client_id), daemon=True).start()

    def run_client_thread(self, host, port, client_id):
        self.loop.run_until_complete(self.run_client(host, port, client_id))

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

    def encode_frame_to_base64(self, frame, frame_type="rgb"):
        # Encode frame as a JPEG image
        if frame_type == "rgb":
            _, buffer = cv2.imencode('.jpg', frame)
        elif frame_type == "depth":
            buffer = frame.tobytes()

        # Encode the buffer into base64 string
        base64_data = base64.b64encode(buffer).decode('utf-8')
        return base64_data

    async def send_frame(self, websocket, frame, frame_type, stream_name):
        base64_frame = self.encode_frame_to_base64(frame, frame_type)
        message = {
            "command": "stream_frame",
            "stream_name": stream_name,
            "frame_type": frame_type,
            "data": base64_frame
        }
        await websocket.send(json.dumps(message))
        print(f"Sent {frame_type} frame to server")

    def update_frames(self):
        while self.running:
            frames = self.pipeline.wait_for_frames()
            color_frame = frames.get_color_frame()
            depth_frame = frames.get_depth_frame()

            if not color_frame or not depth_frame:
                continue

            # Process color frame (convert to RGB image)
            color_image = np.asanyarray(color_frame.get_data())
            color_image = cv2.cvtColor(color_image, cv2.COLOR_BGR2RGB)
            self.rgb_image = ImageTk.PhotoImage(Image.fromarray(color_image))
            self.rgb_label.configure(image=self.rgb_image)

            # Process depth frame (convert to raw data for transmission)
            depth_image = np.asanyarray(depth_frame.get_data())
            depth_display = cv2.convertScaleAbs(depth_image, alpha=0.03)
            self.depth_image = ImageTk.PhotoImage(Image.fromarray(depth_display))
            self.depth_label.configure(image=self.depth_image)

            # Send RGB and depth frames to the server
            if self.websocket and self.websocket.open:
                asyncio.run_coroutine_threadsafe(
                    self.send_frame(self.websocket, color_image, "rgb", "stream_rgb"),
                    self.loop
                )
                asyncio.run_coroutine_threadsafe(
                    self.send_frame(self.websocket, depth_image, "depth", "stream_depth"),
                    self.loop
                )

    def on_close(self):
        self.running = False
        self.pipeline.stop()
        if self.websocket and not self.websocket.closed:
            self.loop.run_until_complete(self.websocket.close())
        self.window.destroy()

    def run(self):
        self.window.mainloop()


if __name__ == "__main__":
    client = WebSocketClient()
    client.run()
