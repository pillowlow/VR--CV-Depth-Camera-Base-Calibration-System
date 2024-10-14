import pyrealsense2 as rs
import numpy as np
import cv2
from cv2 import aruco
import tkinter as tk
from PIL import Image, ImageTk
import threading

class ArUcoDetectionApp:
    def __init__(self, window):
        self.window = window
        self.window.title("ArUco Detection")

        # Initialize RealSense pipeline and ArUco marker detection
        self.pipeline = rs.pipeline()
        self.config = rs.config()
        self.config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)
        self.config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)  # Enable depth stream
        self.align = rs.align(rs.stream.color)

        # ArUco Dictionary and Parameters
        self.aruco_dict = aruco.getPredefinedDictionary(aruco.DICT_6X6_250)
        self.aruco_params = aruco.DetectorParameters_create()

        # Tkinter components for showing frames
        self.frame_label = tk.Label(window)
        self.frame_label.grid(row=0, column=0)

        # Start RealSense pipeline in a separate thread
        self.running = True
        threading.Thread(target=self.detect_aruco_markers, daemon=True).start()

        # Tkinter window close protocol
        self.window.protocol("WM_DELETE_WINDOW", self.on_close)

    def detect_aruco_markers(self):
        # Start streaming from RealSense camera
        self.pipeline.start(self.config)

        while self.running:
            # Capture frames from RealSense camera
            frames = self.pipeline.wait_for_frames()
            aligned_frames = self.align.process(frames)
            color_frame = aligned_frames.get_color_frame()
            depth_frame = aligned_frames.get_depth_frame()

            if not color_frame or not depth_frame:
                continue

            # Convert RealSense frames to numpy arrays
            color_image = np.asanyarray(color_frame.get_data())
            depth_image = np.asanyarray(depth_frame.get_data())

            # Detect ArUco markers
            corners, ids, _ = aruco.detectMarkers(color_image, self.aruco_dict, parameters=self.aruco_params)

            # If markers are detected, draw the markers and their center positions
            if ids is not None:
                color_image = aruco.drawDetectedMarkers(color_image, corners, ids)
                for i, corner in enumerate(corners):
                    cx, cy = np.mean(corner[0], axis=0).astype(int)

                    # Get the depth at the center of the marker
                    depth = depth_frame.get_distance(cx, cy)
                    depth_intrinsics = depth_frame.profile.as_video_stream_profile().intrinsics
                    x, y, z = rs.rs2_deproject_pixel_to_point(depth_intrinsics, [cx, cy], depth)
                    y = -y  # Flip the y-axis

                    # Draw the position and ID on the image
                    cv2.circle(color_image, (cx, cy), 5, (0, 255, 0), -1)
                    cv2.putText(color_image, f"ID: {ids[i][0]} Pos: ({x:.2f}, {y:.2f}, {z:.2f})",
                                (cx, cy - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 2)

            # Update the frame in the Tkinter GUI
            self.update_frame_in_gui(color_image)

    def update_frame_in_gui(self, frame):
        # Convert BGR to RGB for displaying in Tkinter
        frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        image = Image.fromarray(frame)
        image_tk = ImageTk.PhotoImage(image)
        self.frame_label.configure(image=image_tk)
        self.frame_label.image = image_tk

    def on_close(self):
        self.running = False
        self.pipeline.stop()  # Stop the RealSense pipeline
        self.window.destroy()

    def run(self):
        self.window.mainloop()

if __name__ == "__main__":
    root = tk.Tk()
    app = ArUcoDetectionApp(root)
    app.run()
