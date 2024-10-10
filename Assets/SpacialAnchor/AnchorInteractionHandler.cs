using System.Threading.Tasks;
using UnityEngine;
using TMPro;

/// <summary>
/// This script manages the interaction between the pointable object and the spatial anchor.
/// It unbinds the anchor when the object is selected and rebinds it when the object is unselected.
/// </summary>
public class AnchorInteractionHandler : MonoBehaviour
{
    private OVRSpatialAnchor _spatialAnchor;
    private bool _isLocalized;

    // UI Components
    private TextMeshProUGUI idText;
    private TextMeshProUGUI positionText;
     private TextMeshProUGUI rotationText;

    private Canvas canvas;

    private void Awake()
    {
        // Check if the object already has an OVRSpatialAnchor component, if not, add it
        _spatialAnchor = GetComponent<OVRSpatialAnchor>() ?? gameObject.AddComponent<OVRSpatialAnchor>();

        // Subscribe to the anchor localization event
        _spatialAnchor.OnLocalize += HandleLocalization;

        // Initialize the UI components
        InitializeUIComponents();
    }

    private void HandleLocalization(OVRSpatialAnchor.OperationResult result)
    {
        // Check if the anchor was successfully localized
        _isLocalized = result == OVRSpatialAnchor.OperationResult.Success;
    }

    /// <summary>
    /// Initializes the UI components if they are available in the prefab.
    /// </summary>
    private void InitializeUIComponents()
    {
        canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            idText = canvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            positionText = canvas.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            rotationText = canvas.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Updates the text display for the anchor ID and position.
    /// Call this method whenever the anchor's position or state changes.
    /// </summary>
    private void UpdateAnchorDisplay()
    {
        if (canvas != null )
        {

            positionText.text = "Position: " + transform.position.ToString("F3");
            rotationText.text = "Rotation: " + transform.rotation.ToString("F3");
        }
    }

    /// <summary>
    /// This method unbinds the spatial anchor.
    /// Call this when the object is selected.
    /// </summary>
    public async void UnbindAnchor()
    {
        // Check if the object is localized and the anchor exists
        if (_spatialAnchor && _spatialAnchor.Localized)
        {
            // Erase the existing anchor
            var result = await _spatialAnchor.EraseAnchorAsync();
            if (result.Success)
            {
                Debug.Log("Anchor erased successfully.");
                UpdateAnchorDisplay(); // Update the display after unbinding
            }
            else
            {
                Debug.LogError("Failed to erase anchor.");
            }
        }
    }

    /// <summary>
    /// This method rebinds the spatial anchor at the current position.
    /// Call this when the object is unselected.
    /// </summary>
    public async void RebindAnchor()
    {
        // Remove the existing spatial anchor if it exists
        if (_spatialAnchor != null)
        {
            Destroy(_spatialAnchor);
        }

        // Add a new spatial anchor at the current position
        _spatialAnchor = gameObject.AddComponent<OVRSpatialAnchor>();
        
        // Wait for the new anchor to be created and localized
        var success = await _spatialAnchor.WhenCreatedAsync();
        if (success)
        {
            Debug.Log("Anchor created and localized at the current position.");
            UpdateAnchorDisplay(); // Update the display after rebinding
        }
        else
        {
            Debug.LogError("Failed to create or localize anchor.");
        }

        // Subscribe to the new anchor's localization event
        _spatialAnchor.OnLocalize += HandleLocalization;
    }

    /// <summary>
    /// Call this method while the object is being held to update the position in real-time.
    /// </summary>
    private void Update()
    {
        if (_spatialAnchor != null && canvas != null)
        {
            UpdateAnchorDisplay();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the localization event
        if (_spatialAnchor)
        {
            _spatialAnchor.OnLocalize -= HandleLocalization;
        }
    }
}
