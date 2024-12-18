using UnityEngine;
using System.Collections.Generic;



public class MarkerCalibration : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CalibrationCalculator calibrationCalculator;
    [SerializeField] private Transform virtualCameraTransform;
    [SerializeField] private OffsetManager offsetManager;
    
    [Header("Marker Pairs")]
    [SerializeField] private List<MarkerObjectPair> markerPairs = new List<MarkerObjectPair>();

    private void Update()
    {
        UpdateMarkerObjects();
    }

    private void UpdateMarkerObjects()
    {
        var positions = calibrationCalculator.GetAllRelativePositions();

        foreach (var pair in markerPairs)
        {
            if (pair.virtualObject == null) continue;

            if (positions.TryGetValue(pair.markerId, out var markerData))
            {
                // Get real world data
                Vector3 realWorldPosition = markerData.position;
                Quaternion realWorldRotation = Quaternion.Euler(0, markerData.rotation, 0);

                // Apply virtual space alignment
                Vector3 alignedPosition = GetAlignedPosition(realWorldPosition);
                Quaternion alignedRotation = GetAlignedRotation(realWorldRotation);

                // Update virtual object
                pair.virtualObject.transform.position = alignedPosition;
                // align -y
                

                pair.virtualObject.transform.rotation = alignedRotation;

                // Apply offset if available
                if (offsetManager != null)
                {
                    pair.virtualObject.transform.position += offsetManager.GetOffsets();
                }
            }
        }
    }

    private Vector3 GetAlignedPosition(Vector3 realWorldPosition)
    {
        // Convert from RealSense to Unity coordinate system
        // RealSense: y up, z forward
        // Unity: y up, z forward
        // Possibly need to invert some axes
        Vector3 unityPosition = new Vector3(
            -realWorldPosition.x,  // Mirror X
            -realWorldPosition.y,   // Keep Y
            realWorldPosition.z    // Keep Z but might need to invert
        );

        // Transform to virtual camera space
        return virtualCameraTransform.position + virtualCameraTransform.TransformDirection(unityPosition);
    }

    private Quaternion GetAlignedRotation(Quaternion realWorldRotation)
    {
        // Might need to adjust rotation axes similarly
        Vector3 euler = realWorldRotation.eulerAngles;
        Vector3 adjustedEuler = new Vector3(
            -euler.x,  // Mirror X rotation
            -euler.y,   // Keep Y rotation
            euler.z   // Mirror Z rotation
        );
        
        Quaternion adjustedRotation = Quaternion.Euler(adjustedEuler);
        return virtualCameraTransform.rotation * adjustedRotation;
    }
    // Optional: Debug visualization
    public void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        foreach (var pair in markerPairs)
        {
            if (pair.virtualObject != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(pair.virtualObject.transform.position, 0.05f);
            }
        }
    }
}


[System.Serializable]
public class MarkerObjectPair
{
    public int markerId;
    public GameObject virtualObject;
}