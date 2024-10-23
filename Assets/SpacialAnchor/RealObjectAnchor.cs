using UnityEngine;
using TMPro;

public class RealObjectAnchor : MonoBehaviour
{
    public Transform virtualCameraTransform;  // Virtual camera reference
    private Vector3 realWorldPosition;
    private Quaternion realWorldRotation;

    // UI components to display position and rotation
    public TextMeshProUGUI IDText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI rotationText;

    private OVRSpatialAnchor _spatialAnchor;  // Spatial anchor component
    private bool _isLocalized;  // Tracks whether the anchor is localized

    private void Awake()
    {
        // Add or retrieve the OVRSpatialAnchor component
        _spatialAnchor = GetComponent<OVRSpatialAnchor>() ?? gameObject.AddComponent<OVRSpatialAnchor>();

        // Subscribe to localization events
        _spatialAnchor.OnLocalize += HandleLocalization;

        // Initialize UI components (if applicable)
        InitializeUIComponents();
    }

    private void HandleLocalization(OVRSpatialAnchor.OperationResult result)
    {
        // Update localization state based on result
        _isLocalized = result == OVRSpatialAnchor.OperationResult.Success;
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
    /// If the transform changes, rebind the spatial anchor as well.
    /// </summary>
    public async void UpdateAnchor(Vector3 realSensePosition, Quaternion realSenseRotation)
    {
        // Unbind the current spatial anchor
       UnbindAnchor();

        // Update the position and rotation
        realWorldPosition = realSensePosition;
        realWorldRotation = realSenseRotation;

        // Align to the virtual space based on the virtual camera
        AlignToVirtualSpace();

        // Rebind the spatial anchor with the updated position and rotation
        RebindAnchor();
    }

    /// <summary>
    /// Align the real-world position and rotation to the virtual camera space.
    /// </summary>
    private void AlignToVirtualSpace()
    {
        // Convert real-world position and rotation to virtual camera space
        Vector3 alignedPosition = virtualCameraTransform.TransformPoint(realWorldPosition);
        Quaternion alignedRotation = virtualCameraTransform.rotation * realWorldRotation;

        // Update the anchor's position and rotation in Unity
        transform.position = alignedPosition;
        transform.rotation = alignedRotation;

        // Update the position and rotation text after the alignment
        UpdateText();
        Debug.Log("alligned ");
    }

    /// <summary>
    /// Updates the position and rotation display.
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

    /// <summary>
    /// Unbind the spatial anchor from the tracking system.
    /// </summary>
    public async void UnbindAnchor()
    {
        // Ensure the anchor is localized and exists
        if (_spatialAnchor != null && _spatialAnchor.Localized)
        {
            var result = await _spatialAnchor.EraseAnchorAsync();
            if (result.Success)
            {
                Debug.Log("Anchor erased successfully.");
                UpdateText();
            }
            else
            {
                Debug.LogError("Failed to erase anchor.");
            }
        }
    }

    /// <summary>
    /// Rebind the spatial anchor at the current position.
    /// </summary>
    public async void RebindAnchor()
    {
        // Destroy the current spatial anchor, if it exists
        if (_spatialAnchor != null)
        {
            Destroy(_spatialAnchor);
        }

        // Add a new spatial anchor at the current position
        _spatialAnchor = gameObject.AddComponent<OVRSpatialAnchor>();

        // Wait for the anchor to be created and localized
        var success = await _spatialAnchor.WhenCreatedAsync();
        if (success)
        {
            Debug.Log("Anchor created and localized successfully.");
            UpdateText();
        }
        else
        {
            Debug.LogError("Failed to create or localize anchor.");
        }

        // Subscribe to the new localization event
        _spatialAnchor.OnLocalize += HandleLocalization;
    }

    /// <summary>
    /// This method continuously aligns the object to the virtual space when the camera's transform changes.
    /// </summary>
    public void UpdateWithCameraTransform()
    {
        // Each time the virtual camera transform changes, re-align the anchor
        AlignToVirtualSpace();
    }

    
    private void OnDestroy()
    {
        // Unsubscribe from the localization event
        if (_spatialAnchor != null)
        {
            _spatialAnchor.OnLocalize -= HandleLocalization;
        }
    }
}
