# Project Name: Unity-VR with RealSense and Python Server-Client System

## Abstract

This project integrates Unity VR(Meta all in one sdk) with a RealSense camera and a Python-based WebSocket server-client system to create a seamless interaction between the physical and virtual worlds. It allows users to adjust the virtual camera's position by grabbing and moving a Camera Anchor, which automatically updates all real object tracking anchors. The system also includes a calibration offset adjustment panel and modular server-client communication using JSON for data exchange, making it adaptable to various use cases, including object detection and real-time VR applications.

## Simple Demo

Below is a simple demo showcasing the core features of the system:

![Demo Video](./Videos/DemoCalibrationEssense.mp4)


## Get Started

To set up and run this project, you'll need the following hardware and software components.

### Hardware Requirements
- **Meta Quest 3**: VR headset for interaction.
- **Intel RealSense Camera**: For capturing depth and RGB data.
- **Windows Machine**: A Windows PC with a recommended GPU, **NVIDIA RTX 2060 or higher**.

### Software Requirements
- **Unity Engine**: Version **2022.3.9f1** or later.
- **Meta Quest Link**: To connect your Meta Quest headset to the PC.
- **Meta Quest Developer Hub** (recommended): For managing and deploying the VR app.
- **Python 3.11** (recommended): To run the server-side components of the system.

### Installation Instructions

1. **Set up Meta Quest Link**:
   - Download and install the **Meta Quest Link** software from the official Oculus/Meta website.
   - Ensure your Meta Quest 3 is connected to your PC via a USB-C cable or over Wi-Fi using Air Link.

2. **Install Unity Engine**:
   - Download and install **Unity Hub** from [Unity's website](https://unity.com/download).
   - Install **Unity 2022.3.9f1** or later through Unity Hub.

3. **Set up Python**:
   - Download and install **Python 3.10** from [Python's website](https://www.python.org/downloads/).
   - Make sure to check the box for "Add Python to PATH" during installation.

4. **Install Meta Quest Developer Hub** (Optional but recommended):
   - Download the **Meta Quest Developer Hub** from the Meta Developers website to help with managing and debugging the app on the Meta Quest 3.

5. **Python Virtual Environment Setup:

   - To ensure that the Python environment is correctly isolated, follow these steps to set up and activate a virtual environment in the `py_Server` directory.
   - Navigate to the `py_Server` Directory:
   In your terminal or command prompt, navigate to the `py_Server` directory where the Python server files are located:
   ```bash
   cd py_Server
   ```
   and run 
   ```
   ```


