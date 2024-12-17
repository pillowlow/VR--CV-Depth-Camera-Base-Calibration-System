import pyrealsense2 as rs
import numpy as np
import cv2
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(message)s')
logger = logging.getLogger(__name__)

def initialize_realsense():
    pipeline = rs.pipeline()
    config = rs.config()
    
    pipeline_wrapper = rs.pipeline_wrapper(pipeline)
    pipeline_profile = config.resolve(pipeline_wrapper)
    device = pipeline_profile.get_device()
    
    config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)
    config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
    
    pipeline.start(config)
    return pipeline

def detect_aruco(color_image, depth_image, depth_scale, depth_intrin):
    aruco_dict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_4X4_50)
    aruco_params = cv2.aruco.DetectorParameters()
    detector = cv2.aruco.ArucoDetector(aruco_dict, aruco_params)
    
    corners, ids, rejected = detector.detectMarkers(color_image)
    
    if ids is not None:
        # Draw only the markers without text
        cv2.aruco.drawDetectedMarkers(color_image, corners, ids)
        
        for i in range(len(ids)):
            center = np.mean(corners[i][0], axis=0)
            center_x, center_y = int(center[0]), int(center[1])
            
            # Get depth with averaging
            depth_roi = depth_image[max(0, center_y-2):min(depth_image.shape[0], center_y+3),
                                  max(0, center_x-2):min(depth_image.shape[1], center_x+3)]
            depth = np.median(depth_roi[depth_roi != 0]) if depth_roi.size > 0 else 0
            
            if depth > 0:
                depth_point = rs.rs2_deproject_pixel_to_point(
                    depth_intrin, [center_x, center_y], depth * depth_scale)
                
                # Log instead of drawing text
                logger.info(f"Marker ID {ids[i][0]}: XYZ = ({depth_point[0]:.3f}, {depth_point[1]:.3f}, {depth_point[2]:.3f})")

def main():
    pipeline = initialize_realsense()
    
    try:
        while True:
            frames = pipeline.wait_for_frames()
            depth_frame = frames.get_depth_frame()
            color_frame = frames.get_color_frame()
            if not depth_frame or not color_frame:
                continue

            depth_image = np.asanyarray(depth_frame.get_data())
            color_image = np.asanyarray(color_frame.get_data())
            
            depth_scale = pipeline.get_active_profile().get_device().first_depth_sensor().get_depth_scale()
            depth_intrin = depth_frame.profile.as_video_stream_profile().intrinsics
            
            detect_aruco(color_image, depth_image, depth_scale, depth_intrin)
            
            # Show image with only markers, no text overlay
            cv2.imshow('RealSense + ArUco', color_image)
            
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
                
    finally:
        pipeline.stop()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    main()