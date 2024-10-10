using UnityEngine;
using TMPro;

public class CameraAnchor : MonoBehaviour
{
    public Transform virtualCameraTransform;  // Reference to the camera's transform
    public TextMeshProUGUI idText;  // UI element for displaying the ID
    private RealObjectAnchorManager realObjectAnchorManager;  // Reference to the manager

    private void Awake()
    {
        // Assign the ID as "camera"
        idText.text = "ID: camera";

        // Find the RealObjectAnchorManager in the scene
        realObjectAnchorManager = FindObjectOfType<RealObjectAnchorManager>();
    }

    /// <summary>
    /// Call this function when the camera anchor is released after being grabbed.
    /// This will broadcast the current camera transform to all real object anchors.
    /// </summary>
    public void OnCameraReleased()
    {
        if (realObjectAnchorManager != null)
        {
            // Broadcast to all real object anchors to update their transform
            realObjectAnchorManager.BroadcastCameraTransform();
        }
    }
}
