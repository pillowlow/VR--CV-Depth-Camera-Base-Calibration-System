using UnityEngine;

public class SimpleTransformLogger : MonoBehaviour
{
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform rightControllerTransform;
    private bool canRecord = true;

    void Update()
    {
        if (canRecord && (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) || 
            OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)))
        {
            LogTransforms();
            canRecord = false;
            Invoke("ResetRecording", 1f); // Prevent multiple recordings for 1 second
        }
    }

    void LogTransforms()
    {
        Debug.Log("=== Transform Snapshot ===");
        Debug.Log($"Left Controller - Pos: {leftControllerTransform.position}, Rot: {leftControllerTransform.eulerAngles}");
        Debug.Log($"Right Controller - Pos: {rightControllerTransform.position}, Rot: {rightControllerTransform.eulerAngles}");
        
        // Calculate and log relative transforms
        Vector3 relativePos = rightControllerTransform.position - leftControllerTransform.position;
        Quaternion relativeRot = Quaternion.Inverse(leftControllerTransform.rotation) * rightControllerTransform.rotation;
        Debug.Log($"Relative - Pos: {relativePos}, Rot: {relativeRot.eulerAngles}");
    }

    void ResetRecording()
    {
        canRecord = true;
    }
}