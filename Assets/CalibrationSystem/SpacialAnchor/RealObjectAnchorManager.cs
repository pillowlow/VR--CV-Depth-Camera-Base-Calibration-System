using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealObjectAnchorManager : MonoBehaviour
{
    public Transform virtualCameraAnchor;  // Virtual camera reference
    public GameObject realObjectAnchorPrefab;  // Prefab of the RealObjectAnchor
    public OffsetManager offsetManager;  // The global OffsetManager
    public PositionDataWebSocketClient positionDataClient;  // WebSocket client for receiving marker positions

    private Dictionary<int, Transform> realSenseAnchors = new Dictionary<int, Transform>();  // Anchor storage
    private List<RealObjectAnchor> realObjectAnchors = new List<RealObjectAnchor>();  // List of anchors for broadcasting

    private void Start()
    {
        // Start updating anchors every 0.1 seconds
        StartCoroutine(UpdateAnchorsPeriodically(0.05f));
    }

    // Coroutine to update the anchors periodically (e.g., every 0.1 seconds)
    private IEnumerator UpdateAnchorsPeriodically(float interval)
    {
        while (true)
        {
            UpdateAnchors();
            yield return new WaitForSeconds(interval);  // Wait for 0.1 seconds between updates
        }
    }

    // Function to update the anchor positions based on the marker data from the WebSocket client
    private void UpdateAnchors()
    {
        // Retrieve the marker positions from the WebSocket client
        Dictionary<int, Vector3> markerPositions = positionDataClient.GetMarkerPositions();

        if (markerPositions.Count == 0)
        {
            Log("No marker positions received.");
            return;
        }

        // Process the marker positions
        foreach (var marker in markerPositions)
        {
            int markerId = marker.Key;
            Vector3 markerPosition = marker.Value;

            // Log the marker position received
            Log($"Processing Marker ID: {markerId}, Position: X={markerPosition.x}, Y={markerPosition.y}, Z={markerPosition.z}");

            // Check if the anchor for this marker ID already exists
            if (realSenseAnchors.ContainsKey(markerId))
            {
                // Update the existing anchor's position
                UpdateAnchorById(markerId, markerPosition, Quaternion.identity);
            }
            else
            {
                // Register a new anchor for this marker ID
                RegisterTransform(markerId, markerPosition, Quaternion.identity);
            }
        }
    }

    // Function to register a new RealSense space transform by ID
    public void RegisterTransform(int id, Vector3 realSensePosition, Quaternion realSenseRotation)
    {
        // Create a new instance of the anchor prefab
        GameObject anchorInstance = Instantiate(realObjectAnchorPrefab);

        // Set up the RealObjectAnchor component
        RealObjectAnchor anchorScript = anchorInstance.GetComponent<RealObjectAnchor>();
        anchorScript.virtualCameraTransform = virtualCameraAnchor;
        anchorScript.IDText.text = id.ToString();
        anchorScript.SetOffsetManager(offsetManager);

        // Set the initial position and rotation from the RealSense space
        anchorScript.UpdateAnchor(realSensePosition, realSenseRotation);

        // Add this transform to the dictionary with the associated ID
        realSenseAnchors[id] = anchorInstance.transform;

        // Add the anchor to the list for broadcasting camera updates
        if (!realObjectAnchors.Contains(anchorScript))
        {
            realObjectAnchors.Add(anchorScript);
        }

        Log($"Spawned new anchor for marker ID: {id} at position: {realSensePosition}");
    }

    // Function to update a specific anchor's position and rotation by ID
    public void UpdateAnchorById(int id, Vector3 newPosition, Quaternion newRotation)
    {
        if (realSenseAnchors.ContainsKey(id))
        {
            Transform anchorTransform = realSenseAnchors[id];
            RealObjectAnchor anchorScript = anchorTransform.GetComponent<RealObjectAnchor>();

            if (anchorScript != null)
            {
                anchorScript.UpdateAnchor(newPosition, newRotation);
                Log($"Updated anchor for marker ID: {id} to position: {newPosition}");
            }
        }
    }

    // Function to broadcast the camera's transform to all real object anchors
    public void BroadcastCameraTransform()
    {
        foreach (RealObjectAnchor anchor in realObjectAnchors)
        {
            // Update each anchor's transform based on the new camera transform
            anchor.UpdateWithCameraTransform();
        }
        Log("Broadcasted camera transform to all real object anchors.");
    }

    // Logging method to show messages in VR (or Unity's Debug log if in the Editor)
    private void Log(string message)
    {
        if (positionDataClient != null)
        {
            positionDataClient.Log(message);  // Log in VR
        }
        else
        {
            Debug.LogError("positionDataClient is missing. Cannot log message in VR.");
        }
    }
}
