using UnityEngine;
using TMPro;

public class PhysicalObjectAnchor : MonoBehaviour
{
    public Transform virtualCameraTransform;  // Virtual camera reference
    private OffsetManager offsetManager;  // Reference to the global OffsetManager
    private Vector3 realWorldPosition;
    private Quaternion realWorldRotation;

    // Optional UI components to display position and rotation
    public TextMeshProUGUI IDText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI rotationText;

    private void Awake()
    {
        if (offsetManager != null)
        {
            offsetManager.OnOffsetsChanged += AlignToVirtualSpace;  // Listen to offset changes
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

        // Optionally update UI text if they are assigned
        UpdateText();
    }

    /// <summary>
    /// Updates the position and rotation display in the UI, if available.
    /// </summary>
    private void UpdateText()
    {
        // Update text only if the respective fields are assigned
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
