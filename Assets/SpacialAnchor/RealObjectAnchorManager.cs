using System.Collections.Generic;
using UnityEngine;

public class RealObjectAnchorManager : MonoBehaviour
{
    public Transform virtualCameraAnchor;  // Virtual camera reference
    public GameObject realObjectAnchorPrefab;  // Prefab of the RealObjectAnchor
    public OVRInput.Controller controller;  // Reference to the controller for input

    // Dictionary to store real object anchors by their ID (key)
    private Dictionary<int, Transform> realSenseAnchors = new Dictionary<int, Transform>();

    // Temporary variable storing 3-4 sets of different transforms for testing
    private Dictionary<int, (Vector3 position, Quaternion rotation)> testAnchors = new Dictionary<int, (Vector3, Quaternion)>();
    private List<RealObjectAnchor> realObjectAnchors = new List<RealObjectAnchor>();

    void Start()
    {
        // Initialize test anchors with positions and rotations
        testAnchors[0] = (new Vector3(1, 1, 1), Quaternion.Euler(0, 0, 0));
        testAnchors[1] = (new Vector3(-1, 1, 1), Quaternion.Euler(0, 45, 0));
        testAnchors[2] = (new Vector3(1, -1, 1), Quaternion.Euler(0, 90, 0));
        testAnchors[3] = (new Vector3(1, 1, -1), Quaternion.Euler(0, 135, 0));

        // Register the test transforms (for testing purposes)
        foreach (var key in testAnchors.Keys)
        {
            RegisterTransform(key, testAnchors[key].position, testAnchors[key].rotation);
        }
    }

    // Function to register a new RealSense space transform by ID
    public void RegisterTransform(int id, Vector3 realSensePosition, Quaternion realSenseRotation)
    {
        // Create a new transform for the realSense anchor
        GameObject anchorInstance = Instantiate(realObjectAnchorPrefab);
        RealObjectAnchor anchor = anchorInstance.GetComponent<RealObjectAnchor>();
        // register to broadcast system
        if (!realObjectAnchors.Contains(anchor))
        {
            realObjectAnchors.Add(anchor);
        }
        RealObjectAnchor anchorScript = anchorInstance.GetComponent<RealObjectAnchor>();
        anchorScript.virtualCameraTransform = virtualCameraAnchor;

        // Set the initial position and rotation from the RealSense space
        anchorScript.UpdateAnchor(realSensePosition, realSenseRotation);

        // Add this transform to the dictionary with the associated ID
        realSenseAnchors[id] = anchorInstance.transform;
    }

    // Function to eliminate a RealSense space anchor by ID and unbind it
    public void EliminateTransform(int id)
    {
        if (realSenseAnchors.ContainsKey(id))
        {
            // Unbind and destroy the anchor
            Transform anchorToRemove = realSenseAnchors[id];
            RealObjectAnchor anchorScript = anchorToRemove.GetComponent<RealObjectAnchor>();
            
            if (anchorScript != null)
            {
                // Unbind processing can be implemented here as needed
                // For example: anchorScript.UnbindFromSpatialAnchor();
            }

            Destroy(anchorToRemove.gameObject);
            realSenseAnchors.Remove(id);  // Remove from the dictionary
        }
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
            }
        }
    }

     public void BroadcastCameraTransform()
    {
        foreach (RealObjectAnchor anchor in realObjectAnchors)
        {
            // Update each anchor's transform based on the new camera transform
            anchor.UpdateWithCameraTransform();
        }
    }



    // Spawning anchors when button is pressed
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two, controller))
        {
            // For testing, spawn anchors with predefined positions and rotations
            foreach (var key in testAnchors.Keys)
            {
                RegisterTransform(key, testAnchors[key].position, testAnchors[key].rotation);
            }
        }
    }
}
