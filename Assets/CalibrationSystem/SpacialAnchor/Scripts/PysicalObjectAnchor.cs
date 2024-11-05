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
        // Calculate position relative to the virtual camera
        // Convert the real-world position to virtual space by directly adding it relative to the virtual camera
        realWorldPosition.y = -realWorldPosition.y;
        Vector3 relativePosition = virtualCameraTransform.position - virtualCameraTransform.TransformDirection(realWorldPosition);

        // Set the anchor’s position in Unity's virtual world
        transform.position = relativePosition;

        // Apply rotation directly relative to the virtual camera's orientation
        Quaternion alignedRotation = virtualCameraTransform.rotation * realWorldRotation;

        // Update the rotation of the anchor
        transform.rotation = alignedRotation;

        // Apply any additional offsets from offsetManager if available
        if (offsetManager != null)
        {
            transform.position += offsetManager.GetOffsets();
        }

        // Update UI or logs if needed
        UpdateText();
        Debug.Log("Aligned anchor with simplified position calculation.");
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
