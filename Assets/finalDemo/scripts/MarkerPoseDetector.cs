using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Collections;
using static OVRInput;
using System.Linq;  // Add this for ToDictionary
using System;  // Add this for Exception
using UnityEngine.Events;

public class MarkerPoseDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CalibrationCalculator calibrationCalculator;
    
    [Header("Detection Settings")]
    [SerializeField] private float positionTolerance = 0.05f;
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private int requiredConsistentChecks = 4;
    [SerializeField] private bool checkPose = false;

    [Header("Weapon Poses")]
    //[SerializeField] private List<WeaponPose> weaponPoses = new List<WeaponPose>();
    [SerializeField] private Controller controller;
    [SerializeField] private string RecordingPoseName;

    [Header("Pose References")]
    [SerializeField] private List<WeaponPoseData> weaponPoses = new List<WeaponPoseData>();
    [Header("Event when hover")]
    public UnityEvent OnHoverStart;
    public UnityEvent OnHoverEnd;


    private string currentPoseName = "";
    private int consistentChecksCount = 0;
    private WeaponPoseData lastMatchedPose = null;
    private bool isChecking = false;

    private void Update()
    {
        if (checkPose && !isChecking)
        {
            StartCoroutine(CheckPoseRoutine());
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            SaveCurrentPoseAsAsset(RecordingPoseName);
        }
        if(OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller) || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger, controller) ){
            StartHover();
        }
    }
    private void StartHover(){
        OnHoverStart?.Invoke();
    }
    private void EndHover(){
        OnHoverEnd?.Invoke();
        
    }
    private IEnumerator CheckPoseRoutine()
    {
        isChecking = true;
        consistentChecksCount = 0;
        lastMatchedPose = null;

        while (checkPose)
        {
            WeaponPoseData matchedPose = CheckCurrentPose();
            
            if (matchedPose != null)
            {
                if (matchedPose == lastMatchedPose)
                {
                    consistentChecksCount++;
                    Debug.Log("The step of "+ matchedPose.poseName + " is " + consistentChecksCount);
                    if (consistentChecksCount >= requiredConsistentChecks)
                    {   
                        SetCurrentPose(matchedPose.poseName);
                        checkPose = false;
                        Debug.Log("Switch to: "+ matchedPose.poseName + "complete, turn off checkHover");
                        EndHover();
                        break;
                    }
                }
                else
                {
                    consistentChecksCount = 1;
                    Debug.Log("The step of "+ matchedPose.poseName + " is " + consistentChecksCount);
                    StartHover();
                    lastMatchedPose = matchedPose;
                }
            }
            else
            {
                consistentChecksCount = 0;
                lastMatchedPose = null;
            }

            yield return new WaitForSeconds(checkInterval);
        }

        isChecking = false;
    }

     private WeaponPoseData CheckCurrentPose()
    {
        var currentPositions = calibrationCalculator.GetAllRelativePositions();
        
        foreach (var pose in weaponPoses)
        {
            bool poseMatches = true;
            
            foreach (var markerPos in pose.markerPositions)
            {
                if (!currentPositions.TryGetValue(markerPos.markerId, out var currentPos) || 
                    Vector3.Distance(currentPos.position, markerPos.GetPosition()) > positionTolerance)
                {
                    poseMatches = false;
                    break;
                }
            }
            
            if (poseMatches)
                return pose;
        }
        
        return null;
    }

      public void SaveCurrentPoseAsAsset(string poseName)
    {
        #if UNITY_EDITOR
        // Create new pose asset
        var poseAsset = ScriptableObject.CreateInstance<WeaponPoseData>();
        poseAsset.poseName = poseName;
        
        // Get current positions
        var positions = calibrationCalculator.GetAllRelativePositions();
        foreach (var pos in positions)
        {
            poseAsset.markerPositions.Add(new MarkerPosition(
                pos.Key, 
                pos.Value.position
            ));
        }

        // Save asset
        string directory = "Assets/PoseData";
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string assetPath = $"{directory}/{poseName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(poseAsset, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log($"Saved pose asset: {assetPath}");
        #endif
    }
    private string GetPoseSavePath(string poseName)
    {
        // Save directly in Assets/PoseData
        string directory = Path.Combine(Application.dataPath, "PoseData");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.Log($"Created directory at: {directory}");
        }
        
        string fullPath = Path.Combine(directory, $"{poseName}.json");
        Debug.Log($"Full save path: {fullPath}");

        return fullPath;
    }
    

    private void SetCurrentPose(string poseName)
    {
        currentPoseName = poseName;
        Debug.Log($"Detected pose: {poseName}");
    }

    public string GetCurrentPose()
    {
        return currentPoseName;
    }

    public float GetDetectionProgress()
    {
        if (!isChecking || lastMatchedPose == null)
            return 0f;
        
        return (float)consistentChecksCount / requiredConsistentChecks;
    }

    public bool IsChecking()
    {
        return isChecking;
    }
}




[System.Serializable]
public class WeaponPose
{
    public string poseName;
    public List<MarkerPosition> markerPositions = new List<MarkerPosition>();

    // Helper method to convert to dictionary when needed
    public Dictionary<int, Vector3> ToPositionDictionary()
    {
        return markerPositions.ToDictionary(m => m.markerId, m => m.position.ToVector3());
    }
}



[System.Serializable]
public class MarkerPosition
{
    public int markerId;
    public SerializableVector3 position;  // Changed from Vector3 to SerializableVector3

    public MarkerPosition(int id, Vector3 pos)
    {
        markerId = id;
        position = new SerializableVector3(pos);
    }
    public Vector3 GetPosition(){
        return position.ToVector3();
    }
}



[System.Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

