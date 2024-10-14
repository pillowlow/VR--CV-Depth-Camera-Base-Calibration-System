import asyncio
import websockets
import json
import pyrealsense2 as rs
import numpy as np
import cv2
from cv2 import aruco
import threading
import tkinter as tk
from tkinter import messagebox, scrolledtext
from PIL import Image, ImageTk
from simpleClient import WebSocketClient

class LocationSendingWebSocketClient(WebSocketClient):
    def __init__(self):
        super().__init__()

        # Add log area to display detected ArUco marker positions
        self.log_area = scrolledtext.ScrolledText(self.window, width=50, height=10, state='disabled')
        self.log_area.grid(row=4, column=0, columnspan=2)

        # Initialize RealSense pipeline and ArUco detection
        self.pipeline = rs.pipeline()
        self.config = rs.config()
        self.config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)
        self.config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
        self.align = rs.align(rs.stream.color)

        # ArUco Dictionary and Parameters
        self.aruco_dict = aruco.getPredefinedDictionary(aruco.DICT_6X6_250)
        self.aruco_params = aruco.DetectorParameters_create()

        # GUI element to display frames
        self.frame_label = tk.Label(self.window)
        self.frame_label.grid(row=5, column=0, columnspan=2)

        self.detecting = True  # Control flag for detection

    def start_connection(self):
        """Start the connection and the detection process."""
        super().start_connection()
        # Start the ArUco detection in a new thread to avoid blocking the GUI
        threading.Thread(target=self.detect_aruco_markers, daemon=True).start()

    async def send_marker_positions(self, marker_data):
        """Send detected ArUco marker ID and position to the server."""
        message = {
            "command": "stream_data",
            "stream_name": "aruco_position_stream",
            "data": marker_data
        }
        await self.websocket.send(json.dumps(message))
        print(f"Sent marker data: {marker_data}")

    def detect_aruco_markers(self):
        """Detect ArUco markers and stream their positions to the server every 0.1 seconds."""
        self.pipeline.start(self.config)

        while self.detecting:
            try:
                # Capture RealSense frames
                frames = self.pipeline.wait_for_frames()
                aligned_frames = self.align.process(frames)
                color_frame = aligned_frames.get_color_frame()
                depth_frame = aligned_frames.get_depth_frame()

                if not color_frame or not depth_frame:
                    # Log if frames are not available
                    self.update_log("Frames not available. Trying again...")
                    continue

                # Convert RealSense frames to numpy arrays (color and depth)
                color_image = np.asanyarray(color_frame.get_data())
                depth_image = np.asanyarray(depth_frame.get_data())

                # Detect ArUco markers
                corners, ids, _ = aruco.detectMarkers(color_image, self.aruco_dict, parameters=self.aruco_params)

                marker_data = []  # Collect detected marker data (ID, position)
                log_message = "Frame processed.\n"

                if ids is not None:
                    log_message += f"{len(ids)} ArUco marker(s) detected.\n"
                    for i, corner in enumerate(corners):
                        # Get the center of the marker
                        cx, cy = np.mean(corner[0], axis=0).astype(int)

                        # Get depth at the marker's center point
                        depth = depth_frame.get_distance(cx, cy)
                        depth_intrinsics = depth_frame.profile.as_video_stream_profile().intrinsics
                        x, y, z = rs.rs2_deproject_pixel_to_point(depth_intrinsics, [cx, cy], depth)
                        y = -y  # Flip y-axis to match the camera's coordinate system

                        # Add the marker data to the list
                        marker_data.append({
                            "marker_id": int(ids[i][0]),
                            "position": {"x": float(x), "y": float(y), "z": float(z)}
                        })

                        # Create log entry for detected markers
                        log_message += f"Marker ID: {ids[i][0]} - Position: X={x:.2f}, Y={y:.2f}, Z={z:.2f}\n"

                        # Draw the marker ID and center on the frame
                        cv2.circle(color_image, (cx, cy), 5, (0, 255, 0), -1)
                        cv2.putText(color_image, f"ID: {ids[i][0]} ({x:.2f}, {y:.2f}, {z:.2f})",
                                    (cx, cy - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 2)
                else:
                    log_message += "No ArUco markers detected in this frame.\n"

                # Log the positions to the GUI
                self.update_log(log_message)

                # Display the frame with detected markers
                self.update_frame_in_gui(color_image)

                # Send marker positions every 0.1 seconds if connected to the server
                if self.websocket and not self.websocket.closed and marker_data:
                    asyncio.run(self.send_marker_positions(marker_data))

                # Sleep to reduce CPU usage and simulate 10 FPS sending rate
                asyncio.run(asyncio.sleep(0.1))

            except Exception as e:
                print(f"Error during ArUco detection: {e}")
                break

        self.pipeline.stop()

    def update_log(self, message):
        """Update the log area in the GUI with detected marker positions."""
        self.log_area.config(state='normal')
        self.log_area.delete(1.0, tk.END)  # Clear previous logs
        self.log_area.insert(tk.END, message)  # Insert new log
        self.log_area.config(state='disabled')

    def update_frame_in_gui(self, frame):
        """Update the displayed frame in the Tkinter GUI."""
        frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        image = Image.fromarray(frame)
        image_tk = ImageTk.PhotoImage(image)
        self.frame_label.configure(image=image_tk)
        self.frame_label.image = image_tk

    def on_close(self):
        """Handle the window close event."""
        self.detecting = False  # Stop detection loop
        super().on_close()  # Stop WebSocket and close the window


if __name__ == "__main__":
    # Create the client with ArUco detection and WebSocket communication
    client = LocationSendingWebSocketClient()
    client.run()
