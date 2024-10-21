using UnityEngine;
using TMPro;

public class PhysicalObjectAnchor : MonoBehaviour
{
    public Transform virtualCameraTransform;  // Virtual camera reference
    private OffsetManager offsetManager;  // Reference to the global OffsetManager
    private Vector3 realWorldPosition;
    private Quaternion realWorldRotation;

    // UI components to display position and rotation
    public TextMeshProUGUI IDText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI rotationText;

    private void Awake()
    {
        // Initialize UI components (if applicable)
        InitializeUIComponents();
        if (offsetManager != null)
        {
            offsetManager.OnOffsetsChanged += AlignToVirtualSpace;  // Listen to offset changes
        }
    }

    /// <summary>
    /// Initializes the UI components for displaying position and rotation.
    /// </summary>
    private void InitializeUIComponents()
    {
        // Assuming UI components are set up in the child objects
        if (IDText == null || positionText == null || rotationText == null)
        {
            IDText = transform.Find("Canvas/IDText").GetComponent<TextMeshProUGUI>();
            positionText = transform.Find("Canvas/PositionText").GetComponent<TextMeshProUGUI>();
            rotationText = transform.Find("Canvas/RotationText").GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Updates the real-world anchor position and rotation.
    /// </summary>
    public void UpdateAnchor(Vector3 realSensePosition, Quaternion realSenseRotation)
    {
        // Update the position and rotation
        realWorldPosition = realSensePosition;
        realWorldRotation = realSenseRotation;

        // Align to the virtual space based on the virtual camera
        AlignToVirtualSpace();
    }

     public void SetOffsetManager(OffsetManager manager)
    {
        offsetManager = manager;

        // Immediately apply the current offsets
        AlignToVirtualSpace();

        // Listen for any changes in offsets
        offsetManager.OnOffsetsChanged += AlignToVirtualSpace;
    }

    public void UpdateWithCameraTransform()
    {
        // Every time the virtual camera transform changes, re-align the anchor
        AlignToVirtualSpace();
    }


    /// <summary>
    /// Align the real-world position and rotation to the virtual camera space.
    /// </summary>
    private void AlignToVirtualSpace()
    {
        // No need for any complex 180-degree rotations on Y-axis initially
        // Invert Z to match RealSense's "away from the camera" direction to Unity's forward direction
        Vector3 adjustedRealWorldPosition = realWorldPosition;
        adjustedRealWorldPosition.z = -adjustedRealWorldPosition.z; // Invert Z-axis

        // Convert the real-world position to virtual camera space
        Vector3 alignedPosition = virtualCameraTransform.TransformPoint(adjustedRealWorldPosition);

        // Apply the final flip on the X-axis to fix the inversion issue
        alignedPosition.x = -alignedPosition.x;  // Flip X-axis

        // Keep the rotation consistent
        Quaternion alignedRotation = virtualCameraTransform.rotation * realWorldRotation;

        // Update the anchor's position and rotation in Unity
        transform.position = alignedPosition;
        transform.rotation = alignedRotation;


        if (offsetManager != null)
        {
            transform.position += offsetManager.GetOffsets();
        }

        // Update the position and rotation text after the alignment
        UpdateText();
        Debug.Log("Aligned anchor with final X-axis flip");
    }





    /// <summary>
    /// Updates the position and rotation display in the UI.
    /// </summary>
    private void UpdateText()
    {
        if (positionText != null)
        {
            positionText.text = $"Position: {transform.position.ToString("F3")}";
        }
        if (rotationText != null)
        {
            rotationText.text = $"Rotation: {transform.rotation.eulerAngles.ToString("F3")}";
        }
    }
}
