import asyncio
import json
import time
import threading
import pyrealsense2 as rs
import numpy as np
import cv2
from cv2 import aruco
from tkinter import Tk, scrolledtext
import tkinter as tk  # Import tkinter with alias for widgets
from PIL import Image, ImageTk
from test_new_client import WebsocketClient_test
import logging

class LocationSendingWebSocketClient_Matrix(WebsocketClient_test):
    def __init__(self):
        super().__init__()  # Initialize WebSocketClient_test
        self.marker_data = {}  # Dictionary to store marker position data
        self.detecting = True

        # Initialize Tkinter elements
        self.log_area = scrolledtext.ScrolledText(self.window, width=50, height=10, state='disabled')
        self.log_area.grid(row=4, column=0, columnspan=2)
        self.frame_label = tk.Label(self.window)  # Corrected to use tk.Label
        self.frame_label.grid(row=5, column=0, columnspan=2)

        # Initialize RealSense pipeline
        self.pipeline = rs.pipeline()
        self.config = rs.config()
        self.config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)
        self.config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
        self.align = rs.align(rs.stream.color)

        # ArUco Dictionary and Parameters
        self.aruco_dict = aruco.getPredefinedDictionary(aruco.DICT_6X6_250)
        self.aruco_params = aruco.DetectorParameters()

    def start_connection(self):
        super().start_connection()  # Start WebSocket connection in a separate thread
        # Start separate threads for detecting ArUco markers and batch-sending data
        threading.Thread(target=self.detect_aruco_markers, daemon=True).start()
        threading.Thread(target=self.batch_send_marker_positions, daemon=True).start()

    def detect_aruco_markers(self):
        """Detect ArUco markers and store their positions."""
        try:
            self.pipeline.start(self.config)
            logging.info("RealSense pipeline started successfully.")
        except Exception as e:
            logging.error(f"Error starting RealSense pipeline: {e}")
            return  # Stop if the camera couldn't start

        while self.detecting:
            try:
                frames = self.pipeline.wait_for_frames()
                if not frames:
                    logging.warning("No frames received from RealSense.")
                    time.sleep(0.1)
                    continue

                aligned_frames = self.align.process(frames)
                color_frame = aligned_frames.get_color_frame()
                depth_frame = aligned_frames.get_depth_frame()

                if not color_frame or not depth_frame:
                    logging.warning("Color or depth frame not available.")
                    self.update_log("Frames not available. Trying again...")
                    time.sleep(0.1)
                    continue

                logging.debug("Frames received from RealSense camera.")
                color_image = np.asanyarray(color_frame.get_data())

                # Detect ArUco markers
                corners, ids, _ = aruco.detectMarkers(color_image, self.aruco_dict, parameters=self.aruco_params)
                log_message = "Frame processed.\n"

                # Update marker_data with latest positions
                if ids is not None:
                    for i, corner in enumerate(corners):
                        cx, cy = np.mean(corner[0], axis=0).astype(int)
                        depth = depth_frame.get_distance(cx, cy)
                        depth_intrinsics = depth_frame.profile.as_video_stream_profile().intrinsics
                        x, y, z = rs.rs2_deproject_pixel_to_point(depth_intrinsics, [cx, cy], depth)
                        y = -y

                        # Store the latest position for each marker ID
                        self.marker_data[int(ids[i][0])] = {
                            "x": round(x, 2),
                            "y": round(y, 2),
                            "z": round(z, 2)
                        }

                        log_message += f"Marker ID: {ids[i][0]} - Position: X={x:.2f}, Y={y:.2f}, Z={z:.2f}\n"
                        cv2.circle(color_image, (cx, cy), 5, (0, 255, 0), -1)
                        cv2.putText(color_image, f"ID: {ids[i][0]} ({x:.2f}, {y:.2f}, {z:.2f})",
                                    (cx, cy - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 2)
                else:
                    log_message += "No ArUco markers detected in this frame.\n"
                    logging.info("No ArUco markers detected.")

                self.update_log(log_message)
                self.update_frame_in_gui(color_image)

            except Exception as e:
                logging.error(f"Error during ArUco detection: {e}")
                break

        self.pipeline.stop()
        logging.info("RealSense pipeline stopped.")

    async def send_marker_data(self):
        """Send all current marker data as a batch to the server."""
        if self.websocket and not self.websocket.closed:
            marker_matrix = [
                {"marker_id": marker_id, **position}
                for marker_id, position in self.marker_data.items()
            ]
            message = {
                "command": "stream_data",
                "stream_name": "aruco_position_stream",
                "data": marker_matrix
            }
            await self.websocket.send(json.dumps(message))
            print(f"Sent marker data: {marker_matrix}")

    def batch_send_marker_positions(self):
        """Send marker data every 0.05 seconds."""
        while self.detecting:
            try:
                # Run send_marker_data asynchronously on WebSocket loop
                asyncio.run_coroutine_threadsafe(self.send_marker_data(), self.loop)
                time.sleep(0.05)  # Batch send every 0.05 seconds
            except Exception as e:  
                print(f"Error sending marker positions: {e}")
                break

    def update_log(self, message):
        """Update log area in the GUI."""
        self.log_area.config(state='normal')
        self.log_area.delete(1.0, Tk.END)
        self.log_area.insert(Tk.END, message)
        self.log_area.config(state='disabled')

    def update_frame_in_gui(self, frame):
        """Update displayed frame in the Tkinter GUI."""
        frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        image = Image.fromarray(frame)
        image_tk = ImageTk.PhotoImage(image)
        self.frame_label.configure(image=image_tk)
        self.frame_label.image = image_tk

    def on_close(self):
        """Handle window close event."""
        self.detecting = False
        if self.websocket and not self.websocket.closed:
            self.loop.run_until_complete(self.websocket.close())
        self.loop.stop()
        self.window.destroy()

if __name__ == "__main__":
    client = LocationSendingWebSocketClient_Matrix()
    client.run()
