import time
import json
import numpy as np
import cv2
import socket
from ArUcoDetector import ArUcoDetector
from Camera import Camera
import pyrealsense2 as rs

class ArUcoTracker:
    def __init__(self, config_path='./clientConfig.json'):
        # Load config
        with open(config_path) as f:
            config = json.load(f)
        
        # Initialize detector and camera
        self.dict_to_use = config.get('dict_to_use')
        # Default to IDs 1-10 if not specified in config
        self.target_ids = config.get('target_ids', list(range(1, 11)))
        self.arucoDetector = ArUcoDetector(self.dict_to_use)
        self.camera = Camera()
        
        # Initialize UDP client
        self.udp_client = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.unity_address = ('127.0.0.1', 12345)
        self.udp_client.settimeout(0.1)
        
        print(f"Started tracking IDs: {self.target_ids}")
        
        self.is_connected = False
        self.connection_attempts = 0
        self.last_successful_send = 0

    def format_point3d(self, point):
        return {
            'x': float(f"{point[0]:.3f}"),
            'y': float(f"{point[1]:.3f}"),
            'z': float(f"{point[2]:.3f}")
        }
    def send_heartbeat(self):
        heartbeat_packet = {
            'timestamp': float(f"{time.time():.3f}"),
            'markers': {}  # Empty markers dict
        }
        return self.send_to_unity(heartbeat_packet)

    def send_to_unity(self, data):
        try:
            message = json.dumps(data)
            print("Sending JSON:", message)  # Print exact JSON being sent
            self.udp_client.sendto(message.encode(), self.unity_address)
            
            # Wait for acknowledgment
            try:
                ack_data, _ = self.udp_client.recvfrom(1024)
                if ack_data.decode() == "ACK":
                    if not self.is_connected:
                        print("Successfully connected to Unity server!")
                        self.is_connected = True
                    self.last_successful_send = time.time()
                    return True
            except socket.timeout:
                self.connection_attempts += 1
                if self.connection_attempts % 10 == 0:  # Log every 10 attempts
                    print(f"No response from server. Attempt {self.connection_attempts}")
                return False

        except Exception as e:
            print(f"Error sending to Unity: {e}")
            self.is_connected = False
            return False

    def format_point3d(self, point):
        return {
            'x': float(f"{point[0]:.3f}"),
            'y': float(f"{point[1]:.3f}"),
            'z': float(f"{point[2]:.3f}")
        }
    def start(self):
        self.camera.startStreaming()
        print(f"Attempting to connect to Unity at {self.unity_address}")
        print(f"Only tracking markers with IDs: {self.target_ids}")
        
        try:
            while True:
                # Get frame and necessary data
                frame = self.camera.getNextFrame()
                if frame is None:
                    continue

                depth_frame = frame.get_depth_frame()
                depth_intrinsics = depth_frame.profile.as_video_stream_profile().intrinsics
                _, color_image = self.camera.extractImagesFromFrame(frame)

                result = self.arucoDetector.detect(color_image)
                corners, ids, _ = result

                marker_data = {}
                detected_ids = []  # For logging
                if ids is not None:
                    ids = np.array(ids) if isinstance(ids, list) else ids
                    for i, marker_id in enumerate(ids.flatten()):
                        marker_id = int(marker_id)
                        detected_ids.append(marker_id)
                        
                        if marker_id not in self.target_ids:
                            continue

                        corner = corners[i][0]
                        # Round the center calculations
                        center_x = int(np.mean(corner[:, 0]))
                        center_y = int(np.mean(corner[:, 1]))

                        depth = float(f"{depth_frame.get_distance(center_x, center_y):.3f}")
                        if depth > 0:
                            point_3d = rs.rs2_deproject_pixel_to_point(
                                depth_intrinsics, [center_x, center_y], depth)
                            
                            marker_data[int(marker_id)] = {
                                'position': self.format_point3d(point_3d),
                                'rotation': float(f"{self.calculate_rotation(corner):.3f}")
                            }

                if marker_data:
                    data_packet = {
                        'timestamp': float(f"{time.time():.3f}"),
                        'markers': marker_data
                    }
                    sent = self.send_to_unity(data_packet)
                    if sent:
                        print(f"Sent data for markers: {list(marker_data.keys())}")
                        # Print detected vs tracked markers
                        all_detected = set(detected_ids)
                        tracked = set(marker_data.keys())
                        ignored = all_detected - tracked
                        if ignored:
                            print(f"Ignored markers (not in target list): {ignored}")
                else:
                    # Send heartbeat when no markers detected
                    sent = self.send_heartbeat()
                    if sent and time.time() % 5 < 0.1:  # Print every ~5 seconds
                        print("Connection active (heartbeat sent)")

                # Draw markers on image
                color_image = ArUcoDetector.getImageWithMarkers(
                    color_image, result, depth_frame, depth_intrinsics)

                cv2.imshow('ArUco Tracking', color_image)
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break

        finally:
            print("Shutting down tracker...")
            self.camera.stopStreaming()
            cv2.destroyAllWindows()
            self.udp_client.close()

    def calculate_rotation(self, corners):
        dx = corners[1][0] - corners[0][0]
        dy = corners[1][1] - corners[0][1]
        angle = np.arctan2(dy, dx)
        return float(angle)

def main():
    tracker = ArUcoTracker()
    tracker.start()

if __name__ == "__main__":
    main()