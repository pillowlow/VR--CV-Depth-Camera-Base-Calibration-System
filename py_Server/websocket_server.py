import asyncio
import websockets
import json
import logging
import socket
import base64
import numpy as np
import cv2

from typing import TYPE_CHECKING, Dict, Any
if TYPE_CHECKING:
    from server_app import ServerApp

logging.basicConfig(level=logging.INFO, format='%(asctime)s %(message)s')



class WebSocketServer:
    def __init__(self, app: 'ServerApp'):
        self.app = app  # Reference to the ServerApp instance
        self.port = 8080
        self.host = None
        self.server = None
        self.loop = None
        self.should_stop = False
        self.clients: Dict[str, Any] = {}  # Dictionary to store client ID and WebSocket pairs
        self.streams: Dict[str, Any] = {}  # Dictionary to store active streams and their current values
        
    async def register(self, websocket):
        await websocket.send(json.dumps({"command": "REQUEST_ID"}))
        try:
            message = await websocket.recv()
            data = json.loads(message)
            client_id = data.get("client_id")

            if client_id:
                self.clients[client_id] = websocket
                self.app.log_message(f"New client connected: ID {client_id}")
                self.app.add_client(client_id)  # Update the client list in the GUI
                await self.listen_to_client(client_id, websocket)

        except websockets.ConnectionClosed as e:
            self.app.log_message(f"Connection closed: {e}")
        except Exception as e:
            self.app.log_message(f"Error: {e}")
        finally:
            if client_id in self.clients:
                self.clients.pop(client_id)
                self.app.log_message(f"Client disconnected: ID {client_id}")
                self.app.remove_client(client_id)

    async def listen_to_client(self, client_id, websocket):
        try:
            async for message in websocket:
                data = json.loads(message)
                await self.handle_message(client_id, data)
        except websockets.ConnectionClosed as e:
            logging.warning(f"Connection closed: {e}")
            self.app.log_message(f"Connection closed: {e}")
        except Exception as e:
            logging.error(f"Error: {e}")
            self.app.log_message(f"Error: {e}")

    async def handle_message(self, client_id, data):
        command = data.get("command")
        log_message = ""  # Initialize log_message to ensure it is always defined

        if command == "send_to_client":
            target_id = data.get("target_id")
            target_client = self.clients.get(target_id)
            if target_client:
                await target_client.send(json.dumps(data))
                log_message = f"Message from {client_id} sent to {target_id}"
                logging.info(log_message)
                self.app.log_message(log_message)
            else:
                log_message = f"Client {target_id} not found."
                logging.warning(log_message)
                self.app.log_message(log_message)

        elif command == "stream_data":
        # Combined handling for 'start_stream' and 'stream_data'
            stream_name = data.get("stream_name")
            stream_data = data.get("data")

            # Automatically register the stream if it doesn't exist yet
            if stream_name not in self.streams:
                # Register the stream
                self.streams[stream_name] = None  # Initialize with no data
                self.app.refresh_stream_dropdown()  # Refresh the stream dropdown in the UI
                log_message = f"Stream '{stream_name}' started by {client_id}"
                logging.info(log_message)
                self.app.log_message(log_message)

            # Store the actual stream data
            self.streams[stream_name] = stream_data

            # Log data reception
            log_message = f"Received data for stream '{stream_name}' from client {client_id}"
            logging.info(log_message)
            self.app.log_message(log_message)         

        elif command == "request_stream_data":
            stream_name = data.get("stream_name")
            if stream_name in self.streams:
                current_data = self.streams.get(stream_name)
                response = {
                    "command": "stream_data",
                    "stream_name": stream_name,
                    "data": current_data
                }
                await self.clients[client_id].send(json.dumps(response))
                log_message = f"Sent current stream data for '{stream_name}' to {client_id}"
                #logging.info(log_message)
                self.app.log_message(log_message)
            else:
                log_message = f"Stream '{stream_name}' not found."
                logging.warning(log_message)
                self.app.log_message(log_message)

        elif command == "close_stream":
            stream_name = data.get("stream_name")
            self.app.log_message("stream to close: '{stream_name}'")
            logging.info("stream to close: '{stream_name}'")

            if stream_name in self.streams:
                log_message = f"Stream '{stream_name}' closed by {client_id}"
                del self.streams[stream_name]
                self.app.refresh_stream_dropdown()  # Refresh the stream dropdown in the UI
                logging.info(log_message)
                self.app.log_message(log_message)

        
        # Handle the streaming of frame data
        elif command == "stream_frame":
            stream_name = data.get("stream_name")
            frame_type = data.get("frame_type")  # e.g., "rgb" or "depth"
            frame_data = data.get("data")  # This should be a base64-encoded string

            # Decode the frame data for verification and logging purposes (if needed)
            frame_bytes = base64.b64decode(frame_data)
            np_arr = np.frombuffer(frame_bytes, np.uint8)

            if frame_type == "rgb":
                # Convert the byte array back into an image (BGR format for OpenCV)
                frame = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
                if frame is not None:
                    # Store the raw base64-encoded frame data in the streams dictionary
                    self.streams[stream_name] = frame_data
                    self.app.log_message(f"Stored {frame_type} frame for stream '{stream_name}' from client {client_id}, data size: {len(frame_data)}")
                else:
                    self.app.log_message(f"Failed to decode RGB frame from client {client_id}")

            elif frame_type == "depth":
                # Convert the byte array back into an image (depth data)
                frame = np.frombuffer(frame_bytes, dtype=np.uint16).reshape((480, 640))  # Adjust shape if necessary
                if frame is not None:
                    # Store the raw base64-encoded frame data in the streams dictionary
                    self.streams[stream_name] = frame_data
                    self.app.log_message(f"Stored {frame_type} frame for stream '{stream_name}' from client {client_id}, data size: {len(frame_data)}")
                else:
                    self.app.log_message(f"Failed to decode depth frame from client {client_id}")

                    
        elif command == "broadcast":
            broadcast_message = data.get("data")
            await self.broadcast_message(broadcast_message, exclude_client=client_id)
            log_message = f"Broadcast message: {broadcast_message}"
            logging.info(log_message)
            self.app.log_message(log_message)
            
        elif command == "message":
            # Handle generic messages sent from clients
            message = data.get("data")
            log_message = f"Message from {client_id}: {message}"
            logging.info(log_message)
            self.app.log_message(log_message)

        elif command == "client_id":
            log_message = f"Received client_id command from {client_id}: {data.get('client_id')}"
            logging.info(log_message)
            self.app.log_message(log_message)

        else:
            log_message = f"Unknown command from {client_id}: {command}"
            logging.warning(log_message)
            self.app.log_message(log_message)

    async def send_to_client(self, client_id, message):
        client = self.clients.get(client_id)
        if client:
            await client.send(json.dumps({"command": "message", "data": message}))
            log_message = f"Sent message to {client_id}: {message}"
            self.app.log_message(log_message)
        else:
            log_message = f"Client {client_id} not found."
            self.app.log_message(log_message)

    async def broadcast_message(self, message, exclude_client=None):
        for cid, websocket in self.clients.items():
            if cid != exclude_client:
                await websocket.send(json.dumps({
                    "command": "broadcast",
                    "data": message
                }))
        logging.info(f"Broadcasted message: {message}")
        self.app.log_message(f"Broadcasted message: {message}")

    async def main(self):
        logging.info("Server started, waiting for clients to connect...")
        self.app.log_message("Server started, waiting for clients to connect...")
        
        # Increase the buffer size by setting max_size and max_queue
        self.server = await websockets.serve(
            self.register, 
            "0.0.0.0", 
            self.port,
            max_size=10**7,   # Maximum size of each message (10MB in this case)
            max_queue=10000    # Maximum number of messages that can be queued
        )
        
        self.host = self.get_host_ip()
        self.app.update_IP_config(self.host, self.port)
        logging.info(f"host :{self.host}")
        
        try:
            while not self.should_stop:
                await asyncio.sleep(1)
        finally:
            logging.info("Server stopping, disconnecting all clients...")
            self.app.log_message("Server stopping, disconnecting all clients...")
            await self.disconnect_all_clients()
            self.server.close()
            await self.server.wait_closed()
            logging.info("Server has been stopped.")
            self.app.log_message("Server has been stopped.")

            
    def get_host_ip(self):
        """Get the local IP address of the machine."""
        try:
            # This will attempt to connect to an external IP address to determine the local IP
            s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            s.connect(("8.8.8.8", 80))
            ip_address = s.getsockname()[0]
            s.close()
            return ip_address
        except Exception as e:
            logging.error(f"Could not determine IP address: {e}")
            return "127.0.0.1"  # Fallback to localhost

    async def disconnect_all_clients(self):
        if self.clients:
            logging.info("Disconnecting all clients...")
            self.app.log_message("Disconnecting all clients...")
            disconnect_tasks = []
            for client_id, websocket in list(self.clients.items()):
                await websocket.send(json.dumps({"command": "SERVER_CLOSING"}))
                disconnect_tasks.append(websocket.close())
            await asyncio.gather(*disconnect_tasks)
            self.clients.clear()
            logging.info("All clients have been disconnected.")
            self.app.log_message("All clients have been disconnected.")

    def start(self):
        self.loop = asyncio.new_event_loop()
        asyncio.set_event_loop(self.loop)
        self.should_stop = False
        try:
            self.loop.run_until_complete(self.main())
        finally:
            self.loop.run_until_complete(self.loop.shutdown_asyncgens())
            self.loop.close()

    def stop(self):
        self.should_stop = True
        if self.server is not None:
            asyncio.run_coroutine_threadsafe(self.disconnect_all_clients(), self.loop).result()
            self.server.close()
