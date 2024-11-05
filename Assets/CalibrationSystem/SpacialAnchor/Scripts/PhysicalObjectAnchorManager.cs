using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalObjectAnchorManager : MonoBehaviour
{
    public Transform virtualCameraAnchor;  // Virtual camera reference
    public GameObject defaultPhysicalObjectPrefab;  // Default prefab if ID not found in list
    public List<PrefabEntry> prefabEntries;  // List of prefabs to spawn based on ID (visible in the inspector)
    public OffsetManager offsetManager;  // The global OffsetManager
    public PositionDataWebSocketClient positionDataClient;  // WebSocket client for receiving marker positions

    private Dictionary<int, Transform> realSenseAnchors = new Dictionary<int, Transform>();  // Anchor storage
    private List<PhysicalObjectAnchor> physicalObjectAnchors = new List<PhysicalObjectAnchor>();  // List of anchors for broadcasting
    [SerializeField]
    private float update_interval = 0.05f;

    [System.Serializable]
    public class PrefabEntry
    {
        public int id;
        public GameObject prefab;  // The prefab to spawn for this ID
    }

    private void Start()
    {
        // Start updating anchors every 0.05 seconds
        StartCoroutine(UpdateAnchorsPeriodically(update_interval));
    }

    // Coroutine to update the anchors periodically
    private IEnumerator UpdateAnchorsPeriodically(float interval)
    {
        while (true)
        {
            UpdateAnchors();
            yield return new WaitForSeconds(interval);
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

            Log($"Update Anchor Marker ID: {markerId}, Position: X={markerPosition.x}, Y={markerPosition.y}, Z={markerPosition.z}");

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
        // Find the prefab that corresponds to the given ID
        GameObject prefabToSpawn = FindPrefabById(id);

        // Create a new instance of the prefab (or default if no match found)
        GameObject anchorInstance = Instantiate(prefabToSpawn);

        // Set up the PhysicalObjectAnchor component
        PhysicalObjectAnchor anchorScript = anchorInstance.GetComponent<PhysicalObjectAnchor>();
        anchorScript.virtualCameraTransform = virtualCameraAnchor;
        anchorScript.IDText.text = id.ToString();
        anchorScript.SetOffsetManager(offsetManager);

        // Set the initial position and rotation from the RealSense space
        anchorScript.UpdateAnchor(realSensePosition, realSenseRotation);

        // Add this transform to the dictionary with the associated ID
        realSenseAnchors[id] = anchorInstance.transform;

        // Add the anchor to the list for broadcasting camera updates
        if (!physicalObjectAnchors.Contains(anchorScript))
        {
            physicalObjectAnchors.Add(anchorScript);
        }

        Log($"Spawned new anchor for marker ID: {id} at position: {realSensePosition}");
    }

    // Function to find the prefab corresponding to the ID or return the default prefab
    private GameObject FindPrefabById(int id)
    {
        foreach (PrefabEntry entry in prefabEntries)
        {
            if (entry.id == id)
            {
                return entry.prefab;
            }
        }

        // If no matching prefab is found, return the default prefab
        Log($"No matching prefab found for ID: {id}. Using default prefab.");
        return defaultPhysicalObjectPrefab;
    }

    // Function to update a specific anchor's position and rotation by ID
    public void UpdateAnchorById(int id, Vector3 newPosition, Quaternion newRotation)
    {
        if (realSenseAnchors.ContainsKey(id))
        {
            Transform anchorTransform = realSenseAnchors[id];
            PhysicalObjectAnchor anchorScript = anchorTransform.GetComponent<PhysicalObjectAnchor>();

            if (anchorScript != null)
            {
                anchorScript.UpdateAnchor(newPosition, newRotation);
                Log($"Updated anchor for marker ID: {id} to position: {newPosition}");
            }
        }
    }

    // Function to broadcast the camera's transform to all physical object anchors
    public void BroadcastCameraTransform()
    {
        foreach (PhysicalObjectAnchor anchor in physicalObjectAnchors)
        {
            // Update each anchor's transform based on the new camera transform
            anchor.UpdateWithCameraTransform();
        }
        Log("Broadcasted camera transform to all physical object anchors.");
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
